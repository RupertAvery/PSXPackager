using System.Collections.Generic;

namespace PSXPackager.Common.Cue
{
    public class CueFileEntry
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public List<CueTrack> Tracks { get; set; }
    }
}
