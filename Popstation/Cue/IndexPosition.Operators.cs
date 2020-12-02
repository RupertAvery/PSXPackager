namespace Popstation.Cue
{
    public partial class IndexPosition
    {
        public static IndexPosition operator +(IndexPosition positionA, IndexPosition positionB)
        {
            var frames = positionA.Frames + positionB.Frames;
            var framesCarry = 0;

            if (frames >= 75)
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

        public static IndexPosition operator -(IndexPosition positionA, IndexPosition positionB)
        {
            var frames = positionA.Frames - positionB.Frames;

            var secondsBorrow = 0;

            if (frames < 0)
            {
                secondsBorrow = 1;
                frames = 75 + frames;
            }

            var minutesBorrow = 0;

            var seconds = positionA.Seconds - positionB.Seconds - secondsBorrow;

            if (seconds < 0)
            {
                minutesBorrow = 1;
                seconds = 60 + seconds;
            }

            var minutes = positionA.Minutes - positionB.Minutes - minutesBorrow;

            return new IndexPosition()
            {
                Minutes = minutes,
                Seconds = seconds,
                Frames = frames,
            };
        }

        public static IndexPosition operator -(IndexPosition positionA, int framesB)
        {
            var temp = framesB;
            var mm = temp / (60 * 75);
            temp = temp - mm * (60 * 75);
            var ss = temp / 75;
            temp = temp - ss * 75;
            var ff = temp;

            var frames = positionA.Frames - ff;

            var secondsBorrow = 0;

            if (frames < 0)
            {
                secondsBorrow = 1;
                frames = 75 + frames;
            }

            var minutesBorrow = 0;

            var seconds = positionA.Seconds - ss - secondsBorrow;

            if (seconds < 0)
            {
                minutesBorrow = 1;
                seconds = 60 + seconds;
            }

            var minutes = positionA.Minutes - mm - minutesBorrow;

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
    }
}
