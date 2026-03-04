using PSXPackager.Common.Cue;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;

namespace Popstation.M3u
{
    public class M3uFile
    {
        public string Path { get; set; }
        public List<string> FileEntries { get; set; }

        public string GetAbsolutePath(string fileEntry)
        {
            var cuePath = System.IO.Path.GetDirectoryName(Path);
            if (System.IO.Path.IsPathFullyQualified(fileEntry))
            {
                return fileEntry;
            }
            else
            {
                return System.IO.Path.Combine(cuePath, fileEntry);
            }
        }
    }
}