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

        public M3uFile()
        {
            FileEntries = new List<string>();
        }

        public M3uFile(string path)
        {
            Path = path;
            FileEntries = new List<string>();
        }

        public void AddFileEntry(string filePath)
        {
            FileEntries.Add(System.IO.Path.GetRelativePath(Path, filePath));
        }

        public string GetAbsolutePath(string fileEntry)
        {
            var path = System.IO.Path.GetDirectoryName(Path);
            if (System.IO.Path.IsPathFullyQualified(fileEntry))
            {
                return fileEntry;
            }
            else
            {
                return System.IO.Path.Combine(path, fileEntry);
            }
        }
    }
}