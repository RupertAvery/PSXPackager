using System;
using System.IO;

namespace PSXPackager.Common.Iso
{
    /// <summary>
    /// Identifies the sector layout of a disc image and opens it as a raw 2352-byte sector stream.
    /// </summary>
    /// <remarks>
    /// Images come in two flavours: "raw" images (usually .bin) hold complete 2352-byte sectors,
    /// while "cooked" images (usually .iso) hold only the 2048 bytes of user data per sector.
    /// The PBP writer, the TOC and the game ID reader all work in raw sectors, so a cooked image
    /// is wrapped in a <see cref="Mode2Form1Stream"/> that rebuilds the missing sector framing.
    /// The extension is not trusted - the layout is detected from the content.
    /// </remarks>
    public static class DiscImage
    {
        public const int RawSectorSize = 2352;
        public const int UserSectorSize = 2048;

        /// <summary>
        /// The logical sector holding the ISO9660 Primary Volume Descriptor.
        /// </summary>
        private const int PrimaryVolumeDescriptorSector = 16;

        private static readonly byte[] SyncPattern =
        {
            0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00
        };

        /// <summary>"CD001"</summary>
        private static readonly byte[] StandardIdentifier = { 0x43, 0x44, 0x30, 0x30, 0x31 };

        /// <summary>
        /// Determines the sector size of an image, leaving the stream position unchanged.
        /// </summary>
        /// <returns><see cref="RawSectorSize"/> or <see cref="UserSectorSize"/>.</returns>
        public static int DetectSectorSize(Stream stream)
        {
            var originalPosition = stream.Position;

            try
            {
                // A raw sector opens with the 12-byte sync pattern
                var sync = new byte[SyncPattern.Length];
                stream.Position = 0;
                if (ReadFully(stream, sync) && Matches(sync, SyncPattern))
                {
                    return RawSectorSize;
                }

                // A cooked image has the Primary Volume Descriptor at the start of sector 16,
                // one byte in, past the descriptor type
                var identifier = new byte[StandardIdentifier.Length];
                stream.Position = (long)UserSectorSize * PrimaryVolumeDescriptorSector + 1;
                if (ReadFully(stream, identifier) && Matches(identifier, StandardIdentifier))
                {
                    return UserSectorSize;
                }

                // Neither marker was found - fall back to whichever size divides the image evenly
                if (stream.Length % RawSectorSize == 0) return RawSectorSize;
                if (stream.Length % UserSectorSize == 0) return UserSectorSize;

                return RawSectorSize;
            }
            finally
            {
                stream.Position = originalPosition;
            }
        }

        /// <summary>
        /// Determines the sector size of an image file.
        /// </summary>
        public static int DetectSectorSize(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return DetectSectorSize(stream);
            }
        }

        /// <summary>
        /// Reads the sector layout and the size the image will occupy once expanded to raw sectors.
        /// </summary>
        public static DiscImageInfo GetInfo(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                var sectorSize = DetectSectorSize(stream);
                var length = stream.Length;

                var rawSize = sectorSize == RawSectorSize
                    ? length
                    : (length + UserSectorSize - 1) / UserSectorSize * RawSectorSize;

                return new DiscImageInfo(sectorSize, rawSize);
            }
        }

        /// <summary>
        /// The size of an image once presented as raw 2352-byte sectors. For a cooked image this is
        /// larger than the size of the file on disk.
        /// </summary>
        public static long GetRawSize(string path) => GetInfo(path).RawSize;

        /// <summary>
        /// Opens an image for reading as a stream of raw 2352-byte sectors, expanding it if needed.
        /// </summary>
        public static Stream OpenRead(string path)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read);

            try
            {
                if (DetectSectorSize(stream) == UserSectorSize)
                {
                    return new Mode2Form1Stream(stream);
                }

                stream.Position = 0;

                return stream;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        private static bool ReadFully(Stream stream, byte[] buffer)
        {
            var read = 0;

            while (read < buffer.Length)
            {
                var bytesRead = stream.Read(buffer, read, buffer.Length - read);
                if (bytesRead <= 0) return false;
                read += bytesRead;
            }

            return true;
        }

        private static bool Matches(byte[] buffer, byte[] expected)
        {
            for (var i = 0; i < expected.Length; i++)
            {
                if (buffer[i] != expected[i]) return false;
            }

            return true;
        }
    }

    public readonly struct DiscImageInfo
    {
        public DiscImageInfo(int sectorSize, long rawSize)
        {
            SectorSize = sectorSize;
            RawSize = rawSize;
        }

        public int SectorSize { get; }

        /// <summary>
        /// The size of the image once presented as raw 2352-byte sectors.
        /// </summary>
        public long RawSize { get; }

        /// <summary>
        /// True when the image holds only user data and has to be expanded to raw sectors.
        /// </summary>
        public bool IsCooked => SectorSize == DiscImage.UserSectorSize;
    }
}
