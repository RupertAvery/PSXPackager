using System;
using System.Collections.Generic;
using System.IO;
using PSXPackager.Common.Cue;

namespace UnitTestProject
{
    [TestClass]
    public class CueFileReadWriteTests
    {
        private string _tempCuePath = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempCuePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".cue");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempCuePath))
            {
                File.Delete(_tempCuePath);
            }
        }

        private static CueFile BuildSampleCueFile()
        {
            var cueFile = new CueFile();

            var fileEntry = new CueFileEntry
            {
                CueFile = cueFile,
                FileName = "game.bin",
                FileType = FileTypes.BINARY,
                Tracks = new List<CueTrack>(),
            };

            var dataTrack = new CueTrack
            {
                FileEntry = fileEntry,
                Number = 1,
                DataType = DataTypes.DATA,
                Indexes = new List<CueIndex>
                {
                    new CueIndex { Number = 1, Position = new IndexPosition(0, 0, 0) },
                },
            };

            var audioTrack = new CueTrack
            {
                FileEntry = fileEntry,
                Number = 2,
                DataType = DataTypes.AUDIO,
                Indexes = new List<CueIndex>
                {
                    new CueIndex { Number = 0, Position = new IndexPosition(2, 58, 0) },
                    new CueIndex { Number = 1, Position = new IndexPosition(3, 0, 0) },
                },
            };

            dataTrack.Next = audioTrack;

            fileEntry.Tracks.Add(dataTrack);
            fileEntry.Tracks.Add(audioTrack);

            cueFile.FileEntries.Add(fileEntry);

            return cueFile;
        }

        [TestMethod]
        public void WriteThenReadRoundTripsFileTracksAndIndexes()
        {
            var original = BuildSampleCueFile();

            CueFileWriter.Write(original, _tempCuePath);
            var roundTripped = CueFileReader.Read(_tempCuePath);

            Assert.AreEqual(1, roundTripped.FileEntries.Count);

            var entry = roundTripped.FileEntries[0];
            Assert.AreEqual("game.bin", entry.FileName);
            Assert.AreEqual(FileTypes.BINARY, entry.FileType);
            Assert.AreEqual(2, entry.Tracks.Count);

            var dataTrack = entry.Tracks[0];
            Assert.AreEqual(1, dataTrack.Number);
            Assert.AreEqual(DataTypes.DATA, dataTrack.DataType);
            Assert.AreEqual(1, dataTrack.Indexes.Count);
            Assert.AreEqual(0, dataTrack.Indexes[0].Position.Minutes);
            Assert.AreEqual(0, dataTrack.Indexes[0].Position.Seconds);
            Assert.AreEqual(0, dataTrack.Indexes[0].Position.Frames);

            var audioTrack = entry.Tracks[1];
            Assert.AreEqual(2, audioTrack.Number);
            Assert.AreEqual(DataTypes.AUDIO, audioTrack.DataType);
            Assert.AreEqual(2, audioTrack.Indexes.Count);

            Assert.AreEqual(0, audioTrack.Indexes[0].Number);
            Assert.AreEqual(2, audioTrack.Indexes[0].Position.Minutes);
            Assert.AreEqual(58, audioTrack.Indexes[0].Position.Seconds);
            Assert.AreEqual(0, audioTrack.Indexes[0].Position.Frames);

            Assert.AreEqual(1, audioTrack.Indexes[1].Number);
            Assert.AreEqual(3, audioTrack.Indexes[1].Position.Minutes);
            Assert.AreEqual(0, audioTrack.Indexes[1].Position.Seconds);
            Assert.AreEqual(0, audioTrack.Indexes[1].Position.Frames);
        }

        [TestMethod]
        public void ReadLinksConsecutiveTracksWithinAFileEntryViaNext()
        {
            var original = BuildSampleCueFile();

            CueFileWriter.Write(original, _tempCuePath);
            var roundTripped = CueFileReader.Read(_tempCuePath);

            var tracks = roundTripped.FileEntries[0].Tracks;

            Assert.AreSame(tracks[1], tracks[0].Next);
            Assert.IsNull(tracks[1].Next);
        }

        [TestMethod]
        public void GetAbsolutePathCombinesCueDirectoryWithRelativeFileName()
        {
            var cueFile = new CueFile { Path = _tempCuePath };
            var fileEntry = new CueFileEntry { FileName = "game.bin" };

            var absolutePath = cueFile.GetAbsolutePath(fileEntry);

            Assert.AreEqual(Path.Combine(Path.GetDirectoryName(_tempCuePath)!, "game.bin"), absolutePath);
        }

        [TestMethod]
        public void GetAbsolutePathReturnsFileNameUnchangedWhenAlreadyFullyQualified()
        {
            var cueFile = new CueFile { Path = _tempCuePath };
            var fullyQualified = Path.Combine(Path.GetTempPath(), "elsewhere", "game.bin");
            var fileEntry = new CueFileEntry { FileName = fullyQualified };

            var absolutePath = cueFile.GetAbsolutePath(fileEntry);

            Assert.AreEqual(fullyQualified, absolutePath);
        }

        [TestMethod]
        public void DummyCreatesSingleDataTrackStartingAtZero()
        {
            var cueFile = CueFileReader.Dummy("game.bin");

            Assert.AreEqual(1, cueFile.FileEntries.Count);
            Assert.AreEqual("game.bin", cueFile.FileEntries[0].FileName);
            Assert.AreEqual(1, cueFile.FileEntries[0].Tracks.Count);

            var track = cueFile.FileEntries[0].Tracks[0];
            Assert.AreEqual(1, track.Number);
            Assert.AreEqual(DataTypes.DATA, track.DataType);
            Assert.AreEqual(1, track.Indexes.Count);
            Assert.AreEqual(0, track.Indexes[0].Position.Minutes);
            Assert.AreEqual(0, track.Indexes[0].Position.Seconds);
            Assert.AreEqual(0, track.Indexes[0].Position.Frames);
        }
    }
}
