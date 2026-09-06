using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// The sector layouts a CHD track can declare.
    /// </summary>
    public enum ChdTrackType
    {
        Mode1,
        Mode1Raw,
        Mode2,
        Mode2Form1,
        Mode2Form2,
        Mode2FormMix,
        Mode2Raw,
        Audio
    }

    /// <summary>
    /// One track of a CD image, placed both in the file and on the disc it represents.
    /// </summary>
    public sealed class ChdTrack
    {
        public int Number { get; set; }

        public ChdTrackType Type { get; set; }

        /// <summary>The bytes stored for each frame of this track, which may be less than a full sector.</summary>
        public int DataSize { get; set; }

        /// <summary>The number of frames stored in the file for this track.</summary>
        public int Frames { get; set; }

        /// <summary>Frames of padding after the track, which round it up to a 4 frame boundary.</summary>
        public int ExtraFrames { get; set; }

        public int Pregap { get; set; }

        public int Postgap { get; set; }

        /// <summary>
        /// True when the pregap is part of the stored data, which is how a cue sheet with an
        /// INDEX 00 is stored. When false the pregap is not in the file and has to be generated.
        /// </summary>
        public bool PregapStored { get; set; }

        /// <summary>The first frame of this track within the file's frame sequence.</summary>
        public long FileFrame { get; set; }

        /// <summary>The disc address of the first frame stored for this track.</summary>
        public long StartFrame { get; set; }

        /// <summary>The disc address of INDEX 01, where the track proper begins.</summary>
        public long IndexOneFrame { get; set; }

        public bool IsAudio => Type == ChdTrackType.Audio;

        public override string ToString()
        {
            return $"Track {Number} {Type} frames {Frames} at {StartFrame}";
        }
    }

    /// <summary>
    /// The table of contents of a CD image stored in a CHD, read from the file's metadata.
    /// </summary>
    public sealed class ChdCdToc
    {
        // Metadata tags, four characters packed big-endian
        private const uint TrackTag = 0x43485452;    // "CHTR"
        private const uint Track2Tag = 0x43485432;   // "CHT2"
        private const uint GdRomTag = 0x43484744;    // "CHGD"
        private const uint GdRomOldTag = 0x43484754; // "CHGT"

        private ChdCdToc(IReadOnlyList<ChdTrack> tracks, long totalFrames)
        {
            Tracks = tracks;
            TotalFrames = totalFrames;
        }

        public IReadOnlyList<ChdTrack> Tracks { get; }

        /// <summary>The length of the disc this image represents, in frames.</summary>
        public long TotalFrames { get; }

        /// <summary>
        /// Reads the track list from a CHD's metadata and works out where each track sits both in
        /// the file and on the disc.
        /// </summary>
        public static ChdCdToc Parse(ChdFile chd)
        {
            var tracks = new List<ChdTrack>();

            foreach (var entry in chd.Metadata)
            {
                // A GD-ROM lays its tracks out differently and, in its older form, holds audio the
                // other way round. Neither belongs on a PlayStation disc, so rather than produce a
                // quietly wrong image, say so.
                if (entry.Tag == GdRomTag || entry.Tag == GdRomOldTag)
                {
                    throw new InvalidChdException(
                        "The CHD holds a GD-ROM image, which is not a PlayStation disc");
                }

                if (entry.Tag != TrackTag && entry.Tag != Track2Tag) continue;

                tracks.Add(ParseTrack(entry.GetText()));
            }

            if (tracks.Count == 0)
            {
                throw new InvalidChdException(
                    "The CHD holds no CD track metadata, so it is not a CD image");
            }

            tracks = tracks.OrderBy(track => track.Number).ToList();

            var totalFrames = Layout(tracks);

            return new ChdCdToc(tracks, totalFrames);
        }

        /// <summary>
        /// Places every track, following the same rules the CHD tools use.
        /// </summary>
        /// <remarks>
        /// Two positions matter and they drift apart. Within the file each track is padded out to
        /// a 4 frame boundary, so a track starts later in the file than its length alone suggests.
        /// On the disc, a pregap that was not stored still occupies addresses, so a track starts
        /// later on the disc than its stored data accounts for.
        /// </remarks>
        private static long Layout(IReadOnlyList<ChdTrack> tracks)
        {
            long fileFrame = 0;
            long discFrame = 0;

            foreach (var track in tracks)
            {
                if (!track.PregapStored)
                {
                    // The gap is not in the file, but the disc addresses still pass through it
                    discFrame += track.Pregap;
                }

                track.FileFrame = fileFrame;
                track.StartFrame = discFrame;
                track.IndexOneFrame = discFrame + (track.PregapStored ? track.Pregap : 0);

                // A postgap belongs to the space before the next track
                discFrame += track.Postgap;
                discFrame += track.Frames;

                fileFrame += track.Frames + track.ExtraFrames;
            }

            return discFrame;
        }

        private static ChdTrack ParseTrack(string metadata)
        {
            var fields = ParseFields(metadata);

            var track = new ChdTrack
            {
                Number = GetInt(fields, "TRACK"),
                Frames = GetInt(fields, "FRAMES"),
                Pregap = GetInt(fields, "PREGAP"),
                Postgap = GetInt(fields, "POSTGAP")
            };

            if (!fields.TryGetValue("TYPE", out var type))
            {
                throw new InvalidChdException("A CD track in the CHD does not declare a type");
            }

            track.Type = ParseTrackType(type);
            track.DataSize = GetDataSize(track.Type);

            // A pregap type prefixed with V means the pregap's data is stored in the file
            if (track.Pregap > 0 && fields.TryGetValue("PGTYPE", out var pregapType))
            {
                track.PregapStored = pregapType.StartsWith("V", StringComparison.Ordinal);
            }

            // Tracks are padded out to a whole number of 4 frame groups within the file
            var padded = (track.Frames + ChdConstants.TrackPadding - 1)
                         / ChdConstants.TrackPadding * ChdConstants.TrackPadding;

            track.ExtraFrames = padded - track.Frames;

            return track;
        }

        private static Dictionary<string, string> ParseFields(string metadata)
        {
            var fields = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var token in metadata.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = token.IndexOf(':');
                if (separator <= 0) continue;

                fields[token.Substring(0, separator)] = token.Substring(separator + 1);
            }

            return fields;
        }

        private static int GetInt(IReadOnlyDictionary<string, string> fields, string key)
        {
            if (!fields.TryGetValue(key, out var value)) return 0;

            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0;
        }

        private static ChdTrackType ParseTrackType(string type)
        {
            switch (type)
            {
                case "MODE1":
                case "MODE1/2048":
                    return ChdTrackType.Mode1;

                case "MODE1_RAW":
                case "MODE1/2352":
                    return ChdTrackType.Mode1Raw;

                case "MODE2":
                case "MODE2/2336":
                    return ChdTrackType.Mode2;

                case "MODE2_FORM1":
                case "MODE2/2048":
                    return ChdTrackType.Mode2Form1;

                case "MODE2_FORM2":
                case "MODE2/2324":
                    return ChdTrackType.Mode2Form2;

                case "MODE2_FORM_MIX":
                    return ChdTrackType.Mode2FormMix;

                case "MODE2_RAW":
                case "MODE2/2352":
                    return ChdTrackType.Mode2Raw;

                case "AUDIO":
                    return ChdTrackType.Audio;

                default:
                    throw new InvalidChdException($"The CHD holds a track of the unknown type '{type}'");
            }
        }

        private static int GetDataSize(ChdTrackType type)
        {
            switch (type)
            {
                case ChdTrackType.Mode1:
                case ChdTrackType.Mode2Form1:
                    return 2048;

                case ChdTrackType.Mode2Form2:
                    return 2324;

                case ChdTrackType.Mode2:
                case ChdTrackType.Mode2FormMix:
                    return 2336;

                default:
                    return ChdConstants.SectorDataSize;
            }
        }
    }
}
