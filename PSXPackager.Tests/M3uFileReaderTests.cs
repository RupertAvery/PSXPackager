using System.IO;
using Popstation.M3u;

namespace UnitTestProject
{
    [TestClass]
    public class M3uFileReaderTests
    {
        private string _tempM3uPath = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempM3uPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".m3u");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempM3uPath))
            {
                File.Delete(_tempM3uPath);
            }
        }

        [TestMethod]
        public void ReadReturnsEachNonEmptyLineAsAnEntry()
        {
            File.WriteAllLines(_tempM3uPath, new[]
            {
                "Disc 1.cue",
                "Disc 2.cue",
                "Disc 3.cue",
            });

            var m3u = M3uFileReader.Read(_tempM3uPath);

            CollectionAssert.AreEqual(new[] { "Disc 1.cue", "Disc 2.cue", "Disc 3.cue" }, m3u.FileEntries);
        }

        [TestMethod]
        public void ReadSkipsBlankAndWhitespaceOnlyLines()
        {
            File.WriteAllLines(_tempM3uPath, new[]
            {
                "Disc 1.cue",
                "",
                "   ",
                "Disc 2.cue",
            });

            var m3u = M3uFileReader.Read(_tempM3uPath);

            CollectionAssert.AreEqual(new[] { "Disc 1.cue", "Disc 2.cue" }, m3u.FileEntries);
        }

        [TestMethod]
        public void ReadTrimsSurroundingWhitespaceFromEntries()
        {
            File.WriteAllLines(_tempM3uPath, new[]
            {
                "  Disc 1.cue  ",
            });

            var m3u = M3uFileReader.Read(_tempM3uPath);

            Assert.AreEqual("Disc 1.cue", m3u.FileEntries[0]);
        }

        [TestMethod]
        public void ReadSetsPathToTheFileThatWasRead()
        {
            File.WriteAllLines(_tempM3uPath, new[] { "Disc 1.cue" });

            var m3u = M3uFileReader.Read(_tempM3uPath);

            Assert.AreEqual(_tempM3uPath, m3u.Path);
        }
    }
}
