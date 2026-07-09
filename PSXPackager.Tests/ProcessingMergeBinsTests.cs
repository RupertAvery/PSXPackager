using System.Collections.Generic;
using System.IO;
using System.Linq;
using Popstation;
using PSXPackager.Common;
using PSXPackager.Common.Cue;

namespace UnitTestProject
{
    [TestClass]
    public class ProcessingMergeBinsTests
    {
        private const int SectorSize = 2352;

        private string _tempDir = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private string CreateBinFile(string name, byte fill, int sectorCount)
        {
            var path = Path.Combine(_tempDir, name);
            var bytes = Enumerable.Repeat(fill, SectorSize * sectorCount).ToArray();
            File.WriteAllBytes(path, bytes);
            return path;
        }

        [TestMethod]
        public void MergeBinsConcatenatesSourceBinContentInOrder()
        {
            CreateBinFile("track1.bin", 0xAA, sectorCount: 2);
            CreateBinFile("track2.bin", 0xBB, sectorCount: 1);

            var unmergedCue = new CueFile { Path = Path.Combine(_tempDir, "game.cue") };
            unmergedCue.FileEntries.Add(new CueFileEntry
            {
                FileName = "track1.bin",
                FileType = FileTypes.BINARY,
                Tracks = new List<CueTrack>
                {
                    new CueTrack
                    {
                        Number = 1,
                        DataType = DataTypes.DATA,
                        Indexes = new List<CueIndex> { new CueIndex { Number = 1, Position = new IndexPosition(0, 0, 0) } },
                    },
                },
            });
            unmergedCue.FileEntries.Add(new CueFileEntry
            {
                FileName = "track2.bin",
                FileType = FileTypes.BINARY,
                Tracks = new List<CueTrack>
                {
                    new CueTrack
                    {
                        Number = 2,
                        DataType = DataTypes.AUDIO,
                        Indexes = new List<CueIndex> { new CueIndex { Number = 1, Position = new IndexPosition(0, 0, 0) } },
                    },
                },
            });

            using var output = new MemoryStream();

            Processing.MergeBins(output, "merged.bin", unmergedCue);

            var merged = output.ToArray();
            Assert.AreEqual(SectorSize * 3, merged.Length);
            Assert.IsTrue(merged.Take(SectorSize * 2).All(b => b == 0xAA));
            Assert.IsTrue(merged.Skip(SectorSize * 2).All(b => b == 0xBB));
        }

        [TestMethod]
        public void MergeBinsReturnsSingleFileEntryWithGivenName()
        {
            CreateBinFile("track1.bin", 0xAA, sectorCount: 1);

            var unmergedCue = new CueFile { Path = Path.Combine(_tempDir, "game.cue") };
            unmergedCue.FileEntries.Add(new CueFileEntry
            {
                FileName = "track1.bin",
                FileType = FileTypes.BINARY,
                Tracks = new List<CueTrack>
                {
                    new CueTrack
                    {
                        Number = 1,
                        DataType = DataTypes.DATA,
                        Indexes = new List<CueIndex> { new CueIndex { Number = 1, Position = new IndexPosition(0, 0, 0) } },
                    },
                },
            });

            using var output = new MemoryStream();

            var mergedCue = Processing.MergeBins(output, "merged.bin", unmergedCue);

            Assert.AreEqual(1, mergedCue.FileEntries.Count);
            Assert.AreEqual("merged.bin", mergedCue.FileEntries[0].FileName);
            Assert.AreEqual(FileTypes.BINARY, mergedCue.FileEntries[0].FileType);
        }

        [TestMethod]
        public void MergeBinsOffsetsIndexesOfLaterTracksBySectorsConsumedByEarlierFiles()
        {
            // track1.bin is 2 sectors long, so track2's indexes must be shifted forward by 2 frames (sectors)
            CreateBinFile("track1.bin", 0xAA, sectorCount: 2);
            CreateBinFile("track2.bin", 0xBB, sectorCount: 1);

            var unmergedCue = new CueFile { Path = Path.Combine(_tempDir, "game.cue") };
            unmergedCue.FileEntries.Add(new CueFileEntry
            {
                FileName = "track1.bin",
                FileType = FileTypes.BINARY,
                Tracks = new List<CueTrack>
                {
                    new CueTrack
                    {
                        Number = 1,
                        DataType = DataTypes.DATA,
                        Indexes = new List<CueIndex> { new CueIndex { Number = 1, Position = new IndexPosition(0, 0, 0) } },
                    },
                },
            });
            unmergedCue.FileEntries.Add(new CueFileEntry
            {
                FileName = "track2.bin",
                FileType = FileTypes.BINARY,
                Tracks = new List<CueTrack>
                {
                    new CueTrack
                    {
                        Number = 2,
                        DataType = DataTypes.AUDIO,
                        Indexes = new List<CueIndex> { new CueIndex { Number = 1, Position = new IndexPosition(0, 0, 0) } },
                    },
                },
            });

            using var output = new MemoryStream();

            var mergedCue = Processing.MergeBins(output, "merged.bin", unmergedCue);
            var mergedTracks = mergedCue.FileEntries[0].Tracks;

            Assert.AreEqual(2, mergedTracks.Count);

            var firstTrackIndex = mergedTracks[0].Indexes[0].Position;
            Assert.AreEqual(0, firstTrackIndex.Minutes);
            Assert.AreEqual(0, firstTrackIndex.Seconds);
            Assert.AreEqual(0, firstTrackIndex.Frames);

            var secondTrackIndex = mergedTracks[1].Indexes[0].Position;
            Assert.AreEqual(0, secondTrackIndex.Minutes);
            Assert.AreEqual(0, secondTrackIndex.Seconds);
            Assert.AreEqual(2, secondTrackIndex.Frames);
        }

        [TestMethod]
        public void MergeBinsResolvesRelativeBinPathsAgainstTheCueDirectory()
        {
            // FileName in the cue is relative; MergeBins must resolve it against
            // the directory of CueFile.Path, not the current working directory.
            CreateBinFile("relative.bin", 0xCC, sectorCount: 1);

            var unmergedCue = new CueFile { Path = Path.Combine(_tempDir, "game.cue") };
            unmergedCue.FileEntries.Add(new CueFileEntry
            {
                FileName = "relative.bin",
                FileType = FileTypes.BINARY,
                Tracks = new List<CueTrack>
                {
                    new CueTrack
                    {
                        Number = 1,
                        DataType = DataTypes.DATA,
                        Indexes = new List<CueIndex> { new CueIndex { Number = 1, Position = new IndexPosition(0, 0, 0) } },
                    },
                },
            });

            using var output = new MemoryStream();

            Processing.MergeBins(output, "merged.bin", unmergedCue);

            Assert.AreEqual(SectorSize, output.Length);
        }
    }
}
