using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Popstation
{
    public partial class Popstation
    {
        public Task Extract(ExtractIsoInfo extractInfo, CancellationToken cancellationToken)
        {
            return Task.Run(() => ExtractIso(extractInfo, cancellationToken));
        }

        private CueFile ExtractTOC(ExtractIsoInfo extractInfo, PbpStream stream)
        {
            var cueFile = new CueFile()
            {
                FileName = Path.GetFileName(extractInfo.DestinationIso),
                Tracks = new List<CueTrack>(),
                FileType = "BINARY"
            };

            var audioLeadin = new IndexPosition { Seconds = 2 };

            foreach (var track in stream.TOC)
            {
                var position = new IndexPosition
                {
                    Minutes = track.Minutes,
                    Seconds = track.Seconds,
                    Frames = track.Frames,
                };

                var indexes = new List<CueIndex>();

                if (track.TrackType == TrackTypeEnum.Audio)
                {
                    indexes.Add(new CueIndex()
                    {
                        Number = 0,
                        Position = position - audioLeadin,
                    });
                }

                indexes.Add(new CueIndex()
                {
                    Number = 1,
                    Position = position,
                });

                var cueTrack = new CueTrack()
                {
                    DataType = Helper.GetDataType(track.TrackType),
                    Indexes = indexes,
                    Number = track.TrackNo
                };


                cueFile.Tracks.Add(cueTrack);
            }

            return cueFile;
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

                    if (extractInfo.CreateCuesheet)
                    {
                        var filename = Path.GetFileNameWithoutExtension(extractInfo.DestinationIso) + ".cue";

                        var path = Path.GetDirectoryName(extractInfo.DestinationIso);

                        CueWriter.Write(Path.Combine(path, filename), new CueFile[] { ExtractTOC(extractInfo, stream) });
                    }

                }

                OnEvent?.Invoke(PopstationEventEnum.ExtractComplete, null);

            }
        }
    }
}
