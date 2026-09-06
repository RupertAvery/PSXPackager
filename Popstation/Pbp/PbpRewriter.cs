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

        /// <summary>
        /// A rewrite copies the PSAR out of an existing PBP rather than converting a disc image,
        /// so the source is taken at face value and never treated as an image to expand.
        /// </summary>
        protected override long GetSourceSize(DiscInfo disc) => new FileInfo(disc.SourceIso).Length;

        public override void WritePSAR(Stream outputStream, uint psarOffset, CancellationToken cancellationToken)
        {
            var disc = convertInfo.DiscInfos[0];

            using (var stream = new FileStream(convertInfo.DiscInfos[0].SourceIso, FileMode.Open, FileAccess.Read))
            {
                var pbpStreamReader = new PbpReader(stream);
                var buffer = new byte[BUFFER_SIZE];
                int bytesRead;

                var length = pbpStreamReader.Seek(ResourceType.PSAR, stream);
                while ((bytesRead = stream.Read(buffer, 0, BUFFER_SIZE)) > 0)
                {
                    outputStream.Write(buffer, 0, bytesRead);
                }
            }

        }
    }

}