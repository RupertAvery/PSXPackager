using System.Collections.Generic;

namespace Popstation
{
    public class SFOData
    {
        public uint Magic { get; set; }
        public uint Version { get; set; }
        public uint KeyTableOffset { get; set; }
        public uint Padding { get; set; }
        public uint DataTableOffset { get; set; }
        public List<SFODir> Entries { get; set; }
        public uint Size { get; set; }
    }
}
