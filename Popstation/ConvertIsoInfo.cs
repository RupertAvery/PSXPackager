using System.Collections.Generic;

namespace Popstation
{
    public class ConvertIsoInfo
    {
        public string BasePbp { get; set; }
        public string DataPsp { get; set; }
        //public string SourceIso {get; set;}
        public string DestinationPbp { get; set; }
        public string Pic0 { get; set; }
        public string Pic1 { get; set; }
        public string Icon0 { get; set; }
        public string Icon1 { get; set; }
        public string Snd0 { get; set; }
        public string Boot { get; set; }
        public string MainGameTitle { get; set; }
        public string SaveTitle { get; set; }
        public string MainGameID { get; set; }
        public string SaveID { get; set; }
        public int CompressionLevel { get; set; }
        public List<DiscInfo> DiscInfos { get; set; }
        public List<PatchData> Patches { get; set; }
        public bool CheckIfFileExists { get; set; }
    };
}
