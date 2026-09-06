using System.Collections.Generic;
using PSXPackager.Common.Cue;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// Builds a cue sheet describing the disc inside a CHD.
    /// </summary>
    /// <remarks>
    /// A CHD carries its track list in metadata rather than in a cue sheet, but the PBP writer
    /// takes the disc's table of contents from a cue sheet. This turns the one into the other, so
    /// a CHD with audio tracks produces a PBP whose TOC points at the right places.
    /// </remarks>
    public static class ChdCueSheet
    {
        /// <summary>
        /// Reads the track list straight out of a .chd and describes it as a cue sheet.
        /// </summary>
        /// <param name="path">The .chd to read.</param>
        /// <param name="fileName">
        /// The name to put in the FILE entry. Defaults to the CHD's own name, since the disc it
        /// describes is not written out to a separate file.
        /// </param>
        public static CueFile FromChd(string path, string fileName = null)
        {
            using (var chd = ChdFile.Open(path))
            {
                var cue = FromToc(ChdCdToc.Parse(chd), fileName ?? System.IO.Path.GetFileName(path));

                // Anything that resolves the FILE entry relative to the sheet lands back on the
                // CHD, which is where the disc actually is
                cue.Path = path;

                return cue;
            }
        }

        public static CueFile FromToc(ChdCdToc toc, string fileName)
        {
            var cueFile = new CueFile();

            var fileEntry = new CueFileEntry
            {
                CueFile = cueFile,
                FileName = fileName,
                FileType = FileTypes.BINARY,
                Tracks = new List<CueTrack>()
            };

            CueTrack previous = null;

            foreach (var track in toc.Tracks)
            {
                var indexes = new List<CueIndex>();

                // A pregap held in the data is an INDEX 00 ahead of the track proper
                if (track.PregapStored && track.Pregap > 0)
                {
                    indexes.Add(new CueIndex
                    {
                        Number = 0,
                        Position = TOCHelper.PositionFromFrames(track.StartFrame)
                    });
                }

                indexes.Add(new CueIndex
                {
                    Number = 1,
                    Position = TOCHelper.PositionFromFrames(track.IndexOneFrame)
                });

                var cueTrack = new CueTrack
                {
                    FileEntry = fileEntry,
                    Number = track.Number,

                    // Every track is presented as full raw sectors, whatever the CHD stored
                    DataType = track.IsAudio ? DataTypes.AUDIO : DataTypes.DATA,
                    Indexes = indexes
                };

                if (previous != null)
                {
                    previous.Next = cueTrack;
                }

                previous = cueTrack;

                fileEntry.Tracks.Add(cueTrack);
            }

            cueFile.FileEntries.Add(fileEntry);

            return cueFile;
        }
    }
}
