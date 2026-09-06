using PSXPackager.Common;

namespace UnitTestProject
{
    [TestClass]
    public class FileExtensionHelperTests
    {
        [DataTestMethod]
        [DataRow("game.cue", true)]
        [DataRow("game.CUE", true)]
        [DataRow("game.bin", false)]
        [DataRow("game", false)]
        public void IsCue(string filename, bool expected)
        {
            Assert.AreEqual(expected, FileExtensionHelper.IsCue(filename));
        }

        [DataTestMethod]
        [DataRow("game.pbp", true)]
        [DataRow("game.PBP", true)]
        [DataRow("game.bin", false)]
        public void IsPbp(string filename, bool expected)
        {
            Assert.AreEqual(expected, FileExtensionHelper.IsPbp(filename));
        }

        [DataTestMethod]
        [DataRow("playlist.m3u", true)]
        [DataRow("playlist.M3U", true)]
        [DataRow("playlist.cue", false)]
        public void IsM3u(string filename, bool expected)
        {
            Assert.AreEqual(expected, FileExtensionHelper.IsM3u(filename));
        }

        [DataTestMethod]
        [DataRow("archive.rar", true)]
        [DataRow("archive.zip", true)]
        [DataRow("archive.tar", true)]
        [DataRow("archive.gz", true)]
        [DataRow("archive.7z", true)]
        [DataRow("archive.ZIP", true)]
        [DataRow("game.bin", false)]
        [DataRow("game.iso", false)]
        public void IsArchive(string filename, bool expected)
        {
            Assert.AreEqual(expected, FileExtensionHelper.IsArchive(filename));
        }

        [DataTestMethod]
        [DataRow("track01.bin", true)]
        [DataRow("track01.BIN", true)]
        [DataRow("game.iso", false)]
        public void IsBin(string filename, bool expected)
        {
            Assert.AreEqual(expected, FileExtensionHelper.IsBin(filename));
        }

        [DataTestMethod]
        [DataRow("track01.bin", true)]
        [DataRow("disc.img", true)]
        [DataRow("disc.iso", true)]
        [DataRow("disc.cue", false)]
        [DataRow("disc.pbp", false)]
        public void IsImageFile(string filename, bool expected)
        {
            Assert.AreEqual(expected, FileExtensionHelper.IsImageFile(filename));
        }
    }
}
