using System.Collections.Generic;

namespace PSXPackager
{
    public class CueTrack
    {
        public int Number { get; set; }
        public string DataType { get; set; }
        public List<CueIndex> Indexes { get; set; }
    }
}
