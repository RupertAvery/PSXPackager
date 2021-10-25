using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Popstation.Pbp
{

    //Offset Purpose
    //0x00	The PBP signature, always is 00 50 42 50 or the string "<null char>PBP"
    //0x04	Unknown purpose, possibly the version number.Currently is always 0x00000100 or 0x01000100 (some MINIS, PSP remaster and PSP PlayView)
    //0x08	Offset of the file PARAM.SFO(this value should always be 0x28)
    //0x0C	Offset of the file ICON0.PNG
    //0x10	Offset of the file ICON1.PMF or ICON1.PNG
    //0x14	Offset of the file PIC0.PNG or UNKNOWN.PNG (Value can be repeated)
    //0x18	Offset of the file PIC1.PNG or PICT1.PNG
    //0x1C	Offset of the file SND0.AT3
    //0x20	Offset of the file DATA.PSP
    //0x24	Offset of the file DATA.PSAR

    public enum ResourceType
    {
        SFO,
        ICON0,
        ICON1,
        PIC0,
        PIC1,
        SND0,
        PSP,
        PSAR
    }

    public class PbpReader
    {
        //The location of the PSAR offset in the PBP header
        private const int HEADER_SFO_OFFSET = 0x08;
        private const int HEADER_ICON0_OFFSET = 0x0C;
        private const int HEADER_ICON1_OFFSET = 0x10;
        private const int HEADER_PIC0_OFFSET = 0x14;
        private const int HEADER_PIC1_OFFSET = 0x18;
        private const int HEADER_SND0_OFFSET = 0x1C;
        private const int HEADER_PSP_OFFSET = 0x20;
        private const int HEADER_PSAR_OFFSET = 0x24;
        // The size of one "block" of the ISO
        public const int ISO_BLOCK_SIZE = 0x930;

        public SFOData SFOData { get; }

        public List<PbpDiscEntry> Discs { get; }


        public int Seek(ResourceType resource, Stream stream)
        {
            int start;
            int end;
            int offset;
            switch (resource)
            {
                case ResourceType.SFO:
                    offset = HEADER_SFO_OFFSET;
                    break;
                case ResourceType.ICON0:
                    offset = HEADER_ICON0_OFFSET;
                    break;
                case ResourceType.ICON1:
                    offset = HEADER_ICON1_OFFSET;
                    break;
                case ResourceType.PIC0:
                    offset = HEADER_PIC0_OFFSET;
                    break;
                case ResourceType.PIC1:
                    offset = HEADER_PIC1_OFFSET;
                    break;
                case ResourceType.SND0:
                    offset = HEADER_SND0_OFFSET;
                    break;
                case ResourceType.PSP:
                    offset = HEADER_PSP_OFFSET;
                    break;
                case ResourceType.PSAR:
                    offset = HEADER_PSAR_OFFSET;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(resource));
            }
            stream.Seek(offset, SeekOrigin.Begin);
            var reader = new BinaryReader(stream);
            start = reader.ReadInt32();
            if (resource != ResourceType.PSAR)
            {
                end = reader.ReadInt32();
            }
            else
            {
                end = (int)stream.Length;
            }

            stream.Seek(start, SeekOrigin.Begin);

            return end - start;
        }

        public bool TryGetResourceStream(ResourceType resource, Stream stream, out Stream outputStream)
        {
            var length = Seek(resource, stream);
            if (length > 0)
            {
                var buffer = new byte[length];
                stream.Read(buffer, 0, length);
                outputStream = new MemoryStream(buffer);
                return true;
            }
            outputStream = null;
            return false;
        }

        public PbpReader(Stream stream)
        {
            var buffer = new byte[16];
            stream.Read(buffer, 0, 4);

            stream.Seek(HEADER_SFO_OFFSET, SeekOrigin.Begin);
            var sfoOffset = stream.ReadUInteger();

            stream.Seek(sfoOffset, SeekOrigin.Begin);
            SFOData = stream.ReadSFO(sfoOffset);

            stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
            var psarOffset = stream.ReadInteger();

            if (psarOffset == 0 || stream.Position != HEADER_PSAR_OFFSET + sizeof(int))
            {
                throw new Exception("Invalid PSAR offset or corrupted file");
            }

            stream.Seek(psarOffset, SeekOrigin.Begin);
            stream.Read(buffer, 0, 12);
            var header = Encoding.ASCII.GetString(buffer);

            if (header.Substring(0, 12) == "PSISOIMG0000")
            {
                Discs = new List<PbpDiscEntry>()
                {
                    new PbpDiscEntry(stream, psarOffset, 1)
                };

            }
            else
            {
                stream.Read(buffer, 12, 4);
                header = Encoding.ASCII.GetString(buffer);

                if (header.Substring(0, 16) != "PSTITLEIMG000000")
                {
                    throw new Exception("Invalid header");
                }

                //stream.WriteInteger(0, 2);
                stream.ReadInteger();
                stream.ReadInteger();

                var a = stream.ReadUInteger();
                if (a != 0x2CC9C5BC)
                {
                    throw new Exception("Invalid header");
                }
                var b = stream.ReadUInteger();
                if (b != 0x33B5A90F)
                {
                    throw new Exception("Invalid header");
                }
                var c = stream.ReadUInteger();
                if (c != 0x06F6B4B3)
                {
                    throw new Exception("Invalid header");
                }
                var d = stream.ReadUInteger();
                if (d != 0xB25945BA)
                {
                    throw new Exception("Invalid header");
                }

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
                stream.Read(iso_positions, 0, sizeof(uint) * 5);

                Discs = iso_positions
                    .Where(x => x > 0)
                    .Select((x, i) => new PbpDiscEntry(stream, psarOffset + (int)x, i + 1)).ToList();

            }

        }
    }
}
