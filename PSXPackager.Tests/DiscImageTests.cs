using PSXPackager.Common.Iso;

namespace UnitTestProject
{
    [TestClass]
    public class DiscImageTests
    {
        [TestMethod]
        public void DetectsRawImageFromTheSyncPattern()
        {
            var image = new byte[4 * DiscImage.RawSectorSize];
            WriteSync(image);

            Assert.AreEqual(DiscImage.RawSectorSize, DiscImage.DetectSectorSize(new MemoryStream(image)));
        }

        [TestMethod]
        public void DetectsCookedImageFromTheVolumeDescriptor()
        {
            var image = BuildCookedImage(20);

            Assert.AreEqual(DiscImage.UserSectorSize, DiscImage.DetectSectorSize(new MemoryStream(image)));
        }

        /// <summary>
        /// A cooked image whose length happens to be a multiple of 2352 must still be recognised
        /// as cooked, so the volume descriptor has to win over the size heuristic.
        /// </summary>
        [TestMethod]
        public void VolumeDescriptorBeatsTheSizeHeuristic()
        {
            // 147 * 2048 == 128 * 2352, so the length divides evenly by both sector sizes
            var image = BuildCookedImage(147);
            Assert.AreEqual(0, image.Length % DiscImage.RawSectorSize);

            Assert.AreEqual(DiscImage.UserSectorSize, DiscImage.DetectSectorSize(new MemoryStream(image)));
        }

        [TestMethod]
        public void FallsBackToTheSizeHeuristicWhenNoMarkerIsFound()
        {
            var raw = new MemoryStream(new byte[3 * DiscImage.RawSectorSize]);
            Assert.AreEqual(DiscImage.RawSectorSize, DiscImage.DetectSectorSize(raw));

            var cooked = new MemoryStream(new byte[3 * DiscImage.UserSectorSize]);
            Assert.AreEqual(DiscImage.UserSectorSize, DiscImage.DetectSectorSize(cooked));
        }

        [TestMethod]
        public void DetectionLeavesTheStreamPositionAlone()
        {
            var stream = new MemoryStream(BuildCookedImage(20)) { Position = 1234 };

            DiscImage.DetectSectorSize(stream);

            Assert.AreEqual(1234L, stream.Position);
        }

        [TestMethod]
        public void CookedImagesReportTheirExpandedRawSize()
        {
            var path = WriteTempFile(BuildCookedImage(20));

            try
            {
                var info = DiscImage.GetInfo(path);

                Assert.IsTrue(info.IsCooked);
                Assert.AreEqual(DiscImage.UserSectorSize, info.SectorSize);
                Assert.AreEqual(20L * DiscImage.RawSectorSize, info.RawSize);
                Assert.AreEqual(0, info.RawSize % DiscImage.RawSectorSize);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void RawImagesReportTheirSizeOnDisk()
        {
            var image = new byte[4 * DiscImage.RawSectorSize];
            WriteSync(image);
            var path = WriteTempFile(image);

            try
            {
                var info = DiscImage.GetInfo(path);

                Assert.IsFalse(info.IsCooked);
                Assert.AreEqual(image.Length, info.RawSize);
            }
            finally
            {
                File.Delete(path);
            }
        }

        [TestMethod]
        public void OpenReadExpandsCookedImagesAndPassesRawImagesThrough()
        {
            var cookedPath = WriteTempFile(BuildCookedImage(20));

            var rawImage = new byte[4 * DiscImage.RawSectorSize];
            WriteSync(rawImage);
            var rawPath = WriteTempFile(rawImage);

            try
            {
                using (var stream = DiscImage.OpenRead(cookedPath))
                {
                    Assert.IsInstanceOfType<Mode2Form1Stream>(stream);
                    Assert.AreEqual(20L * DiscImage.RawSectorSize, stream.Length);
                }

                using (var stream = DiscImage.OpenRead(rawPath))
                {
                    Assert.IsInstanceOfType<FileStream>(stream);
                    Assert.AreEqual(0L, stream.Position);
                    Assert.AreEqual(rawImage.Length, stream.Length);
                }
            }
            finally
            {
                File.Delete(cookedPath);
                File.Delete(rawPath);
            }
        }

        private static void WriteSync(byte[] image)
        {
            for (var i = 1; i <= 10; i++) image[i] = 0xFF;
        }

        /// <summary>
        /// Builds a cooked image of the given number of 2048-byte sectors, carrying an ISO9660
        /// Primary Volume Descriptor at sector 16.
        /// </summary>
        private static byte[] BuildCookedImage(int sectors)
        {
            var image = new byte[sectors * DiscImage.UserSectorSize];
            var offset = 16 * DiscImage.UserSectorSize;

            image[offset] = 0x01; // Primary Volume Descriptor
            "CD001"u8.CopyTo(image.AsSpan(offset + 1));

            return image;
        }

        private static string WriteTempFile(byte[] contents)
        {
            var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            File.WriteAllBytes(path, contents);
            return path;
        }
    }
}