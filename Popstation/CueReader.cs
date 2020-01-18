using Popstation;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Popstation
{

    public static class CueReader
    {
        static Regex fileRegex = new Regex("^FILE \"(.*?)\" (.*?)\\s*$");
        static Regex trackRegex = new Regex("^\\s*TRACK (\\d+) (.*?)\\s*$");
        static Regex indexRegex = new Regex("^\\s*INDEX (\\d+) (\\d+:\\d+:\\d+)\\s*$");

        public static List<CueFile> Read(string file)
        {
            var cueFiles = new List<CueFile>();
            CueFile cueFile = null;
            CueTrack cueTrack = null;
            var cueLines = File.ReadAllLines(file);
            foreach (var line in cueLines)
            {
                var fileMatch = fileRegex.Match(line);
                var trackMatch = trackRegex.Match(line);
                var indexMatch = indexRegex.Match(line);

                if (fileMatch.Success)
                {
                    cueFile = new CueFile
                    {
                        FileName = fileMatch.Groups[1].Value,
                        FileType = fileMatch.Groups[2].Value,
                        Tracks = new List<CueTrack>()
                    };
                    cueFiles.Add(cueFile);
                }
                else if (trackMatch.Success)
                {
                    //Define the numberth track, whose type is data - type.The value number is a strictly positive integer and must be one greater than the last one supplied to a previous TRACK command, 
                    //notwithstanding number can be greater than 1 in the first occurrence of TRACK command; however, it cannot exceed 99 in any case. Usually number is padded with a 0 on the left when smaller than 10, 
                    //in order to keep track numbers two digit wide uniformly throughout the CUE sheet.The data-type argument is one of those described at MODE(Compact Disc fields).

                    cueTrack = new CueTrack
                    {
                        Number = int.Parse(trackMatch.Groups[1].Value),
                        DataType = trackMatch.Groups[2].Value,
                        Indexes = new List<CueIndex>()
                    };
                    cueFile.Tracks.Add(cueTrack);
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
            return cueFiles;
        }

    }
}
