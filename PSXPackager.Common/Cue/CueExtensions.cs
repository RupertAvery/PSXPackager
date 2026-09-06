using System;
using System.Linq;

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

    /// <summary>
    /// Finds one of a track's indexes, or null if the track does not have it.
    /// </summary>
    public static IndexPosition FindIndex(this CueTrack track, int number)
    {
        return track.Indexes?.FirstOrDefault(index => index.Number == number)?.Position;
    }

    /// <summary>
    /// The sectors holding a track's own data: from its INDEX 01 up to where the next track's
    /// data begins, or to the end of the disc for the last track.
    /// </summary>
    /// <param name="track">The track to measure.</param>
    /// <param name="discLength">The length of the whole disc image in bytes.</param>
    /// <returns>The first sector of the track, and the first sector past it.</returns>
    public static (int Start, int End) GetSectorRange(this CueTrack track, long discLength)
    {
        var start = track.FindIndex(1)
                    ?? throw new InvalidOperationException($"Track {track.Number} has no INDEX 01");

        if (track.Next == null)
        {
            return (start.ToSector(), (int)(discLength / SectorSize));
        }

        // A pregap belongs to the track that follows it, so this track stops where that gap
        // starts rather than running on into it
        var next = track.Next.FindIndex(0) ?? track.Next.FindIndex(1)
                   ?? throw new InvalidOperationException($"Track {track.Next.Number} has no INDEX 01");

        return (start.ToSector(), next.ToSector());
    }
}
