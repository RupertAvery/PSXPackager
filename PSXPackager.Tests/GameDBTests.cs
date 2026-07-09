using System.IO;
using Popstation.Database;

namespace UnitTestProject
{
    [TestClass]
    public class GameDBTests
    {
        private string _tempDbPath = string.Empty;

        [TestInitialize]
        public void Setup()
        {
            _tempDbPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".db");
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempDbPath))
            {
                File.Delete(_tempDbPath);
            }
        }

        [DataTestMethod]
        [DataRow("SLUS-00001", "U")]
        [DataRow("SLUS00001", "U")]
        [DataRow("slus-00001", "U")]
        [DataRow("SCUS-00001", "U")]
        [DataRow("SLES-00001", "P")]
        [DataRow("SCES-00001", "P")]
        [DataRow("SCED-00001", "P")]
        [DataRow("SLED-00001", "P")]
        [DataRow("SLPS-00001", "J")]
        [DataRow("SLPM-00001", "J")]
        [DataRow("SCPS-00001", "J")]
        [DataRow("SIPS-00001", "U")]
        [DataRow("not-a-game-id", "U")]
        public void GetRegionLetterMapsPartyCodeToRegion(string gameId, string expectedRegion)
        {
            Assert.AreEqual(expectedRegion, GameDB.GetRegionLetter(gameId));
        }

        [TestMethod]
        public void ConstructorMapsSemicolonDelimitedFieldsToGameEntry()
        {
            File.WriteAllLines(_tempDbPath, new[]
            {
                "SLUS-00001;MAIN1;Main Game Title;Disc 1 Title;NTSC-U;MAIN1",
            });

            var gameDb = new GameDB(_tempDbPath);

            Assert.AreEqual(1, gameDb.GameEntries.Count);
            var entry = gameDb.GameEntries[0];
            Assert.AreEqual("SLUS-00001", entry.SerialID);
            Assert.AreEqual("MAIN1", entry.MainGameID);
            Assert.AreEqual("Main Game Title", entry.MainGameTitle);
            Assert.AreEqual("Disc 1 Title", entry.Title);
            Assert.AreEqual("NTSC-U", entry.Region);
            Assert.AreEqual("MAIN1", entry.GameID);
        }

        [TestMethod]
        public void ConstructorPopulatesDiscCountWhenGameIdMatchesMainGameId()
        {
            // DiscCount is looked up by GameEntry.GameID (parts[5]) against a table keyed
            // by MainGameID (parts[1]) - it only ends up populated for rows where those two
            // columns hold the same value, as with the "main" disc's row here.
            File.WriteAllLines(_tempDbPath, new[]
            {
                "SLUS-00001;MAIN1;Main Game Title;Disc 1 Title;NTSC-U;MAIN1",
            });

            var gameDb = new GameDB(_tempDbPath);

            Assert.AreEqual(1, gameDb.GameEntries[0].DiscCount);
        }

        [TestMethod]
        public void ConstructorLeavesDiscCountAtZeroWhenGameIdDiffersFromMainGameId()
        {
            File.WriteAllLines(_tempDbPath, new[]
            {
                "SLUS-00002;MAIN1;Main Game Title;Disc 2 Title;NTSC-U;GID2",
            });

            var gameDb = new GameDB(_tempDbPath);

            Assert.AreEqual(0, gameDb.GameEntries[0].DiscCount);
        }

        [TestMethod]
        public void ConstructorAssignsDiscIndexOneToEveryRow()
        {
            // DiscIndex is meant to increment for consecutive rows sharing the same
            // MainGameID, but the comparison is against a tracking variable that is never
            // updated after initialization - so every row currently gets DiscIndex 1,
            // even across consecutive same-MainGameID rows. This test pins that behavior.
            File.WriteAllLines(_tempDbPath, new[]
            {
                "SLUS-00001;MAIN1;Main Game Title;Disc 1 Title;NTSC-U;MAIN1",
                "SLUS-00002;MAIN1;Main Game Title;Disc 2 Title;NTSC-U;GID2",
                "SLUS-00003;MAIN2;Other Game Title;Other Game Title;NTSC-U;MAIN2",
            });

            var gameDb = new GameDB(_tempDbPath);

            Assert.AreEqual(1, gameDb.GameEntries[0].DiscIndex);
            Assert.AreEqual(1, gameDb.GameEntries[1].DiscIndex);
            Assert.AreEqual(1, gameDb.GameEntries[2].DiscIndex);
        }

        [TestMethod]
        public void GetEntryByGameIDFindsEntryStoredInUppercase()
        {
            File.WriteAllLines(_tempDbPath, new[]
            {
                "SLUS-00001;MAIN1;Main Game Title;Disc 1 Title;NTSC-U;MAIN1",
            });

            var gameDb = new GameDB(_tempDbPath);

            var entry = gameDb.GetEntryByGameID("main1");

            Assert.IsNotNull(entry);
            Assert.AreEqual("MAIN1", entry.GameID);
        }

        [TestMethod]
        public void GetEntryByGameIDReturnsNullWhenStoredGameIdIsNotUppercase()
        {
            // GetEntryByGameID compares the stored GameID as-is against gameId.ToUpper(),
            // so a stored GameID that isn't already uppercase can never match.
            File.WriteAllLines(_tempDbPath, new[]
            {
                "SLUS-00001;MAIN1;Main Game Title;Disc 1 Title;NTSC-U;Main1",
            });

            var gameDb = new GameDB(_tempDbPath);

            Assert.IsNull(gameDb.GetEntryByGameID("main1"));
        }

        [TestMethod]
        public void GetEntryByGameIDReturnsNullWhenNotFound()
        {
            File.WriteAllLines(_tempDbPath, new[]
            {
                "SLUS-00001;MAIN1;Main Game Title;Disc 1 Title;NTSC-U;MAIN1",
            });

            var gameDb = new GameDB(_tempDbPath);

            Assert.IsNull(gameDb.GetEntryByGameID("DOES-NOT-EXIST"));
        }
    }
}
