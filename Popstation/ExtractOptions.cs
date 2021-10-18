using System;
using System.Collections.Generic;

namespace Popstation
{
    public interface ICheckIfFileExists
    {
        bool CheckIfFileExists { get; set; }
        bool SkipIfFileExists { get; set; }
    }

    public class GameInfo
    {
        public string GameID { get; set; }
        public string Title{ get; set; }
        public string GameName { get; set; }
        public string MainGameID { get; set; }
        public string Region { get; set; }
    }

    public class ExtractOptions : ICheckIfFileExists
    {
        public string SourcePbp { get; set; }
        public string DiscName { get; set; }
        public bool CreateCuesheet { get; set; }
        public bool CreatePlaylist { get; set; }
        public IEnumerable<int> Discs { get; set; }
        public bool CheckIfFileExists { get; set; }
        public bool SkipIfFileExists { get; set; }
        public string OutputPath { get; set; }
        public Func<string, GameInfo> GetGameInfo { get; set; }
        public string FileNameFormat { get; set; }
        public string ExtractResources { get; set; }
        public string GenerateResourceFolders { get; set; }
        public string ResourceFoldersPath { get; set; }
    }
}
