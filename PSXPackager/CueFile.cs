using System.Collections.Generic;

namespace PSXPackager
{
    public class CueFile
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public List<CueTrack> Tracks { get; set; }
    }
}
