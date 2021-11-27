using System;
using System.Collections.Generic;
using System.IO;
using Popstation.Iso;
using Popstation.Pbp;

namespace Popstation
{
    public static class StreamExtensions
    {
        public static void Read(this Stream stream, uint[] buffer, int count)
        {
            var uintBuffer = new byte[sizeof(uint)];
            for (var i = 0; i < count; i++)
            {
                stream.Read(uintBuffer, 0, 4);
                buffer[i] = BitConverter.ToUInt32(uintBuffer, 0);
            }
        }

        public static void Read(this Stream stream, int[] buffer, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var temp_buffer = new byte[sizeof(int)];
                stream.Read(temp_buffer, 0, 4);
                buffer[i] = BitConverter.ToInt32(temp_buffer, 0);
            }
        }

        public static ushort ReadUShort(this Stream stream)
        {
            var temp_buffer = new byte[sizeof(ushort)];
            stream.Read(temp_buffer, 0, sizeof(ushort));
            return BitConverter.ToUInt16(temp_buffer, 0);
        }
        public static short ReadShort(this Stream stream)
        {
            var temp_buffer = new byte[sizeof(short)];
            stream.Read(temp_buffer, 0, sizeof(short));
            return BitConverter.ToInt16(temp_buffer, 0);
        }

        public static uint ReadUInteger(this Stream stream)
        {
            var temp_buffer = new byte[sizeof(uint)];
            stream.Read(temp_buffer, 0, sizeof(uint));
            return BitConverter.ToUInt32(temp_buffer, 0);
        }

        public static int ReadInteger(this Stream stream)
        {
            var temp_buffer = new byte[sizeof(int)];
            stream.Read(temp_buffer, 0, sizeof(uint));
            return BitConverter.ToInt32(temp_buffer, 0);
        }

        public static void Write(this Stream stream, IsoIndex[] isoIndices, int offset, int size)
        {
            foreach (var index in isoIndices)
            {
                var membuf = BitConverter.GetBytes(index.Offset);
                stream.Write(membuf, 0, sizeof(uint));

                membuf = BitConverter.GetBytes(index.Length);
                stream.Write(membuf, 0, sizeof(uint));

                for (var j = 0; j < index.Dummy.Length; j++)
                {
                    membuf = BitConverter.GetBytes(index.Dummy[j]);
                    stream.Write(membuf, 0, sizeof(uint));
                }
            }
        }

        public static void Write(this Stream stream, string buffer, int offset, int size)
        {
            var membuf = System.Text.Encoding.ASCII.GetBytes(buffer);
            stream.Write(membuf, offset, size);
        }

        public static void Write(this Stream stream, uint[] buffer, int count, int size)
        {
            var p = 0;
            var i = 0;
            while (p < size)
            {
                var tempBuffer = BitConverter.GetBytes(buffer[i]);
                stream.Write(tempBuffer, 0, sizeof(uint));
                p += sizeof(uint);
                i++;
            }
        }

        public static void WriteRandom(this Stream stream, int count)
        {
            var buffer = new byte[count];
            var r = new Random();
            r.NextBytes(buffer);
            stream.Write(buffer, 0, count);
        }

        public static void WriteChar(this Stream stream, byte value, int count)
        {
            var p = 0;
            while (p < count)
            {
                stream.WriteByte(value);
                p += 1;
            }
        }


        public static void Write(this Stream stream, char value)
        {
            stream.WriteByte((byte)value);
        }

        public static void WriteUInt16(this Stream stream, ushort value, int count)
        {
            var p = 0;
            while (p < count)
            {
                var tempBuffer = BitConverter.GetBytes(value);
                stream.Write(tempBuffer, 0, sizeof(ushort));
                p += 1;
            }
        }

        public static void WriteInt16(this Stream stream, short value, int count)
        {
            var p = 0;
            while (p < count)
            {
                var tempBuffer = BitConverter.GetBytes(value);
                stream.Write(tempBuffer, 0, sizeof(short));
                p += 1;
            }
        }

