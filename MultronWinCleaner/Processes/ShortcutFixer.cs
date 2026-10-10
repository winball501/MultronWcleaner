using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MultronWinCleaner.Processes
{
    public static class ShortcutFixer
    {
        public sealed class BrokenShortcut
        {
            public string ShortcutPath { get; set; } = "";
            public string TargetPath { get; set; } = "";
            public string RepairTarget { get; set; }
            public string Location { get; set; } = "";
            public string Name => Path.GetFileNameWithoutExtension(ShortcutPath);
            public string Action => RepairTarget != null ? Loc.F("Will be repaired: {0}", RepairTarget) : Loc.T("Will be moved to the Recycle Bin");
        }

        private static readonly EnumerationOptions LinkSearch = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        private static IEnumerable<(string Folder, string Location)> ShortcutFolders()
        {
            yield return (Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Desktop");
            yield return (Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), "Public Desktop");
            yield return (Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Start Menu");
            yield return (Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms), "Start Menu (all users)");
        }

        public static List<BrokenShortcut> FindBrokenShortcuts()
        {
            var result = new List<BrokenShortcut>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var shell = new IWshRuntimeLibrary.WshShell();

            foreach (var (folder, location) in ShortcutFolders())
            {
                if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
                    continue;

                foreach (string file in Directory.EnumerateFiles(folder, "*.lnk", LinkSearch))
                {
                    if (!seen.Add(file))
                        continue;

                    string target;
                    try
                    {
                        var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(file);
                        target = shortcut.TargetPath;
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (!IsMissing(target))
                        continue;

                    result.Add(new BrokenShortcut
                    {
                        ShortcutPath = file,
                        TargetPath = target,
                        RepairTarget = FindReplacement(target),
                        Location = location
                    });
                }
            }
            return result.OrderBy(r => r.Location).ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool Exists(string path) => File.Exists(path) || Directory.Exists(path);

        private static bool IsMissing(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
                return false;

            string path = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));
            if (path.Contains('%') || path.StartsWith(@"\\") || !Path.IsPathFullyQualified(path))
                return false;

            try
            {
                string root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root) || Exists(path))
                    return false;

                string x86 = @"\Program Files (x86)\", x64 = @"\Program Files\";
                if (path.Contains(x86, StringComparison.OrdinalIgnoreCase) && Exists(path.Replace(x86, x64, StringComparison.OrdinalIgnoreCase)))
                    return false;
                if (path.Contains(x64, StringComparison.OrdinalIgnoreCase) && Exists(path.Replace(x64, x86, StringComparison.OrdinalIgnoreCase)))
                    return false;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static string VersionlessName(string name) => Regex.Replace(name, @"[\d\.\-_ ]+", "").ToLowerInvariant();

        private static string FindReplacement(string target)
        {
            try
            {
                string path = Environment.ExpandEnvironmentVariables(target.Trim().Trim('"'));
                string ancestor = Path.GetDirectoryName(path);
                while (!string.IsNullOrEmpty(ancestor) && !Directory.Exists(ancestor))
                    ancestor = Path.GetDirectoryName(ancestor);
                if (string.IsNullOrEmpty(ancestor))
                    return null;

                string[] segments = path.Substring(ancestor.TrimEnd('\\').Length).TrimStart('\\').Split('\\');
                if (segments.Length < 2 || !segments[0].Any(char.IsDigit))
                    return null;

                string missingFolder = VersionlessName(segments[0]);
                string rest = Path.Combine(segments.Skip(1).ToArray());
                var candidates = Directory.EnumerateDirectories(ancestor)
                    .Where(d => VersionlessName(Path.GetFileName(d)) == missingFolder && File.Exists(Path.Combine(d, rest)))
                    .ToList();
                return candidates.Count == 1 ? Path.Combine(candidates[0], rest) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static (int Repaired, int Removed, List<BrokenShortcut> Failed) Fix(IEnumerable<BrokenShortcut> shortcuts)
        {
            int repaired = 0, removed = 0;
            var failed = new List<BrokenShortcut>();
            var shell = new IWshRuntimeLibrary.WshShell();

            foreach (var item in shortcuts)
            {
                try
                {
                    if (!File.Exists(item.ShortcutPath))
                        continue;

                    if (item.RepairTarget != null && File.Exists(item.RepairTarget))
                    {
                        var shortcut = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(item.ShortcutPath);
                        string oldFolder = Path.GetDirectoryName(Environment.ExpandEnvironmentVariables(item.TargetPath));
                        string newFolder = Path.GetDirectoryName(item.RepairTarget);
                        shortcut.TargetPath = item.RepairTarget;
                        if (!string.IsNullOrEmpty(shortcut.WorkingDirectory) && Environment.ExpandEnvironmentVariables(shortcut.WorkingDirectory).StartsWith(oldFolder, StringComparison.OrdinalIgnoreCase))
                            shortcut.WorkingDirectory = newFolder + Environment.ExpandEnvironmentVariables(shortcut.WorkingDirectory).Substring(oldFolder.Length);
                        if (!string.IsNullOrEmpty(shortcut.IconLocation) && Environment.ExpandEnvironmentVariables(shortcut.IconLocation).StartsWith(oldFolder, StringComparison.OrdinalIgnoreCase))
                            shortcut.IconLocation = newFolder + Environment.ExpandEnvironmentVariables(shortcut.IconLocation).Substring(oldFolder.Length);
                        shortcut.Save();
                        repaired++;
                    }
                    else
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(item.ShortcutPath,
                            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                        removed++;
                    }
                }
                catch (Exception)
                {
                    failed.Add(item);
                }
            }
            return (repaired, removed, failed);
        }
    }
}
