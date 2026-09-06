using NAudio.Lame;
using NAudio.Wave;
using Popstation.Media;
using PSXPackager.Common.Cue;

namespace PSXPackager.Audio
{
    public enum CDAudioFormat
    {
        Wav,
        Mp3
    }

    /// <summary>How far an extraction has got.</summary>
    public readonly struct CDAudioExtractProgress
    {
        public CDAudioExtractProgress(long bytesRead, long totalBytes)
        {
            BytesRead = bytesRead;
            TotalBytes = totalBytes;
        }

        /// <summary>How much of the track has been read, in bytes of raw CD audio.</summary>
        public long BytesRead { get; }

        /// <summary>The size of the whole track, in bytes of raw CD audio.</summary>
        public long TotalBytes { get; }

        public double Percent => TotalBytes == 0 ? 0 : BytesRead / (double)TotalBytes * 100;
    }

    /// <summary>
    /// Writes a cue sheet's audio track out as a WAV or an MP3.
    /// </summary>
    /// <remarks>
    /// A CD audio track is already 44.1kHz 16-bit stereo PCM, so writing a WAV only wraps the
    /// sectors in a header. An MP3 is encoded from those same sectors by LAME.
    /// </remarks>
    public static class CDAudioExtractor
    {
        const int SectorSize = 2352;

        // Reporting every sector would be thousands of updates for a single track, more than
        // anything watching has any use for
        const int SectorsPerProgressReport = 64;

        // Red Book audio: 44.1kHz, 16-bit, stereo
        public static WaveFormat WaveFormat => new WaveFormat(44100, 16, 2);

        /// <summary>The extension a format is normally saved with, including the dot.</summary>
        public static string GetExtension(CDAudioFormat format) => format switch
        {
            CDAudioFormat.Mp3 => ".mp3",
            _ => ".wav"
        };

        /// <summary>
        /// Extracts one audio track to a file.
        /// </summary>
        /// <param name="track">The track to extract. It must be an AUDIO track.</param>
        /// <param name="path">The file to write.</param>
        /// <param name="format">Whether to write a WAV or an MP3.</param>
        /// <param name="bitRate">The MP3 bit rate in kbps. Ignored when writing a WAV.</param>
        /// <param name="progress">
        /// Told how far the extraction has got, if given. Reported every so often rather than for
        /// every sector, so a caller can put it straight on screen.
        /// </param>
        /// <param name="cancellationToken">Stops the extraction part way.</param>
        public static void Extract(
            CueTrack track,
            string path,
            CDAudioFormat format,
            int bitRate = 192,
            IProgress<CDAudioExtractProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (track == null) throw new ArgumentNullException(nameof(track));

            if (!string.Equals(track.DataType, "AUDIO", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Track {track.Number} is not an audio track");
            }

            // Whether the track lives in a .bin, a .chd or a disc inside an EBOOT, it reads the same
            using var source = DiscSource.ForTrack(track);

            var (startSector, endSector) = track.GetSectorRange(source.Length);

            using Stream writer = format == CDAudioFormat.Mp3
                ? new LameMP3FileWriter(path, WaveFormat, bitRate)
                : new WaveFileWriter(path, WaveFormat);

            Write(source, writer, startSector, endSector, progress, cancellationToken);
        }

        private static void Write(
            DiscSource source,
            Stream writer,
            int startSector,
            int endSector,
            IProgress<CDAudioExtractProgress>? progress,
            CancellationToken cancellationToken)
        {
            var total = (long)(endSector - startSector) * SectorSize;

            source.Stream.Seek((long)startSector * SectorSize, SeekOrigin.Begin);

            var sector = new byte[SectorSize];
            long written = 0;

            for (var currentSector = startSector; currentSector < endSector; currentSector++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (source.Stream.Read(sector, 0, sector.Length) != sector.Length)
                {
                    // The disc ran out before the cue sheet said it would
                    break;
                }

                writer.Write(sector, 0, sector.Length);

                written += sector.Length;

                if ((currentSector - startSector) % SectorsPerProgressReport == 0)
                {
                    progress?.Report(new CDAudioExtractProgress(written, total));
                }
            }

            writer.Flush();

            progress?.Report(new CDAudioExtractProgress(written, total));
        }
    }
}
