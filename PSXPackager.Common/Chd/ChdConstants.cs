namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// The fixed sizes of the CD-ROM layout CHD stores.
    /// </summary>
    internal static class ChdConstants
    {
        /// <summary>A full raw sector, without subcode.</summary>
        public const int SectorDataSize = 2352;

        /// <summary>The subcode that CHD stores alongside every sector.</summary>
        public const int SubcodeSize = 96;

        /// <summary>One CD frame as stored in a hunk: a sector followed by its subcode.</summary>
        public const int FrameSize = SectorDataSize + SubcodeSize;

        /// <summary>Tracks are padded out to a multiple of this many frames within the file.</summary>
        public const int TrackPadding = 4;

        /// <summary>The 12-byte sync pattern that opens every raw data sector.</summary>
        public static readonly byte[] SyncHeader =
        {
            0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00
        };
    }
}
