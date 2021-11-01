namespace Popstation.Database
{
    public class GameEntry
    {
        //Game ID;Eboot Save Folder;Eboot Save Description;Game Name;Video Format;Scanner ID
        public string GameID { get; set; }
        public string SaveFolderName { get; set; }
        public string SaveDescription { get; set; }
        public string GameName { get; set; }
        public string Format { get; set; }
        public string ScannerID { get; set; }
    }
}
