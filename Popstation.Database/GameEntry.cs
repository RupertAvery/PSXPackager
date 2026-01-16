namespace Popstation.Database
{
    public class GameEntry
    {
        //Game ID;Eboot Save Folder;Eboot Save Description;Game Name;Video Format;Scanner ID
        /// <summary>
        /// GameID with a dash
        /// </summary>
        public string SerialID { get; set; }
        /// <summary>
        /// Main GameID (Eboot Save Folder)
        /// </summary>
        public string MainGameID { get; set; }
        /// <summary>
        /// Main Game Title (Eboot Save Description/Game name without disc numbering)
        /// </summary>
        public string MainGameTitle { get; set; }
        /// <summary>
        /// Game Name (Individual Disc Title)
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Region (Video Format, NTSC/PAL)
        /// </summary>
        public string Region { get; set; }
        /// <summary>
        /// GameID with a no dash
        /// </summary>
        public string GameID { get; set; }
    }
}
