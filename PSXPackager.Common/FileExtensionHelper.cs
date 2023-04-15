using System.IO;

namespace PSXPackager.Common
{
    public static class FileExtensionHelper
    {
        public static bool IsCue(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".cue";
        }

        public static bool IsPbp(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".pbp";
        }

        public static bool IsM3u(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".m3u";
        }

        public static bool IsArchive(string filename)
        {
            // https://github.com/adamhathcock/sharpcompress/blob/master/FORMATS.md
            return
            Path.GetExtension(filename).ToLower() == ".rar" ||
            Path.GetExtension(filename).ToLower() == ".zip" ||
            Path.GetExtension(filename).ToLower() == ".tar" ||
            Path.GetExtension(filename).ToLower() == ".gz" ||
            Path.GetExtension(filename).ToLower() == ".7z";
        }

        public static bool IsBin(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".bin";
        }

        public static bool IsImageFile(string filename)
        {
            return Path.GetExtension(filename).ToLower() == ".bin" ||
            Path.GetExtension(filename).ToLower() == ".img" ||
            Path.GetExtension(filename).ToLower() == ".iso";
        }

    }
}
