using Popstation.Pbp;

using System.IO;
using System.Threading;

namespace Popstation
{

    public partial class Popstation
    {
        public bool Repack(ConvertOptions options, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(options.DiscInfos[0].SourceIso, FileMode.Open, FileAccess.Read))
            {
                var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                try
                {
                    //ExtractResources(stream, (type, extension) => GetResourcePath(options, gameInfo, type, extension));

                    var writer = new PbpRewriter(options);

                    var directory = options.OutputPath;
                    var ext = ".pbp";

                    var title = options.MainGameTitle;
                    var code = options.MainGameID;
                    var region = options.MainGameRegion;

                    var outputFilename = GetFilename(options.FileNameFormat,
                        options.OriginalFilename,
                        code,
                        code,
                        title,
                        title,
                        region
                        );

                    var outputPath = Path.Combine(directory, $"{outputFilename}{ext}");

                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    using (var outputStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        writer.Write(outputStream, cancellationToken);
                    }

                }
                finally
                {
                    Directory.Delete(path, true);
                }

                return cancellationToken.IsCancellationRequested == false;

            }
        }

        private void TryDelete(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
