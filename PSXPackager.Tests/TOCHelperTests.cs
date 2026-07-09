using System;
using System.Collections.Generic;
using PSXPackager.Common;
using PSXPackager.Common.Cue;

namespace UnitTestProject
{
    [TestClass]
    public class TOCHelperTests
    {
        [TestMethod]
        public void GetDataTypeAndGetTrackTypeRoundTrip()
        {
            Assert.AreEqual(CueTrackType.Data, TOCHelper.GetDataType(TrackTypeEnum.Data));
            Assert.AreEqual(CueTrackType.Audio, TOCHelper.GetDataType(TrackTypeEnum.Audio));

            Assert.AreEqual(TrackTypeEnum.Data, TOCHelper.GetTrackType(CueTrackType.Data));
            Assert.AreEqual(TrackTypeEnum.Audio, TOCHelper.GetTrackType(CueTrackType.Audio));
        }

        [TestMethod]
        public void GetTrackTypeThrowsForUnknownDataType()
        {
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => TOCHelper.GetTrackType("BOGUS"));
        }

        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(5, 0x05)]
        [DataRow(10, 0x10)]
        [DataRow(25, 0x25)]
        [DataRow(99, 0x99)]
        public void ToBinaryDecimalEncodesEachDigitAsNibble(int value, int expected)
        {
            Assert.AreEqual((byte)expected, TOCHelper.ToBinaryDecimal(value));
        }

        [DataTestMethod]
        [DataRow(0x00, 0)]
        [DataRow(0x05, 5)]
        [DataRow(0x10, 10)]
        [DataRow(0x25, 25)]
        [DataRow(0x99, 99)]
        public void FromBinaryDecimalIsInverseOfToBinaryDecimal(int packed, int expected)
        {
            Assert.AreEqual(expected, TOCHelper.FromBinaryDecimal((byte)packed));
        }

        [TestMethod]
        public void PositionFromFramesZero()
        {
            var position = TOCHelper.PositionFromFrames(0);

            Assert.AreEqual(0, position.Minutes);
            Assert.AreEqual(0, position.Seconds);
            Assert.AreEqual(0, position.Frames);
        }

        [TestMethod]
        public void PositionFromFramesWithinFirstSecond()
        {
            var position = TOCHelper.PositionFromFrames(30);

            Assert.AreEqual(0, position.Minutes);
            Assert.AreEqual(0, position.Seconds);
            Assert.AreEqual(30, position.Frames);
        }

        [TestMethod]
        public void PositionFromFramesCarriesIntoSeconds()
        {
            // 75 frames per second
            var position = TOCHelper.PositionFromFrames(76);

            Assert.AreEqual(0, position.Minutes);
            Assert.AreEqual(1, position.Seconds);
            Assert.AreEqual(1, position.Frames);
        }

        [TestMethod]
        public void PositionFromFramesCarriesIntoMinutes()
        {
            // 60 seconds * 75 frames + 20 extra frames
            var position = TOCHelper.PositionFromFrames(60 * 75 + 20);

            Assert.AreEqual(1, position.Minutes);
            Assert.AreEqual(0, position.Seconds);
            Assert.AreEqual(20, position.Frames);
        }

        [TestMethod]
        public void TOCtoCUEProducesSingleFileEntryWithGivenName()
        {
            var tocEntries = new List<TOCEntry>
            {
                new TOCEntry { TrackNo = 1, TrackType = TrackTypeEnum.Data, Minutes = 0, Seconds = 2, Frames = 0 },
            };

            var cueFile = TOCHelper.TOCtoCUE(tocEntries, "game.bin");

            Assert.AreEqual(1, cueFile.FileEntries.Count);
            Assert.AreEqual("game.bin", cueFile.FileEntries[0].FileName);
            Assert.AreEqual(FileTypes.BINARY, cueFile.FileEntries[0].FileType);
        }

        [TestMethod]
        public void TOCtoCUEDataTrackHasSingleIndexAtTrackPosition()
        {
            var tocEntries = new List<TOCEntry>
            {
                new TOCEntry { TrackNo = 1, TrackType = TrackTypeEnum.Data, Minutes = 0, Seconds = 2, Frames = 0 },
            };

            var cueFile = TOCHelper.TOCtoCUE(tocEntries, "game.bin");
            var track = cueFile.FileEntries[0].Tracks[0];

            Assert.AreEqual(1, track.Number);
            Assert.AreEqual(CueTrackType.Data, track.DataType);
            Assert.AreEqual(1, track.Indexes.Count);
            Assert.AreEqual(1, track.Indexes[0].Number);
            Assert.AreEqual(0, track.Indexes[0].Position.Minutes);
            Assert.AreEqual(2, track.Indexes[0].Position.Seconds);
            Assert.AreEqual(0, track.Indexes[0].Position.Frames);
        }

        [TestMethod]
        public void TOCtoCUEAudioTrackGetsTwoSecondPregapIndex()
        {
            var tocEntries = new List<TOCEntry>
            {
                new TOCEntry { TrackNo = 1, TrackType = TrackTypeEnum.Data, Minutes = 0, Seconds = 2, Frames = 0 },
                new TOCEntry { TrackNo = 2, TrackType = TrackTypeEnum.Audio, Minutes = 3, Seconds = 0, Frames = 0 },
            };

            var cueFile = TOCHelper.TOCtoCUE(tocEntries, "game.bin");
            var audioTrack = cueFile.FileEntries[0].Tracks[1];

            Assert.AreEqual(2, audioTrack.Indexes.Count);

            // INDEX 00 is the 2-second pregap before the track's actual start
            Assert.AreEqual(0, audioTrack.Indexes[0].Number);
            Assert.AreEqual(2, audioTrack.Indexes[0].Position.Minutes);
            Assert.AreEqual(58, audioTrack.Indexes[0].Position.Seconds);
            Assert.AreEqual(0, audioTrack.Indexes[0].Position.Frames);

            // INDEX 01 is the track's actual start position
            Assert.AreEqual(1, audioTrack.Indexes[1].Number);
            Assert.AreEqual(3, audioTrack.Indexes[1].Position.Minutes);
            Assert.AreEqual(0, audioTrack.Indexes[1].Position.Seconds);
            Assert.AreEqual(0, audioTrack.Indexes[1].Position.Frames);
        }

        [TestMethod]
        public void TOCtoCUELinksTracksInOrderViaNext()
        {
            var tocEntries = new List<TOCEntry>
            {
                new TOCEntry { TrackNo = 1, TrackType = TrackTypeEnum.Data, Minutes = 0, Seconds = 2, Frames = 0 },
                new TOCEntry { TrackNo = 2, TrackType = TrackTypeEnum.Audio, Minutes = 3, Seconds = 0, Frames = 0 },
            };

            var cueFile = TOCHelper.TOCtoCUE(tocEntries, "game.bin");
            var tracks = cueFile.FileEntries[0].Tracks;

            Assert.AreSame(tracks[1], tracks[0].Next);
            Assert.IsNull(tracks[1].Next);
        }
    }
}
