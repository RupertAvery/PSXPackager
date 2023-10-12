using System.IO;

namespace PSXPackager.Common.Cue
{
    public class CueFileWriter
    {

        public static void Write(CueFile cueFile, string file)
        {
            using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write))
            {
                using (var writer = new StreamWriter(stream))
                {
                    foreach (var entry in cueFile.FileEntries)
                    {
                        writer.WriteLine($"FILE \"{entry.FileName}\" {entry.FileType}");
                        foreach (var cueTrack in entry.Tracks)
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