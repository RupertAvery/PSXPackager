namespace PSXPackager
{
    public static class ApplicationInfo
    {
        public static string AppPath { get; private set; }
        static ApplicationInfo()
        {
            // Not working in WSL/Mono
            //string path = System.Reflection.Assembly.GetExecutingAssembly().CodeBase.Replace("file:\\\\\\", "").Replace("file:///", "");
            //AppPath = System.IO.Path.GetDirectoryName(path);
            AppPath = ".";
        }

    }
}
