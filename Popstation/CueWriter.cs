using System.Collections.Generic;
using System.IO;

namespace Popstation
{
    public static class CueWriter
    {
        public static void Write(string file, IEnumerable<CueFile> cueFiles)
        {
            using (var stream = new FileStream(file, FileMode.Create))
            {
                using (var writer = new StreamWriter(stream))
                {
                    foreach (var cueFile in cueFiles)
                    {
                        writer.WriteLine($"FILE \"{cueFile.FileName}\" {cueFile.FileType}");
                        foreach (var cueTrack in cueFile.Tracks)
                        {
                            writer.WriteLine($"  TRACK {cueTrack.Number:00} {cueTrack.DataType}");

                            foreach (var cueIndex in cueTrack.Indexes)
                            {
                                writer.WriteLine($"    INDEX {cueIndex.Number:00} {cueIndex.Position.ToString()}");

                            }
                        }
                    }
                    writer.Flush();
                }
            }
        }

    }
}
