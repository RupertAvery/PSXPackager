using System.Collections.Generic;

namespace PSXPackager.Common
{
    public class ProcessOptions
    {
        public IReadOnlyList<string> Files { get; set; }
        public string OutputPath { get; set; }
        public string TempPath { get; set; }
        public IEnumerable<int> Discs { get; set; }
        public bool CheckIfFileExists { get; set; }
        public bool SkipIfFileExists { get; set; }
        public int CompressionLevel { get; set; }
        public string FileNameFormat{ get; set; }
        public bool Log { get; set; }
        public int Verbosity { get; set; }
        public string ExtractResources { get; set; }
        public string ImportResources { get; set; }
        public string GenerateResourceFolders { get; set; }
        public string ResourceFoldersPath { get; set; }
    }
}