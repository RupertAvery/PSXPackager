using PSXPackager.Common.Iso;

namespace UnitTestProject
{
    [TestClass]
    public class Mode2Form1StreamTests
    {
        /// <summary>
        /// Sector 24 of a real PlayStation disc (The Legend of Dragoon, SCUS-94586), the sector
        /// holding the PS-X EXE header. Complete with sync pattern, MSF header, subheader,
        /// 2048 bytes of user data, EDC and ECC as pressed by Sony.
        /// </summary>
        private const long RealSectorLba = 24;

        private const string RealSectorBase64 = """
            AP////////////8AAAIkAgAACAAAAAgAUFMtWCBFWEUAAAAAAAAAAOj0G4AAAAAAiIgYgADgAwAAAAAAAAAAAAAAAAAAAAAA
            8P8fgAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAFNvbnkgQ29tcHV0ZXIgRW50ZXJ0YWlubWVudCBJbmMuIGZvciBOb3J0aCBB
            bWVyaWNhIGFyZWEAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
            AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAABq3b56AACWnOZuAGWcaBhs
            dv0hYK9rjRIXFmqd5mif5qaodO2eDfsRbOfrFpxcFOf7kWifF52eavtqF59qAAAAoKrhCwAAAAAAAAAAAAAAAAAAAAAAAL56
            344AAMXziBcoJvMFYBlSy34YykChMmViC/SIBfqIOnwmA/0j23eLHdPY885jj9vQBfpl9P0L2wtl+gsAAABQVf6LAAAAAAAA
            AAAAAAAAAAAAAAAA1Kdh9JcgFkU/LMoDDcQzo6QMSOjO02Fy1fPeZtEbdcxKHdE1tk6UjV27jvgszl8Dcs9XqIbhHMlcvDtP
            oiZ3XZ9yCDpvu/FsljxYQKGZA1JsYeN0M5FXKNl+VZwBfk1irO2f01YNMtH9wVbR
            """;

        /// <summary>
        /// The whole point of the class: given only the 2048 bytes of user data that a cooked .iso
        /// retains, rebuild the sector Sony actually pressed - sync, MSF header, subheader, EDC and
        /// both ECC parity blocks - byte for byte.
        /// </summary>
        [TestMethod]
        public void RebuildsRealSectorFromUserDataAlone()
        {
            var expected = Convert.FromBase64String(RealSectorBase64);
            Assert.AreEqual(DiscImage.RawSectorSize, expected.Length);

            var userData = expected.AsSpan(24, DiscImage.UserSectorSize).ToArray();

            using var stream = new Mode2Form1Stream(BuildCookedImage(RealSectorLba, userData));

            stream.Position = RealSectorLba * DiscImage.RawSectorSize;
            var actual = ReadExact(stream, DiscImage.RawSectorSize);

            CollectionAssert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void LengthIsScaledToRawSectors()
        {
            var source = new MemoryStream(new byte[10 * DiscImage.UserSectorSize]);

            using var stream = new Mode2Form1Stream(source);

            Assert.AreEqual(10L * DiscImage.RawSectorSize, stream.Length);
        }

        [TestMethod]
        public void TrailingPartialSectorIsPaddedToAWholeSector()
        {
            // One full sector plus a single byte
            var source = new MemoryStream(new byte[DiscImage.UserSectorSize + 1]);

            using var stream = new Mode2Form1Stream(source);

            Assert.AreEqual(2L * DiscImage.RawSectorSize, stream.Length);

            stream.Position = DiscImage.RawSectorSize + 24;
            var userData = ReadExact(stream, DiscImage.UserSectorSize);

            Assert.IsTrue(userData.All(b => b == 0), "The padding of the last sector should be zeroed");
        }

        /// <summary>
        /// The PBP writer reads in 1MB and 0x9300 chunks, neither of which lines up with a sector,
        /// so reads have to be able to start and end part way through a sector.
        /// </summary>
        [TestMethod]
        public void UnalignedReadsReturnTheSameBytesAsSectorAlignedReads()
        {
            var userData = new byte[8 * DiscImage.UserSectorSize];
            new Random(1234).NextBytes(userData);

            byte[] sectorAligned;
            using (var stream = new Mode2Form1Stream(new MemoryStream(userData)))
            {
                sectorAligned = ReadExact(stream, (int)stream.Length);
            }

            foreach (var chunkSize in new[] { 1, 7, 1000, 2351, 2353, 5000 })
            {
                using var stream = new Mode2Form1Stream(new MemoryStream(userData));
                using var buffer = new MemoryStream();

                var chunk = new byte[chunkSize];
                int read;
                while ((read = stream.Read(chunk, 0, chunkSize)) > 0)
                {
                    buffer.Write(chunk, 0, read);
                }

                CollectionAssert.AreEqual(sectorAligned, buffer.ToArray(), $"Mismatch at chunk size {chunkSize}");
            }
        }

        [TestMethod]
        public void SeekMovesToTheRequestedPosition()
        {
            var source = new MemoryStream(new byte[4 * DiscImage.UserSectorSize]);

            using var stream = new Mode2Form1Stream(source);

            Assert.AreEqual(100L, stream.Seek(100, SeekOrigin.Begin));
            Assert.AreEqual(150L, stream.Seek(50, SeekOrigin.Current));
            Assert.AreEqual(stream.Length - 10, stream.Seek(-10, SeekOrigin.End));
        }

        [TestMethod]
        public void ReadingPastTheEndReturnsZero()
        {
            using var stream = new Mode2Form1Stream(new MemoryStream(new byte[DiscImage.UserSectorSize]));

            stream.Position = stream.Length;

            Assert.AreEqual(0, stream.Read(new byte[16], 0, 16));
        }

        private static MemoryStream BuildCookedImage(long lba, byte[] userData)
        {
            var cooked = new MemoryStream();
            cooked.SetLength((lba + 1) * DiscImage.UserSectorSize);
            cooked.Position = lba * DiscImage.UserSectorSize;
            cooked.Write(userData, 0, userData.Length);
            return cooked;
        }

        private static byte[] ReadExact(Stream stream, int count)
        {
            var buffer = new byte[count];
            var read = 0;

            while (read < count)
            {
                var bytesRead = stream.Read(buffer, read, count - read);
                if (bytesRead <= 0) throw new EndOfStreamException();
                read += bytesRead;
            }

            return buffer;
        }
    }
}
