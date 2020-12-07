using System;
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
        private string GetFilename(string filenameFormat, string sourceFilename, string gameid, string maingameId, string title, string maintitle, string region)
        {
            var output = filenameFormat.ToUpper().Replace("%FILENAME%", Path.GetFileNameWithoutExtension(sourceFilename));
            output = output.Replace("%GAMEID%", gameid);
            output = output.Replace("%MAINGAMEID%", maingameId);
            output = output.Replace("%TITLE%", title);
            output = output.Replace("%MAINTITLE%", maintitle);
            output = output.Replace("%REGION%", region);
            return output;
        }

        public void Extract(ExtractOptions options, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(options.SourcePbp, FileMode.Open, FileAccess.Read))
            {
                var pbpStreamReader = new PbpStreamReader(stream);


                var ext = ".bin";

                if (pbpStreamReader.Discs.Count > 1)
                {
                    foreach (var disc in pbpStreamReader.Discs.Where(d => options.Discs.Contains(d.Index)))
                    {
                        var gameInfo = options.GetGameInfo(disc.DiscID);

                        if (gameInfo == null)
                        {
                            //var mainGameId = (string)pbpStreamReader.SFOData.Entries.FirstOrDefault(x => x.Key == SFOKeys.DISC_ID)?.Value;
                            options.FileNameFormat = "%FILENAME%";
                            gameInfo = new GameInfo();
                        }

                        var title = GetFilename(options.FileNameFormat,
                            options.SourcePbp,
                            disc.DiscID,
                            gameInfo.MainGameID,
                            gameInfo.GameName,
                            gameInfo.Title,
                            gameInfo.Region
                        );

                        Notify?.Invoke(PopstationEventEnum.Info, $"Using Title '{title}'");

                        var discName = options.DiscName.Replace("{0}", disc.Index.ToString());

                        var isoPath = Path.Combine(options.OutputPath, $"{title} {discName}{ext}");

                        ExtractISO(disc, isoPath, options, cancellationToken);
            
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }

                    }
                }
                else
                {
                    var disc = pbpStreamReader.Discs[0];

                    var gameInfo = options.GetGameInfo(disc.DiscID);

                    if (gameInfo == null)
                    {
                        //var mainGameId = (string)pbpStreamReader.SFOData.Entries.FirstOrDefault(x => x.Key == SFOKeys.DISC_ID)?.Value;
                        options.FileNameFormat = "%FILENAME%";
                        gameInfo = new GameInfo();
                    }

                    var title = GetFilename(options.FileNameFormat,
                        options.SourcePbp,
                        disc.DiscID,
                        gameInfo.MainGameID,
                        gameInfo.GameName,
                        gameInfo.Title,
                        gameInfo.Region
                    );

                    var isoPath = Path.Combine(options.OutputPath, $"{title}{ext}");

                    ExtractISO(disc, isoPath, options, cancellationToken);
                }

                Notify?.Invoke(PopstationEventEnum.ExtractComplete, null);
            }
        }


        private void ExtractISO(PbpDiscEntry disc, string path, ExtractOptions extractInfo, CancellationToken cancellationToken)
        {
            try
            {
                disc.ProgressEvent += ProgressEvent;

                if (!ContinueIfFileExists(extractInfo, path))
                {
                    return;
                }
                
                Notify?.Invoke(PopstationEventEnum.Info, $"Writing {path}...");
                Notify?.Invoke(PopstationEventEnum.GetIsoSize, disc.IsoSize);
                Notify?.Invoke(PopstationEventEnum.ExtractStart, disc.Index);

                var cueFilename = Path.GetFileNameWithoutExtension(path) + ".cue";
                var dirPath = Path.GetDirectoryName(path);
                var cuePath = Path.Combine(dirPath, cueFilename);

                TempFiles.Add(path);
                TempFiles.Add(cuePath);

                using (var isoStream = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    disc.CopyTo(isoStream, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested) return;
                
                TempFiles.Remove(path);

                if (!extractInfo.CreateCuesheet) return;


                var cueFile = TOCtoCUE(disc.TOC, Path.GetFileName(path));

                CueFileWriter.Write(cueFile, cuePath);

                TempFiles.Remove(cuePath);

                Notify?.Invoke(PopstationEventEnum.ExtractComplete, null);
            }
            finally
            {
                disc.ProgressEvent -= ProgressEvent;
            }
        }

        private bool ContinueIfFileExists(ICheckIfFileExists options, string path)
        {
            if (!options.CheckIfFileExists || !File.Exists(path)) return true;
            var response = ActionIfFileExists(path);

            switch (response)
            {
                case ActionIfFileExistsEnum.OverwriteAll:
                    options.CheckIfFileExists = false;
                    break;
                case ActionIfFileExistsEnum.Skip:
                    return false;
                case ActionIfFileExistsEnum.Abort:
                    throw new CancellationException("Operation was aborted");
            }

            return true;
        }

        private void ProgressEvent(uint bytes)
        {
            Notify.Invoke(PopstationEventEnum.ConvertProgress, bytes);
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
