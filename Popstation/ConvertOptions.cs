using System.Collections.Generic;

namespace Popstation
{
    public class ConvertOptions : ICheckIfFileExists
    {
        public string BasePbp { get; set; }
        public Resource DataPsp { get; set; }
        //public string SourceIso {get; set;}
        public string DestinationPbp { get; set; }
        public Resource Icon0 { get; set; }
        public Resource Icon1 { get; set; }
        public Resource Pic0 { get; set; }
        public Resource Pic1 { get; set; }
        public Resource Snd0 { get; set; }
        public string Boot { get; set; }
        public string MainGameTitle { get; set; }
        public string SaveTitle { get; set; }
        public string MainGameID { get; set; }
        public string SaveID { get; set; }
        public int CompressionLevel { get; set; }
        public List<DiscInfo> DiscInfos { get; set; }
        public List<PatchData> Patches { get; set; }
        public bool CheckIfFileExists { get; set; }

        public bool SkipIfFileExists { get; set; }
        public string FileNameFormat { get; set; }
        public string MainGameRegion { get; set; }
    };
}
