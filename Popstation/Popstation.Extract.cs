using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Popstation
{
    public partial class Popstation
    {
        public Task Extract(ExtractIsoInfo extractInfo, CancellationToken cancellationToken)
        {
            return Task.Run(() => ExtractIso(extractInfo, cancellationToken));
        }

        private void ExtractIso(ExtractIsoInfo extractInfo, CancellationToken cancellationToken)
        {

            using (var iso_stream = new FileStream(extractInfo.DestinationIso, FileMode.Create, FileAccess.Write))
            {
                using (var stream = new PbpStream(extractInfo.SourcePbp, FileMode.Open, FileAccess.Read))
                {
                    OnEvent?.Invoke(PopstationEventEnum.GetIsoSize, stream.IsoSize);

                    OnEvent?.Invoke(PopstationEventEnum.ExtractStart, null);

                    uint totSize = 0;
                    int i;

                    byte[] out_buffer = new byte[16 * PbpStream.ISO_BLOCK_SIZE];

                    for (i = 0; i < stream.IsoIndex.Count; i++)
                    {
                        uint bufferSize = stream.ReadBlock(i, out_buffer);

                        totSize += bufferSize;

                        if (totSize > stream.IsoSize)
                        {
                            bufferSize = bufferSize - (totSize - stream.IsoSize);
                            totSize = stream.IsoSize;
                        }

                        iso_stream.Write(out_buffer, 0, (int)bufferSize);

                        OnEvent?.Invoke(PopstationEventEnum.ExtractProgress, totSize);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }

                OnEvent?.Invoke(PopstationEventEnum.ExtractComplete, null);

            }
        }
    }
}
