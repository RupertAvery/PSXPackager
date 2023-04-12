namespace UnitTestProject
{
    [TestClass]
    public class IndexPositionWithPositionMathTests
    {
        [TestMethod]
        public void AddIndexPositions()
        {
            var indexA = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexB = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexC = indexA + indexB;

            Assert.AreEqual(10, indexC.Frames);
            Assert.AreEqual(10, indexC.Seconds);
            Assert.AreEqual(10, indexC.Minutes);
        }

        [TestMethod]
        public void AddIndexPositionsCarryFrames()
        {
            var indexA = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexB = new IndexPosition()
            {
                Frames = 70,
                Seconds = 5,
                Minutes = 5,
            };

            var indexC = indexA + indexB;

            Assert.AreEqual(0, indexC.Frames);
            Assert.AreEqual(11, indexC.Seconds);
            Assert.AreEqual(10, indexC.Minutes);
        }

        [TestMethod]
        public void AddIndexPositionsCarrySeconds()
        {
            var indexA = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexB = new IndexPosition()
            {
                Frames = 5,
                Seconds = 55,
                Minutes = 5,
            };

            var indexC = indexA + indexB;

            Assert.AreEqual(10, indexC.Frames);
            Assert.AreEqual(0, indexC.Seconds);
            Assert.AreEqual(11, indexC.Minutes);
        }

        [TestMethod]
        public void AddIndexPositionsCarryFramesAndSeconds()
        {
            var indexA = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexB = new IndexPosition()
            {
                Frames = 70,
                Seconds = 54,
                Minutes = 5,
            };

            var indexC = indexA + indexB;

            Assert.AreEqual(0, indexC.Frames);
            Assert.AreEqual(0, indexC.Seconds);
            Assert.AreEqual(11, indexC.Minutes);
        }

        [TestMethod]
        public void SubtractIndexPositions()
        {
            var indexA = new IndexPosition()
            {
                Frames = 10,
                Seconds = 10,
                Minutes = 10,
            };

            var indexB = new IndexPosition()
            {
                Frames = 5,
                Seconds = 5,
                Minutes = 5,
            };

            var indexC = indexA - indexB;

            Assert.AreEqual(5, indexC.Frames);
            Assert.AreEqual(5, indexC.Seconds);
            Assert.AreEqual(5, indexC.Minutes);
        }

        [TestMethod]
        public void SubtractIndexPositionsBorrowSeconds()
        {
            var indexA = new IndexPosition()
            {
                Frames = 10,
                Seconds = 10,
                Minutes = 10,
            };

            var indexB = new IndexPosition()
            {
                Frames = 11,
                Seconds = 5,
                Minutes = 5,
            };

            var indexC = indexA - indexB;

            Assert.AreEqual(74, indexC.Frames);
            Assert.AreEqual(4, indexC.Seconds);
            Assert.AreEqual(5, indexC.Minutes);
        }

        [TestMethod]
        public void SubtractIndexPositionsBorrowMinutes()
        {
            var indexA = new IndexPosition()
            {
                Frames = 10,
                Seconds = 10,
                Minutes = 10,
            };

            var indexB = new IndexPosition()
            {
                Frames = 10,
                Seconds = 11,
                Minutes = 5,
            };

            var indexC = indexA - indexB;

            Assert.AreEqual(0, indexC.Frames);
            Assert.AreEqual(59, indexC.Seconds);
            Assert.AreEqual(4, indexC.Minutes);
        }

        [TestMethod]
        public void SubtractIndexPositionsBorrowSecondsAndMinutes()
        {
            var indexA = new IndexPosition()
            {
                Frames = 10,
                Seconds = 10,
                Minutes = 10,
            };

            var indexB = new IndexPosition()
            {
                Frames = 11,
                Seconds = 10,
                Minutes = 5,
            };

            var indexC = indexA - indexB;

            Assert.AreEqual(74, indexC.Frames);
            Assert.AreEqual(59, indexC.Seconds);
            Assert.AreEqual(4, indexC.Minutes);
        }

    }
}
