using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using Popstation.Pbp;
using PSXPackager.Common;
using PSXPackager.Common.Cue;

namespace Popstation
{

    //Offset Purpose
    //0x00	The PBP signature, always is 00 50 42 50 or the string "<null char>PBP"
    //0x04	Unknown purpose, possibly the version number.Currently is always 0x00000100 or 0x01000100 (some MINIS, PSP remaster and PSP PlayView)
    //0x08	Offset of the file PARAM.SFO(this value should always be 0x28)
    //0x0C	Offset of the file ICON0.PNG
    //0x10	Offset of the file ICON1.PMF or ICON1.PNG
    //0x14	Offset of the file PIC0.PNG or UNKNOWN.PNG (Value can be repeated)
    //0x18	Offset of the file PIC1.PNG or PICT1.PNG
    //0x1C	Offset of the file SND0.AT3
    //0x20	Offset of the file DATA.PSP
    //0x24	Offset of the file DATA.PSAR

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

        private void ExtractResource(Stream stream, string path, string filename)
        {
            if (stream.Length > 0)
            {
                using (var file = new FileStream(Path.Combine(path, filename), FileMode.OpenOrCreate, FileAccess.Write))
                {
                    stream.CopyTo(file);
                    stream.Flush();
                }
            }
            stream.Dispose();
        }

        private string GetResouceFolderPath(ExtractOptions processOptions, string mode, GameInfo entry, string srcIso)
        {
            string path;

            if (!string.IsNullOrEmpty(mode))
            {
                string filename = Path.GetFileNameWithoutExtension(srcIso);
                path = Path.GetDirectoryName(srcIso);

                if (!string.IsNullOrEmpty(processOptions.ResourceFoldersPath))
                {
                    path = processOptions.ResourceFoldersPath;
                }

                switch (mode.ToLower())
                {
                    case "gameid":
                        path = Path.Combine(path, entry.GameID);
                        break;
                    case "title":
                        path = Path.Combine(path, entry.GameName);
                        break;
                    case "filename":
                        path = Path.Combine(path, filename);
                        break;
                    default:
                        path = Path.Combine(path, filename);
                        break;
                }

            }
            else
            {
                path = Path.GetDirectoryName(srcIso);
            }

            return path;
        }

        private void ExtractResources(Stream stream, string path)
        {
            Stream resourceStream;

            var pbpStreamReader = new PbpReader(stream);

            if (pbpStreamReader.TryGetResourceStream(ResourceType.ICON0, stream, out resourceStream))
            {
                ExtractResource(resourceStream, path, "ICON0.png");
            }

            if (pbpStreamReader.TryGetResourceStream(ResourceType.ICON1, stream, out resourceStream))
            {
                ExtractResource(resourceStream, path, "ICON1.pmf");
            }

            if (pbpStreamReader.TryGetResourceStream(ResourceType.PIC0, stream, out resourceStream))
            {
                ExtractResource(resourceStream, path, "PIC0.png");
            }

            if (pbpStreamReader.TryGetResourceStream(ResourceType.PIC1, stream, out resourceStream))
            {
                ExtractResource(resourceStream, path, "PIC1.png");
            }

            if (pbpStreamReader.TryGetResourceStream(ResourceType.SND0, stream, out resourceStream))
            {
                ExtractResource(resourceStream, path, "SND0.at3");
            }

        }

        public void Extract(ExtractOptions options, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(options.SourcePbp, FileMode.Open, FileAccess.Read))
            {
                var pbpStreamReader = new PbpReader(stream);

                if (!string.IsNullOrEmpty(options.GenerateResourceFolders))
                {
                    var disc = pbpStreamReader.Discs[0];

                    var gameInfo = options.GetGameInfo(disc.DiscID);

                    var path = GetResouceFolderPath(options, options.GenerateResourceFolders, gameInfo, options.SourcePbp);

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    if (options.GenerateResourceFolders.ToLower() == "gameid")
                    {
                        using (File.Create(Path.Combine(path, gameInfo.GameName)))
                        {
                        }
                    }

                    return;
                }

                if (!string.IsNullOrEmpty(options.ExtractResources))
                {
                    var disc = pbpStreamReader.Discs[0];

                    var gameInfo = options.GetGameInfo(disc.DiscID);

                    var path = GetResouceFolderPath(options, options.ExtractResources, gameInfo, options.SourcePbp);

                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    ExtractResources(stream, path);

                    if (options.ExtractResources.ToLower() == "gameid")
                    {
                        using (File.Create(Path.Combine(path, gameInfo.GameName)))
                        {
                        }
                    }

                    return;
                }

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


                var cueFile = TOCHelper.TOCtoCUE(disc.TOC, Path.GetFileName(path));

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
            var fileExists = File.Exists(path);
            if (options.SkipIfFileExists && fileExists) return false;
            if (!options.CheckIfFileExists || !fileExists) return true;
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

       

    }

}
