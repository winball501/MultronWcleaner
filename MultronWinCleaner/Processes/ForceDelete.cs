using MFK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace MultronWinCleaner.Processes
{
    public static class ForceDelete
    {
        public enum Outcome { Deleted, ScheduledForReboot, Failed }

        public sealed class Result
        {
            public Outcome Outcome;
            public List<string> Steps = new List<string>();
            public string Error = "";
        }

        private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, int dwFlags);

        public static Result Delete(string path)
        {
            var result = new Result();
            path = Path.GetFullPath(path);

            if (File.Exists(path))
                KillProcessesStartedFrom(path, result);
            if (TryDelete(path, result))
                return Done(result, Outcome.Deleted);

            if (ClearAttributes(path))
                result.Steps.Add(Loc.T("Removed read-only, hidden and system attributes"));
            if (TryDelete(path, result))
                return Done(result, Outcome.Deleted);

            if (CloseLockingProcesses(path, result) && TryDeleteWithRetries(path, result))
                return Done(result, Outcome.Deleted);

            if (TakeOwnership(path, result) && TryDeleteWithRetries(path, result))
                return Done(result, Outcome.Deleted);

            string target = path;
            try
            {
                string renamed = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + ".mwc_del.tmp");
                File.Move(path, renamed);
                target = renamed;
                result.Steps.Add(Loc.T("Renamed the file so it can no longer start by its original name"));
                if (TryDelete(target, result))
                    return Done(result, Outcome.Deleted);
            }
            catch (Exception) { }

            if (MoveFileEx(target, null, MOVEFILE_DELAY_UNTIL_REBOOT))
            {
                result.Steps.Add(Loc.T("Scheduled the file for deletion at the next restart"));
                return Done(result, Outcome.ScheduledForReboot);
            }

            result.Error = string.IsNullOrEmpty(result.Error) ? new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message : result.Error;
            return Done(result, Outcome.Failed);
        }

        public static Result Move(string path, string destination)
        {
            var result = new Result();
            path = Path.GetFullPath(path);

            bool TryMove()
            {
                try
                {
                    File.Move(path, destination);
                    return true;
                }
                catch (Exception ex)
                {
                    result.Error = ex.Message;
                    return false;
                }
            }

            bool TryMoveWithRetries()
            {
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    if (TryMove())
                        return true;
                    Thread.Sleep(300);
                }
                return false;
            }

            if (File.Exists(path))
                KillProcessesStartedFrom(path, result);
            if (TryMove())
                return Done(result, Outcome.Deleted);

            if (ClearAttributes(path))
                result.Steps.Add(Loc.T("Removed read-only, hidden and system attributes"));
            if (TryMove())
                return Done(result, Outcome.Deleted);

            if (CloseLockingProcesses(path, result) && TryMoveWithRetries())
                return Done(result, Outcome.Deleted);

            if (TakeOwnership(path, result) && TryMoveWithRetries())
                return Done(result, Outcome.Deleted);

            return Done(result, Outcome.Failed);
        }

        private static Result Done(Result result, Outcome outcome)
        {
            result.Outcome = outcome;
            return result;
        }

        private static bool TryDelete(string path, Result result)
        {
            try
            {
                if (!File.Exists(path))
                    return true;
                File.Delete(path);
                return !File.Exists(path);
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return false;
            }
        }

        private static bool TryDeleteWithRetries(string path, Result result)
        {
            for (int attempt = 0; attempt < 5; attempt++)
            {
                if (TryDelete(path, result))
                    return true;
                Thread.Sleep(300);
            }
            return false;
        }

        private static bool ClearAttributes(string path)
        {
            try
            {
                FileAttributes attributes = File.GetAttributes(path);
                FileAttributes cleared = attributes & ~(FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System);
                if (cleared == attributes)
                    return false;
                File.SetAttributes(path, cleared == 0 ? FileAttributes.Normal : cleared);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void KillProcessesStartedFrom(string path, Result result)
        {
            foreach (Process process in Process.GetProcesses())
            {
                using (process)
                {
                    int id;
                    string name;
                    try
                    {
                        id = process.Id;
                        name = process.ProcessName;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    if (!string.Equals(ImagePath(id), path, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (whousef.IsProtectedProcess(id, name))
                    {
                        result.Steps.Add(Loc.F("Skipped Windows process {0} (PID {1})", name, id));
                        continue;
                    }

                    try
                    {
                        process.Kill(entireProcessTree: true);
                        process.WaitForExit(5000);
                        result.Steps.Add(Loc.F("Force-closed {0} (PID {1}) and the programs it started", name, id));
                    }
                    catch (Exception ex)
                    {
                        result.Steps.Add(Loc.F("Could not close {0} (PID {1}): {2}", name, id, ex.Message));
                    }
                }
            }
        }

        private static bool CloseLockingProcesses(string path, Result result)
        {
            var targets = new Dictionary<int, string>();
            foreach (var locker in whousef.GetLockers(path))
                targets[locker.Id] = locker.Name;

            string processName = Path.GetFileNameWithoutExtension(path);
            if (!string.IsNullOrEmpty(processName))
            {
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    using (process)
                    {
                        try
                        {
                            if (!targets.ContainsKey(process.Id) && string.Equals(ImagePath(process.Id), path, StringComparison.OrdinalIgnoreCase))
                                targets[process.Id] = process.ProcessName;
                        }
                        catch (Exception) { }
                    }
                }
            }

            bool closedAny = false;
            foreach (var target in targets)
            {
                if (whousef.IsProtectedProcess(target.Key, target.Value))
                {
                    result.Steps.Add(Loc.F("Skipped Windows process {0} (PID {1})", target.Value, target.Key));
                    continue;
                }

                try
                {
                    using (Process process = Process.GetProcessById(target.Key))
                    {
                        process.CloseMainWindow();
                        if (!process.WaitForExit(5000))
                        {
                            process.Kill();
                            process.WaitForExit(5000);
                        }
                    }
                    result.Steps.Add(Loc.F("Closed {0} (PID {1})", target.Value, target.Key));
                    closedAny = true;
                }
                catch (ArgumentException)
                {
                    closedAny = true;
                }
                catch (Exception ex)
                {
                    result.Steps.Add(Loc.F("Could not close {0} (PID {1}): {2}", target.Value, target.Key, ex.Message));
                }
            }
            return closedAny;
        }

        private const int PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(int access, bool inheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr process, int flags, System.Text.StringBuilder name, ref int size);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        private static string? ImagePath(int processId)
        {
            IntPtr handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (handle == IntPtr.Zero)
                return null;
            try
            {
                var name = new System.Text.StringBuilder(1024);
                int size = name.Capacity;
                return QueryFullProcessImageName(handle, 0, name, ref size) ? Path.GetFullPath(name.ToString()) : null;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        private static bool TakeOwnership(string path, Result result)
        {
            bool owned = RunTool("takeown.exe", $"/F \"{path}\" /A");
            bool granted = RunTool("icacls.exe", $"\"{path}\" /grant *S-1-5-32-544:F /C");
            if (owned || granted)
                result.Steps.Add(Loc.T("Took ownership and gave Administrators full control"));
            return owned || granted;
        }

        private static bool RunTool(string fileName, string arguments)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, fileName),
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process process = Process.Start(startInfo))
                {
                    if (process == null)
                        return false;
                    process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    if (!process.WaitForExit(15000))
                    {
                        process.Kill();
                        return false;
                    }
                    return process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
