using System;
using System.IO;

namespace PSXPackagerGUI
{
    public static class ApplicationInfo
    {
        public static string AppPath { get; private set; }

        static ApplicationInfo()
        {
            AppPath = AppContext.BaseDirectory;
            //AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}