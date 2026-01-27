using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace PSXPackager.Common.Cue
{
    public static class FileTypes
    {
        public static string BINARY = "BINARY";
    }

    public static class DataTypes
    {
        public static string DATA = "MODE2/2352";
        public static string AUDIO = "AUDIO";
    }

    public class CueFileReader
    {
        private static readonly Regex FileRegex = new Regex("^FILE \"(.*?)\" (.*?)\\s*$");
        private static readonly Regex TrackRegex = new Regex("^\\s*TRACK (\\d+) (.*?)\\s*$");
        private static readonly Regex IndexRegex = new Regex("^\\s*INDEX (\\d+) (\\d+:\\d+:\\d+)\\s*$");

        public static CueFile Dummy(string file)
        {
            var cueFile = new CueFile();

            var fileEntry = new CueFileEntry
            {
                CueFile = cueFile,
                FileName = file,
                FileType = FileTypes.BINARY,

            };


            var indexes = new CueIndex
            {
                Number = 0,
                Position =
                new IndexPosition() {
                    Frames = 0,
                    Minutes = 0,
                    Seconds = 0
                }
            };

            var track = new CueTrack
            {
                FileEntry = fileEntry,
                Number = 1,
                DataType = DataTypes.DATA,
                Indexes =
                [
                    indexes
                ]
            };

            fileEntry.Tracks =
            [
                track
            ];

            cueFile.FileEntries =
            [
                fileEntry
            ];

            return cueFile;
        }

        public static CueFile Read(string file)
        {
            var cueFile = new CueFile() { Path = file };

            CueFileEntry cueFileEntry = null;
            CueTrack cueTrack = null;
            CueTrack lastTrack = null;

            var cueLines = File.ReadAllLines(file);

            foreach (var line in cueLines)
            {
                var fileMatch = FileRegex.Match(line);
                var trackMatch = TrackRegex.Match(line);
                var indexMatch = IndexRegex.Match(line);

                if (fileMatch.Success)
                {
                    cueFileEntry = new CueFileEntry
                    {
                        CueFile = cueFile,
                        FileName = fileMatch.Groups[1].Value,
                        FileType = fileMatch.Groups[2].Value,
                        Tracks = new List<CueTrack>()
                    };
                    cueFile.FileEntries.Add(cueFileEntry);

                    lastTrack = null;
                }
                else if (trackMatch.Success)
                {
                    //Define the numberth track, whose type is data - type.The value number is a strictly positive integer and must be one greater than the last one supplied to a previous TRACK command, 
                    //notwithstanding number can be greater than 1 in the first occurrence of TRACK command; however, it cannot exceed 99 in any case. Usually number is padded with a 0 on the left when smaller than 10, 
                    //in order to keep track numbers two digit wide uniformly throughout the CUE sheet.The data-type argument is one of those described at MODE(Compact Disc fields).

                    cueTrack = new CueTrack
                    {
                        FileEntry = cueFileEntry,
                        Number = int.Parse(trackMatch.Groups[1].Value),
                        DataType = trackMatch.Groups[2].Value,
                        Indexes = new List<CueIndex>()
                    };

                    cueFileEntry.Tracks.Add(cueTrack);

                    if (lastTrack != null)
                    {
                        lastTrack.Next = cueTrack;
                    }

                    lastTrack = cueTrack;
                }
                else if (indexMatch.Success)
                {
                    //If number is 0, then consider the time specified by the next INDEX command as the track pre-gap length present in the file declared by the current FILE command context.
                    //If number is 1, then define the starting time of the index of this track as mm minutes plus ss seconds plus ff frames.This index specify the starting time of track data and is the only stored in the table-of - contents of the disc.
                    //If number is 2 or more, then define the starting time of the(number-1)th sub-index within this track as mm minutes plus ss seconds plus ff frames.
                    //If this is the first INDEX command of the current TRACK command context, then it must start at 00:00:00 and number must be either 0 or 1.
                    //If this is not the first INDEX command of the current TRACK command context, then number must be one greater than that of the last TRACK command.
                    //The value number cannot be greater than 99 and is usually padded with a 0 on the left when smaller than 10, in order to keep index and sub-index numbers two digit wide uniformly throughout the CUE sheet.
                    //The time mm: ss: ff is an offset relative to the beginning of the file specified by the current FILE command context.
                    //The values mm, ss and ff must be non-negative integers.
                    //There are 75 frames per second.

                    var positionMatch = indexMatch.Groups[2].Value.Split(new char[] { ':' });

                    var cueIndex = new CueIndex
                    {
                        Number = int.Parse(indexMatch.Groups[1].Value),
                        Position = new IndexPosition()
                        {
                            Minutes = int.Parse(positionMatch[0]),
                            Seconds = int.Parse(positionMatch[1]),
                            Frames = int.Parse(positionMatch[2]),
                        }
                    };
                    cueTrack.Indexes.Add(cueIndex);
                }

            }

            return cueFile;
        }


    }
}