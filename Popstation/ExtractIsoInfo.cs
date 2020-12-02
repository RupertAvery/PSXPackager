using System.Collections.Generic;

namespace Popstation
{
    public class ExtractIsoInfo
    {
        public string SourcePbp { get; set; }
        public string DestinationIso { get; set; }
        public string DiscName { get; set; }
        public bool CreateCuesheet { get; set; }
        public bool CreatePlaylist { get; set; }
        public List<int> Discs { get; set; }
        public bool CheckIfFileExists { get; set; }
    }
}
