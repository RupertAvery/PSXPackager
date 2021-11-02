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
        public bool ExtractResources { get; set; }
        public bool ImportResources { get; set; }
        public bool GenerateResourceFolders { get; set; }
        public string CustomResourceFormat { get; set; }
        public string ResourceFoldersPath { get; set; }
    }
}