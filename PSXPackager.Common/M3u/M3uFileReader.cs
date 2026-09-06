using System.Collections.Generic;
using System.IO;

namespace Popstation.M3u
{
    public class M3uFileReader
    {
        public static M3uFile Read(string file)
        {
            var m3u = new M3uFile();

            m3u.Path = file;
            m3u.FileEntries = new List<string>();

            var lines = File.ReadAllLines(file);

            foreach (var line in lines)
            {
                if (line.Trim() != string.Empty)
                {
                    m3u.FileEntries.Add(line.Trim());
                }
            }

            return m3u;
        }
    }

    public class M3uFileWriter
    {
        public static void Write(M3uFile m3uFile, string path)
        {
            File.WriteAllLines(path, m3uFile.FileEntries);
        }
    }
}