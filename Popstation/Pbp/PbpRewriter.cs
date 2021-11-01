using System.IO;
using System.Threading;
using PSXPackager.Common;

namespace Popstation.Pbp
{
    public class PbpRewriter : PbpWriter
    {
        public PbpRewriter(ConvertOptions convertInfo) : base(convertInfo)
        {

        }

        public override void WritePSAR(Stream outputStream, uint psarOffset, CancellationToken cancellationToken)
        {
            var disc = convertInfo.DiscInfos[0];

            using (var stream = new FileStream(convertInfo.DiscInfos[0].SourceIso, FileMode.Open, FileAccess.Read))
            {
                var pbpStreamReader = new PbpReader(stream);
                var buffer = new byte[BUFFER_SIZE];
                int bytesRead;

                var length = pbpStreamReader.Seek(ResourceType.PSAR, stream);
                while((bytesRead = stream.Read(buffer, 0, BUFFER_SIZE)) > 0)
                {
                    outputStream.Write(buffer, 0, bytesRead);
                }
            }

        }
    }

}