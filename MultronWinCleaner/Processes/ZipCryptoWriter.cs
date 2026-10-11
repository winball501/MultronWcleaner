using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace MultronWinCleaner.Processes
{
    public sealed class ZipCryptoWriter : IDisposable
    {
        private sealed class CentralRecord
        {
            public byte[] Name = Array.Empty<byte>();
            public ushort Method;
            public ushort Time;
            public ushort Date;
            public uint Crc;
            public uint CompressedSize;
            public uint Size;
            public uint Offset;
        }

        private const ushort Flags = 0x0001 | 0x0800;
        private static readonly uint[] CrcTable = BuildCrcTable();

        private readonly FileStream output;
        private readonly byte[] password;
        private readonly List<CentralRecord> records = new();
        private bool finished;

        public ZipCryptoWriter(string path, string password)
        {
            this.password = Encoding.UTF8.GetBytes(password);
            output = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 1 << 16);
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1;
                table[n] = c;
            }
            return table;
        }

        private static uint UpdateCrc(uint crc, byte b) => CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);

        private static uint Crc32(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            foreach (byte b in data)
                crc = UpdateCrc(crc, b);
            return ~crc;
        }

        private sealed class Cipher
        {
            private uint key0 = 0x12345678, key1 = 0x23456789, key2 = 0x34567890;

            public Cipher(byte[] password)
            {
                foreach (byte b in password)
                    UpdateKeys(b);
            }

            private void UpdateKeys(byte b)
            {
                key0 = UpdateCrc(key0, b);
                key1 = (key1 + (key0 & 0xFF)) * 134775813 + 1;
                key2 = UpdateCrc(key2, (byte)(key1 >> 24));
            }

            public byte Encrypt(byte plain)
            {
                uint temp = (key2 | 2) & 0xFFFF;
                byte cipher = (byte)(plain ^ (byte)((temp * (temp ^ 1)) >> 8));
                UpdateKeys(plain);
                return cipher;
            }
        }

        private static (ushort Time, ushort Date) DosDateTime(DateTime value)
        {
            if (value.Year < 1980) value = new DateTime(1980, 1, 1);
            ushort time = (ushort)((value.Hour << 11) | (value.Minute << 5) | (value.Second / 2));
            ushort date = (ushort)(((value.Year - 1980) << 9) | (value.Month << 5) | value.Day);
            return (time, date);
        }

        private void WriteUInt16(ushort value) { output.WriteByte((byte)value); output.WriteByte((byte)(value >> 8)); }

        private void WriteUInt32(uint value) { WriteUInt16((ushort)value); WriteUInt16((ushort)(value >> 16)); }

        public void Add(string name, byte[] content, DateTime modified)
        {
            if (finished) throw new InvalidOperationException();

            byte[] payload = content;
            ushort method = 0;
            using (var deflated = new MemoryStream())
            {
                using (var deflate = new DeflateStream(deflated, CompressionLevel.Optimal, leaveOpen: true))
                    deflate.Write(content, 0, content.Length);
                if (deflated.Length < content.Length)
                {
                    payload = deflated.ToArray();
                    method = 8;
                }
            }

            uint crc = Crc32(content);
            var cipher = new Cipher(password);
            var header = RandomNumberGenerator.GetBytes(12);
            header[11] = (byte)(crc >> 24);
            var encrypted = new byte[header.Length + payload.Length];
            for (int i = 0; i < header.Length; i++)
                encrypted[i] = cipher.Encrypt(header[i]);
            for (int i = 0; i < payload.Length; i++)
                encrypted[header.Length + i] = cipher.Encrypt(payload[i]);

            if (output.Position > uint.MaxValue || (long)encrypted.Length > uint.MaxValue)
                throw new IOException("The export is too large for a ZIP file.");

            var (time, date) = DosDateTime(modified);
            var record = new CentralRecord
            {
                Name = Encoding.UTF8.GetBytes(name),
                Method = method,
                Time = time,
                Date = date,
                Crc = crc,
                CompressedSize = (uint)encrypted.Length,
                Size = (uint)content.Length,
                Offset = (uint)output.Position
            };

            WriteUInt32(0x04034B50);
            WriteUInt16(20);
            WriteUInt16(Flags);
            WriteUInt16(record.Method);
            WriteUInt16(record.Time);
            WriteUInt16(record.Date);
            WriteUInt32(record.Crc);
            WriteUInt32(record.CompressedSize);
            WriteUInt32(record.Size);
            WriteUInt16((ushort)record.Name.Length);
            WriteUInt16(0);
            output.Write(record.Name);
            output.Write(encrypted);
            records.Add(record);
        }

        private void Finish()
        {
            if (finished) return;
            finished = true;
            long start = output.Position;
            foreach (var record in records)
            {
                WriteUInt32(0x02014B50);
                WriteUInt16(20);
                WriteUInt16(20);
                WriteUInt16(Flags);
                WriteUInt16(record.Method);
                WriteUInt16(record.Time);
                WriteUInt16(record.Date);
                WriteUInt32(record.Crc);
                WriteUInt32(record.CompressedSize);
                WriteUInt32(record.Size);
                WriteUInt16((ushort)record.Name.Length);
                WriteUInt16(0);
                WriteUInt16(0);
                WriteUInt16(0);
                WriteUInt16(0);
                WriteUInt32(0);
                WriteUInt32(record.Offset);
                output.Write(record.Name);
            }
            long size = output.Position - start;
            WriteUInt32(0x06054B50);
            WriteUInt16(0);
            WriteUInt16(0);
            WriteUInt16((ushort)records.Count);
            WriteUInt16((ushort)records.Count);
            WriteUInt32((uint)size);
            WriteUInt32((uint)start);
            WriteUInt16(0);
        }

        public void Dispose()
        {
            try { Finish(); }
            finally { output.Dispose(); }
        }
    }
}
