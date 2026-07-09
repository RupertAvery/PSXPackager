using System;
using System.IO;
using System.Runtime.CompilerServices;
using Popstation.Pbp;

namespace UnitTestProject
{
    [TestClass]
    public class PbpReaderTests
    {
        // Header slot offsets, mirroring the private constants in PbpReader
        private const int Icon0Offset = 0x0C;
        private const int Icon1Offset = 0x10;
        private const int Pic0Offset = 0x14;
        private const int Snd0Offset = 0x1C;
        private const int PsarOffset = 0x24;

        private static void WriteInt32At(byte[] buffer, int offset, int value)
        {
            BitConverter.GetBytes(value).CopyTo(buffer, offset);
        }

        /// <summary>
        /// Seek() and TryGetResourceStream() only operate on the stream argument they're
        /// given - they don't touch the Discs/SFOData built by the constructor. The
        /// constructor itself requires a byte-perfect valid PBP (including a parseable
        /// disc TOC), which is impractical to hand-build just to exercise this header
        /// arithmetic, so tests here bypass it and construct an uninitialized instance.
        /// </summary>
        private static PbpReader CreateReaderWithoutRunningConstructor()
        {
            return (PbpReader)RuntimeHelpers.GetUninitializedObject(typeof(PbpReader));
        }

        [TestMethod]
        public void SeekReturnsDifferenceBetweenStartAndEndHeaderSlots()
        {
            // ICON0's length is defined by the gap between its own header slot and the
            // next resource's header slot (ICON1), which stores ICON0's end offset.
            var buffer = new byte[Icon1Offset + 4];
            WriteInt32At(buffer, Icon0Offset, 100);
            WriteInt32At(buffer, Icon1Offset, 150);

            using var stream = new MemoryStream(buffer);
            var reader = CreateReaderWithoutRunningConstructor();

            var length = reader.Seek(ResourceType.ICON0, stream);

            Assert.AreEqual(50, length);
        }

        [TestMethod]
        public void SeekPositionsStreamAtTheStartOffset()
        {
            var buffer = new byte[Icon1Offset + 4];
            WriteInt32At(buffer, Icon0Offset, 100);
            WriteInt32At(buffer, Icon1Offset, 150);

            using var stream = new MemoryStream(buffer);
            var reader = CreateReaderWithoutRunningConstructor();

            reader.Seek(ResourceType.ICON0, stream);

            Assert.AreEqual(100, stream.Position);
        }

        [TestMethod]
        public void SeekForPsarUsesStreamLengthAsTheEndOffset()
        {
            // PSAR has no "next" header slot - it runs to the end of the file.
            var buffer = new byte[48];
            WriteInt32At(buffer, PsarOffset, 16);

            using var stream = new MemoryStream(buffer);
            var reader = CreateReaderWithoutRunningConstructor();

            var length = reader.Seek(ResourceType.PSAR, stream);

            Assert.AreEqual(buffer.Length - 16, length);
            Assert.AreEqual(16, stream.Position);
        }

        [TestMethod]
        public void SeekThrowsForAResourceTypeWithNoHeaderSlot()
        {
            var buffer = new byte[Icon1Offset + 4];
            using var stream = new MemoryStream(buffer);
            var reader = CreateReaderWithoutRunningConstructor();

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => reader.Seek(ResourceType.BOOT, stream));
        }

        [TestMethod]
        public void TryGetResourceStreamReturnsTrueWithResourceBytesWhenLengthIsPositive()
        {
            var buffer = new byte[24];
            WriteInt32At(buffer, Icon0Offset, 20);
            WriteInt32At(buffer, Icon1Offset, 24);
            buffer[20] = 0xDE;
            buffer[21] = 0xAD;
            buffer[22] = 0xBE;
            buffer[23] = 0xEF;

            using var stream = new MemoryStream(buffer);
            var reader = CreateReaderWithoutRunningConstructor();

            var found = reader.TryGetResourceStream(ResourceType.ICON0, stream, out var resourceStream);

            Assert.IsTrue(found);
            using var memoryStream = new MemoryStream();
            resourceStream.CopyTo(memoryStream);
            CollectionAssert.AreEqual(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, memoryStream.ToArray());
        }

        [TestMethod]
        public void TryGetResourceStreamReturnsFalseWhenStartEqualsEnd()
        {
            var buffer = new byte[Snd0Offset + 8];
            WriteInt32At(buffer, Pic0Offset, 10);
            // ICON1..PIC0's "end" slot is PIC0 itself in this arrangement; make it degenerate
            // by pointing PIC0's own start and the following slot at the same offset.
            WriteInt32At(buffer, Pic0Offset + 4, 10);

            using var stream = new MemoryStream(buffer);
            var reader = CreateReaderWithoutRunningConstructor();

            var found = reader.TryGetResourceStream(ResourceType.PIC0, stream, out var resourceStream);

            Assert.IsFalse(found);
            Assert.IsNull(resourceStream);
        }

        [TestMethod]
        public void ConstructorThrowsWhenMagicHeaderIsInvalid()
        {
            var buffer = new byte[32]; // all zeros - not the PBP magic

            using var stream = new MemoryStream(buffer);

            Assert.ThrowsException<Exception>(() => new PbpReader(stream));
        }
    }
}
