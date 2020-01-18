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
            var frames = positionA.Frames + positionB.Frames;
            var framesCarry = 0;
            if(frames >= 75)  
            {
                framesCarry = frames / 75;
                frames = frames % 75;
            }
            var secondsCarry = 0;
            var seconds = positionA.Seconds + positionB.Seconds + framesCarry;
            if (seconds >= 60)
            {
                secondsCarry = seconds / 60;
                seconds = seconds % 60;
            }

            var minutes = positionA.Minutes + positionB.Minutes + secondsCarry;

            return new IndexPosition()
            {
                Minutes = minutes,
                Seconds = seconds,
                Frames = frames,
            };
        }

        public static IndexPosition operator +(IndexPosition positionA, int framesB)
        {
            var frames = positionA.Frames + framesB;
            var framesCarry = 0;
            if (frames >= 75)
            {
                framesCarry = frames / 75;
                frames = frames % 75;
            }
            var secondsCarry = 0;
            var seconds = positionA.Seconds + framesCarry;
            if (seconds >= 60)
            {
                secondsCarry = seconds / 60;
                seconds = seconds % 60;
            }

            var minutes = positionA.Minutes + secondsCarry;

            return new IndexPosition()
            {
                Minutes = minutes,
                Seconds = seconds,
                Frames = frames,
            };
        }

        public override string ToString()
        {
            return $"{Minutes:00}:{Seconds:00}:{Frames:00}";
        }
    }
}
