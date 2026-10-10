using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace MultronWinCleaner.Processes
{
    public static class QuarantineManager
    {
        public class Entry
        {
            public string OriginalPath { get; set; } = "";
            public string FileName { get; set; } = "";
            public string ThreatName { get; set; } = "";
            public string Sha256 { get; set; } = "";
            public long Size { get; set; }
            public DateTime QuarantinedAt { get; set; }

            public string OriginalFileName => Path.GetFileName(OriginalPath);

            public string FormattedSize
            {
                get
                {
                    double s = Size;
                    string[] units = { "B", "KB", "MB", "GB" };
                    int u = 0;
                    while (s >= 1024 && u < units.Length - 1) { s /= 1024; u++; }
                    return $"{s:0.#} {units[u]}";
                }
            }

            public string FormattedDate => QuarantinedAt.ToString("yyyy-MM-dd HH:mm");
        }

        public static string QuarantineFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Quarantine");
        private static string IndexFile => Path.Combine(QuarantineFolder, "index.json");

        public static List<Entry> LoadEntries()
        {
            try
            {
                if (!File.Exists(IndexFile)) return new();
                string json = File.ReadAllText(IndexFile);
                return JsonSerializer.Deserialize<List<Entry>>(json) ?? new();
            }
            catch { return new(); }
        }

        private static void SaveEntries(List<Entry> entries)
        {
            Directory.CreateDirectory(QuarantineFolder);
            File.WriteAllText(IndexFile, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        }

        public static Entry Quarantine(string filePath, string threatName, string sha256, long size, List<string>? steps = null)
        {
            Directory.CreateDirectory(QuarantineFolder);
            string qName = DateTime.UtcNow.Ticks.ToString("X") + "_" + Path.GetFileName(filePath) + ".qf";
            foreach (char c in Path.GetInvalidFileNameChars())
                qName = qName.Replace(c, '_');
            string qPath = Path.Combine(QuarantineFolder, qName);

            if (File.Exists(qPath))
                qName = DateTime.UtcNow.Ticks.ToString("X") + "_2_" + Path.GetFileName(filePath) + ".qf";
            qPath = Path.Combine(QuarantineFolder, qName);

            var moved = ForceDelete.Move(filePath, qPath);
            steps?.AddRange(moved.Steps);
            if (moved.Outcome == ForceDelete.Outcome.Failed)
                throw new IOException(moved.Error);
            File.SetAttributes(qPath, FileAttributes.Hidden | FileAttributes.ReadOnly);

            var entry = new Entry
            {
                OriginalPath = Path.GetFullPath(filePath),
                FileName = qName,
                ThreatName = threatName,
                Sha256 = sha256,
                Size = size,
                QuarantinedAt = DateTime.Now
            };

            var entries = LoadEntries();
            entries.Add(entry);
            SaveEntries(entries);
            return entry;
        }

        public static bool Restore(Entry entry)
        {
            string qPath = Path.Combine(QuarantineFolder, entry.FileName);
            if (!File.Exists(qPath)) return false;

            File.SetAttributes(qPath, FileAttributes.Normal);
            string? dir = Path.GetDirectoryName(entry.OriginalPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.Move(qPath, entry.OriginalPath, true);

            var entries = LoadEntries();
            entries.RemoveAll(e => e.FileName == entry.FileName);
            SaveEntries(entries);
            return true;
        }

        public static bool Delete(Entry entry)
        {
            string qPath = Path.Combine(QuarantineFolder, entry.FileName);
            if (File.Exists(qPath))
            {
                File.SetAttributes(qPath, FileAttributes.Normal);
                File.Delete(qPath);
            }

            var entries = LoadEntries();
            entries.RemoveAll(e => e.FileName == entry.FileName);
            SaveEntries(entries);
            return true;
        }

        public static void DeleteAll()
        {
            var entries = LoadEntries();
            foreach (var entry in entries)
            {
                try
                {
                    string qPath = Path.Combine(QuarantineFolder, entry.FileName);
                    if (File.Exists(qPath))
                    {
                        File.SetAttributes(qPath, FileAttributes.Normal);
                        File.Delete(qPath);
                    }
                }
                catch { }
            }
            SaveEntries(new());
        }

        public static int Count => LoadEntries().Count;
    }
}
