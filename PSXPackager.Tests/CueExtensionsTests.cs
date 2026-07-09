using PSXPackager.Common.Cue;

namespace UnitTestProject
{
    [TestClass]
    public class CueExtensionsTests
    {
        [TestMethod]
        public void ToSectorConvertsMinutesSecondsFramesToAbsoluteFrameCount()
        {
            var position = new IndexPosition(1, 2, 3);

            // 1 minute * 60 * 75 + 2 seconds * 75 + 3 frames
            Assert.AreEqual(1 * 60 * 75 + 2 * 75 + 3, position.ToSector());
        }

        [TestMethod]
        public void ToSectorAtOriginIsZero()
        {
            var position = new IndexPosition(0, 0, 0);

            Assert.AreEqual(0, position.ToSector());
        }

        [TestMethod]
        public void ToByteOffsetMultipliesSectorBy2352()
        {
            var position = new IndexPosition(0, 1, 0);

            Assert.AreEqual(75L * 2352, position.ToByteOffset());
        }

        [TestMethod]
        public void ToByteOffsetAtOriginIsZero()
        {
            var position = new IndexPosition(0, 0, 0);

            Assert.AreEqual(0L, position.ToByteOffset());
        }
    }
}
