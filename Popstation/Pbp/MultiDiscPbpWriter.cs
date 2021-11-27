using PSXPackager.Common;
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
            uint totSize;

            var title = convertInfo.MainGameTitle;
            var code = convertInfo.MainGameID;

            uint[] dummy = new uint[6];

            uint i, offset, x;
            uint p1_offset, p2_offset, m_offset, end_offset;
            uint[] iso_positions = new uint[5];

            Notify?.Invoke(PopstationEventEnum.WritePsTitle, null);

            outputStream.Write("PSTITLEIMG000000", 0, 16);

            // Save this offset position
            p1_offset = (uint)outputStream.Position;

            outputStream.WriteInt32(0, 2);
            outputStream.WriteInt32(0x2CC9C5BC, 1);
            outputStream.WriteInt32(0x33B5A90F, 1);
            outputStream.WriteInt32(0x06F6B4B3, 1);
            outputStream.WriteUInt32(0xB25945BA, 1);
            outputStream.WriteInt32(0, 0x76);

            m_offset = (uint)outputStream.Position;

            //memset(iso_positions, 0, sizeof(iso_positions));
            outputStream.Write(iso_positions, 1, sizeof(uint) * 5);

            outputStream.WriteRandom(12);
            outputStream.WriteInt32(0, 8);

            outputStream.Write('_');
            outputStream.Write(code, 0, 4);
            outputStream.Write('_');
            outputStream.Write(code, 4, 5);

            outputStream.WriteChar(0, 0x15);

            p2_offset = (uint)outputStream.Position;
            outputStream.WriteInt32(0, 2);

            outputStream.Write(Popstation.data3, 0, Popstation.data3.Length);
            outputStream.Write(title, 0, title.Length);

            outputStream.WriteChar(0, 0x80 - title.Length);
            outputStream.WriteInt32(7, 1);
            outputStream.WriteInt32(0, 0x1C);

            for (var discNo = 0; discNo < convertInfo.DiscInfos.Count; discNo++)
            {
                var disc = convertInfo.DiscInfos[discNo];
                uint curSize = 0;

                offset = (uint)outputStream.Position;

                if (offset % 0x8000 == 0)
                {
                    x = 0x8000 - (offset % 0x8000);
                    outputStream.WriteChar(0, (int)x);
                }

                iso_positions[discNo] = (uint)(outputStream.Position - psarOffset);

                Notify?.Invoke(PopstationEventEnum.DiscStart, discNo + 1);

                WriteDisc(outputStream, disc, psarOffset, true,  cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    Notify?.Invoke(PopstationEventEnum.DiscComplete, discNo + 1);
                }
                else
                {
                    return;
                }
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
            outputStream.WriteUInt32(end_offset, 1);

            end_offset += 0x2d31;
            outputStream.Seek(p2_offset, SeekOrigin.Begin);
            outputStream.WriteUInt32(end_offset, 1);

            outputStream.Seek(m_offset, SeekOrigin.Begin);
            outputStream.Write(iso_positions, 1, sizeof(uint) * iso_positions.Length);

            outputStream.Seek(offset, SeekOrigin.Begin);
        }
    }
}
