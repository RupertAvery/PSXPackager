using Popstation;
using Popstation.Pbp;

namespace UnitTestProject
{
    [TestClass]
    public class PopstationFilenameFormatTests
    {
        [DataTestMethod]
        [DataRow("%FILENAME%")]
        [DataRow("%GAMEID%")]
        [DataRow("%MAINGAMEID%")]
        [DataRow("%TITLE%")]
        [DataRow("%MAINTITLE%")]
        [DataRow("%REGION%")]
        public void CheckFormatIsTrueForEachRecognizedPlaceholder(string placeholder)
        {
            Assert.IsTrue(Popstation.Popstation.CheckFormat($"prefix {placeholder} suffix"));
        }

        [TestMethod]
        public void CheckFormatIsFalseWhenNoPlaceholderIsPresent()
        {
            Assert.IsFalse(Popstation.Popstation.CheckFormat("just a plain name"));
        }

        [TestMethod]
        public void CheckResourceFormatRecognizesResourcePlaceholder()
        {
            Assert.IsTrue(Popstation.Popstation.CheckResourceFormat("%RESOURCE%"));
            Assert.IsFalse(Popstation.Popstation.CheckResourceFormat("no placeholders here"));
        }

        [TestMethod]
        public void GetFilenameReplacesAllBasePlaceholders()
        {
            var result = Popstation.Popstation.GetFilename(
                "%TITLE% (%REGION%) [%GAMEID%] {%MAINTITLE%|%MAINGAMEID%} %FILENAME%",
                "source.iso",
                "SLUS-00001",
                "SLUS00001",
                "My Game",
                "My Game Main",
                "NTSC-U");

            Assert.AreEqual("My Game (NTSC-U) [SLUS-00001] {My Game Main|SLUS00001} source", result);
        }

        [TestMethod]
        public void GetFilenamePlaceholdersAreCaseInsensitive()
        {
            var result = Popstation.Popstation.GetFilename(
                "%title% - %gameid%",
                @"source.iso",
                "SLUS-00001",
                "SLUS00001",
                "My Game",
                "My Game Main",
                "NTSC-U");

            Assert.AreEqual("My Game - SLUS-00001", result);
        }

        [TestMethod]
        public void GetResourceFilenameAlsoReplacesResourceAndExtPlaceholders()
        {
            var result = Popstation.Popstation.GetResourceFilename(
                "%FILENAME%\\%RESOURCE%.%EXT%",
                @"source.iso",
                "SLUS-00001",
                "SLUS00001",
                "My Game",
                "My Game Main",
                "NTSC-U",
                ResourceType.ICON0,
                "png");

            Assert.AreEqual("source\\ICON0.png", result);
        }

        [TestMethod]
        public void GetResourceFolderDoesNotReplaceResourceOrExtPlaceholders()
        {
            // %RESOURCE% and %EXT% are only defined for GetResourceFilename, not for
            // the plain filename/folder helpers - this pins that (pre-existing) behavior.
            var result = Popstation.Popstation.GetResourceFolder(
                "%FILENAME%\\%RESOURCE%",
                @"source.iso",
                "SLUS-00001",
                "SLUS00001",
                "My Game",
                "My Game Main",
                "NTSC-U");

            Assert.AreEqual("source\\%RESOURCE%", result);
        }
    }
}
