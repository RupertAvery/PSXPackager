namespace PSXPackager.Common.Cue;

public static class CueExtensions
{
    public const int FramesPerSecond = 75;
    public const int SectorSize = 2352;

    public static int ToSector(this IndexPosition pos)
    {
        return (pos.Minutes * 60 * FramesPerSecond)
               + (pos.Seconds * FramesPerSecond)
               + pos.Frames;
    }

    public static long ToByteOffset(this IndexPosition pos)
    {
        return (long)pos.ToSector() * SectorSize;
    }
}