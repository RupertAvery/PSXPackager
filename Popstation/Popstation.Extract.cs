using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Popstation.Cue;
using Popstation.Pbp;

namespace Popstation
{
    public partial class Popstation
    {
        public void Extract(ExtractIsoInfo extractInfo, CancellationToken cancellationToken)
        { 
            ExtractIso(extractInfo, cancellationToken);
        }

        private CueFileEntry ExtractTOC(string isoPath, PbpDiscEntry disc)
        {
            var cueFile = new CueFileEntry()
            {
                FileName = Path.GetFileName(isoPath),
                Tracks = new List<CueTrack>(),
                FileType = "BINARY"
            };

            var audioLeadin = new IndexPosition { Seconds = 2 };

            foreach (var track in disc.TOC)
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

        private void ReadIso(PbpDiscEntry disc, Stream destination, string isoPath, bool createCuesheet, CancellationToken cancellationToken)
        {
            uint totSize = 0;
            int i;

            byte[] out_buffer = new byte[16 * PbpStream.ISO_BLOCK_SIZE];

            OnEvent?.Invoke(PopstationEventEnum.Info, $"Writing {isoPath}...");

            OnEvent?.Invoke(PopstationEventEnum.GetIsoSize, disc.IsoSize);

            OnEvent?.Invoke(PopstationEventEnum.ExtractStart, disc.Index);

            for (i = 0; i < disc.IsoIndex.Count; i++)
            {
                uint bufferSize = disc.ReadBlock(i, out_buffer);

                totSize += bufferSize;

                if (totSize > disc.IsoSize)
                {
                    bufferSize = bufferSize - (totSize - disc.IsoSize);
                    totSize = disc.IsoSize;
                }

                destination.Write(out_buffer, 0, (int)bufferSize);

                OnEvent?.Invoke(PopstationEventEnum.ExtractProgress, totSize);

                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                if (createCuesheet)
                {
                    var filename = Path.GetFileNameWithoutExtension(isoPath) + ".cue";

                    var path = Path.GetDirectoryName(isoPath);

                    var cueFile = new CueFile(new[] { ExtractTOC(isoPath, disc) });

                    CueFileWriter.Write(cueFile, Path.Combine(path, filename));
                }
            }
            OnEvent?.Invoke(PopstationEventEnum.ExtractComplete, null);
        }

        private void ExtractIso(ExtractIsoInfo extractInfo, CancellationToken cancellationToken)
        {
            using (var stream = new PbpStream(extractInfo.SourcePbp, FileMode.Open, FileAccess.Read))
            {
                if (stream.Discs.Count > 1)
                {
                    var ext = Path.GetExtension(extractInfo.DestinationIso);
                    var fileName = Path.GetFileNameWithoutExtension(extractInfo.DestinationIso);
                    var path = Path.GetDirectoryName(extractInfo.DestinationIso);

                    foreach (var disc in stream.Discs.Where(d => extractInfo.Discs.Contains(d.Index)))
                    {
                        var discName = extractInfo.DiscName.Replace("{0}", disc.Index.ToString());
                        var isoPath = Path.Combine(path, $"{fileName} {discName}{ext}");

                        if (extractInfo.CheckIfFileExists && File.Exists(isoPath))
                        {
                            var response = ActionIfFileExists(isoPath);
                            if (response == ActionIfFileExistsEnum.OverwriteAll)
                            {
                                extractInfo.CheckIfFileExists = false;
                            }
                            else if (response == ActionIfFileExistsEnum.Skip)
                            {
                                continue;
                            }
                            else if (response == ActionIfFileExistsEnum.Abort)
                            {
                                throw new CancellationException("Operation was aborted");
                            }
                        }

                        using (var iso_stream = new FileStream(isoPath, FileMode.Create, FileAccess.Write))
                        {
                            ReadIso(disc, iso_stream, isoPath, extractInfo.CreateCuesheet, cancellationToken);
                        }

                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    if (extractInfo.CheckIfFileExists && File.Exists(extractInfo.DestinationIso))
                    {
                        var response = ActionIfFileExists(extractInfo.DestinationIso);
                        if (response == ActionIfFileExistsEnum.OverwriteAll)
                        {
                            extractInfo.CheckIfFileExists = false;
                        }
                        else if (response == ActionIfFileExistsEnum.Skip)
                        {
                            return;
                        }
                        else if (response == ActionIfFileExistsEnum.Abort)
                        {
                            throw new CancellationException("Operation was aborted");
                        }
                    }

                    using (var iso_stream = new FileStream(extractInfo.DestinationIso, FileMode.Create, FileAccess.Write))
                    {
                        ReadIso(stream.Discs[0], iso_stream, extractInfo.DestinationIso, extractInfo.CreateCuesheet, cancellationToken);
                    }
                }

                OnEvent?.Invoke(PopstationEventEnum.ExtractComplete, null);
            }
        }
    }
}
