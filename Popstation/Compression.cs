using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.IO;

namespace Popstation
{
    public class Compression
    {
        public static byte[] Decompress(byte[] bytes)
        {
            using (var stream = new InflaterInputStream(new MemoryStream(bytes), new Inflater(true)))
            {
                MemoryStream memory = new MemoryStream();
                byte[] writeData = new byte[4096];
                int size;

                while (true)
                {
                    size = stream.Read(writeData, 0, writeData.Length);
                    if (size > 0)
                    {
                        memory.Write(writeData, 0, size);
                    }
                    else break;
                }
                return memory.ToArray();
            }
        }

        public static int Decompress(byte[] bytes, byte[] outbuf)
        {
            using (var stream = new InflaterInputStream(new MemoryStream(bytes), new Inflater(true)))
            {
                MemoryStream memory = new MemoryStream(outbuf);
                byte[] writeData = new byte[4096];
                int size;

                while (true)
                {
                    size = stream.Read(writeData, 0, writeData.Length);
                    if (size > 0)
                    {
                        memory.Write(writeData, 0, size);
                    }
                    else break;
                }
                return (int)memory.Position;
            }
        }

        public static byte[] Compress(byte[] inbuf, int level)
        {
            using (var ms = new MemoryStream())
            {
                var deflater = new Deflater(level, true);
                using (var outStream = new DeflaterOutputStream(ms, deflater))
                {
                    outStream.Write(inbuf, 0, inbuf.Length);
                    outStream.Flush();
                    outStream.Finish();
                    return ms.ToArray();
                }
            }
        }

        public static int Compress(byte[] inbuf, byte[] outbuf, int level)
        {
            using (var ms = new MemoryStream(outbuf))
            {
                var deflater = new Deflater(level, true);
                using (var outStream = new DeflaterOutputStream(ms, deflater))
                {
                    outStream.Write(inbuf, 0, inbuf.Length);
                    outStream.Flush();
                    outStream.Finish();
                    return (int)ms.Position;
                }
            }
        }
    }
}