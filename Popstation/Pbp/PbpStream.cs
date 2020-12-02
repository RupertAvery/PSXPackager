using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Popstation.Pbp
{
    public class PbpStream : IDisposable
    {
        //The location of the PSAR offset in the PBP header
        const int HEADER_PSAR_OFFSET = 0x24;

        // The size of one "block" of the ISO
        public const int ISO_BLOCK_SIZE = 0x930;

        private readonly Stream stream;

        public bool MultiDisc { get; private set; }
        public int TotalDiscs { get; private set; }

        public List<PbpDiscEntry> Discs { get; }


        private int psar_offset;

        public PbpStream(string path, FileMode mode, FileAccess access)
        {
            stream = new FileStream(path, mode, access);

            stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
            psar_offset = stream.ReadInteger();

            if (psar_offset == 0 || stream.Position != HEADER_PSAR_OFFSET + sizeof(int))
            {
                throw new Exception("Invalid PSAR offset or corrupted file");
            }

            stream.Seek(psar_offset, SeekOrigin.Begin);
            var buffer = new byte[16];
            stream.Read(buffer, 0, 12);
            var header = ASCIIEncoding.ASCII.GetString(buffer);

            if (header.Substring(0, 12) == "PSISOIMG0000")
            {
                Discs = new List<PbpDiscEntry>()
                {
                    new PbpDiscEntry(stream, psar_offset)
                };
            }
            else
            {
                stream.Read(buffer, 12, 4);
                header = ASCIIEncoding.ASCII.GetString(buffer);
                MultiDisc = true;
                //stream.WriteInteger(0, 2);
                stream.ReadInteger();
                stream.ReadInteger();

                var a = stream.ReadInteger();
                var b = stream.ReadInteger();
                var c = stream.ReadInteger();
                var d = stream.ReadInteger();

                //stream.WriteInteger(0x2CC9C5BC, 1);
                //stream.WriteInteger(0x33B5A90F, 1);
                //stream.WriteInteger(0x06F6B4B3, 1);
                //stream.WriteInteger(0xB25945BA, 1);

                for (var i = 0; i < 0x76; i++)
                {
                    stream.ReadInteger();
                }

                //stream.WriteInteger(0, 0x76);

                uint[] iso_positions = new uint[5];
                stream.Read(iso_positions, 0, 5);

                Discs = iso_positions
                    .Where(x => x > 0)
                    .Select(x => new PbpDiscEntry(stream, psar_offset + (int)x)).ToList();

            }

        }


        public void Dispose()
        {
            stream?.Dispose();
        }
    }
}
