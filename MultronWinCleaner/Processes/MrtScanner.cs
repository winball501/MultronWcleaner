using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultronWinCleaner.Processes
{
    public static class MrtScanner
    {
        public enum Outcome { Clean, Threats, Failed, Stopped }

        public sealed class Result
        {
            public Outcome Outcome;
            public int ExitCode = -1;
            public string Message = "";
            public List<string> Summary = new List<string>();
            public int ThreatCount;
            public TimeSpan Elapsed;
            public string LogText = "";
            public bool FullScan;
            public bool Removed;

            public bool CanRemove => Outcome == Outcome.Threats && (!Removed || ExitCode == 6);
        }

        public static string MrtPath => Path.Combine(Environment.SystemDirectory, "MRT.exe");

        public static string LogPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "debug", "mrt.log");

        public static bool IsAvailable => File.Exists(MrtPath);

        private static Process? running;

        public static void KillRunning()
        {
            try
            {
                var process = running;
                if (process != null && !process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch { }
        }

        public static async Task<Result> RunAsync(bool fullScan, bool removeThreats, IProgress<(double Percent, TimeSpan Elapsed)>? progress, CancellationToken ct)
        {
            var result = new Result();
            if (!IsAvailable)
            {
                result.Outcome = Outcome.Failed;
                result.Message = Loc.T("MRT.exe was not found. It is installed by Windows Update; check that Windows Update works.");
                return result;
            }

            string args = "/Q" + (fullScan ? " /F" : "") + (removeThreats ? "" : " /N");
            var startInfo = new ProcessStartInfo(MrtPath, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Environment.SystemDirectory
            };

            DateTime startedUtc = DateTime.UtcNow;
            long usedBytes = fullScan ? UsedBytesOnFixedDrives() : 0;
            var timer = Stopwatch.StartNew();

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                result.Outcome = Outcome.Failed;
                result.Message = Loc.T("MRT.exe could not be started.");
                return result;
            }

            running = process;
            try
            {
                Task exited = process.WaitForExitAsync();
                while (!exited.IsCompleted)
                {
                    await Task.WhenAny(exited, Task.Delay(1000, ct));
                    ct.ThrowIfCancellationRequested();
                    if (!exited.IsCompleted)
                        progress?.Report((EstimatePercent(process, timer.Elapsed, fullScan, usedBytes), timer.Elapsed));
                }
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                result.Outcome = Outcome.Stopped;
                result.Elapsed = timer.Elapsed;
                result.Message = Loc.T("The MRT scan was stopped.");
                return result;
            }
            finally
            {
                running = null;
            }

            result.Elapsed = timer.Elapsed;
            result.ExitCode = process.ExitCode;
            progress?.Report((100, timer.Elapsed));

            result.Summary = ReadLastSummary(startedUtc, out result.LogText);
            result.Removed = removeThreats;
            result.FullScan = fullScan;
            bool logSaysClean = result.Summary.Any(l => l.Contains("No infection found", StringComparison.OrdinalIgnoreCase));
            int foundLines = result.Summary.Count(l => l.StartsWith("Found", StringComparison.OrdinalIgnoreCase)
                || l.StartsWith("->", StringComparison.Ordinal));

            switch (result.ExitCode)
            {
                case 0:
                    result.Outcome = Outcome.Clean;
                    result.Message = Loc.T("Microsoft MRT found no malicious software.");
                    break;
                case 6:
                    result.Outcome = Outcome.Threats;
                    result.Message = Loc.T("Microsoft MRT found malicious software. Nothing was removed.");
                    break;
                case 7:
                    result.Outcome = Outcome.Threats;
                    result.Message = Loc.T("Microsoft MRT found malicious software and removed it.");
                    break;
                case 8:
                    result.Outcome = Outcome.Threats;
                    result.Message = Loc.T("Microsoft MRT found malicious software and removed part of it. Some steps must be done by hand, see mrt.log.");
                    break;
                case >= 9 and <= 12:
                    result.Outcome = Outcome.Threats;
                    result.Message = Loc.T("Microsoft MRT found malicious software. A restart may be needed to finish the removal, see mrt.log.");
                    break;
                case 1:
                    result.Outcome = Outcome.Failed;
                    result.Message = Loc.T("Microsoft MRT reported an operating system environment error.");
                    break;
                case 2:
                    result.Outcome = Outcome.Failed;
                    result.Message = Loc.T("Microsoft MRT needs administrator rights.");
                    break;
                case 3:
                    result.Outcome = Outcome.Failed;
                    result.Message = Loc.T("Microsoft MRT does not support this version of Windows.");
                    break;
                case 4:
                    result.Outcome = Outcome.Failed;
                    result.Message = Loc.T("Microsoft MRT could not start its scanner. Install the newest MRT through Windows Update.");
                    break;
                default:
                    result.Outcome = logSaysClean ? Outcome.Clean : Outcome.Failed;
                    result.Message = logSaysClean
                        ? Loc.T("Microsoft MRT found no malicious software.")
                        : Loc.F("Microsoft MRT finished with exit code {0}. Details are in mrt.log.", result.ExitCode);
                    break;
            }

            if (result.Outcome == Outcome.Threats)
                result.ThreatCount = Math.Max(1, foundLines);
            return result;
        }

        private static double EstimatePercent(Process process, TimeSpan elapsed, bool fullScan, long usedBytes)
        {
            if (fullScan && usedBytes > 0)
            {
                try
                {
                    if (GetProcessIoCounters(process.Handle, out IO_COUNTERS counters))
                        return Math.Clamp(counters.ReadTransferCount * 100.0 / usedBytes, 1, 97);
                }
                catch { }
            }

            double tau = fullScan ? 1500 : 120;
            return 97 * (1 - Math.Exp(-elapsed.TotalSeconds / tau));
        }

        private static long UsedBytesOnFixedDrives()
        {
            long used = 0;
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                        used += drive.TotalSize - drive.TotalFreeSpace;
                }
                catch { }
            }
            return used;
        }

        private static List<string> ReadLastSummary(DateTime startedUtc, out string runText)
        {
            var summary = new List<string>();
            runText = "";
            try
            {
                if (!File.Exists(LogPath) || File.GetLastWriteTimeUtc(LogPath) < startedUtc.AddSeconds(-5))
                    return summary;

                string text;
                using (var stream = new FileStream(LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (var reader = new StreamReader(stream, Encoding.Default, detectEncodingFromByteOrderMarks: true))
                    text = reader.ReadToEnd();

                string[] lines = text.Replace("\r", "").Split('\n');
                int runStart = Array.FindLastIndex(lines, l => l.StartsWith("Microsoft Windows Malicious Software Removal Tool v", StringComparison.OrdinalIgnoreCase));
                if (runStart < 0) return summary;
                runText = string.Join(Environment.NewLine, lines.Skip(runStart)).Trim();
                int summaryStart = Array.FindIndex(lines, runStart, l => l.Trim().Equals("Results Summary:", StringComparison.OrdinalIgnoreCase));
                if (summaryStart < 0) return summary;

                for (int i = summaryStart + 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (line.StartsWith("Microsoft Windows Malicious Software Removal Tool Finished", StringComparison.OrdinalIgnoreCase)
                        || line.StartsWith("Return code", StringComparison.OrdinalIgnoreCase))
                        break;
                    if (line.Length == 0 || line.All(c => c == '-') || line.StartsWith("Successfully Submitted", StringComparison.OrdinalIgnoreCase))
                        continue;
                    summary.Add(line);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Could not read mrt.log: " + ex.Message);
            }
            return summary;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS counters);
    }
}
