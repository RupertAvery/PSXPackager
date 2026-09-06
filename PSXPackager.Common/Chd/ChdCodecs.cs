using System;
using System.IO;
using System.IO.Compression;
using PSXPackager.Common.Iso;
using SharpCompress.Compressors.LZMA;
using ZstdSharp;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// The codec identifiers a CHD header can name. A v5 file names up to four of them by tag and
    /// picks the cheapest one per hunk; a v1-v4 file names a single one by number.
    /// </summary>
    internal static class ChdCompression
    {
        public const uint LegacyNone = 0;
        public const uint LegacyZlib = 1;
        public const uint LegacyZlibPlus = 2;
        public const uint LegacyAv = 3;

        public const uint Zlib = 0x7A6C6962;    // "zlib"
        public const uint Lzma = 0x6C7A6D61;    // "lzma"
        public const uint Huffman = 0x68756666; // "huff"
        public const uint Flac = 0x666C6163;    // "flac"
        public const uint Zstd = 0x7A737464;    // "zstd"
        public const uint CdZlib = 0x63647A6C;  // "cdzl"
        public const uint CdLzma = 0x63646C7A;  // "cdlz"
        public const uint CdFlac = 0x6364666C;  // "cdfl"
        public const uint CdZstd = 0x63647A73;  // "cdzs"
        public const uint AvHuff = 0x61766875;  // "avhu"

        /// <summary>
        /// Renders a codec identifier the way the CHD tools name it, for error messages.
        /// </summary>
        public static string Describe(uint compression)
        {
            switch (compression)
            {
                case LegacyNone: return "none";
                case LegacyZlib: return "zlib";
                case LegacyZlibPlus: return "zlib+";
                case LegacyAv: return "avhuff";
            }

            return new string(new[]
            {
                (char)((compression >> 24) & 0xFF),
                (char)((compression >> 16) & 0xFF),
                (char)((compression >> 8) & 0xFF),
                (char)(compression & 0xFF)
            });
        }
    }

    /// <summary>
    /// Decompresses a single hunk.
    /// </summary>
    internal abstract class ChdCodec
    {
        public abstract void Decompress(
            byte[] source,
            int sourceOffset,
            int sourceLength,
            byte[] destination,
            int destinationLength);

        /// <summary>
        /// Creates the codec named by a v5 tag or a v1-v4 compression number.
        /// </summary>
        public static ChdCodec Create(uint compression, int hunkBytes)
        {
            switch (compression)
            {
                case ChdCompression.LegacyNone:
                    return new ChdNoneCodec();

                case ChdCompression.LegacyZlib:
                case ChdCompression.LegacyZlibPlus:
                case ChdCompression.Zlib:
                    return new ChdZlibCodec();

                case ChdCompression.Lzma:
                    return new ChdLzmaCodec(hunkBytes);

                case ChdCompression.Zstd:
                    return new ChdZstdCodec();

                case ChdCompression.CdZlib:
                    return new ChdCdCodec(new ChdZlibCodec(), hunkBytes);

                case ChdCompression.CdLzma:
                    return new ChdCdCodec(new ChdLzmaCodec(hunkBytes), hunkBytes);

                case ChdCompression.CdZstd:
                    return new ChdCdCodec(new ChdZstdCodec(), hunkBytes);

                case ChdCompression.CdFlac:
                    return new ChdCdFlacCodec(hunkBytes);

                default:
                    throw new InvalidChdException(
                        "The CHD uses the unsupported compression codec " + ChdCompression.Describe(compression));
            }
        }
    }

    /// <summary>Stores the hunk verbatim.</summary>
    internal sealed class ChdNoneCodec : ChdCodec
    {
        public override void Decompress(byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationLength)
        {
            if (sourceLength < destinationLength)
            {
                throw new InvalidChdException("An uncompressed hunk is shorter than the hunk size");
            }

            Buffer.BlockCopy(source, sourceOffset, destination, 0, destinationLength);
        }
    }

    /// <summary>Raw deflate, without the zlib wrapper.</summary>
    internal sealed class ChdZlibCodec : ChdCodec
    {
        public override void Decompress(byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationLength)
        {
            using (var input = new MemoryStream(source, sourceOffset, sourceLength, false))
            using (var deflate = new DeflateStream(input, CompressionMode.Decompress))
            {
                ChdCodecStream.ReadExactly(deflate, destination, destinationLength, "deflate");
            }
        }
    }

    /// <summary>
    /// Raw LZMA1. The stream carries no properties of its own, so they are derived from the hunk
    /// size the same way the encoder derived them.
    /// </summary>
    internal sealed class ChdLzmaCodec : ChdCodec
    {
        private readonly byte[] _properties;

        public ChdLzmaCodec(int hunkBytes)
        {
            _properties = BuildProperties(hunkBytes);
        }

        public override void Decompress(byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationLength)
        {
            using (var input = new MemoryStream(source, sourceOffset, sourceLength, false))
            using (var lzma = new LzmaStream(_properties, input, sourceLength, destinationLength))
            {
                ChdCodecStream.ReadExactly(lzma, destination, destinationLength, "LZMA");
            }
        }

        /// <summary>
        /// Rebuilds the 5-byte LZMA property block the encoder would have produced: the defaults
        /// for preset level 9 (lc=3, lp=0, pb=2) and a dictionary reduced to fit a single hunk.
        /// </summary>
        private static byte[] BuildProperties(int hunkBytes)
        {
            const int literalContextBits = 3;
            const int literalPositionBits = 0;
            const int positionBits = 2;

            var dictionarySize = GetDictionarySize(hunkBytes);

            return new[]
            {
                (byte)((positionBits * 5 + literalPositionBits) * 9 + literalContextBits),
                (byte)dictionarySize,
                (byte)(dictionarySize >> 8),
                (byte)(dictionarySize >> 16),
                (byte)(dictionarySize >> 24)
            };
        }

        private static uint GetDictionarySize(int hunkBytes)
        {
            // Preset level 9 asks for a 1GB dictionary, which the encoder then shrinks to the
            // smallest 2^n or 3*2^n that still covers everything it will be given
            var dictionarySize = 1u << 30;

            if (dictionarySize <= (uint)hunkBytes) return dictionarySize;

            for (var i = 11; i <= 30; i++)
            {
                if ((uint)hunkBytes <= 2u << i) return 2u << i;
                if ((uint)hunkBytes <= 3u << i) return 3u << i;
            }

            return dictionarySize;
        }
    }

    /// <summary>Zstandard, used by CHDs written by newer versions of the CHD tools.</summary>
    internal sealed class ChdZstdCodec : ChdCodec
    {
        private readonly Decompressor _decompressor = new Decompressor();

        public override void Decompress(byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationLength)
        {
            var written = _decompressor.Unwrap(
                new ReadOnlySpan<byte>(source, sourceOffset, sourceLength),
                new Span<byte>(destination, 0, destinationLength));

            if (written != destinationLength)
            {
                throw new InvalidChdException(
                    $"A Zstandard hunk decompressed to {written} bytes instead of {destinationLength}");
            }
        }
    }

    /// <summary>
    /// The CD flavour of the general purpose codecs. Sector data and subcode are compressed
    /// separately, and a data sector's sync header and ECC are left out of the file and recomputed
    /// here.
    /// </summary>
    internal sealed class ChdCdCodec : ChdCodec
    {
        private readonly ChdCodec _sectorCodec;
        private readonly byte[] _sectors;

        public ChdCdCodec(ChdCodec sectorCodec, int hunkBytes)
        {
            _sectorCodec = sectorCodec;
            _sectors = new byte[hunkBytes / ChdConstants.FrameSize * ChdConstants.SectorDataSize];
        }

        public override void Decompress(byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationLength)
        {
            var frames = destinationLength / ChdConstants.FrameSize;

            // One flag bit per frame says whether its sync header and ECC were dropped, followed
            // by the compressed length of the sector data. The subcode stream follows that.
            var flagBytes = (frames + 7) / 8;
            var lengthBytes = destinationLength < 65536 ? 2 : 3;
            var headerBytes = flagBytes + lengthBytes;

            if (sourceLength < headerBytes)
            {
                throw new InvalidChdException("A CD hunk is too short to hold its header");
            }

            var sectorLength = (source[sourceOffset + flagBytes] << 8) | source[sourceOffset + flagBytes + 1];

            if (lengthBytes > 2)
            {
                sectorLength = (sectorLength << 8) | source[sourceOffset + flagBytes + 2];
            }

            if (sourceLength < headerBytes + sectorLength)
            {
                throw new InvalidChdException("A CD hunk is shorter than the length it declares");
            }

            _sectorCodec.Decompress(
                source,
                sourceOffset + headerBytes,
                sectorLength,
                _sectors,
                frames * ChdConstants.SectorDataSize);

            for (var frame = 0; frame < frames; frame++)
            {
                var sector = frame * ChdConstants.FrameSize;

                Buffer.BlockCopy(_sectors, frame * ChdConstants.SectorDataSize, destination, sector, ChdConstants.SectorDataSize);

                // The subcode is compressed separately and is not needed to rebuild an image
                Array.Clear(destination, sector + ChdConstants.SectorDataSize, ChdConstants.SubcodeSize);

                if ((source[sourceOffset + frame / 8] & (1 << (frame % 8))) != 0)
                {
                    Array.Copy(ChdConstants.SyncHeader, 0, destination, sector, ChdConstants.SyncHeader.Length);

                    // A Mode 2 sector's parity covers the header as zeroes; a Mode 1 sector's does not
                    EccEdc.WriteEcc(destination, sector, destination[sector + 15] == 2);
                }
            }
        }
    }

    /// <summary>
    /// The FLAC codec, chosen for hunks that are mostly CD audio. Unlike the other CD codecs it
    /// has no header of its own: the FLAC stream starts at the first byte, and the subcode, which
    /// we do not need, follows it.
    /// </summary>
    internal sealed class ChdCdFlacCodec : ChdCodec
    {
        private readonly ChdFlacDecoder _decoder = new ChdFlacDecoder();
        private readonly byte[] _sectors;

        public ChdCdFlacCodec(int hunkBytes)
        {
            _sectors = new byte[hunkBytes / ChdConstants.FrameSize * ChdConstants.SectorDataSize];
        }

        public override void Decompress(byte[] source, int sourceOffset, int sourceLength, byte[] destination, int destinationLength)
        {
            var frames = destinationLength / ChdConstants.FrameSize;
            var sampleBytes = frames * ChdConstants.SectorDataSize;

            _decoder.Decode(
                source,
                sourceOffset,
                sourceLength,
                ChdFlacDecoder.GetBlockSize(sampleBytes),
                _sectors,
                sampleBytes);

            for (var frame = 0; frame < frames; frame++)
            {
                var sector = frame * ChdConstants.FrameSize;

                Buffer.BlockCopy(_sectors, frame * ChdConstants.SectorDataSize, destination, sector, ChdConstants.SectorDataSize);
                Array.Clear(destination, sector + ChdConstants.SectorDataSize, ChdConstants.SubcodeSize);
            }
        }
    }

    internal static class ChdCodecStream
    {
        public static void ReadExactly(Stream stream, byte[] destination, int count, string codec)
        {
            var read = 0;

            while (read < count)
            {
                var bytes = stream.Read(destination, read, count - read);
                if (bytes <= 0) break;
                read += bytes;
            }

            if (read != count)
            {
                throw new InvalidChdException($"A {codec} hunk decompressed to {read} bytes instead of {count}");
            }
        }
    }
}
