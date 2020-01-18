using Popstation;
using System.Collections.Generic;

namespace PSXPackager
{
    public class MergedBin
    {
        public string Path { get; set; }
        public List<CueFile> CueFiles { get; set; }
    }
}
