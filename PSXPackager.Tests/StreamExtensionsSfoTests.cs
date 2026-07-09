using System.IO;
using Popstation;
using Popstation.Pbp;

namespace UnitTestProject
{
    [TestClass]
    public class StreamExtensionsSfoTests
    {
        [TestMethod]
        public void WriteSFOThenReadSFORoundTripsStringAndIntEntries()
        {
            var builder = new SFOBuilder();
            builder.AddEntry(SFOKeys.DISC_ID, "SLUS-00001");
            builder.AddEntry(SFOKeys.BOOTABLE, 1);
            builder.AddEntry(SFOKeys.TITLE, "My Game");
            var sfo = builder.Build();

            using var stream = new MemoryStream();
            stream.WriteSFO(sfo);
            stream.Position = 0;

            var roundTripped = stream.ReadSFO(0);

            Assert.AreEqual(sfo.Magic, roundTripped.Magic);
            Assert.AreEqual(sfo.Version, roundTripped.Version);
            Assert.AreEqual(sfo.KeyTableOffset, roundTripped.KeyTableOffset);
            Assert.AreEqual(sfo.DataTableOffset, roundTripped.DataTableOffset);
            Assert.AreEqual(3, roundTripped.Entries.Count);

            var discId = roundTripped.Entries[0];
            Assert.AreEqual(SFOKeys.DISC_ID, discId.Key);
            Assert.AreEqual("SLUS-00001", discId.Value);

            var bootable = roundTripped.Entries[1];
            Assert.AreEqual(SFOKeys.BOOTABLE, bootable.Key);
            Assert.AreEqual(1u, bootable.Value);

            var title = roundTripped.Entries[2];
            Assert.AreEqual(SFOKeys.TITLE, title.Key);
            Assert.AreEqual("My Game", title.Value);
        }

        [TestMethod]
        public void WriteSFOThenReadSFOPreservesKeyOffsetFormatAndLengthMetadata()
        {
            var builder = new SFOBuilder();
            builder.AddEntry(SFOKeys.DISC_ID, "SLUS-00001");
            builder.AddEntry(SFOKeys.TITLE, "My Game");
            var sfo = builder.Build();

            using var stream = new MemoryStream();
            stream.WriteSFO(sfo);
            stream.Position = 0;

            var roundTripped = stream.ReadSFO(0);

            for (var i = 0; i < sfo.Entries.Count; i++)
            {
                Assert.AreEqual(sfo.Entries[i].KeyOffset, roundTripped.Entries[i].KeyOffset);
                Assert.AreEqual(sfo.Entries[i].Format, roundTripped.Entries[i].Format);
                Assert.AreEqual(sfo.Entries[i].Length, roundTripped.Entries[i].Length);
                Assert.AreEqual(sfo.Entries[i].MaxLength, roundTripped.Entries[i].MaxLength);
                Assert.AreEqual(sfo.Entries[i].DataOffset, roundTripped.Entries[i].DataOffset);
            }
        }
    }
}
