using System;
using System.IO;

namespace PSXPackagerGUI.Common
{
    public static class Logger
    {
        private static string LogPath = Path.Combine(ApplicationInfo.AppPath, "logs.txt");

        public static void LogError(string message, Exception exception)
        {
            File.AppendAllText(LogPath, $"{message}. {exception}\r\n");
        }

        public static void LogInfo(string message)
        {
            File.AppendAllText(LogPath, $"{message}\r\n");
        }
    }
}
