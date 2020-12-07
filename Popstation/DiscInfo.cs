namespace Popstation
{
    public class DiscInfo
    {
        public string SourceIso { get; set; }
        public string GameName { get; set; }
        public string Region { get; set; }
        public string GameTitle { get; set; }
        public string GameID { get; set; }
        public string MainGameID { get; set; }
        public string SourceToc { get; set; }
        public byte[] TocData { get; set; }
        public uint IsoSize { get; set; }

    }
}
