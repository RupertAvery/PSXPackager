using System;
using System.IO;
using NAudio.Flac;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// Decodes the raw FLAC streams CHD stores for CD audio.
    /// </summary>
    /// <remarks>
    /// A CHD does not store the "fLaC" marker or the STREAMINFO block - only the frames - so a
    /// header describing the stream is synthesised and prepended before handing the data to the
    /// decoder. Samples are written back out big-endian, which is the byte order a CHD hunk holds.
    /// </remarks>
    internal sealed class ChdFlacDecoder
    {
        private const int SampleRate = 44100;
        private const int Channels = 2;

        /// <summary>
        /// A "fLaC" marker followed by a last-block STREAMINFO of 0x22 bytes. The block size,
        /// sample rate and channel count are patched in; the frame sizes, sample count and MD5
        /// are all left as "unknown".
        /// </summary>
        private static readonly byte[] HeaderTemplate =
        {
            0x66, 0x4C, 0x61, 0x43,                         // +00: 'fLaC'
            0x80,                                           // +04: STREAMINFO, last block
            0x00, 0x00, 0x22,                               // +05: block length
            0x00, 0x00,                                     // +08: minimum block size
            0x00, 0x00,                                     // +0A: maximum block size
            0x00, 0x00, 0x00,                               // +0C: minimum frame size (unknown)
            0x00, 0x00, 0x00,                               // +0F: maximum frame size (unknown)
            0x0A, 0xC4, 0x42, 0xF0, 0x00, 0x00, 0x00, 0x00, // +12: rate, channels, bits, samples
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // +1A: MD5 (unknown)
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        private byte[] _stream = Array.Empty<byte>();

        /// <summary>
        /// The block size a CHD encoder uses for a hunk holding <paramref name="sampleBytes"/>
        /// bytes of audio.
        /// </summary>
        public static int GetBlockSize(int sampleBytes)
        {
            var blockSize = sampleBytes / 4;

            while (blockSize > ChdConstants.SectorDataSize)
            {
                blockSize /= 2;
            }

            return blockSize;
        }

        /// <summary>
        /// Decodes <paramref name="destinationLength"/> bytes of big-endian 16-bit stereo samples.
        /// </summary>
        public void Decode(
            byte[] source,
            int sourceOffset,
            int sourceLength,
            int blockSize,
            byte[] destination,
            int destinationLength)
        {
            PrependHeader(source, sourceOffset, sourceLength, blockSize);

            var read = 0;

            using (var input = new MemoryStream(_stream, 0, HeaderTemplate.Length + sourceLength, false))
            {
                FlacReader reader = null;

                try
                {
                    reader = new FlacReader(input, FlacPreScanMode.None);

                    while (read < destinationLength)
                    {
                        var count = reader.Read(destination, read, destinationLength - read);
                        if (count <= 0) break;
                        read += count;
                    }
                }
                catch (Exception ex) when (read >= destinationLength)
                {
                    // The audio we needed is already decoded; the decoder ran on into the subcode
                    // data that follows the FLAC stream, which we do not use.
                    _ = ex;
                }
                catch (Exception ex)
                {
                    throw new InvalidChdException($"Failed to decode the FLAC data in a hunk: {ex.Message}");
                }
                finally
                {
                    reader?.Dispose();
                }
            }

            if (read < destinationLength)
            {
                throw new InvalidChdException("The FLAC data in a hunk ended before the hunk was filled");
            }

            SwapSampleBytes(destination, destinationLength);
        }

        private void PrependHeader(byte[] source, int sourceOffset, int sourceLength, int blockSize)
        {
            var total = HeaderTemplate.Length + sourceLength;

            if (_stream.Length < total)
            {
                _stream = new byte[total];
            }

            Array.Copy(HeaderTemplate, 0, _stream, 0, HeaderTemplate.Length);

            // The reference encoder writes the interleaved sample count, not the block size, into
            // the block size fields; the frame headers carry the real value either way.
            var declaredBlockSize = blockSize * Channels;

            _stream[0x08] = _stream[0x0A] = (byte)(declaredBlockSize >> 8);
            _stream[0x09] = _stream[0x0B] = (byte)declaredBlockSize;
            // 20 bits of sample rate, then 3 bits of channel count, then the sample width, which
            // the template already carries as 16 bits
            _stream[0x12] = (byte)((SampleRate >> 12) & 0xFF);
            _stream[0x13] = (byte)((SampleRate >> 4) & 0xFF);
            _stream[0x14] = (byte)(((SampleRate << 4) | ((Channels - 1) << 1)) & 0xFF);

            Array.Copy(source, sourceOffset, _stream, HeaderTemplate.Length, sourceLength);
        }

        /// <summary>
        /// Converts the decoder's little-endian samples to the big-endian order a hunk stores.
        /// </summary>
        private static void SwapSampleBytes(byte[] buffer, int length)
        {
            for (var i = 0; i + 1 < length; i += 2)
            {
                (buffer[i], buffer[i + 1]) = (buffer[i + 1], buffer[i]);
            }
        }
    }
}
