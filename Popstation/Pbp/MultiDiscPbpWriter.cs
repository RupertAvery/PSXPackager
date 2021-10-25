using System;
using System.IO;
using System.Threading;

namespace Popstation.Pbp
{
    public class MultiDiscPbpWriter : PbpWriter
    {
        public MultiDiscPbpWriter(ConvertOptions convertInfo) : base(convertInfo)
        {
        }

        public override void WritePSAR(Stream outputStream, uint psarOffset, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1 * 1048576];
            byte[] buffer2 = new byte[BLOCK_SIZE];
            uint totSize;

            var title = convertInfo.MainGameTitle;
            var code = convertInfo.MainGameID;
            var region = convertInfo.MainGameRegion;

            uint[] dummy = new uint[6];

            uint i, offset, isosize, x;
            uint p1_offset, p2_offset, m_offset, end_offset;
            uint[] iso_positions = new uint[5];
            end_offset = 0;

            Notify?.Invoke(PopstationEventEnum.WritePsTitle, null);

            outputStream.Write("PSTITLEIMG000000", 0, 16);

            // Save this offset position
            p1_offset = (uint)outputStream.Position;

            outputStream.WriteInteger(0, 2);
            outputStream.WriteInteger(0x2CC9C5BC, 1);
            outputStream.WriteInteger(0x33B5A90F, 1);
            outputStream.WriteInteger(0x06F6B4B3, 1);
            outputStream.WriteInteger(0xB25945BA, 1);
            outputStream.WriteInteger(0, 0x76);

            m_offset = (uint)outputStream.Position;

            //memset(iso_positions, 0, sizeof(iso_positions));
            outputStream.Write(iso_positions, 1, sizeof(uint) * 5);

            outputStream.WriteRandom(12);
            outputStream.WriteInteger(0, 8);

            outputStream.Write('_');
            outputStream.Write(code, 0, 4);
            outputStream.Write('_');
            outputStream.Write(code, 4, 5);

            outputStream.WriteChar(0, 0x15);

            p2_offset = (uint)outputStream.Position;
            outputStream.WriteInteger(0, 2);

            outputStream.Write(Popstation.data3, 0, Popstation.data3.Length);
            outputStream.Write(title, 0, title.Length);

            outputStream.WriteChar(0, 0x80 - title.Length);
            outputStream.WriteInteger(7, 1);
            outputStream.WriteInteger(0, 0x1C);

            Stream _in;
            //Get size of all isos
            totSize = 0;

            int ciso;


            //TODO: Callback
            //PostMessage(convertInfo.callback, WM_CONVERT_SIZE, 0, totSize);

            totSize = 0;

            var lastTicks = DateTime.Now.Ticks;

            for (ciso = 0; ciso < convertInfo.DiscInfos.Count; ciso++)
            {
                var disc = convertInfo.DiscInfos[ciso];
                uint curSize = 0;


                offset = (uint)outputStream.Position;

                if (offset % 0x8000 == 0)
                {
                    x = 0x8000 - (offset % 0x8000);
                    outputStream.WriteChar(0, (int)x);
                }

                iso_positions[ciso] = (uint)outputStream.Position - psarOffset;

                Notify?.Invoke(PopstationEventEnum.WriteStart, ciso + 1);

                WriteDisc(disc, iso_positions[ciso], psarOffset, true, outputStream, cancellationToken);

                Notify?.Invoke(PopstationEventEnum.WriteComplete, null);
            }

            x = (uint)outputStream.Position;

            if ((x % 0x10) != 0)
            {
                end_offset = x + (0x10 - (x % 0x10));

                for (i = 0; i < (end_offset - x); i++)
                {
                    outputStream.Write('0');
                }
            }
            else
            {
                end_offset = x;
            }

            end_offset -= psarOffset;

            offset = (uint)outputStream.Position;

            outputStream.Seek(p1_offset, SeekOrigin.Begin);
            outputStream.WriteInteger(end_offset, 1);

            end_offset += 0x2d31;
            outputStream.Seek(p2_offset, SeekOrigin.Begin);
            outputStream.WriteInteger(end_offset, 1);

            outputStream.Seek(m_offset, SeekOrigin.Begin);
            outputStream.Write(iso_positions, 1, sizeof(uint) * iso_positions.Length);

            outputStream.Seek(offset, SeekOrigin.Begin);
        }
    }
}
