using System;
using System.IO;

namespace PSXPackager.Common
{
    public static class ApplicationInfo
    {
        public static string AppPath { get; private set; }

        static ApplicationInfo()
        {
            AppPath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
        }
    }
}
