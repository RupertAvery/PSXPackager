using PSXPackager.Common.Chd;
using UnitTestProject.Chd;

namespace UnitTestProject
{
    [TestClass]
    public class ChdFileTests
    {
        private const int FrameSize = 2448;
        private const int FramesPerHunk = 4;
        private const int HunkBytes = FramesPerHunk * FrameSize;

        [TestMethod]
        public void ReadsAnUncompressedFile()
        {
            var builder = NewBuilder();
            var hunk = Pattern(HunkBytes, 1);

            builder.AddHunk(ChdBuilder.TypeNone, hunk);

            using var chd = Open(builder);

            CollectionAssert.AreEqual(hunk, chd.GetHunk(0));
        }

        [TestMethod]
        public void ReadsAZlibCompressedHunk()
        {
            var builder = NewBuilder();
            builder.Compressors[0] = 0x7A6C6962; // "zlib"

            var hunk = Pattern(HunkBytes, 7);
            builder.AddHunk(ChdBuilder.TypeCodec0, ChdBuilder.Deflate(hunk));

            using var chd = Open(builder);

            CollectionAssert.AreEqual(hunk, chd.GetHunk(0));
        }

        /// <summary>
        /// A hunk stored with the "none" type sits in a compressed file verbatim, and its map
        /// entry carries no length of its own.
        /// </summary>
        [TestMethod]
        public void ReadsAnUncompressedHunkInACompressedFile()
        {
            var builder = NewBuilder();
            builder.Compressors[0] = 0x7A6C6962;

            var stored = Pattern(HunkBytes, 3);
            var compressedHunk = Pattern(HunkBytes, 9);

            builder.AddHunk(ChdBuilder.TypeNone, stored);
            builder.AddHunk(ChdBuilder.TypeCodec0, ChdBuilder.Deflate(compressedHunk));

            using var chd = Open(builder);

            CollectionAssert.AreEqual(stored, chd.GetHunk(0));
            CollectionAssert.AreEqual(compressedHunk, chd.GetHunk(1));
        }

        /// <summary>
        /// A file can name several codecs and choose between them per hunk.
        /// </summary>
        [TestMethod]
        public void ReadsHunksStoredWithDifferentCodecs()
        {
            var builder = NewBuilder();
            builder.Compressors[0] = 0x7A6C6962; // "zlib"
            builder.Compressors[1] = 0x63647A6C; // "cdzl"

            var plain = Pattern(HunkBytes, 11);
            var cd = Pattern(HunkBytes, 13);

            builder.AddHunk(ChdBuilder.TypeCodec0, ChdBuilder.Deflate(plain));
            builder.AddHunk(ChdBuilder.TypeCodec1, ChdBuilder.CompressCdZlib(cd, HunkBytes));

            using var chd = Open(builder);

            CollectionAssert.AreEqual(plain, chd.GetHunk(0));

            // The CD codec drops the subcode, which an image does not need
            CollectionAssert.AreEqual(WithoutSubcode(cd), chd.GetHunk(1));
        }

        [TestMethod]
        public void ReadsAnLzmaCompressedHunk()
        {
            var builder = NewBuilder();
            builder.Compressors[0] = 0x6C7A6D61; // "lzma"

            var hunk = Pattern(HunkBytes, 5);
            builder.AddHunk(ChdBuilder.TypeCodec0, CompressLzma(hunk, HunkBytes));

            using var chd = Open(builder);

            CollectionAssert.AreEqual(hunk, chd.GetHunk(0));
        }

        /// <summary>
        /// A CD hunk stores a data sector without its sync header and ECC, and the reader has to
        /// put both back.
        /// </summary>
        [TestMethod]
        public void RebuildsTheSyncHeaderAndEccOfAStrippedSector()
        {
            var complete = new byte[HunkBytes];
            var sector = BuildMode2Form1Sector(150);

            Array.Copy(sector, 0, complete, 0, sector.Length);

            // What the file holds: the same sector with its sync pattern and ECC blanked out
            var stripped = (byte[])complete.Clone();
            Array.Clear(stripped, 0, 12);
            Array.Clear(stripped, 2076, 276);

            var builder = NewBuilder();
            builder.Compressors[0] = 0x63647A6C; // "cdzl"
            builder.AddHunk(ChdBuilder.TypeCodec0, ChdBuilder.CompressCdZlib(stripped, HunkBytes, new[] { 0 }));

            using var chd = Open(builder);

            var read = chd.GetHunk(0);

            CollectionAssert.AreEqual(
                WithoutSubcode(complete).Take(2352).ToArray(),
                read.Take(2352).ToArray());
        }

