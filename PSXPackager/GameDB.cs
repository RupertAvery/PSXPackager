using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PSXPackager
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
            return _gameEntries.FirstOrDefault(x => x.ScannerID == scannerID);
        }

    }
}
