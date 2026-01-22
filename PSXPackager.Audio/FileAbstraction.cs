using System.Text.RegularExpressions;
using Popstation.Pbp;

namespace PSXPackager.Audio;

/// <summary>
/// Provides utility methods for working with file URIs, including support for extracting and accessing disc entries
/// from PBP files.
/// </summary>
/// <remarks>This static class offers methods to interpret URIs that may refer to either standard files or
/// specific disc entries within PBP files. Methods in this class handle both standard file paths and special PBP
/// URIs of the form "//pbp/{filename}.pbp/{discIndex}". This enables seamless access to embedded disc images within
/// PBP containers as if they were standalone files.</remarks>
public static class FileAbstraction
{
    public static bool TryGetPbpDiscEntryFromUri(string uri, out PbpDiscEntry? discEntry)
    {
        var match = IsPbpUri(uri);
        if (match.Success)
        {
            var stream = new FileStream(match.Groups[1].Value, FileMode.Open, FileAccess.Read);
            var pbpReader = new PbpReader(stream);
            discEntry = pbpReader.Discs[int.Parse(match.Groups[2].Value)];
            return true;
        }
        else
        {
            discEntry = null;
            return false;
        }
    }


    public static Match IsPbpUri(string uri)
    {
        var pbpRegex = new Regex("pbp://(?<pbp>.*\\.pbp)/disc(?<disc>\\d)", RegexOptions.IgnoreCase);
        var match = pbpRegex.Match(uri);
        return match;
    }

    public static long GetFileSizeFromUri(string uri)
    {
        // Ugh, need to dispose the disc entry after getting the size
        if (TryGetPbpDiscEntryFromUri(uri,  out var discEntry))
        {
            try
            {
                return discEntry!.IsoSize;
            }
            finally
            {
                discEntry?.Dispose();
            }
        }
        else
        {
            return new FileInfo(uri).Length;
        }
    }

    public static Stream GetStreamFromUri(string uri)
    {
        if (TryGetPbpDiscEntryFromUri(uri, out var discEntry))
        {
            return discEntry.GetDiscStream();
        }
        else
        {
            return File.OpenRead(uri);
        }
    }
}