        [TestMethod]
        public void ASelfReferencingHunkRepeatsAnEarlierHunk()
        {
            var builder = NewBuilder();
            builder.Compressors[0] = 0x7A6C6962;

            var hunk = Pattern(HunkBytes, 21);

            builder.AddHunk(ChdBuilder.TypeCodec0, ChdBuilder.Deflate(hunk));
            builder.AddReference(ChdBuilder.TypeSelf, 0);

            using var chd = Open(builder);

            CollectionAssert.AreEqual(hunk, chd.GetHunk(1));
        }

        /// <summary>
        /// Runs of hunks that share a compression type are collapsed in the map, so the type of a
        /// hunk can be implied by the ones before it.
        /// </summary>
        [TestMethod]
        public void ExpandsARunOfRepeatedCompressionTypes()
        {
            var builder = NewBuilder();
            builder.UseRunLengthEncoding = true;
            builder.Compressors[0] = 0x7A6C6962;

            var hunks = Enumerable.Range(0, 5).Select(i => Pattern(HunkBytes, (byte)(30 + i))).ToList();

            foreach (var hunk in hunks)
            {
                builder.AddHunk(ChdBuilder.TypeCodec0, ChdBuilder.Deflate(hunk));
            }

            using var chd = Open(builder);

            for (var i = 0; i < hunks.Count; i++)
            {
                CollectionAssert.AreEqual(hunks[i], chd.GetHunk(i), $"hunk {i}");
            }
        }

        [TestMethod]
        public void ReadsTheMetadataChain()
        {
            var builder = NewBuilder();
            builder.AddHunk(ChdBuilder.TypeNone, new byte[HunkBytes]);
            builder.AddTrack(1, "MODE2_RAW", 100);
            builder.AddTrack(2, "AUDIO", 200);

            using var chd = Open(builder);

            Assert.AreEqual(2, chd.Metadata.Count);
            StringAssert.Contains(chd.Metadata[0].GetText(), "TRACK:1");
            StringAssert.Contains(chd.Metadata[1].GetText(), "TRACK:2");
        }

        [TestMethod]
        public void RejectsAFileThatIsNotAChd()
        {
            var image = new byte[1024];

            Assert.IsFalse(ChdFile.IsChd(new MemoryStream(image)));
            Assert.ThrowsException<InvalidChdException>(() => ChdFile.Open(new MemoryStream(image)));
        }

        [TestMethod]
        public void RejectsAnUnsupportedCodec()
        {
            var builder = NewBuilder();
            builder.Compressors[0] = 0x61766875; // "avhu"
            builder.AddHunk(ChdBuilder.TypeCodec0, new byte[16]);

            Assert.ThrowsException<InvalidChdException>(() => Open(builder));
        }

        /// <summary>
        /// The property block is not stored with an LZMA hunk, so it has to be derived from the
        /// hunk size exactly as the encoder derived it.
        /// </summary>
        [TestMethod]
        public void DerivesTheLzmaPropertiesFromTheHunkSize()
        {
            using var output = new MemoryStream();

            using (var encoder = new SharpCompress.Compressors.LZMA.LzmaStream(
                       new SharpCompress.Compressors.LZMA.LzmaEncoderProperties(false, DictionarySize(HunkBytes)),
                       false,
                       output))
            {
                // The properties are fixed at construction; the encoder reports them back
                Assert.AreEqual(0x5D, encoder.Properties[0], "lc=3, lp=0, pb=2");
                Assert.AreEqual(DictionarySize(HunkBytes), BitConverter.ToInt32(encoder.Properties, 1));
            }
        }

        private static ChdBuilder NewBuilder()
        {
            return new ChdBuilder { HunkBytes = HunkBytes, UnitBytes = FrameSize };
        }

        private static ChdFile Open(ChdBuilder builder)
        {
            return ChdFile.Open(new MemoryStream(builder.Build()));
        }

        /// <summary>
        /// The dictionary size preset level 9 settles on once it is told it will never be given
        /// more than one hunk at a time.
        /// </summary>
        private static int DictionarySize(int hunkBytes)
        {
            for (var i = 11; i <= 30; i++)
            {
                if (hunkBytes <= 2 << i) return 2 << i;
                if (hunkBytes <= 3 << i) return 3 << i;
            }

            return 1 << 30;
        }

