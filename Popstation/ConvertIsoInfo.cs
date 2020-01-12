namespace Popstation
{
    public class ConvertIsoInfo
    {
        public string _base;
        public string data_psp;
        public string srcISO;
        public string dstPBP;
        public string pic0;
        public string pic1;
        public string icon0;
        public string icon1;
        public string snd0;
        public string boot;
        public bool srcIsPbp;
        public string gameTitle;
        public string saveTitle;
        public string gameID;
        public string saveID;
        public int compLevel;
        public int tocSize;
        public byte[] tocData;
        public MultiDiscInfo multiDiscInfo;
        public int patchCount;
        public PatchData[] patchData;
    };
}
