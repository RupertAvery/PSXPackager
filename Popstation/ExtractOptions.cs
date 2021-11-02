using System;
using System.Collections.Generic;
using Popstation.Database;

namespace Popstation
{
    public interface ICheckIfFileExists
    {
        bool CheckIfFileExists { get; set; }
        bool SkipIfFileExists { get; set; }
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
        public Func<string, GameEntry> FindGame { get; set; }
        public string FileNameFormat { get; set; }
        public bool ExtractResources { get; set; }
        public string CustomResourceFormat { get; set; }
        public bool GenerateResourceFolders { get; set; }
        public string ResourceFoldersPath { get; set; }
    }
}
