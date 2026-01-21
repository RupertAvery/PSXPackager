using System;
using System.Collections.Generic;
using PSXPackager.Common.Cue;

namespace PSXPackager.Common
{
    public static class CueTrackType
    {
        public const string Data = "MODE2/2352";
        public const string Audio = "AUDIO";
    }

    public static class TOCHelper
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

        public static CueFile TOCtoCUE(List<TOCEntry> tocEntries, string dataPath)
        {
            var cueFile = new CueFile();

            var cueFileEntry = new CueFileEntry()
            {
                CueFile = cueFile,
                FileName = dataPath,
                Tracks = new List<CueTrack>(),
                FileType = FileTypes.BINARY
            };

            cueFile.FileEntries.Add(cueFileEntry);

            var audioLeadIn = new IndexPosition { Seconds = 2 };

            foreach (var track in tocEntries)
            {
                var position = new IndexPosition
                {
                    Minutes = track.Minutes,
                    Seconds = track.Seconds,
                    Frames = track.Frames,
                };

                var indexes = new List<CueIndex>();

                if (track.TrackType == TrackTypeEnum.Audio)
                {
                    indexes.Add(new CueIndex()
                    {
                        Number = 0,
                        Position = position - audioLeadIn,
                    });
                }

                indexes.Add(new CueIndex()
                {
                    Number = 1,
                    Position = position,
                });

                var cueTrack = new CueTrack()
                {
                    FileEntry = cueFileEntry,
                    DataType = GetDataType(track.TrackType),
                    Indexes = indexes,
                    Number = track.TrackNo
                };


                cueFileEntry.Tracks.Add(cueTrack);
            }

            return cueFile;
        }
    }
}