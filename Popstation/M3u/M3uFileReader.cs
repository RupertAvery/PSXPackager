using System.Collections.Generic;
using System.IO;

namespace Popstation.M3u
{
    public class M3uFileReader
    {
        public static M3uFile Read(string file)
        {
            var m3u = new M3uFile();
            m3u.FileEntries = new List<string>();
            var cueLines = File.ReadAllLines(file);
            foreach (var line in cueLines)
            {
                if (line.Trim() != string.Empty)
                {
                    m3u.FileEntries.Add(line.Trim());
                }
            }
            return m3u;
        }
    }
}