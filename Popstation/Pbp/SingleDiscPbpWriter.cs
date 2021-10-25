using System.IO;
using System.Threading;

namespace Popstation.Pbp
{
    public class SingleDiscPbpWriter : PbpWriter
    {
        public SingleDiscPbpWriter(ConvertOptions convertInfo) : base(convertInfo)
        {
        }

        public override void WritePSAR(Stream outputStream, uint psarOffset, CancellationToken cancellationToken)
        {
            var disc = convertInfo.DiscInfos[0];

            var iso_position = (uint)outputStream.Position - psarOffset;

            Notify?.Invoke(PopstationEventEnum.WriteStart, 1);

            WriteDisc(disc, iso_position, psarOffset, false, outputStream, cancellationToken);

            Notify?.Invoke(PopstationEventEnum.WriteComplete, null);


        }
    }

}