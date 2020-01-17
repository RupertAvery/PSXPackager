using System.Diagnostics;

namespace Popstation
{
    [DebuggerDisplay("{Minutes}:{Seconds}:{Frames}")]
    public class IndexPosition
    {
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Frames { get; set; }

        public static IndexPosition operator +(IndexPosition positionA, IndexPosition positionB)
        {
            return new IndexPosition()
            {
                Minutes = positionA.Minutes + positionB.Minutes,
                Seconds = positionA.Seconds + positionB.Seconds,
                Frames = positionA.Frames + positionB.Frames,
            };
        }

        public override string ToString()
        {
            return $"{Minutes:00}:{Seconds:00}:{Frames:00}";
        }
    }
}
