using System.Diagnostics;

namespace PSXPackager.Common.Cue
{
    [DebuggerDisplay("{Minutes}:{Seconds}:{Frames}")]
    public partial class IndexPosition
    {
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Frames { get; set; }

        public override string ToString()
        {
            return $"{Minutes:00}:{Seconds:00}:{Frames:00}";
        }
    }
}
