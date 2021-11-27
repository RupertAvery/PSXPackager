using System.IO;
using System.Threading;
using PSXPackager.Common;

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

            Notify?.Invoke(PopstationEventEnum.DiscStart, 1);

            WriteDisc(outputStream, disc, psarOffset, false, cancellationToken);


            if (!cancellationToken.IsCancellationRequested)
            {
                Notify?.Invoke(PopstationEventEnum.DiscComplete, 1);
            }


        }
    }

}