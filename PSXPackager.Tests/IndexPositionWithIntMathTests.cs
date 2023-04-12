using PSXPackager.Common.Cue;

namespace UnitTestProject
{
    [TestClass]
    public class IndexPositionWithIntMathTests
    {
        [TestMethod]
        public void AddIndexPositionWithInt()
        {
            var indexA = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexC = indexA + 5;

            Assert.AreEqual(10, indexC.Frames);
            Assert.AreEqual(5, indexC.Seconds);
            Assert.AreEqual(5, indexC.Minutes);
        }

        [TestMethod]
        public void AddIndexPositionWithIntCarryFrames()
        {
            var indexA = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexC = indexA + 70;

            Assert.AreEqual(0, indexC.Frames);
            Assert.AreEqual(6, indexC.Seconds);
            Assert.AreEqual(5, indexC.Minutes);
        }

        [TestMethod]
        public void AddIndexPositionWithIntCarrySeconds()
        {
            var indexA = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexC = indexA + 4125; // 55 seconds

            Assert.AreEqual(5, indexC.Frames);
            Assert.AreEqual(0, indexC.Seconds);
            Assert.AreEqual(6, indexC.Minutes);
        }

        [TestMethod]
        public void AddIndexPositionWithIntCarryFramesAndSeconds()
        {
            var indexA = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexC = indexA + 4120; // 70 frames + 54 seconds

            Assert.AreEqual(0, indexC.Frames);
            Assert.AreEqual(0, indexC.Seconds);
            Assert.AreEqual(6, indexC.Minutes);
        }

        [TestMethod]
        public void SubtractIndexPositionWithFrame()
        {
            var indexA = new IndexPosition()
            {
                Frames = 10,
                Seconds = 10,
                Minutes = 10,
            };

            var indexC = indexA - 5;

            Assert.AreEqual(5, indexC.Frames);
            Assert.AreEqual(10, indexC.Seconds);
            Assert.AreEqual(10, indexC.Minutes);
        }

        [TestMethod]
        public void SubtractIndexPositionWithFrameBorrowSeconds()
        {
            var indexA = new IndexPosition()
            {
                Frames = 10,
                Seconds = 10,
                Minutes = 10,
            };

            var indexC = indexA - 11;

            Assert.AreEqual(74, indexC.Frames);
            Assert.AreEqual(9, indexC.Seconds);
            Assert.AreEqual(10, indexC.Minutes);
        }

        [TestMethod]
        public void SubtractIndexPositionWithFrameBorrowMinutes()
        {
            var indexA = new IndexPosition()
            {
                Frames = 10,
                Seconds = 10,
                Minutes = 10,
            };

            var indexC = indexA - 825; // 11 seconds

            Assert.AreEqual(10, indexC.Frames);
            Assert.AreEqual(59, indexC.Seconds);
            Assert.AreEqual(9, indexC.Minutes);
        }

        [TestMethod]
        public void SubtractIndexPositionWithFrameBorrowSecondsAndMinutes()
        {
            var indexA = new IndexPosition()
            {
                Frames = 10,
                Seconds = 10,
                Minutes = 10,
            };

            var indexC = indexA - 761; // 10 seconds + 11 frames

            Assert.AreEqual(74, indexC.Frames);
            Assert.AreEqual(59, indexC.Seconds);
            Assert.AreEqual(9, indexC.Minutes);
        }

    }
}
