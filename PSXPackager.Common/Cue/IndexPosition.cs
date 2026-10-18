using System;
using System.Diagnostics;

namespace PSXPackager.Common.Cue
{
    [DebuggerDisplay("{Minutes}:{Seconds}:{Frames}")]
    public partial struct IndexPosition
    {
        public IndexPosition()
        {

        }

        public IndexPosition(int minutes, int seconds, int frames)
        {
            Minutes = minutes;
            Seconds = seconds;
            Frames = frames;
        }

        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Frames { get; set; }

        public override string ToString()
        {
            return $"{Minutes:00}:{Seconds:00}:{Frames:00}";
        }

        public override bool Equals(object obj)
        {
            return obj is IndexPosition position &&
                   Minutes == position.Minutes &&
                   Seconds == position.Seconds &&
                   Frames == position.Frames;
        }

        public int Sectors => (Minutes * 60 + Seconds) * 75 + Frames;

        public override int GetHashCode()
        {
            return HashCode.Combine(Minutes, Seconds, Frames);
        }
    }
}
