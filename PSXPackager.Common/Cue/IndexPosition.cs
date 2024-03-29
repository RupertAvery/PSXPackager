﻿using System.Diagnostics;

namespace PSXPackager.Common.Cue
{
    [DebuggerDisplay("{Minutes}:{Seconds}:{Frames}")]
    public partial class IndexPosition
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
    }
}
