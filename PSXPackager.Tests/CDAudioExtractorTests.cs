using PSXPackager.Audio;
using PSXPackager.Common.Cue;

namespace UnitTestProject
{
    /// <summary>
    /// Saving an audio track out of a disc, as the Tracks list offers to do.
    /// </summary>
    [TestClass]
    public class CDAudioExtractorTests
    {
        private const int SectorSize = 2352;

        // A two track disc: 10 sectors of data, then 20 sectors of audio
        private const int DataSectors = 10;
        private const int AudioSectors = 20;

        private string _directory = null!;
        private string _cuePath = null!;

        [TestInitialize]
        public void CreateDisc()
        {
            _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_directory);

            var bin = new byte[(DataSectors + AudioSectors) * SectorSize];

            for (var i = 0; i < bin.Length; i++)
            {
                bin[i] = (byte)(i * 17 + 3);
            }

            File.WriteAllBytes(Path.Combine(_directory, "game.bin"), bin);

            _cuePath = Path.Combine(_directory, "game.cue");

            File.WriteAllText(_cuePath, string.Join(Environment.NewLine,
                "FILE \"game.bin\" BINARY",
                "  TRACK 01 MODE2/2352",
                "    INDEX 01 00:00:00",
                "  TRACK 02 AUDIO",
                "    INDEX 01 00:00:10"));
        }

        [TestCleanup]
        public void RemoveDisc()
        {
            if (Directory.Exists(_directory)) Directory.Delete(_directory, true);
        }

        [TestMethod]
        public void WritesTheTracksSectorsIntoAWav()
        {
            var path = Path.Combine(_directory, "track.wav");

            CDAudioExtractor.Extract(AudioTrack(), path, CDAudioFormat.Wav);

            var expected = new byte[AudioSectors * SectorSize];

            using (var bin = File.OpenRead(Path.Combine(_directory, "game.bin")))
            {
                bin.Position = DataSectors * SectorSize;
                bin.ReadExactly(expected);
            }

            var written = File.ReadAllBytes(path);

            // A WAV of CD audio is the sectors with a header in front of them
            CollectionAssert.AreEqual(expected, written[^expected.Length..]);
        }

        [TestMethod]
        public void WritesAnMp3()
        {
            var path = Path.Combine(_directory, "track.mp3");

            CDAudioExtractor.Extract(AudioTrack(), path, CDAudioFormat.Mp3);

            var written = File.ReadAllBytes(path);

            Assert.IsTrue(written.Length > 0, "the encoder should have produced something");

            // Every MP3 frame starts with the 11 sync bits, ID3 tag or no
            var start = written.Length >= 3 && written[0] == 'I' && written[1] == 'D' && written[2] == '3'
                ? Array.FindIndex(written, b => b == 0xFF)
                : 0;

            Assert.AreEqual(0xFF, written[start]);
            Assert.AreEqual(0xE0, written[start + 1] & 0xE0);
        }

        [TestMethod]
        public void ReportsHowFarItHasGot()
        {
            var reports = new List<CDAudioExtractProgress>();

            CDAudioExtractor.Extract(
                AudioTrack(),
                Path.Combine(_directory, "track.wav"),
                CDAudioFormat.Wav,
                progress: new SynchronousProgress(reports.Add));

            Assert.AreNotEqual(0, reports.Count);
            Assert.AreEqual(AudioSectors * SectorSize, reports[^1].BytesRead);
            Assert.AreEqual(AudioSectors * SectorSize, reports[^1].TotalBytes);
            Assert.AreEqual(100, reports[^1].Percent);
        }

        [TestMethod]
        public void RefusesToSaveADataTrack()
        {
            var dataTrack = CueFileReader.Read(_cuePath).FileEntries.Single().Tracks[0];

            Assert.ThrowsException<InvalidOperationException>(() =>
                CDAudioExtractor.Extract(dataTrack, Path.Combine(_directory, "track.wav"), CDAudioFormat.Wav));
        }

        [TestMethod]
        public void LeavesNothingOpenAfterwards()
        {
            CDAudioExtractor.Extract(AudioTrack(), Path.Combine(_directory, "track.wav"), CDAudioFormat.Wav);

            // Neither the disc it read nor the file it wrote may still be held
            File.Delete(Path.Combine(_directory, "game.bin"));
            File.Delete(Path.Combine(_directory, "track.wav"));
        }

        private CueTrack AudioTrack() => CueFileReader.Read(_cuePath).FileEntries.Single().Tracks[1];

        /// <summary>
        /// Collects reports on the calling thread, unlike <see cref="Progress{T}"/>.
        /// </summary>
        private class SynchronousProgress : IProgress<CDAudioExtractProgress>
        {
            private readonly Action<CDAudioExtractProgress> _report;

            public SynchronousProgress(Action<CDAudioExtractProgress> report) => _report = report;

            public void Report(CDAudioExtractProgress value) => _report(value);
        }
    }
}
