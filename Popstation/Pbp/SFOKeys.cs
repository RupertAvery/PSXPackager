using System.Collections.Generic;

namespace Popstation.Pbp
{
    public class SFOKeys
    {
        public const string BOOTABLE = "BOOTABLE";
        public const string CATEGORY = "CATEGORY";
        public const string DISC_ID = "DISC_ID";
        public const string DISC_VERSION = "DISC_VERSION";
        public const string LICENSE = "LICENSE";
        public const string PARENTAL_LEVEL = "PARENTAL_LEVEL";
        public const string PSP_SYSTEM_VER = "PSP_SYSTEM_VER";
        public const string REGION = "REGION";
        public const string TITLE = "TITLE";

        public IReadOnlyCollection<string> Keys =
        [
            "BOOTABLE", 
            "CATEGORY", 
            "DISC_ID", 
            "DISC_NUMBER", 
            "DISC_TOTAL", 
            "DISC_VERSION", 
            "DRIVER_PATH", 
            "LANGUAGE",
            "PARENTAL_LEVEL", 
            "PSP_SYSTEM_VER",
            "REGION", 
            "SAVEDATA_DETAIL", 
            "SAVEDATA_DIRECTORY", 
            "SAVEDATA_FILE_LIST", 
            "SAVEDATA_PARAMS",
            "SAVEDATA_TITLE", 
            "TITLE", 
            "TITLE_0", 
            "TITLE_2", 
            "TITLE_3",
            "TITLE_4", 
            "TITLE_5", 
            "TITLE_6", 
            "TITLE_7", 
            "TITLE_8"
        ];
    }

}