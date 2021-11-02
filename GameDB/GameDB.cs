using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DiscUtils.Iso9660;

namespace Popstation.Database
{
    public class GameDB
    {
        private readonly List<GameEntry> _gameEntries;

        public GameDB(string path)
        {
            _gameEntries = new List<GameEntry>();

            foreach (var item in File.ReadAllLines(path))
            {
                var parts = item.Split(new char[] { ';' });
                _gameEntries.Add(new GameEntry()
                {
                    GameID = parts[0],
                    SaveFolderName = parts[1],
                    SaveDescription = parts[2],
                    GameName = parts[3],
                    Format = parts[4],
                    ScannerID = parts[5],
                });
            }
        }

        public GameEntry GetEntryByScannerID(string scannerID)
        {
            return _gameEntries.FirstOrDefault(x => x.ScannerID == scannerID.ToUpper());
        }


        public static string FindGameId(string srcIso)
        {
            var regex = new Regex("(SCUS|SLUS|SLES|SCES|SCED|SLPS|SLPM|SCPS|SLED|SLPS|SIPS|ESPM|PBPX)[_-](\\d{3})\\.(\\d{2})", RegexOptions.IgnoreCase);
            var bootRegex = new Regex("BOOT\\s*=\\s*cdrom:\\\\?(?:.*?\\\\)?(.*?);1");

            using (var stream = new FileStream(srcIso, FileMode.Open))
            {
                var cdReader = new CDReader(stream, false, 2352);

                // Why doesn't a root file check not work?
                //foreach (var file in cdReader.GetFiles("\\"))
                //{
                //    var filename = file.Substring(1, file.LastIndexOf(";"));
                //    var match = regex.Match(filename);
                //    if (match.Success)
                //    {
                //        gameId = $"{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}";
                //        break;
                //    }
                //}
                var syscnfFound = false;

                foreach (var file in cdReader.GetFiles("\\"))
                {
                    var filename = file.Substring(1, file.LastIndexOf(";") - 1);
                    if (filename != "SYSTEM.CNF") continue;

                    syscnfFound = true;

                    using (var datastream = cdReader.OpenFile(file, FileMode.Open))
                    {
                        datastream.Seek(24, SeekOrigin.Begin);
                        var textReader = new StreamReader(datastream);
                        var bootLine = textReader.ReadLine();
                        var bootmatch = bootRegex.Match(bootLine);
                        if (!bootmatch.Success) continue;

                        var match = regex.Match(bootmatch.Groups[1].Value);
                        if (match.Success)
                        {
                            return $"{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}";
                        }
                    }
                }
            }

            return null;
        }

    }
}
