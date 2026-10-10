using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace MultronWinCleaner.Processes
{
    public static class ShellContextMenu
    {
        public const string ScanArgument = "-malscan";
        private const string VerbName = "MultronMalwareScan";
        private static readonly string[] Targets = { "*", "Directory", "Drive" };

        private const string ClassicMenuKey = @"Software\Classes\CLSID\{86ca1aa0-34aa-4e8b-a509-50c905bae2a2}";

        public static bool IsWindows11 => Environment.OSVersion.Version.Build >= 22000;

        private static string VerbKey(string target) => $@"Software\Classes\{target}\shell\{VerbName}";

        private static string AppFolder => Path.GetDirectoryName(typeof(ShellContextMenu).Assembly.Location) ?? AppContext.BaseDirectory;

        private static string ExePath => Path.Combine(AppFolder, "MultronWinCleaner.exe");

        private static string SendToShortcut => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SendTo), "Multron Malware Scan.lnk");

        public static bool IsScanRegistered()
        {
            using var key = Registry.CurrentUser.OpenSubKey(VerbKey("*") + @"\command");
            return key != null || File.Exists(SendToShortcut);
        }

        public static void RegisterScan()
        {
            string exe = ExePath;
            foreach (string target in Targets)
            {
                using var verb = Registry.CurrentUser.CreateSubKey(VerbKey(target));
                verb.SetValue("MUIVerb", Loc.T("Scan with Multron Malware Scan"));
                verb.SetValue("Icon", $"\"{exe}\",0");
                verb.SetValue("MultiSelectModel", "Single");
                using var command = verb.CreateSubKey("command");
                command.SetValue("", $"\"{exe}\" {ScanArgument} \"%1\"");
            }

            var shell = new IWshRuntimeLibrary.WshShell();
            var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(SendToShortcut);
            shortcut.TargetPath = exe;
            shortcut.Arguments = ScanArgument;
            shortcut.WorkingDirectory = AppFolder;
            shortcut.IconLocation = exe + ",0";
            shortcut.Description = Loc.T("Scan with Multron Malware Scan");
            shortcut.Save();
        }

        public static void UnregisterScan()
        {
            foreach (string target in Targets)
                Registry.CurrentUser.DeleteSubKeyTree(VerbKey(target), false);
            if (File.Exists(SendToShortcut))
                File.Delete(SendToShortcut);
        }

        public static void RefreshScan()
        {
            if (IsScanRegistered())
                RegisterScan();
        }

        public static bool IsClassicMenuOn()
        {
            using var key = Registry.CurrentUser.OpenSubKey(ClassicMenuKey + @"\InprocServer32");
            return key != null;
        }

        public static void SetClassicMenu(bool on)
        {
            if (on)
            {
                using var key = Registry.CurrentUser.CreateSubKey(ClassicMenuKey + @"\InprocServer32");
                key.SetValue("", "");
            }
            else
            {
                Registry.CurrentUser.DeleteSubKeyTree(ClassicMenuKey, false);
            }
        }

        public static void RestartExplorer()
        {
            foreach (var process in Process.GetProcessesByName("explorer"))
            {
                try { process.Kill(); } catch { }
                process.Dispose();
            }
        }
    }
}
