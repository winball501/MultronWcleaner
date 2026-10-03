using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultronWinCleaner.Processes
{
    public static class SystemChecks
    {
        public sealed class ComponentStoreInfo
        {
            public bool Parsed;
            public long ActualSize;
            public long SharedWithWindows;
            public long Backups;
            public long Cache;
            public int ReclaimablePackages;
            public bool? CleanupRecommended;
            public string LastCleanup;
            public bool RestartPending;
            public long Reclaimable => Math.Max(0, Backups) + Math.Max(0, Cache);
        }

        public enum HealthState { Healthy, Repairable, NotRepairable, Unknown }
        public enum SfcState { Clean, Violations, Repaired, NotRepaired, Unknown }

        private static readonly Regex SizeLine = new Regex(
            @"^\s*(?<label>Actual Size of Component Store|Shared with Windows|Backups and Disabled Features|Cache and Temporary Data)\s*:\s*(?<value>[\d\.,]+)\s*(?<unit>bytes|KB|MB|GB|TB)",
            RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ExitCodeRx = new Regex(@"#=#(-?\d+)\s*$", RegexOptions.Compiled);

        public static int? ExitCode(string output)
        {
            var match = ExitCodeRx.Match(output ?? "");
            return match.Success && int.TryParse(match.Groups[1].Value, out int code) ? code : null;
        }

        private static long ToBytes(string value, string unit)
        {
            if (!double.TryParse(value.Replace(",", ""), NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
                return -1;
            return unit.ToUpperInvariant() switch
            {
                "KB" => (long)(number * 1024),
                "MB" => (long)(number * 1024 * 1024),
                "GB" => (long)(number * 1024 * 1024 * 1024),
                "TB" => (long)(number * 1024L * 1024 * 1024 * 1024),
                _ => (long)number
            };
        }

        private static string ValueOf(string output, string label)
        {
            var match = Regex.Match(output ?? "", @"^\s*" + Regex.Escape(label) + @"\s*:\s*(?<value>.+?)\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["value"].Value : null;
        }

        public static ComponentStoreInfo ParseAnalyze(string output)
        {
            var info = new ComponentStoreInfo { Backups = -1, Cache = -1, ActualSize = -1, SharedWithWindows = -1 };
            foreach (Match match in SizeLine.Matches(output ?? ""))
            {
                long bytes = ToBytes(match.Groups["value"].Value, match.Groups["unit"].Value);
                string label = match.Groups["label"].Value.ToLowerInvariant();
                if (label.StartsWith("actual")) info.ActualSize = bytes;
                else if (label.StartsWith("shared")) info.SharedWithWindows = bytes;
                else if (label.StartsWith("backups")) info.Backups = bytes;
                else if (label.StartsWith("cache")) info.Cache = bytes;
            }
            info.Parsed = info.Backups >= 0 || info.Cache >= 0;

            if (int.TryParse(ValueOf(output, "Number of Reclaimable Packages"), out int packages))
                info.ReclaimablePackages = packages;
            string recommended = ValueOf(output, "Component Store Cleanup Recommended");
            if (recommended != null)
                info.CleanupRecommended = recommended.StartsWith("Yes", StringComparison.OrdinalIgnoreCase);
            info.LastCleanup = ValueOf(output, "Date of Last Cleanup");
            info.RestartPending = ExitCode(output) == 3010;
            return info;
        }

        public static HealthState ParseScanHealth(string output)
        {
            string text = output ?? "";
            if (text.Contains("No component store corruption detected", StringComparison.OrdinalIgnoreCase)) return HealthState.Healthy;
            if (text.Contains("cannot be repaired", StringComparison.OrdinalIgnoreCase)) return HealthState.NotRepairable;
            if (text.Contains("is repairable", StringComparison.OrdinalIgnoreCase)) return HealthState.Repairable;
            return HealthState.Unknown;
        }

        public static SfcState ParseSfc(string output)
        {
            string text = (output ?? "").Replace("\0", "");
            if (Has(text, "did not find any integrity violations", "herhangi bir bütünlük ihlali bulamadı")) return SfcState.Clean;
            if (Has(text, "successfully repaired", "başarıyla onardı")) return SfcState.Repaired;
            if (Has(text, "unable to fix some", "bazılarını düzeltemedi", "bazılarını onaramadı")) return SfcState.NotRepaired;
            if (Has(text, "found integrity violations", "bütünlük ihlalleri buldu", "bütünlük ihlali buldu")) return SfcState.Violations;
            return SfcState.Unknown;
        }

        private static bool Has(string text, params string[] phrases) =>
            phrases.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));

        public static string LastLine(string output)
        {
            string text = ExitCodeRx.Replace((output ?? "").Replace("\0", ""), "");
            return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                       .Select(l => l.Trim())
                       .LastOrDefault(l => l.Length > 0 && !Regex.IsMatch(l, @"^[\[\]=\s\d\.%]+$")) ?? "";
        }

        public static (bool Ok, string Text) DescribeDism(string output)
        {
            int? code = ExitCode(output);
            if (code == null)
                return (false, "Stopped: " + LastLine(output));
            if (code == 0)
                return (true, "The operation completed successfully.");
            if (code == 3010)
                return (true, "Completed. Restart the PC to finish.");
            return (false, $"Error {code}: {LastLine(output)}");
        }

        public static (bool Ok, string Text) DescribeSfc(string output)
        {
            return ParseSfc(output) switch
            {
                SfcState.Clean => (true, "No integrity violations were found."),
                SfcState.Repaired => (true, "Corrupt files were found and repaired."),
                SfcState.NotRepaired => (false, "Corrupt files were found but some could not be repaired. Details are in C:\\Windows\\Logs\\CBS\\CBS.log."),
                SfcState.Violations => (false, "Integrity violations were found."),
                _ => (false, LastLine(output))
            };
        }
    }
}
