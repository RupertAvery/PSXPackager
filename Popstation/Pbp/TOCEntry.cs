using System.Collections.Generic;
using Popstation.Cue;

namespace Popstation.Pbp
{
    public class TOCEntry
    {
        public TrackTypeEnum TrackType { get; set; }
        public int TrackNo { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Frames { get; set; }
    }


}