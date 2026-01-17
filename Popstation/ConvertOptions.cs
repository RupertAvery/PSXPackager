using System.Collections.Generic;
using Popstation.Pbp;

namespace Popstation
{
    public class ConvertOptions : ICheckIfFileExists
    {
        public string BasePbp { get; set; }
        public Resource DataPsp { get; set; }
        //public string SourceIso {get; set;}
        public string OriginalFilename { get; set; }
        public string OutputPath { get; set; }
        public Resource Icon0 { get; set; }
        public Resource Icon1 { get; set; }
        public Resource Pic0 { get; set; }
        public Resource Pic1 { get; set; }
        public Resource Snd0 { get; set; }
        public Resource Boot { get; set; }
        /// <summary>
        /// Used for default PARAM.SFO DISC_ID and DATA.PSAR
        /// </summary>
        public string MainGameID { get; set; }
        /// <summary>
        /// Used for default PARAM.SFO TITLE and DATA.PSAR
        /// </summary>
        public string MainGameTitle { get; set; }
        /// <summary>
        /// Only used to generate a filename
        /// </summary>
        public string MainGameRegion { get; set; }

        // Set but never used
        //public string SaveID { get; set; }
        //// Set but never used
        //public string SaveTitle { get; set; }
        public int CompressionLevel { get; set; }
        public List<DiscInfo> DiscInfos { get; set; }
        public List<PatchData> Patches { get; set; }
        public bool CheckIfFileExists { get; set; }

        public bool SkipIfFileExists { get; set; }
        public string FileNameFormat { get; set; }
        public string OriginalPath { get; set; }
        public IReadOnlyCollection<SFOEntry> SFOEntries { get; set; }
    };
}
