using System;
using Popstation.Pbp;

namespace UnitTestProject
{
    [TestClass]
    public class SFOBuilderTests
    {
        [TestMethod]
        public void BuildComputesHeaderAndTableOffsetsForGivenEntries()
        {
            var builder = new SFOBuilder();
            builder.AddEntry(SFOKeys.DISC_ID, "SLUS-00001"); // 10 chars
            builder.AddEntry(SFOKeys.TITLE, "Test Game");    // 9 chars

            var sfo = builder.Build();

            Assert.AreEqual(0x46535000u, sfo.Magic);
            Assert.AreEqual(0x00000101u, sfo.Version);

            // headerSize(20) + indexTableSize(2 entries * 16)
            Assert.AreEqual(52u, sfo.KeyTableOffset);

            // keyTableSize = ("DISC_ID".Length+1) + ("TITLE".Length+1) = 8 + 6 = 14, padded to a multiple of 4
            Assert.AreEqual(2u, sfo.Padding);
            Assert.AreEqual(68u, sfo.DataTableOffset);

            // dataOffset accumulates each entry's MaxLength: DISC_ID(16) + TITLE(128)
            Assert.AreEqual(212u, sfo.Size);
        }

        [TestMethod]
        public void BuildAssignsIncreasingKeyAndDataOffsetsInEntryOrder()
        {
            var builder = new SFOBuilder();
            builder.AddEntry(SFOKeys.DISC_ID, "SLUS-00001");
            builder.AddEntry(SFOKeys.TITLE, "Test Game");

            var sfo = builder.Build();

            Assert.AreEqual(2, sfo.Entries.Count);

            var discIdEntry = sfo.Entries[0];
            Assert.AreEqual(SFOKeys.DISC_ID, discIdEntry.Key);
            Assert.AreEqual((ushort)0, discIdEntry.KeyOffset);
            Assert.AreEqual(0u, discIdEntry.DataOffset);
            Assert.AreEqual(16u, discIdEntry.MaxLength);

            var titleEntry = sfo.Entries[1];
            Assert.AreEqual(SFOKeys.TITLE, titleEntry.Key);
            Assert.AreEqual((ushort)8, titleEntry.KeyOffset); // "DISC_ID".Length + 1
            Assert.AreEqual(16u, titleEntry.DataOffset);      // after DISC_ID's MaxLength
            Assert.AreEqual(128u, titleEntry.MaxLength);
        }

        [TestMethod]
        public void BuildUsesStringFormatForTextKeysAndIntFormatForNumericKeys()
        {
            const ushort stringType = 0x0204;
            const ushort intType = 0x0404;

            var builder = new SFOBuilder();
            builder.AddEntry(SFOKeys.TITLE, "Test Game");
            builder.AddEntry(SFOKeys.REGION, 1);

            var sfo = builder.Build();

            Assert.AreEqual(stringType, sfo.Entries[0].Format);
            Assert.AreEqual(intType, sfo.Entries[1].Format);
        }

        [TestMethod]
        public void BuildOmitsEntriesWithNullOrBlankStringValues()
        {
            var builder = new SFOBuilder();
            builder.AddEntry(SFOKeys.DISC_ID, "SLUS-00001");
            builder.AddEntry(SFOKeys.TITLE, "");
            builder.AddEntry(SFOKeys.LICENSE, "   ");
            builder.AddEntry(SFOKeys.DISC_VERSION, null);

            var sfo = builder.Build();

            Assert.AreEqual(1, sfo.Entries.Count);
            Assert.AreEqual(SFOKeys.DISC_ID, sfo.Entries[0].Key);
        }

        [TestMethod]
        public void BuildKeepsIntEntriesEvenWhenZero()
        {
            var builder = new SFOBuilder();
            builder.AddEntry(SFOKeys.REGION, 0);

            var sfo = builder.Build();

            Assert.AreEqual(1, sfo.Entries.Count);
            Assert.AreEqual(SFOKeys.REGION, sfo.Entries[0].Key);
        }

        [TestMethod]
        public void BuildThrowsWhenValueExceedsMaxLengthForKey()
        {
            var builder = new SFOBuilder();
            // DISC_ID has a 16-byte max length (including null terminator)
            builder.AddEntry(SFOKeys.DISC_ID, new string('X', 20));

            Assert.ThrowsException<Exception>(() => builder.Build());
        }

        [TestMethod]
        public void BuildThrowsForUnrecognizedKey()
        {
            var builder = new SFOBuilder();
            builder.AddEntry("NOT_A_REAL_KEY", "value");

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => builder.Build());
        }
    }
}
