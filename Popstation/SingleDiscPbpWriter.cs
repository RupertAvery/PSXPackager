using Popstation.Cue;
using Popstation.Iso;
using Popstation.Pbp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace Popstation
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

            Notify?.Invoke(PopstationEventEnum.ConvertComplete, null);

        }
    }

}