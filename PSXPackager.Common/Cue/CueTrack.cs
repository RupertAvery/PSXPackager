#nullable enable

using System.Collections.Generic;

namespace PSXPackager.Common.Cue
{
    public class CueTrack
    {
        public int Number { get; set; }
        public string DataType { get; set; } = string.Empty;
        public List<CueIndex> Indexes { get; set; } = new();
        public CueFileEntry FileEntry { get; set; } = null!;
        public CueTrack? Next { get; set; }
    }
}
