namespace Popstation.Database
{
    public class GameEntry
    {
        //Game ID;Eboot Save Folder;Eboot Save Description;Game Name;Video Format;Scanner ID
        /// <summary>
        /// GameID with a dash
        /// </summary>
        public string GameID { get; set; }
        /// <summary>
        /// Main GameID
        /// </summary>
        public string SaveFolderName { get; set; }
        /// <summary>
        /// Main Game Title
        /// </summary>
        public string SaveDescription { get; set; }
        /// <summary>
        /// Disc Title
        /// </summary>
        public string GameName { get; set; }
        /// <summary>
        /// Region
        /// </summary>
        public string Format { get; set; }
        /// <summary>
        /// GameID with a no dash
        /// </summary>
        public string ScannerID { get; set; }
    }
}
