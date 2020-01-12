using System;
using System.IO;

namespace Popstation
{
    public static class StreamExtensions
    {
        public static void Read(this Stream stream, uint[] buffer, int count)
        {
            for (var i = 0; i < count; i++)
            {
                var temp_buffer = new byte[sizeof(uint)];
                stream.Read(temp_buffer, 0, 4);
                buffer[i] = BitConverter.ToUInt32(temp_buffer, 0);
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

        public static int ReadInteger(this Stream stream)
        {
            var temp_buffer = new byte[sizeof(int)];
            stream.Read(temp_buffer, 0, 4);
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

        public static void Read(this Stream stream, uint[] buffer, int count, int size)
        {
            var p = 0;
            var i = 0;
            var b = new byte[sizeof(uint)];
            while (p < size)
            {
                stream.Read(b, 0, sizeof(uint));
                buffer[i] = BitConverter.ToUInt32(b, 0);
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

        public static void WriteInteger(this Stream stream, uint value, int count)
        {
            var p = 0;
            while (p < count)
            {
                var tempBuffer = BitConverter.GetBytes(value);
                stream.Write(tempBuffer, 0, sizeof(uint));
                p += 1;
            }
        }



        public static void WriteInteger(this Stream stream, int value, int count)
        {
            var p = 0;
            while (p < count)
            {
                var tempBuffer = BitConverter.GetBytes(value);
                stream.Write(tempBuffer, 0, sizeof(int));
                p += 1;
            }
        }

    }
}
