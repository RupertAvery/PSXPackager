using System;
using System.IO;
using System.Text.RegularExpressions;
using Popstation.Pbp;
using PSXPackager.Common.Cue;
using PSXPackager.Common.Iso;

namespace Popstation.Media
{
    /// <summary>
    /// The disc a cue sheet's tracks are read from, presented as raw 2352-byte sectors.
    /// </summary>
    /// <remarks>
    /// A cue sheet names its data one of two ways: a file on disk - a .bin, .img, .iso or .chd,
    /// each with its own layout - or a "pbp://" URI naming one disc inside an EBOOT. Callers do
    /// not need to know which. Every source reads the same way, and disposing the source releases
    /// whatever had to be opened to produce it.
    /// </remarks>
    public sealed class DiscSource : IDisposable
    {
        private static readonly Regex PbpUriPattern =
            new Regex(@"^pbp://(?<pbp>.*\.pbp)/disc(?<disc>\d+)$", RegexOptions.IgnoreCase);

        private DiscSource(Stream stream, string name)
        {
            Stream = stream;
            Name = name;
        }

        /// <summary>The disc, as a stream of raw 2352-byte sectors.</summary>
        public Stream Stream { get; }

        /// <summary>Where the disc came from, for messages.</summary>
        public string Name { get; }

        /// <summary>
        /// The size of the whole disc image. For a .chd or a cooked .iso this is larger than the
        /// file on disk, because both hold the disc in a more compact form.
        /// </summary>
        public long Length => Stream.Length;

        /// <summary>
        /// Opens the disc holding a cue sheet track. A relative FILE entry is resolved against the
        /// cue sheet that named it.
        /// </summary>
        public static DiscSource ForTrack(CueTrack track)
        {
            if (track?.FileEntry == null)
            {
                throw new ArgumentException("The track does not belong to a cue sheet", nameof(track));
            }

            var cuePath = track.FileEntry.CueFile?.Path;

            var basePath = string.IsNullOrEmpty(cuePath) ? null : Path.GetDirectoryName(cuePath);

            return Open(track.FileEntry.FileName, basePath);
        }

        /// <summary>
        /// Opens a disc named by a path or a "pbp://" URI.
        /// </summary>
        /// <param name="source">The image path or URI.</param>
        /// <param name="basePath">The directory a relative path is resolved against.</param>
        public static DiscSource Open(string source, string basePath = null)
        {
            if (string.IsNullOrEmpty(source))
            {
                throw new ArgumentException("No disc was named", nameof(source));
            }

            if (TryParsePbpUri(source, out var pbpPath, out var discIndex))
            {
                return OpenPbp(pbpPath, discIndex);
            }

            var path = source;

            if (!Path.IsPathFullyQualified(path) && !string.IsNullOrEmpty(basePath))
            {
                path = Path.Combine(basePath, path);
            }

            // Whatever the layout, this hands back raw sectors
            return new DiscSource(DiscImage.OpenRead(path), path);
        }

        /// <summary>
        /// Recognises the "pbp://{path}/disc{n}" form that names one disc inside an EBOOT.
        /// </summary>
        public static bool TryParsePbpUri(string source, out string pbpPath, out int discIndex)
        {
            pbpPath = null;
            discIndex = 0;

            if (string.IsNullOrEmpty(source)) return false;

            var match = PbpUriPattern.Match(source);

            if (!match.Success) return false;

            pbpPath = match.Groups["pbp"].Value;
            discIndex = int.Parse(match.Groups["disc"].Value);

            return true;
        }

        private static DiscSource OpenPbp(string path, int discIndex)
        {
            var file = new FileStream(path, FileMode.Open, FileAccess.Read);

            try
            {
                var reader = new PbpReader(file);

                if (discIndex < 0 || discIndex >= reader.Discs.Count)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(discIndex),
                        $"{path} holds {reader.Discs.Count} disc(s), so there is no disc {discIndex + 1}");
                }

                // The stream takes ownership, so closing it closes the EBOOT too
                var stream = new PbpDiscStream(reader.Discs[discIndex], true);

                return new DiscSource(stream, $"{path} disc {discIndex + 1}");
            }
            catch
            {
                file.Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
