using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSXPackager.Common.Cue
{
    public class CueBuilder
    {
        public static bool CheckPaths(List<string> binPaths)
        {
            var folderGroups = binPaths.Select(d => Path.GetDirectoryName(d))
                .GroupBy(d => d);

            //var baseFolder = folderGroups.First().Key;

            return folderGroups.Count() == 1;
        }

        public static CueFile GenerateCue(List<string> binPaths)
        {
            var cueFile = new CueFile();

            var index = 1;

            foreach (var binPath in binPaths)
            {
                var fileName = Path.GetFileName(binPath);

                cueFile.FileEntries.Add(new CueFileEntry()
                {
                    FileName = fileName,
                    FileType = "BINARY",
                    Tracks = index == 1
                        ?
                        [
                            // Data track 
                            new CueTrack()
                            {
                                DataType = CueTrackType.Data,
                                Number = index,
                                Indexes = new List<CueIndex>()
                                {
                                    // No pre-gap for first track
                                    new CueIndex() { Number = 1, Position = new IndexPosition(0, 0, 0) }
                                }
                            }
                        ]
                        :
                        [
                            // Audio track
                            new CueTrack()
                            {
                                DataType = CueTrackType.Audio,
                                Number = index,
                                Indexes = new List<CueIndex>()
                                {
                                    // Pre-gap index
                                    new CueIndex() { Number = 0, Position = new IndexPosition(0, 0, 0) },
                                    new CueIndex() { Number = 1, Position = new IndexPosition(0, 2, 0) }
                                }
                            }
                        ]
                });

                index++;
            }

            //cueFile.Path = Path.Combine(baseFolder, "DUMMY.CUE");

            return cueFile;
        }
    }
}
