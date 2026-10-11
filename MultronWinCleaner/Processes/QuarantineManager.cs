using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
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
            public bool Compressed { get; set; }
            public bool Encrypted { get; set; }
            public string EncryptionKey { get; set; } = "";
            public long StoredSize { get; set; }
            public string ContentSha256 { get; set; } = "";

            public string OriginalFileName => Path.GetFileName(OriginalPath);

            public string FormattedSize => FormatSize(Size);

            public string FormattedStoredSize => Compressed || Encrypted ? FormatSize(StoredSize) : FormatSize(Size);

            public string FormattedDate => QuarantinedAt.ToString("yyyy-MM-dd HH:mm");
        }

        private static readonly object sync = new();

        private static readonly byte[] Magic = { (byte)'M', (byte)'W', (byte)'C', (byte)'Q', 1 };
        private const int ChunkSize = 1 << 20;
        private const int NoncePrefixSize = 8;
        private const int TagSize = 16;

        public static string QuarantineFolder => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Quarantine");
        private static string IndexFile => Path.Combine(QuarantineFolder, "index.json");

        public static string FormatSize(long size)
        {
            double s = size;
            string[] units = { "B", "KB", "MB", "GB" };
            int u = 0;
            while (s >= 1024 && u < units.Length - 1) { s /= 1024; u++; }
            return $"{s:0.#} {units[u]}";
        }

        public static List<Entry> LoadEntries()
        {
            lock (sync)
            {
                try
                {
                    if (!File.Exists(IndexFile)) return new();
                    string json = File.ReadAllText(IndexFile);
                    return JsonSerializer.Deserialize<List<Entry>>(json) ?? new();
                }
                catch { return new(); }
            }
        }

        private static void SaveEntries(List<Entry> entries)
        {
            Directory.CreateDirectory(QuarantineFolder);
            File.WriteAllText(IndexFile, JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
        }

        private static void UpdateEntries(Action<List<Entry>> change)
        {
            lock (sync)
            {
                var entries = LoadEntries();
                change(entries);
                SaveEntries(entries);
            }
        }

        private static string HashFile(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
            return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
        }

        private static void CompressFile(string source, string destination)
        {
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
            using (var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 16))
            using (var gzip = new GZipStream(output, CompressionLevel.SmallestSize))
                input.CopyTo(gzip);
        }

        private static void DecompressFile(string source, string destination)
        {
            using (var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16))
                gzip.CopyTo(output);
        }

        private static byte[] ChunkNonce(byte[] prefix, long index)
        {
            var nonce = new byte[12];
            prefix.CopyTo(nonce, 0);
            BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(NoncePrefixSize), checked((uint)index));
            return nonce;
        }

        private static byte[] ChunkData(long index, bool last)
        {
            var data = new byte[9];
            BinaryPrimitives.WriteInt64BigEndian(data, index);
            data[8] = last ? (byte)1 : (byte)0;
            return data;
        }

        private static void EncryptFile(string source, string destination, byte[] key)
        {
            var prefix = RandomNumberGenerator.GetBytes(NoncePrefixSize);
            using var aes = new AesGcm(key, TagSize);
            using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
            using var output = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 16);
            output.Write(Magic);
            output.Write(prefix);

            var plain = new byte[ChunkSize];
            var cipher = new byte[ChunkSize];
            var tag = new byte[TagSize];
            var length = new byte[4];
            long index = 0;
            while (true)
            {
                int read = input.ReadAtLeast(plain, ChunkSize, throwOnEndOfStream: false);
                bool last = input.Position >= input.Length;
                aes.Encrypt(ChunkNonce(prefix, index), plain.AsSpan(0, read), cipher.AsSpan(0, read), tag, ChunkData(index, last));
                BinaryPrimitives.WriteInt32BigEndian(length, read);
                output.Write(length);
                output.Write(tag);
                output.Write(cipher, 0, read);
                index++;
                if (last) break;
            }
        }

        private static void DecryptFile(string source, string destination, byte[] key)
        {
            using var input = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
            using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
            DecryptStream(input, output, key);
        }

        private static void DecryptStream(Stream input, Stream output, byte[] key)
        {
            using var aes = new AesGcm(key, TagSize);

            var header = new byte[Magic.Length];
            var prefix = new byte[NoncePrefixSize];
            if (input.ReadAtLeast(header, header.Length, false) != header.Length || !header.AsSpan().SequenceEqual(Magic) ||
                input.ReadAtLeast(prefix, prefix.Length, false) != prefix.Length)
                throw new InvalidDataException("Bad quarantine header.");

            var cipher = new byte[ChunkSize];
            var plain = new byte[ChunkSize];
            var tag = new byte[TagSize];
            var length = new byte[4];
            long index = 0;
            while (true)
            {
                if (input.ReadAtLeast(length, 4, false) != 4)
                    throw new InvalidDataException("Truncated quarantine file.");
                int size = BinaryPrimitives.ReadInt32BigEndian(length);
                if (size < 0 || size > ChunkSize ||
                    input.ReadAtLeast(tag, TagSize, false) != TagSize ||
                    input.ReadAtLeast(cipher, size, false) != size)
                    throw new InvalidDataException("Truncated quarantine file.");
                bool last = input.Position >= input.Length;
                aes.Decrypt(ChunkNonce(prefix, index), cipher.AsSpan(0, size), tag, plain.AsSpan(0, size), ChunkData(index, last));
                output.Write(plain, 0, size);
                index++;
                if (last) break;
            }
        }

        private static string NewStoredName(string filePath)
        {
            string name = DateTime.UtcNow.Ticks.ToString("X") + "_" + Path.GetFileName(filePath) + ".qf";
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            for (int i = 2; File.Exists(Path.Combine(QuarantineFolder, name)) || File.Exists(Path.Combine(QuarantineFolder, name + ".raw")); i++)
                name = DateTime.UtcNow.Ticks.ToString("X") + "_" + i + "_" + Path.GetFileName(filePath) + ".qf";
            return name;
        }

        private static void DeleteStoredFile(string path)
        {
            if (!File.Exists(path)) return;
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }

        private static void TryDeleteStoredFile(string path)
        {
            try { DeleteStoredFile(path); }
            catch (Exception) { }
        }

        private static bool TryProtect(string rawPath, string qPath, Entry entry)
        {
            string gzPath = qPath + ".gz";
            try
            {
                entry.ContentSha256 = HashFile(rawPath);
                CompressFile(rawPath, gzPath);
                EncryptCompressed(gzPath, qPath, entry);
                DeleteStoredFile(rawPath);
                return true;
            }
            catch (Exception)
            {
                TryDeleteStoredFile(gzPath);
                TryDeleteStoredFile(qPath);
                entry.Compressed = false;
                entry.Encrypted = false;
                entry.EncryptionKey = "";
                entry.StoredSize = 0;
                return false;
            }
        }

        private static void EncryptCompressed(string gzPath, string qPath, Entry entry)
        {
            var key = RandomNumberGenerator.GetBytes(32);
            EncryptFile(gzPath, qPath, key);
            entry.Compressed = true;
            entry.Encrypted = true;
            entry.EncryptionKey = Convert.ToBase64String(key);
            entry.StoredSize = new FileInfo(qPath).Length;
            DeleteStoredFile(gzPath);
        }

        public static Entry Quarantine(string filePath, string threatName, string sha256, long size, List<string>? steps = null, Action<string>? progress = null)
        {
            Directory.CreateDirectory(QuarantineFolder);
            string qName = NewStoredName(filePath);
            string qPath = Path.Combine(QuarantineFolder, qName);
            string rawPath = qPath + ".raw";

            var moved = ForceDelete.Move(filePath, rawPath, progress);
            steps?.AddRange(moved.Steps);
            if (moved.Outcome == ForceDelete.Outcome.Failed)
                throw new IOException(moved.Error);

            var entry = new Entry
            {
                OriginalPath = Path.GetFullPath(filePath),
                FileName = qName,
                ThreatName = threatName,
                Sha256 = sha256,
                Size = size > 0 ? size : new FileInfo(rawPath).Length,
                QuarantinedAt = DateTime.Now
            };

            progress?.Invoke(Loc.T("Compressing and encrypting the file in quarantine..."));
            if (TryProtect(rawPath, qPath, entry))
            {
                string text = Loc.F("Compressed and encrypted the file in quarantine with AES-256 ({0} → {1})", entry.FormattedSize, FormatSize(entry.StoredSize));
                steps?.Add(text);
                progress?.Invoke(text);
            }
            else
            {
                File.Move(rawPath, qPath);
                string text = Loc.T("The file could not be compressed and encrypted and is kept as it is in quarantine");
                steps?.Add(text);
                progress?.Invoke(text);
            }
            File.SetAttributes(qPath, FileAttributes.Hidden | FileAttributes.ReadOnly);

            UpdateEntries(entries => entries.Add(entry));
            return entry;
        }

        public static bool Restore(Entry entry)
        {
            string qPath = Path.Combine(QuarantineFolder, entry.FileName);
            if (!File.Exists(qPath)) return false;

            string? dir = Path.GetDirectoryName(entry.OriginalPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            if (entry.Compressed || entry.Encrypted)
            {
                string gzPath = qPath + ".gz";
                string tempPath = qPath + ".restore";
                try
                {
                    try
                    {
                        string compressedPath = qPath;
                        if (entry.Encrypted)
                        {
                            DecryptFile(qPath, gzPath, Convert.FromBase64String(entry.EncryptionKey));
                            compressedPath = gzPath;
                        }
                        DecompressFile(compressedPath, tempPath);
                    }
                    catch (Exception ex) when (ex is InvalidDataException or CryptographicException or FormatException)
                    {
                        throw new InvalidDataException(Loc.T("The quarantined file is damaged and cannot be restored."));
                    }
                    if (!string.IsNullOrEmpty(entry.ContentSha256) &&
                        !string.Equals(HashFile(tempPath), entry.ContentSha256, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidDataException(Loc.T("The quarantined file is damaged and cannot be restored."));
                    File.Move(tempPath, entry.OriginalPath, true);
                }
                finally
                {
                    TryDeleteStoredFile(gzPath);
                    TryDeleteStoredFile(tempPath);
                }
                DeleteStoredFile(qPath);
            }
            else
            {
                File.SetAttributes(qPath, FileAttributes.Normal);
                File.Move(qPath, entry.OriginalPath, true);
            }

            UpdateEntries(entries => entries.RemoveAll(e => e.FileName == entry.FileName));
            return true;
        }

        public const string ExportPassword = "infected";

        public static byte[] ReadContent(Entry entry)
        {
            string qPath = Path.Combine(QuarantineFolder, entry.FileName);
            if (!File.Exists(qPath))
                throw new FileNotFoundException(Loc.T("The quarantined file was not found."), qPath);

            byte[] content;
            try
            {
                using var file = new FileStream(qPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1 << 16);
                Stream compressed = file;
                using var decrypted = new MemoryStream();
                if (entry.Encrypted)
                {
                    DecryptStream(file, decrypted, Convert.FromBase64String(entry.EncryptionKey));
                    decrypted.Position = 0;
                    compressed = decrypted;
                }
                using var plain = new MemoryStream();
                if (entry.Compressed)
                {
                    using var gzip = new GZipStream(compressed, CompressionMode.Decompress, leaveOpen: true);
                    gzip.CopyTo(plain);
                }
                else
                {
                    compressed.CopyTo(plain);
                }
                content = plain.ToArray();
            }
            catch (Exception ex) when (ex is InvalidDataException or CryptographicException or FormatException)
            {
                throw new InvalidDataException(Loc.T("The quarantined file is damaged and cannot be restored."));
            }

            if (!string.IsNullOrEmpty(entry.ContentSha256) &&
                !string.Equals(Convert.ToHexString(SHA256.HashData(content)), entry.ContentSha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException(Loc.T("The quarantined file is damaged and cannot be restored."));
            return content;
        }

        public static void ExportZip(IReadOnlyList<Entry> entries, string zipPath)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var info = new System.Text.StringBuilder();
            info.AppendLine("Multron Win Cleaner " + Updater.CurrentAppVersion + " - " + Loc.T("Quarantine export"));
            info.AppendLine(Loc.F("Exported: {0}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")));
            info.AppendLine(Loc.T("WARNING: this archive contains malware detected by the cloud malware scan. Do not run these files."));
            info.AppendLine(new string('=', 72));

            string tempZip = zipPath + ".tmp";
            try
            {
                using (var writer = new ZipCryptoWriter(tempZip, ExportPassword))
                {
                    foreach (var entry in entries)
                    {
                        byte[] content = ReadContent(entry);
                        string name = string.IsNullOrWhiteSpace(entry.OriginalFileName) ? entry.FileName : entry.OriginalFileName;
                        foreach (char c in Path.GetInvalidFileNameChars())
                            name = name.Replace(c, '_');
                        string unique = name;
                        for (int i = 2; !names.Add(unique); i++)
                            unique = Path.GetFileNameWithoutExtension(name) + "_" + i + Path.GetExtension(name);

                        writer.Add(unique, content, entry.QuarantinedAt);
                        info.AppendLine();
                        info.AppendLine(Loc.F("File: {0}", unique));
                        info.AppendLine(Loc.F("Original path: {0}", entry.OriginalPath));
                        info.AppendLine(Loc.F("Threat: {0}", entry.ThreatName));
                        info.AppendLine("SHA-256: " + Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant());
                        info.AppendLine(Loc.F("Size: {0}", entry.FormattedSize));
                        info.AppendLine(Loc.F("Quarantined: {0}", entry.QuarantinedAt.ToString("yyyy-MM-dd HH:mm:ss")));
                    }
                    writer.Add("_info.txt", System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(info.ToString())).ToArray(), DateTime.Now);
                }
                File.Move(tempZip, zipPath, true);
            }
            finally
            {
                TryDeleteStoredFile(tempZip);
            }
        }

        public static bool Delete(Entry entry)
        {
            DeleteStoredFile(Path.Combine(QuarantineFolder, entry.FileName));
            UpdateEntries(entries => entries.RemoveAll(e => e.FileName == entry.FileName));
            return true;
        }

        public static void DeleteAll()
        {
            lock (sync)
            {
                foreach (var entry in LoadEntries())
                {
                    try { DeleteStoredFile(Path.Combine(QuarantineFolder, entry.FileName)); }
                    catch { }
                }
                SaveEntries(new());
            }
        }

        public static int ProtectLegacyEntries()
        {
            int protectedCount = 0;
            lock (sync)
            {
                var entries = LoadEntries();
                foreach (var entry in entries.Where(e => !e.Encrypted))
                {
                    string qPath = Path.Combine(QuarantineFolder, entry.FileName);
                    string movedPath = qPath + (entry.Compressed ? ".gz" : ".raw");
                    try
                    {
                        if (!File.Exists(qPath)) continue;
                        File.SetAttributes(qPath, FileAttributes.Normal);
                        File.Move(qPath, movedPath);
                        bool done;
                        if (entry.Compressed)
                        {
                            try
                            {
                                EncryptCompressed(movedPath, qPath, entry);
                                done = true;
                            }
                            catch (Exception)
                            {
                                TryDeleteStoredFile(qPath);
                                entry.Encrypted = false;
                                entry.EncryptionKey = "";
                                done = false;
                            }
                        }
                        else
                        {
                            done = TryProtect(movedPath, qPath, entry);
                        }
                        if (done)
                            protectedCount++;
                        else
                            File.Move(movedPath, qPath);
                        File.SetAttributes(qPath, FileAttributes.Hidden | FileAttributes.ReadOnly);
                    }
                    catch (Exception)
                    {
                        try
                        {
                            if (!File.Exists(qPath) && File.Exists(movedPath))
                                File.Move(movedPath, qPath);
                        }
                        catch (Exception) { }
                    }
                }
                if (protectedCount > 0)
                    SaveEntries(entries);
            }
            return protectedCount;
        }

        public static int Count => LoadEntries().Count;
    }
}
