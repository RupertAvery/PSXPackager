using PSXPackager.Common.Chd;
using UnitTestProject.Chd;

namespace UnitTestProject
{
    [TestClass]
    public class ChdDiscStreamTests
    {
        private const int SectorSize = 2352;
        private const int FrameSize = 2448;
        private const int FramesPerHunk = 4;
        private const int HunkBytes = FramesPerHunk * FrameSize;

        /// <summary>
        /// A track is padded out to a 4 frame boundary in the file, and the padding must not
        /// appear on the disc. Track 1 here is 10 frames long, so 2 frames of padding sit between
        /// it and track 2.
        /// </summary>
        [TestMethod]
        public void SkipsThePaddingBetweenTracks()
        {
            using var disc = BuildTwoTrackDisc(dataFrames: 10, audioFrames: 6, pregap: 2, pregapStored: true);

            Assert.AreEqual(16 * SectorSize, disc.Length);

            // Frame 10 on the disc is the first audio frame, which is frame 12 in the file
            AssertSectorMatchesFileFrame(disc, lba: 9, fileFrame: 9, audio: false);
            AssertSectorMatchesFileFrame(disc, lba: 10, fileFrame: 12, audio: true);
        }

        /// <summary>
        /// A CHD holds audio the opposite way round from a .bin file, so the samples have to be
        /// swapped back on the way out.
        /// </summary>
        [TestMethod]
        public void SwapsTheSampleBytesOfAnAudioTrack()
        {
            using var disc = BuildTwoTrackDisc(dataFrames: 4, audioFrames: 4, pregap: 0, pregapStored: false);

            var stored = FileFrame(4);
            var read = ReadSector(disc, 4);

            for (var i = 0; i < SectorSize; i += 2)
            {
                Assert.AreEqual(stored[i + 1], read[i], $"byte {i}");
                Assert.AreEqual(stored[i], read[i + 1], $"byte {i + 1}");
            }
        }

        [TestMethod]
        public void LeavesTheSampleBytesOfADataTrackAlone()
        {
            using var disc = BuildTwoTrackDisc(dataFrames: 4, audioFrames: 4, pregap: 0, pregapStored: false);

            CollectionAssert.AreEqual(FileFrame(0), ReadSector(disc, 0));
        }

        /// <summary>
        /// A pregap the file does not store still takes up disc addresses, and reads back blank.
        /// </summary>
        [TestMethod]
        public void GeneratesAPregapThatIsNotStored()
        {
            using var disc = BuildTwoTrackDisc(dataFrames: 8, audioFrames: 4, pregap: 150, pregapStored: false);

            // 8 data frames, then 150 generated frames, then 4 audio frames
            Assert.AreEqual((8 + 150 + 4) * SectorSize, disc.Length);

            CollectionAssert.AreEqual(new byte[SectorSize], ReadSector(disc, 8));
            CollectionAssert.AreEqual(new byte[SectorSize], ReadSector(disc, 157));

            // The audio track's own data starts once the gap is over
            Assert.AreNotEqual(0, ReadSector(disc, 158).Sum(b => (int)b));
        }

        /// <summary>
        /// A pregap that is in the data is part of the track's stored frames, and only shifts
        /// where INDEX 01 falls.
        /// </summary>
        [TestMethod]
        public void PlacesIndexOneAfterAStoredPregap()
        {
            using var disc = BuildTwoTrackDisc(dataFrames: 10, audioFrames: 6, pregap: 2, pregapStored: true);

            var audio = disc.Toc.Tracks[1];

            Assert.AreEqual(10, audio.StartFrame, "the stored data, pregap included, starts here");
            Assert.AreEqual(12, audio.IndexOneFrame, "the track proper starts two frames later");
            Assert.AreEqual(12, audio.FileFrame, "track 1 occupies 12 padded frames in the file");
        }

        [TestMethod]
        public void ReadsAcrossSectorAndHunkBoundaries()
        {
            using var disc = BuildTwoTrackDisc(dataFrames: 10, audioFrames: 6, pregap: 2, pregapStored: true);

            var whole = new byte[disc.Length];
            disc.Position = 0;

            var read = 0;
            while (read < whole.Length)
            {
                // An awkward size, so reads land mid-sector and straddle hunks
                var count = disc.Read(whole, read, Math.Min(1000, whole.Length - read));
                if (count <= 0) break;
                read += count;
            }

            Assert.AreEqual(whole.Length, read);

            for (var lba = 0; lba < 16; lba++)
            {
                CollectionAssert.AreEqual(
                    ReadSector(disc, lba),
                    whole.Skip(lba * SectorSize).Take(SectorSize).ToArray(),
                    $"sector {lba}");
            }
        }

