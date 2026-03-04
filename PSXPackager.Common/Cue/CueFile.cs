using System.Collections.Generic;
using System.Linq;

namespace PSXPackager.Common.Cue
{
    public class CueFile
    {
        public string Path { get; set; }
        public List<CueFileEntry> FileEntries { get; set; }

        public CueFile()
        {
            FileEntries = new List<CueFileEntry>();
        }

        public CueFile(IEnumerable<CueFileEntry> cueFileEntry)
        {
            FileEntries = cueFileEntry.ToList();
        }

        public string GetAbsolutePath(CueFileEntry fileEntry)
        {
            var cuePath = System.IO.Path.GetDirectoryName(Path);
            if (System.IO.Path.IsPathFullyQualified(fileEntry.FileName))
            {
                return fileEntry.FileName;
            }
            else
            {
                return System.IO.Path.Combine(cuePath, fileEntry.FileName);
            }
        }
    }
}