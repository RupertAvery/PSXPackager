using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Popstation.Pbp;
using PSXPackager.Common;

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

            var directory = convertInfo.OutputPath;
            var ext = ".pbp";

            var title = convertInfo.MainGameTitle;
            var code = convertInfo.MainGameID;
            var region = convertInfo.MainGameRegion;

            var originalFilename = convertInfo.OriginalFilename;
            if (Directory.Exists(convertInfo.OriginalPath))
            {
                originalFilename += ".dir";
            }

            var outputFilename = GetFilename(convertInfo.FileNameFormat,
                originalFilename,
                code,
                code,
                title,
                title,
                region
                );

            var outputPath = Path.Combine(directory, $"{outputFilename}{ext}");

            var finalDirectory = Directory.GetParent(outputPath);

            if (finalDirectory != null && !finalDirectory.Exists)
            {
                finalDirectory.Create();
            }

            using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                writer.Write(outputStream, cancellationToken);
            }

            return cancellationToken.IsCancellationRequested == false;
        }

    }
}
