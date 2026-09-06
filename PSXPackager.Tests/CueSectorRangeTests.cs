using PSXPackager.Common;
using PSXPackager.Common.Cue;

namespace UnitTestProject
{
    /// <summary>
    /// Works out which sectors a track occupies, which is what the player reads and what decides
    /// where one track stops and the next begins.
    /// </summary>
    [TestClass]
    public class CueSectorRangeTests
    {
        private const int SectorSize = 2352;

        [TestMethod]
        public void ATrackRunsUpToTheNextTrack()
        {
            var tracks = BuildTracks((1, null, 0), (2, null, 4500));

            var (start, end) = tracks[0].GetSectorRange(10000L * SectorSize);

            Assert.AreEqual(0, start);
            Assert.AreEqual(4500, end);
        }

        /// <summary>
        /// A pregap belongs to the track that follows it, so the track before must stop at the gap
        /// rather than play two seconds of it.
        /// </summary>
        [TestMethod]
        public void ATrackStopsWhereTheNextTracksPregapBegins()
        {
            var tracks = BuildTracks((1, null, 0), (2, 4350, 4500));

            var (start, end) = tracks[0].GetSectorRange(10000L * SectorSize);

            Assert.AreEqual(0, start);
            Assert.AreEqual(4350, end, "the 150 frame pregap belongs to track 2");
        }

        [TestMethod]
        public void ATrackStartsAtIndexOneNotAtItsPregap()
        {
            var tracks = BuildTracks((1, null, 0), (2, 4350, 4500));

            var (start, _) = tracks[1].GetSectorRange(10000L * SectorSize);

            Assert.AreEqual(4500, start);
        }

        [TestMethod]
        public void TheLastTrackRunsToTheEndOfTheDisc()
        {
            var tracks = BuildTracks((1, null, 0), (2, 4350, 4500));

            var (_, end) = tracks[1].GetSectorRange(10000L * SectorSize);

            Assert.AreEqual(10000, end);
        }

        [TestMethod]
        public void ATrackWithoutIndexOneIsRejected()
        {
            var track = new CueTrack
            {
                Number = 1,
                DataType = DataTypes.AUDIO,
                Indexes = new List<CueIndex>
                {
                    new() { Number = 0, Position = TOCHelper.PositionFromFrames(0) }
                }
            };

            Assert.ThrowsException<InvalidOperationException>(
                () => track.GetSectorRange(1000L * SectorSize));
        }

        /// <summary>
        /// Builds a chain of tracks from (number, index 00, index 01) triples.
        /// </summary>
        private static List<CueTrack> BuildTracks(params (int Number, int? Index0, int Index1)[] definitions)
        {
            var tracks = new List<CueTrack>();

            foreach (var definition in definitions)
            {
                var indexes = new List<CueIndex>();

                if (definition.Index0.HasValue)
                {
                    indexes.Add(new CueIndex
                    {
                        Number = 0,
                        Position = TOCHelper.PositionFromFrames(definition.Index0.Value)
                    });
                }

                indexes.Add(new CueIndex
                {
                    Number = 1,
                    Position = TOCHelper.PositionFromFrames(definition.Index1)
                });

                tracks.Add(new CueTrack
                {
                    Number = definition.Number,
                    DataType = DataTypes.AUDIO,
                    Indexes = indexes
                });
            }

            for (var i = 0; i + 1 < tracks.Count; i++)
            {
                tracks[i].Next = tracks[i + 1];
            }

            return tracks;
        }
    }
}
