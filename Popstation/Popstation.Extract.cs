using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Popstation.Database;
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

    public static class StringExtensions
    {
        public static string ReplaceIngoreCase(this string input, string find, string replace)
        {
            return Regex.Replace(input, find, replace, RegexOptions.IgnoreCase);
        }
    }

    public partial class Popstation
    {

        public static bool CheckFormat(string filenameFormat)
        {
            var output = filenameFormat.Contains("%FILENAME%");
            output |= filenameFormat.Contains("%GAMEID%");
            output |= filenameFormat.Contains("%MAINGAMEID%");
            output |= filenameFormat.Contains("%TITLE%");
            output |= filenameFormat.Contains("%MAINTITLE%");
            output |= filenameFormat.Contains("%REGION%");
            return output;
        }

        public static bool CheckResourceFormat(string filenameFormat)
        {
            var output = filenameFormat.Contains("%FILENAME%");
            output |= filenameFormat.Contains("%GAMEID%");
            output |= filenameFormat.Contains("%MAINGAMEID%");
            output |= filenameFormat.Contains("%TITLE%");
            output |= filenameFormat.Contains("%MAINTITLE%");
            output |= filenameFormat.Contains("%REGION%");
            output |= filenameFormat.Contains("%RESOURCE%");
            return output;
        }


        public static string GetFilename(string filenameFormat, string sourceFilename, string gameid, string maingameId, string title, string maintitle, string region)
        {
            var output = filenameFormat.ReplaceIngoreCase("%FILENAME%", Path.GetFileNameWithoutExtension(sourceFilename));
            output = output.ReplaceIngoreCase("%GAMEID%", gameid);
            output = output.ReplaceIngoreCase("%MAINGAMEID%", maingameId);
            output = output.ReplaceIngoreCase("%TITLE%", title);
            output = output.ReplaceIngoreCase("%MAINTITLE%", maintitle);
            output = output.ReplaceIngoreCase("%REGION%", region);
            return output;
        }

        public static string GetResourceFilename(string filenameFormat, string sourceFilename, string gameid, string maingameId, string title, string maintitle, string region, ResourceType resourceType, string ext)
        {
            var output = filenameFormat.ReplaceIngoreCase("%FILENAME%", Path.GetFileNameWithoutExtension(sourceFilename));
            output = output.ReplaceIngoreCase("%GAMEID%", gameid);
            output = output.ReplaceIngoreCase("%MAINGAMEID%", maingameId);
            output = output.ReplaceIngoreCase("%TITLE%", title);
            output = output.ReplaceIngoreCase("%MAINTITLE%", maintitle);
            output = output.ReplaceIngoreCase("%REGION%", region);
            output = output.ReplaceIngoreCase("%RESOURCE%", resourceType.ToString());
            output = output.ReplaceIngoreCase("%EXT%", ext);
            return output;
        }

        public static string GetResourceFolder(string filenameFormat, string sourceFilename, string gameid, string maingameId, string title, string maintitle, string region)
        {
            var output = filenameFormat.ReplaceIngoreCase("%FILENAME%", Path.GetFileNameWithoutExtension(sourceFilename));
            output = output.ReplaceIngoreCase("%GAMEID%", gameid);
            output = output.ReplaceIngoreCase("%MAINGAMEID%", maingameId);
            output = output.ReplaceIngoreCase("%TITLE%", title);
            output = output.ReplaceIngoreCase("%MAINTITLE%", maintitle);
            output = output.ReplaceIngoreCase("%REGION%", region);
            return output;
        }

        private void ExtractResource(Stream stream, string path)
        {
            if (stream.Length > 0)
            {
                using (var file = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    stream.CopyTo(file);
                    stream.Flush();
                }
            }
            stream.Dispose();
        }


        private void ExtractResources(Stream stream, Func<ResourceType, string, string> getResourcePath)
        {
            Stream resourceStream;

            var pbpStreamReader = new PbpReader(stream);

            if (pbpStreamReader.TryGetResourceStream(ResourceType.ICON0, stream, out resourceStream))
            {
                ExtractResource(resourceStream, getResourcePath(ResourceType.ICON0, ".png"));
            }

            if (pbpStreamReader.TryGetResourceStream(ResourceType.ICON1, stream, out resourceStream))
            {
                ExtractResource(resourceStream, getResourcePath(ResourceType.ICON1, ".pmf"));
            }

            if (pbpStreamReader.TryGetResourceStream(ResourceType.PIC0, stream, out resourceStream))
            {
                ExtractResource(resourceStream, getResourcePath(ResourceType.PIC0, ".png"));
            }

            if (pbpStreamReader.TryGetResourceStream(ResourceType.PIC1, stream, out resourceStream))
            {
                ExtractResource(resourceStream, getResourcePath(ResourceType.PIC1, ".png"));
            }

            if (pbpStreamReader.TryGetResourceStream(ResourceType.SND0, stream, out resourceStream))
            {
                ExtractResource(resourceStream, getResourcePath(ResourceType.SND0, ".at3"));
            }

        }

        private string GetResourcePath(ExtractOptions options, GameEntry entry, ResourceType type, string ext)
        {
            var path = GetResourceFilename(options.ResourceFormat, Path.GetFileNameWithoutExtension(options.SourcePbp), entry.SerialID, entry.MainGameID, entry.Title, entry.MainGameTitle, entry.Region, type, ext);

            if (string.IsNullOrEmpty(options.ResourceFoldersPath))
            {
                options.ResourceFoldersPath = Path.GetDirectoryName(options.SourcePbp);
            }

            return Path.Combine(options.ResourceFoldersPath, path);
        }

        private void EnsureResourcePathExists(ExtractOptions options, GameEntry entry)
        {
            var path = GetResourceFolder(options.ResourceFormat, Path.GetFileNameWithoutExtension(options.SourcePbp), entry.SerialID, entry.MainGameID, entry.Title, entry.MainGameTitle, entry.Region);

            if (string.IsNullOrEmpty(options.ResourceFoldersPath))
            {
                options.ResourceFoldersPath = Path.GetDirectoryName(options.SourcePbp);
            }

            path = Path.Combine(options.ResourceFoldersPath, path);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public void Extract(ExtractOptions options, CancellationToken cancellationToken)
        {
            using (var stream = new FileStream(options.SourcePbp, FileMode.Open, FileAccess.Read))
            {
                var pbpStreamReader = new PbpReader(stream);

                if (options.GenerateResourceFolders)
                {
                    var disc = pbpStreamReader.Discs[0];

                    var gameInfo = options.FindGame(disc.DiscID);

                    EnsureResourcePathExists(options, gameInfo);

                    return;
                }

                if (options.ExtractResources)
                {
                    var disc = pbpStreamReader.Discs[0];

                    var gameInfo = options.FindGame(disc.DiscID);

                    EnsureResourcePathExists(options, gameInfo);

                    if (string.IsNullOrEmpty(options.ResourceFoldersPath))
                    {
                        options.ResourceFoldersPath = Path.GetDirectoryName(options.SourcePbp);
                    }

                    ExtractResources(stream, (type, extension) => GetResourcePath(options, gameInfo, type, extension));

                    return;
                }

                var ext = ".bin";

                if (pbpStreamReader.Discs.Count > 1)
                {
                    foreach (var disc in pbpStreamReader.Discs.Where(d => options.Discs.Contains(d.Index)))
                    {
                        var gameInfo = options.FindGame(disc.DiscID);

                        if (gameInfo == null)
                        {
                            //var mainGameId = (string)pbpStreamReader.SFOData.Entries.FirstOrDefault(x => x.Key == SFOKeys.DISC_ID)?.Value;
                            options.FileNameFormat = "%FILENAME%";
                            gameInfo = new GameEntry();
                        }

                        var title = GetFilename(options.FileNameFormat,
                            options.SourcePbp,
                            disc.DiscID,
                            gameInfo.MainGameID,
                            gameInfo.Title,
                            gameInfo.MainGameTitle,
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

                    var gameInfo = options.FindGame(disc.DiscID);

                    if (gameInfo == null)
                    {
                        //var mainGameId = (string)pbpStreamReader.SFOData.Entries.FirstOrDefault(x => x.Key == SFOKeys.DISC_ID)?.Value;
                        options.FileNameFormat = "%FILENAME%";
                        gameInfo = new GameEntry();
                    }

                    var title = GetFilename(options.FileNameFormat,
                        options.SourcePbp,
                        disc.DiscID,
                        gameInfo.MainGameID,
                        gameInfo.Title,
                        gameInfo.MainGameTitle,
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