        private static byte[] CompressLzma(byte[] data, int hunkBytes)
        {
            using var output = new MemoryStream();

            using (var encoder = new SharpCompress.Compressors.LZMA.LzmaStream(
                       new SharpCompress.Compressors.LZMA.LzmaEncoderProperties(false, DictionarySize(hunkBytes)),
                       false,
                       output))
            {
                encoder.Write(data);
            }

            return output.ToArray();
        }

        private static byte[] Pattern(int length, byte seed)
        {
            var data = new byte[length];

            for (var i = 0; i < length; i++)
            {
                data[i] = (byte)(i * seed + seed);
            }

            return data;
        }

        /// <summary>
        /// The hunk a CD codec produces: the same sectors, with the subcode zeroed out.
        /// </summary>
        private static byte[] WithoutSubcode(byte[] hunk)
        {
            var result = (byte[])hunk.Clone();

            for (var frame = 0; frame < hunk.Length / FrameSize; frame++)
            {
                Array.Clear(result, frame * FrameSize + 2352, 96);
            }

            return result;
        }

        private static byte[] BuildMode2Form1Sector(long lba)
        {
            var sector = new byte[2352];

            byte[] sync = { 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00 };
            sync.CopyTo(sector, 0);

            var frame = lba + 150;
            sector[12] = ToBcd((int)(frame / 75 / 60 % 100));
            sector[13] = ToBcd((int)(frame / 75 % 60));
            sector[14] = ToBcd((int)(frame % 75));
            sector[15] = 0x02;

            for (var copy = 0; copy < 2; copy++)
            {
                sector[16 + copy * 4 + 2] = 0x08;
            }

            for (var i = 0; i < 2048; i++)
            {
                sector[24 + i] = (byte)(i * 3 + 1);
            }

            // The reader recomputes the parity but not the EDC, which the file keeps
            var edc = ComputeEdc(sector, 16, 8 + 2048);
            sector[2072] = (byte)edc;
            sector[2073] = (byte)(edc >> 8);
            sector[2074] = (byte)(edc >> 16);
            sector[2075] = (byte)(edc >> 24);

            WriteEcc(sector);

            return sector;
        }

        private static byte ToBcd(int value) => (byte)(((value / 10) << 4) | (value % 10));

        private static readonly byte[] EccForward = new byte[256];
        private static readonly byte[] EccBackward = new byte[256];
        private static readonly uint[] EdcTable = new uint[256];

        static ChdFileTests()
        {
            for (var i = 0; i < 256; i++)
            {
                var j = (i << 1) ^ ((i & 0x80) != 0 ? 0x11D : 0);

                EccForward[i] = (byte)j;
                EccBackward[i ^ j] = (byte)i;

                var edc = (uint)i;

                for (var bit = 0; bit < 8; bit++)
                {
                    edc = (edc >> 1) ^ ((edc & 1) != 0 ? 0xD8018001u : 0);
                }

                EdcTable[i] = edc;
            }
        }

        private static uint ComputeEdc(byte[] sector, int offset, int count)
        {
            uint edc = 0;

            for (var i = 0; i < count; i++)
            {
                edc = (edc >> 8) ^ EdcTable[(edc ^ sector[offset + i]) & 0xFF];
            }

            return edc;
        }

        private static void WriteEcc(byte[] sector)
        {
            var address = new byte[4];
            Array.Copy(sector, 12, address, 0, 4);
            Array.Clear(sector, 12, 4);

            ComputeBlock(sector, 86, 24, 2, 86, 0x81C);
            ComputeBlock(sector, 52, 43, 86, 88, 0x8C8);

            Array.Copy(address, 0, sector, 12, 4);
        }

        private static void ComputeBlock(byte[] sector, int majorCount, int minorCount, int majorMult, int minorInc, int destination)
        {
            var size = majorCount * minorCount;

            for (var major = 0; major < majorCount; major++)
            {
                var index = (major >> 1) * majorMult + (major & 1);
                byte eccA = 0;
                byte eccB = 0;

                for (var minor = 0; minor < minorCount; minor++)
                {
                    var value = sector[0x0C + index];

                    index += minorInc;
                    if (index >= size) index -= size;

                    eccA ^= value;
                    eccB ^= value;
                    eccA = EccForward[eccA];
                }

                eccA = EccBackward[EccForward[eccA] ^ eccB];

                sector[destination + major] = eccA;
                sector[destination + major + majorCount] = (byte)(eccA ^ eccB);
            }
        }
    }
}