        /// <summary>
        /// The generated cue sheet is what the PBP writer builds the disc's table of contents
        /// from, so the track types and index positions have to survive the round trip.
        /// </summary>
        [TestMethod]
        public void BuildsACueSheetFromTheTrackList()
        {
            using var disc = BuildTwoTrackDisc(dataFrames: 10, audioFrames: 6, pregap: 2, pregapStored: true);

            var cue = ChdCueSheet.FromToc(disc.Toc, "game.chd");
            var tracks = cue.FileEntries.Single().Tracks;

            Assert.AreEqual(2, tracks.Count);

            Assert.AreEqual("MODE2/2352", tracks[0].DataType);
            Assert.AreEqual(1, tracks[0].Indexes.Single().Number);
            Assert.AreEqual("00:00:00", tracks[0].Indexes[0].Position.ToString());

            Assert.AreEqual("AUDIO", tracks[1].DataType);

            // The stored pregap shows up as an INDEX 00 two frames ahead of INDEX 01
            Assert.AreEqual(0, tracks[1].Indexes[0].Number);
            Assert.AreEqual("00:00:10", tracks[1].Indexes[0].Position.ToString());
            Assert.AreEqual(1, tracks[1].Indexes[1].Number);
            Assert.AreEqual("00:00:12", tracks[1].Indexes[1].Position.ToString());
        }

        [TestMethod]
        public void RejectsAChdThatIsNotACdImage()
        {
            var builder = new ChdBuilder { HunkBytes = HunkBytes, UnitBytes = 512 };
            builder.AddHunk(ChdBuilder.TypeNone, new byte[HunkBytes]);

            var chd = ChdFile.Open(new MemoryStream(builder.Build()));

            Assert.ThrowsException<InvalidChdException>(() => new ChdDiscStream(chd));
        }

        [TestMethod]
        public void RejectsAGdRomImage()
        {
            var builder = new ChdBuilder { HunkBytes = HunkBytes, UnitBytes = FrameSize };
            builder.AddHunk(ChdBuilder.TypeNone, new byte[HunkBytes]);
            builder.AddMetadata(0x43484744, // "CHGD"
                "TRACK:1 TYPE:MODE1_RAW SUBTYPE:NONE FRAMES:100 PAD:0 PREGAP:0 PGTYPE:NONE PGSUB:NONE POSTGAP:0");

            var chd = ChdFile.Open(new MemoryStream(builder.Build()));

            Assert.ThrowsException<InvalidChdException>(() => new ChdDiscStream(chd));
        }

        [TestMethod]
        public void RejectsAChdWithNoTrackMetadata()
        {
            var builder = new ChdBuilder { HunkBytes = HunkBytes, UnitBytes = FrameSize };
            builder.AddHunk(ChdBuilder.TypeNone, new byte[HunkBytes]);

            var chd = ChdFile.Open(new MemoryStream(builder.Build()));

            Assert.ThrowsException<InvalidChdException>(() => new ChdDiscStream(chd));
        }

        /// <summary>
        /// Builds a disc of one data track followed by one audio track, each frame filled with a
        /// pattern derived from its position in the file so it can be identified after reading.
        /// </summary>
        private static ChdDiscStream BuildTwoTrackDisc(int dataFrames, int audioFrames, int pregap, bool pregapStored)
        {
            var builder = new ChdBuilder { HunkBytes = HunkBytes, UnitBytes = FrameSize };
            builder.Compressors[0] = 0x63647A6C; // "cdzl"

            var paddedData = Pad(dataFrames);
            var paddedAudio = Pad(audioFrames);
            var totalFrames = paddedData + paddedAudio;
            var hunks = (totalFrames + FramesPerHunk - 1) / FramesPerHunk;

            for (var hunk = 0; hunk < hunks; hunk++)
            {
                var data = new byte[HunkBytes];

                for (var frame = 0; frame < FramesPerHunk; frame++)
                {
                    FileFrame(hunk * FramesPerHunk + frame).CopyTo(data, frame * FrameSize);
                }

                builder.AddHunk(ChdBuilder.TypeCodec0, ChdBuilder.CompressCdZlib(data, HunkBytes));
            }

            builder.LogicalBytes = (long)hunks * HunkBytes;

            builder.AddTrack(1, "MODE2_RAW", dataFrames);
            builder.AddTrack(2, "AUDIO", audioFrames, pregap, pregapStored ? "V_AUDIO" : "AUDIO");

            return new ChdDiscStream(ChdFile.Open(new MemoryStream(builder.Build())));
        }

        private static int Pad(int frames)
        {
            return (frames + 3) / 4 * 4;
        }

        /// <summary>The sector data stored at a given frame of the file.</summary>
        private static byte[] FileFrame(int index)
        {
            var frame = new byte[SectorSize];

            for (var i = 0; i < SectorSize; i++)
            {
                frame[i] = (byte)(i * 31 + index * 7 + 1);
            }

            return frame;
        }

        private static byte[] ReadSector(ChdDiscStream disc, int lba)
        {
            var sector = new byte[SectorSize];

            disc.Position = (long)lba * SectorSize;

            var read = 0;
            while (read < SectorSize)
            {
                var count = disc.Read(sector, read, SectorSize - read);
                if (count <= 0) break;
                read += count;
            }

            Assert.AreEqual(SectorSize, read);

            return sector;
        }

        private static void AssertSectorMatchesFileFrame(ChdDiscStream disc, int lba, int fileFrame, bool audio)
        {
            var expected = FileFrame(fileFrame);

            if (audio)
            {
                for (var i = 0; i + 1 < expected.Length; i += 2)
                {
                    (expected[i], expected[i + 1]) = (expected[i + 1], expected[i]);
                }
            }

            CollectionAssert.AreEqual(expected, ReadSector(disc, lba), $"sector {lba}");
        }
    }
}
