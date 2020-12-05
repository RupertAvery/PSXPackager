using System.IO;
using System.Threading;
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
            using (var stream = new FileStream(extractInfo.SourcePbp, FileMode.Open, FileAccess.Read))
            {
                var pbpStreamReader = new PbpStreamReader(stream);
                if (pbpStreamReader.Discs.Count > 1)
                {
                    var ext = Path.GetExtension(extractInfo.DestinationIso);
                    var fileName = Path.GetFileNameWithoutExtension(extractInfo.DestinationIso);
                    var path = Path.GetDirectoryName(extractInfo.DestinationIso);

                    foreach (var disc in pbpStreamReader.Discs.Where(d => extractInfo.Discs.Contains(d.Index)))
                    {
                        var discName = extractInfo.DiscName.Replace("{0}", disc.Index.ToString());
                        var isoPath = Path.Combine(path, $"{fileName} {discName}{ext}");

                        ExtractISO(disc, isoPath, extractInfo, cancellationToken);
            
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                    }
                }
                else
                {
                    ExtractISO(pbpStreamReader.Discs[0], extractInfo.DestinationIso, extractInfo, cancellationToken);
                }

                OnEvent?.Invoke(PopstationEventEnum.ExtractComplete, null);
            }
        }


        private void ExtractISO(PbpDiscEntry disc, string path, ExtractIsoInfo extractInfo, CancellationToken cancellationToken)
        {
            disc.ProgressEvent += ProgressEvent;

            CheckIfFileExists(extractInfo, path);

            OnEvent?.Invoke(PopstationEventEnum.Info, $"Writing {path}...");
            OnEvent?.Invoke(PopstationEventEnum.GetIsoSize, disc.IsoSize);
            OnEvent?.Invoke(PopstationEventEnum.ExtractStart, disc.Index);

            using (var isoStream = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                disc.CopyTo(isoStream, cancellationToken);
            }

            OnEvent?.Invoke(PopstationEventEnum.ExtractComplete, null);

            disc.ProgressEvent -= ProgressEvent;

            if (cancellationToken.IsCancellationRequested) return;
            if (!extractInfo.CreateCuesheet) return;

            var cueFilename = Path.GetFileNameWithoutExtension(path) + ".cue";
            var dirPath = Path.GetDirectoryName(path);

            var cueFile = TOCtoCUE(disc.TOC, Path.GetFileName(path));

            CueFileWriter.Write(cueFile, Path.Combine(dirPath, cueFilename));
        }

        private void CheckIfFileExists(ExtractIsoInfo extractInfo, string path)
        {
            if (extractInfo.CheckIfFileExists && File.Exists(path))
            {
                var response = ActionIfFileExists(path);
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
        }

        private void ProgressEvent(uint bytes)
        {
            OnEvent.Invoke(PopstationEventEnum.ConvertProgress, bytes);
        }

        private static CueFile TOCtoCUE(List<TOCEntry> tocEntries, string dataPath)
        {
            var cueFile = new CueFileEntry()
            {
                FileName = dataPath,
                Tracks = new List<CueTrack>(),
                FileType = "BINARY"
            };

            var audioLeadin = new IndexPosition { Seconds = 2 };

            foreach (var track in tocEntries)
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
                    DataType = TOCHelper.GetDataType(track.TrackType),
                    Indexes = indexes,
                    Number = track.TrackNo
                };


                cueFile.Tracks.Add(cueTrack);
            }

            return new CueFile(new[] { cueFile });
        }

    }

}
