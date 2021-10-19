using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Popstation.Cue;
using Popstation.Iso;
using Popstation.Pbp;

namespace Popstation
{


    public partial class Popstation
    {
        public Action<PopstationEventEnum, object> Notify { get; set; }
        public Func<string, ActionIfFileExistsEnum> ActionIfFileExists { get; set; }
        public List<string> TempFiles { get; set; }

        public bool Convert(ConvertOptions convertInfo, CancellationToken cancellationToken)
        {
            PbpWriter writer;

            if (convertInfo.DiscInfos.Count == 1)
            {
                writer = new SingleDiscPbpWriter(convertInfo);
            }
            else
            {
                writer = new MultiDiscPbpWriter(convertInfo);
            }

            writer.Notify = Notify;
            writer.ActionIfFileExists = ActionIfFileExists;
            writer.TempFiles = TempFiles;

            var directory = Path.GetDirectoryName(convertInfo.DestinationPbp);
            var ext = Path.GetExtension(convertInfo.DestinationPbp);

            var title = convertInfo.MainGameTitle;
            var code = convertInfo.MainGameID;
            var region = convertInfo.MainGameRegion;

            var outputFilename = GetFilename(convertInfo.FileNameFormat,
                convertInfo.DestinationPbp,
                code,
                code,
                title,
                title,
                region
                );

            var outputPath = Path.Combine(directory, $"{outputFilename}{ext}");


            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                writer.Write(outputStream, cancellationToken);
            }

            return cancellationToken.IsCancellationRequested;
        }

    }
}