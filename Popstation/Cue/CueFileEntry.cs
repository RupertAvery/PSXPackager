using System.Collections.Generic;

namespace Popstation.Cue
{
    public class CueFileEntry
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public List<CueTrack> Tracks { get; set; }
    }
}
