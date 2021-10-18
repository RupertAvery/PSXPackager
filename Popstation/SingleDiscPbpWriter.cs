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

            WriteDisc(disc, psarOffset, outputStream, 0, cancellationToken);

            Notify?.Invoke(PopstationEventEnum.ConvertComplete, null);

        }
    }

}