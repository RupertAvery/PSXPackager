using System;

namespace Popstation
{
    public static class CueTrackType
    {
        public const string Data = "MODE2/2352";
        public const string Audio = "AUDIO";
    }


    public static class Helper
    {
        public static string GetDataType(TrackTypeEnum trackType)
        {
            switch (trackType)
            {
                case TrackTypeEnum.Data:
                    return CueTrackType.Data;
                case TrackTypeEnum.Audio:
                    return CueTrackType.Audio;
            }
            throw new ArgumentOutOfRangeException();
        }


        public static TrackTypeEnum GetTrackType(string dataType)
        {
            switch (dataType)
            {
                case CueTrackType.Data:
                    return TrackTypeEnum.Data;
                case CueTrackType.Audio:
                    return TrackTypeEnum.Audio;
            }
            throw new ArgumentOutOfRangeException();
        }


        public static byte ToBinaryDecimal(int value)
        {
            var ones = value % 10;
            var tens = value / 10;
            return (byte)(tens * 0x10 + ones);
        }

        public static int FromBinaryDecimal(byte value)
        {
            var ones = value % 16;
            var tens = value / 16;
            return (byte)(tens * 10 + ones);
        }

        public static IndexPosition PositionFromFrames(long frames)
        {
            int totalSeconds = (int)(frames / 75);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            frames = frames % 75;

            var position = new IndexPosition()
            {
                Minutes = minutes,
                Seconds = seconds,
                Frames = (int)frames,
            };

            return position;
        }
    }
}