using PSXPackager.Common.Chd;
using UnitTestProject.Chd;

namespace UnitTestProject
{
    [TestClass]
    public class ChdFlacTests
    {
        private const int SectorSize = 2352;
        private const int FrameSize = 2448;
        private const int FramesPerHunk = 4;
        private const int HunkBytes = FramesPerHunk * FrameSize;

        /// <summary>
        /// The samples per channel a FLAC block holds in a hunk of this size, which the reader has
        /// to work out for itself because the CHD does not record it.
        /// </summary>
        private const int BlockSize = 2352;

        /// <summary>
        /// A hunk stored with the FLAC codec has to come back byte for byte, since the codec is
        /// lossless and the PBP is built from the bytes.
        /// </summary>
        [TestMethod]
        public void ReadsAFlacCompressedHunk()
        {
            var hunk = AudioHunk();

            using var chd = OpenFlacChd(hunk);

            CollectionAssert.AreEqual(WithoutSubcode(hunk), chd.GetHunk(0));
        }

        /// <summary>
        /// A CHD holds audio big-endian, so the decoded samples have to be written back that way
        /// round before anything else looks at the hunk.
        /// </summary>
        [TestMethod]
        public void KeepsTheStoredSampleOrder()
        {
            var hunk = new byte[HunkBytes];

            // A sample whose two bytes differ, so the wrong order cannot pass unnoticed
            for (var frame = 0; frame < FramesPerHunk; frame++)
            {
                for (var i = 0; i < SectorSize; i += 2)
                {
                    hunk[frame * FrameSize + i] = 0x12;
                    hunk[frame * FrameSize + i + 1] = 0x34;
                }
            }

            using var chd = OpenFlacChd(hunk);

            var read = chd.GetHunk(0);

            Assert.AreEqual(0x12, read[0]);
            Assert.AreEqual(0x34, read[1]);
        }

        /// <summary>
        /// The whole point of the FLAC path is the audio track of a game with CD music, so the
        /// bytes have to survive all the way out to the disc image, swapped once.
        /// </summary>
        [TestMethod]
        public void ReadsAFlacAudioTrackThroughTheDiscStream()
        {
            var hunk = AudioHunk();

            var builder = NewBuilder();
            builder.AddHunk(ChdBuilder.TypeCodec0, EncodeFlac(hunk));
            builder.LogicalBytes = HunkBytes;
            builder.AddTrack(1, "AUDIO", FramesPerHunk);

            using var disc = new ChdDiscStream(ChdFile.Open(new MemoryStream(builder.Build())));

            var sector = new byte[SectorSize];
            disc.Position = 0;

            var read = 0;
            while (read < SectorSize)
            {
                var count = disc.Read(sector, read, SectorSize - read);
                if (count <= 0) break;
                read += count;
            }

            Assert.AreEqual(SectorSize, read);

            // The stream hands back little-endian samples, the order a .bin file uses
            for (var i = 0; i < SectorSize; i += 2)
            {
                Assert.AreEqual(hunk[i + 1], sector[i], $"byte {i}");
                Assert.AreEqual(hunk[i], sector[i + 1], $"byte {i + 1}");
            }
        }

        private static ChdFile OpenFlacChd(byte[] hunk)
        {
            var builder = NewBuilder();
            builder.AddHunk(ChdBuilder.TypeCodec0, EncodeFlac(hunk));

            return ChdFile.Open(new MemoryStream(builder.Build()));
        }

        private static ChdBuilder NewBuilder()
        {
            var builder = new ChdBuilder { HunkBytes = HunkBytes, UnitBytes = FrameSize };
            builder.Compressors[0] = 0x6364666C; // "cdfl"

            return builder;
        }

        /// <summary>
        /// Encodes the sector data of a hunk the way the FLAC codec stores it: the bytes read as
        /// big-endian samples, with the subcode left off entirely.
        /// </summary>
        private static byte[] EncodeFlac(byte[] hunk)
        {
            var samples = new short[FramesPerHunk * SectorSize / 2];
            var index = 0;

            for (var frame = 0; frame < FramesPerHunk; frame++)
            {
                for (var i = 0; i < SectorSize; i += 2)
                {
                    var offset = frame * FrameSize + i;
                    samples[index++] = (short)((hunk[offset] << 8) | hunk[offset + 1]);
                }
            }

            return FlacFrameWriter.Write(samples, BlockSize);
        }

        private static byte[] AudioHunk()
        {
            var hunk = new byte[HunkBytes];

            for (var frame = 0; frame < FramesPerHunk; frame++)
            {
                for (var i = 0; i < SectorSize; i++)
                {
                    hunk[frame * FrameSize + i] = (byte)(i * 17 + frame * 53 + 3);
                }

                // The subcode is stored separately and is dropped on the way in
                for (var i = 0; i < 96; i++)
                {
                    hunk[frame * FrameSize + SectorSize + i] = 0xAA;
                }
            }

            return hunk;
        }

        private static byte[] WithoutSubcode(byte[] hunk)
        {
            var result = (byte[])hunk.Clone();

            for (var frame = 0; frame < hunk.Length / FrameSize; frame++)
            {
                Array.Clear(result, frame * FrameSize + SectorSize, 96);
            }

            return result;
        }
    }
}
