using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using DiscUtils.Iso9660;

namespace Popstation.Database
{
    public class GameDB
    {
        public List<GameEntry> GameEntries { get; }

        public GameDB(string path)
        {
            GameEntries = new List<GameEntry>();

            foreach (var item in File.ReadAllLines(path))
            {
                var parts = item.Split(new char[] { ';' });
                GameEntries.Add(new GameEntry()
                {
                    SerialID = parts[0],
                    MainGameID = parts[1],
                    MainGameTitle = parts[2],
                    Title = parts[3],
                    Region = parts[4],
                    GameID = parts[5],
                });
            }
        }

        public GameEntry GetEntryByGameID(string gameId)
        {
            return GameEntries.FirstOrDefault(x => x.GameID == gameId.ToUpper());
        }

        static Regex GameIdRegex = new Regex("(SCUS|SLUS|SLES|SCES|SCED|SLPS|SLPM|SCPS|SLED|SIPS|ESPM|PBPX)-?(\\d{5})", RegexOptions.IgnoreCase);

        public static string GetRegionLetter(string gameId)
        {
            var match = GameIdRegex.Match(gameId);

            if (match.Success)
            {
                var partyCode = match.Groups[1].Value.ToUpper();
                switch (partyCode)
                {
                    case "SCUS":
                    case "SLUS":
                        return "U";
                    case "SLES":
                    case "SCES":
                    case "SCED":
                    case "SLED":
                        return "P";
                    case "SCPS":
                    case "SLPS":
                    case "SLPM":
                        return "J";
                    default:
                        return "U";
                }
            }

            return "U";
        }

        static Regex systemCnfEntryRegex = new Regex("(SCUS|SLUS|SLES|SCES|SCED|SLPS|SLPM|SCPS|SLED|SIPS|ESPM|PBPX)[_-](\\d{3})\\.(\\d{2})", RegexOptions.IgnoreCase);
        static Regex bootRegex = new Regex("BOOT\\s*=\\s*cdrom:\\\\?(?:.*?\\\\)?(.*?);1");

        public static bool TryFindGameId(string srcIso, out string gameId)
        {
            gameId = FindGameId(srcIso);
            return gameId != null;
        }


        public static string FindGameId(string srcIso)
        {
            using (var stream = new FileStream(srcIso, FileMode.Open, FileAccess.Read))
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

                foreach (var file in cdReader.GetFiles("\\"))
                {
                    var filename = file.Substring(1, file.LastIndexOf(";") - 1);

                    if (filename != "SYSTEM.CNF") continue;

                    using (var datastream = cdReader.OpenFile(file, FileMode.Open))
                    {
                        datastream.Seek(24, SeekOrigin.Begin);
                        var textReader = new StreamReader(datastream);
                        var bootLine = textReader.ReadLine();
                        var bootmatch = bootRegex.Match(bootLine);
                        if (!bootmatch.Success) continue;

                        var match = systemCnfEntryRegex.Match(bootmatch.Groups[1].Value);
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
