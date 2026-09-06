using Popstation.Media;
using PSXPackager.Common.Chd;
using PSXPackager.Common.Cue;
using UnitTestProject.Chd;

namespace UnitTestProject
{
    /// <summary>
    /// A cue sheet's tracks can live in a .bin, a .chd, or a disc inside an EBOOT, and the player
    /// should not have to know which.
    /// </summary>
    [TestClass]
    public class DiscSourceTests
    {
        private const int SectorSize = 2352;
        private const int FrameSize = 2448;
        private const int FramesPerHunk = 4;
        private const int HunkBytes = FramesPerHunk * FrameSize;

        private string _directory = null!;

        [TestInitialize]
        public void CreateWorkingDirectory()
        {
            _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);
        }

        [TestCleanup]
        public void RemoveWorkingDirectory()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        /// <summary>
        /// The case that started all this: a track list read out of a CHD has to lead back to the
        /// CHD's own sectors.
        /// </summary>
        [TestMethod]
        public void OpensATrackThatLivesInAChd()
        {
            var path = Path.Combine(_directory, "game.chd");
            File.WriteAllBytes(path, BuildChd());

            var track = ChdCueSheet.FromChd(path).FileEntries.Single().Tracks[1];

            using var source = DiscSource.ForTrack(track);

            // 10 data frames then 6 audio frames, padding skipped
            Assert.AreEqual(16L * SectorSize, source.Length);

            var sector = ReadSectorAt(source, 10);

            Assert.AreNotEqual(0, sector.Sum(b => (int)b), "the audio track should hold data");
        }

        /// <summary>
        /// A CHD track reads back exactly what the disc stream produces, byte swapping included.
        /// </summary>
        [TestMethod]
        public void ReadsTheSameBytesAsTheDiscStream()
        {
            var path = Path.Combine(_directory, "game.chd");
            File.WriteAllBytes(path, BuildChd());

            byte[] expected;

            using (var disc = ChdDiscStream.Open(path))
            {
                expected = new byte[SectorSize];
                disc.Position = 10L * SectorSize;
                disc.ReadExactly(expected, 0, SectorSize);
            }

            var track = ChdCueSheet.FromChd(path).FileEntries.Single().Tracks[1];

            using var source = DiscSource.ForTrack(track);

            CollectionAssert.AreEqual(expected, ReadSectorAt(source, 10));
        }

        [TestMethod]
        public void ResolvesARelativePathAgainstTheCueSheet()
        {
            var binPath = Path.Combine(_directory, "game.bin");
            File.WriteAllBytes(binPath, new byte[4 * SectorSize]);

            var cue = CueFileReader.Dummy("game.bin");
            cue.Path = Path.Combine(_directory, "game.cue");

            using var source = DiscSource.ForTrack(cue.FileEntries.Single().Tracks.Single());

            Assert.AreEqual(4L * SectorSize, source.Length);
        }

        /// <summary>
        /// Nothing may stay open once the source is closed, or the file cannot be replaced.
        /// </summary>
        [TestMethod]
        public void ReleasesTheFileWhenClosed()
        {
            var path = Path.Combine(_directory, "game.chd");
            File.WriteAllBytes(path, BuildChd());

            var track = ChdCueSheet.FromChd(path).FileEntries.Single().Tracks[0];

            var source = DiscSource.ForTrack(track);
            source.Dispose();

            File.Delete(path);

            Assert.IsFalse(File.Exists(path));
        }

        [TestMethod]
        public void RecognisesAPbpUri()
        {
            Assert.IsTrue(DiscSource.TryParsePbpUri(
                @"pbp://C:\games\Game (USA).pbp/disc2", out var path, out var index));

            Assert.AreEqual(@"C:\games\Game (USA).pbp", path);
            Assert.AreEqual(2, index);
        }

        [TestMethod]
        public void DoesNotMistakeAPathForAPbpUri()
        {
            Assert.IsFalse(DiscSource.TryParsePbpUri(@"C:\games\Game (USA).chd", out _, out _));
            Assert.IsFalse(DiscSource.TryParsePbpUri(@"C:\games\Game (USA).pbp", out _, out _));
            Assert.IsFalse(DiscSource.TryParsePbpUri(null!, out _, out _));
        }

        private static byte[] ReadSectorAt(DiscSource source, int lba)
        {
            var sector = new byte[SectorSize];

            source.Stream.Seek((long)lba * SectorSize, SeekOrigin.Begin);
            source.Stream.ReadExactly(sector, 0, SectorSize);

            return sector;
        }

        /// <summary>
        /// A two track disc: 10 data frames, then 6 audio frames with a stored two frame pregap.
        /// </summary>
        private static byte[] BuildChd()
        {
            const int dataFrames = 10;
            const int audioFrames = 6;

            var builder = new ChdBuilder { HunkBytes = HunkBytes, UnitBytes = FrameSize };
            builder.Compressors[0] = 0x63647A6C; // "cdzl"

            var totalFrames = 12 + 8;
            var hunks = (totalFrames + FramesPerHunk - 1) / FramesPerHunk;

            for (var hunk = 0; hunk < hunks; hunk++)
            {
                var data = new byte[HunkBytes];

                for (var i = 0; i < HunkBytes; i++)
                {
                    data[i] = (byte)(i * 13 + hunk * 7 + 1);
                }

                builder.AddHunk(ChdBuilder.TypeCodec0, ChdBuilder.CompressCdZlib(data, HunkBytes));
            }

            builder.LogicalBytes = (long)hunks * HunkBytes;
            builder.AddTrack(1, "MODE2_RAW", dataFrames);
            builder.AddTrack(2, "AUDIO", audioFrames, 2, "V_AUDIO");

            return builder.Build();
        }
    }
}
