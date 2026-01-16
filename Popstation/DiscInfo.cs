namespace Popstation
{
    public class DiscInfo
    {
        public string SourceIso { get; set; }
        /// <summary>
        /// The GameID of the individual disc, distinct from the serial/main GameID. <br/>
        /// Written to DATA.PSAR in the DATA1 section
        /// </summary>
        public string GameID { get; set; }
        /// <summary>
        /// The title of the individual disc, including disc number information if any. <br/>
        /// Written to DATA.PSAR in the DATA2 section
        /// </summary>
        public string GameTitle { get; set; }
        public string SourceToc { get; set; }
        public byte[] TocData { get; set; }
        public uint IsoSize { get; set; }

    }
}