        public static void WriteUInt32(this Stream stream, uint value, int count)
        {
            var p = 0;
            while (p < count)
            {
                var tempBuffer = BitConverter.GetBytes(value);
                stream.Write(tempBuffer, 0, sizeof(uint));
                p += 1;
            }
        }

        public static void WriteInt32(this Stream stream, int value, int count)
        {
            var p = 0;
            while (p < count)
            {
                var tempBuffer = BitConverter.GetBytes(value);
                stream.Write(tempBuffer, 0, sizeof(int));
                p += 1;
            }
        }

        public static void WriteSFO(this Stream stream, SFOData sfo)
        {
            stream.WriteUInt32(sfo.Magic, 1);
            stream.WriteUInt32(sfo.Version, 1);
            stream.WriteUInt32(sfo.KeyTableOffset, 1);
            stream.WriteUInt32(sfo.DataTableOffset, 1);
            stream.WriteInt32((ushort)sfo.Entries.Count, 1);

            for (var i = 0; i < sfo.Entries.Count; i++)
            {
                var entry = sfo.Entries[i];
                stream.WriteUInt16(entry.KeyOffset, 1);
                stream.WriteUInt16(entry.Format, 1);
                stream.WriteUInt32(entry.Length, 1);
                stream.WriteUInt32(entry.MaxLength, 1);
                stream.WriteUInt32(entry.DataOffset, 1);
            }

            for (var i = 0; i < sfo.Entries.Count; i++)
            {
                var key = sfo.Entries[i].Key;
                stream.Write(key, 0, key.Length);
                stream.WriteByte(0);
            }

            for (var i = 0; i < sfo.Padding; i++)
            {
                stream.WriteByte(0);
            }

            for (var i = 0; i < sfo.Entries.Count; i++)
            {
                var entry = sfo.Entries[i];
                var value = entry.Value;
                switch (entry.Format)
                {
                    case 0x0204:
                        stream.Write((string)value, 0, ((string)value).Length);
                        stream.WriteByte(0);
                        for (var j = 0; j < entry.MaxLength - entry.Length; j++)
                        {
                            stream.WriteByte(0);
                        }
                        break;
                    case 0x0404:
                        stream.WriteInt32(Convert.ToInt32(value), 1);
                        break;
                }
            }

        }


        public static string ReadString(this Stream stream, int length)
        {
            var temp_buffer = new byte[length];
            stream.Read(temp_buffer, 0, length);
            return System.Text.Encoding.ASCII.GetString(temp_buffer, 0, length);
        }

        public static SFOData ReadSFO(this Stream stream, uint sfoOffset)
        {
            var sfo = new SFOData();
            sfo.Magic = stream.ReadUInteger();
            sfo.Version = stream.ReadUInteger();
            sfo.KeyTableOffset = stream.ReadUInteger();
            sfo.DataTableOffset = stream.ReadUInteger();
            var entries = stream.ReadUInteger();

            sfo.Entries = new List<SFODir>();
            for (var i = 0; i < entries; i++)
            {
                var entry = new SFODir
                {
                    KeyOffset = stream.ReadUShort(),
                    Format = stream.ReadUShort(),
                    Length = stream.ReadUInteger(),
                    MaxLength = stream.ReadUInteger(),
                    DataOffset = stream.ReadUInteger()
                };
                sfo.Entries.Add(entry);
            }

            for (var i = 0; i < sfo.Entries.Count; i++)
            {
                var entry = sfo.Entries[i];
                stream.Seek(sfoOffset + sfo.KeyTableOffset + entry.KeyOffset, SeekOrigin.Begin);
                var key = stream.ReadString(128);
                entry.Key = key.Substring(0, key.IndexOf('\0'));
            }

            for (var i = 0; i < sfo.Entries.Count; i++)
            {
                var entry = sfo.Entries[i];
                stream.Seek(sfoOffset + sfo.DataTableOffset + entry.DataOffset, SeekOrigin.Begin);
                switch (entry.Format)
                {
                    case 0x0204:
                        entry.Value = stream.ReadString((int)entry.Length - 1);
                        break;
                    case 0x0404:
                        entry.Value = stream.ReadUInteger();
                        break;
                }
            }

            return sfo;
        }
    }
}
