using System;
using System.Collections.Generic;
using System.Linq;
using Popstation.Cue;

namespace Popstation
{
    public static class CueFileExtensions
    {

        public static CueFile GetDummyCueFile()
        {
            return new CueFile()
            {
                FileEntries =
                {
                    new CueFileEntry()
                    {
                        FileType = "BINARY",
                        Tracks = new List<CueTrack>()
                        {
                            new CueTrack()
                            {
                                DataType = CueTrackType.Data,
                                Number = 1,
                                Indexes = new List<CueIndex>()
                                {
                                    new CueIndex()
                                    {
                                        Number = 1,
                                        Position = new IndexPosition()
                                        {
                                            Frames = 0, Minutes = 0, Seconds = 0
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }


        public static byte[] GetTOCData(this CueFile cue, uint isosize)
        {
            var tracks = cue.FileEntries.SelectMany(cf => cf.Tracks).ToList();

            byte[] tocData = new byte[0xA * (tracks.Count + 3)];

            var trackBuffer = new byte[0xA];

            var frames = isosize / 2352;
            var position = TOCHelper.PositionFromFrames(frames);

            var ctr = 0;

            trackBuffer[0] = (byte)TOCHelper.GetTrackType(tracks.First().DataType);
            trackBuffer[1] = 0x00;
            trackBuffer[2] = 0xA0;
            trackBuffer[3] = 0x00;
            trackBuffer[4] = 0x00;
            trackBuffer[5] = 0x00;
            trackBuffer[6] = 0x00;
            trackBuffer[7] = TOCHelper.ToBinaryDecimal(tracks.First().Number);
            trackBuffer[8] = TOCHelper.ToBinaryDecimal(0x20);
            trackBuffer[9] = 0x00;

            Array.Copy(trackBuffer, 0, tocData, ctr, 0xA);
            ctr += 0xA;

            trackBuffer[0] = (byte)TOCHelper.GetTrackType(tracks.Last().DataType);
            trackBuffer[2] = 0xA1;
            trackBuffer[7] = TOCHelper.ToBinaryDecimal(tracks.Last().Number);
            trackBuffer[8] = 0x00;

            Array.Copy(trackBuffer, 0, tocData, ctr, 0xA);
            ctr += 0xA;

            trackBuffer[0] = 0x01;
            trackBuffer[2] = 0xA2;
            trackBuffer[7] = TOCHelper.ToBinaryDecimal(position.Minutes);
            trackBuffer[8] = TOCHelper.ToBinaryDecimal(position.Seconds);
            trackBuffer[9] = TOCHelper.ToBinaryDecimal(position.Frames);

            Array.Copy(trackBuffer, 0, tocData, ctr, 0xA);
            ctr += 0xA;

            foreach (var track in tracks)
            {
                trackBuffer[0] = (byte)TOCHelper.GetTrackType(track.DataType);
                trackBuffer[1] = 0x00;
                trackBuffer[2] = TOCHelper.ToBinaryDecimal(track.Number);
                var pos = track.Indexes.First(idx => idx.Number == 1).Position;
                trackBuffer[3] = TOCHelper.ToBinaryDecimal(pos.Minutes);
                trackBuffer[4] = TOCHelper.ToBinaryDecimal(pos.Seconds);
                trackBuffer[5] = TOCHelper.ToBinaryDecimal(pos.Frames);
                trackBuffer[6] = 0x00;
                pos = pos + (2 * 75); // add 2 seconds for lead in (75 frames / second)
                trackBuffer[7] = TOCHelper.ToBinaryDecimal(pos.Minutes);
                trackBuffer[8] = TOCHelper.ToBinaryDecimal(pos.Seconds);
                trackBuffer[9] = TOCHelper.ToBinaryDecimal(pos.Frames);

                Array.Copy(trackBuffer, 0, tocData, ctr, 0xA);
                ctr += 0xA;
            }

            //0x00    1 byte Track type - 0x41 = data track, 0x01 = audio track
            //0x01    1 byte Always null
            //0x02    1 byte The track number in "binary decimal"
            //0x03    3 bytes The absolute track start address in "binary decimal" - first byte is minutes, second is seconds, third is frames
            //0x06    1 byte Always null
            //0x07    3 bytes The "relative" track address -same as before, and uses MM: SS: FF format

            return tocData;
        }
    }
}
