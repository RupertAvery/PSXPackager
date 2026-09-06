using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// Reads a Compressed Hunks of Data (.chd) file: the header, the hunk map, the metadata chain
    /// and the hunks themselves.
    /// </summary>
    /// <remarks>
    /// Versions 3, 4 and 5 are supported. A v5 file names up to four codecs in its header and
    /// records which one each hunk used, so a single file mixes codecs freely; v3 and v4 name one
    /// codec for the whole file. Files that reference a parent CHD are rejected, since the data
    /// they omit lives in another file entirely.
    /// </remarks>
    public sealed class ChdFile : IDisposable
    {
        private static readonly byte[] MagicTag = Encoding.ASCII.GetBytes("MComprHD");

        private const int MetadataHeaderSize = 16;
        private const int LegacyMapEntrySize = 16;

        /// <summary>How many decoded hunks to keep, so a self-referencing hunk is cheap.</summary>
        private const int HunkCacheSize = 8;

        private enum HunkKind : byte
        {
            /// <summary>Compressed with the codec in <see cref="_hunkCodec"/>.</summary>
            Compressed,

            /// <summary>Stored verbatim in the file.</summary>
            Uncompressed,

            /// <summary>Identical to another hunk in this file, named by its index.</summary>
            Self,

            /// <summary>An 8-byte pattern repeated to fill the hunk.</summary>
            Mini,

            /// <summary>All zeroes.</summary>
            Zero
        }

        private readonly Stream _stream;
        private readonly bool _leaveOpen;
        private readonly ChdCodec[] _codecs = new ChdCodec[4];

        private HunkKind[] _hunkKind;
        private byte[] _hunkCodec;
        private long[] _hunkOffset;
        private uint[] _hunkLength;

        private byte[] _compressed = Array.Empty<byte>();

        private readonly int[] _cacheIndex = new int[HunkCacheSize];
        private readonly byte[][] _cacheData = new byte[HunkCacheSize][];
        private int _cacheNext;
        private int _reentrancy;

        private ChdFile(Stream stream, bool leaveOpen)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;

            for (var i = 0; i < HunkCacheSize; i++)
            {
                _cacheIndex[i] = -1;
            }
        }

        public int Version { get; private set; }

        /// <summary>The size of a decompressed hunk.</summary>
        public int HunkBytes { get; private set; }

        /// <summary>The size of the smallest addressable unit; a CD image uses one frame.</summary>
        public int UnitBytes { get; private set; }

        public int HunkCount { get; private set; }

        /// <summary>The size of the data the file represents, before compression.</summary>
        public long LogicalBytes { get; private set; }

        public IReadOnlyList<ChdMetadata> Metadata { get; private set; } = Array.Empty<ChdMetadata>();

        /// <summary>
        /// True if the file starts with the CHD signature.
        /// </summary>
        public static bool IsChd(Stream stream)
        {
            if (!stream.CanSeek || stream.Length < MagicTag.Length) return false;

            var position = stream.Position;

            try
            {
                var tag = new byte[MagicTag.Length];
                stream.Position = 0;

                if (!ReadFully(stream, tag, tag.Length)) return false;

                for (var i = 0; i < MagicTag.Length; i++)
                {
                    if (tag[i] != MagicTag[i]) return false;
                }

                return true;
            }
            finally
            {
                stream.Position = position;
            }
        }

        /// <summary>
        /// True if the file at <paramref name="path"/> starts with the CHD signature.
        /// </summary>
        public static bool IsChd(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                return IsChd(stream);
            }
        }

        public static ChdFile Open(string path)
        {
            var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);

            try
            {
                return Open(stream, false);
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        public static ChdFile Open(Stream stream, bool leaveOpen = false)
        {
            var chd = new ChdFile(stream, leaveOpen);
            chd.ReadHeader();
            chd.ReadMetadata();
            return chd;
        }

        /// <summary>
        /// Returns the decoded contents of a hunk. The array is owned by this reader and is reused,
        /// so callers must not modify it or hold on to it across another call.
        /// </summary>
        public byte[] GetHunk(int hunkIndex)
        {
            if (hunkIndex < 0 || hunkIndex >= HunkCount)
            {
                throw new ArgumentOutOfRangeException(nameof(hunkIndex));
            }

            for (var i = 0; i < HunkCacheSize; i++)
            {
                if (_cacheIndex[i] == hunkIndex) return _cacheData[i];
            }

            var slot = _cacheNext;
            _cacheNext = (_cacheNext + 1) % HunkCacheSize;

            var destination = _cacheData[slot] ?? (_cacheData[slot] = new byte[HunkBytes]);

            // Mark the slot as empty first: if decoding throws we must not leave stale data
            // claiming to be this hunk
            _cacheIndex[slot] = -1;

            DecodeHunk(hunkIndex, destination);

            _cacheIndex[slot] = hunkIndex;

            return destination;
        }

        private void DecodeHunk(int hunkIndex, byte[] destination)
        {
            switch (_hunkKind[hunkIndex])
            {
                case HunkKind.Compressed:
                {
                    var length = (int)_hunkLength[hunkIndex];

                    if (_compressed.Length < length)
                    {
                        _compressed = new byte[length];
                    }

                    Seek(_hunkOffset[hunkIndex], length);
                    ReadOrThrow(_compressed, length);

                    var codec = _codecs[_hunkCodec[hunkIndex]]
                                ?? throw new InvalidChdException("A hunk names a codec the header does not declare");

                    codec.Decompress(_compressed, 0, length, destination, HunkBytes);
                    break;
                }

                case HunkKind.Uncompressed:
                    Seek(_hunkOffset[hunkIndex], HunkBytes);
                    ReadOrThrow(destination, HunkBytes);
                    break;

                case HunkKind.Self:
                {
                    var target = (int)_hunkOffset[hunkIndex];

                    if (target < 0 || target >= HunkCount || target == hunkIndex)
                    {
                        throw new InvalidChdException("A hunk refers to a hunk outside the file");
                    }

                    // A self-reference normally points straight at a stored hunk. Following a
                    // chain of them is legal, but a cycle would never terminate.
                    _reentrancy++;

                    try
                    {
                        if (_reentrancy > 4)
                        {
                            throw new InvalidChdException("The hunk map contains a circular self-reference");
                        }

                        Buffer.BlockCopy(GetHunk(target), 0, destination, 0, HunkBytes);
                    }
                    finally
                    {
                        _reentrancy--;
                    }

                    break;
                }

                case HunkKind.Mini:
                {
                    WriteBigEndianUInt64(destination, 0, (ulong)_hunkOffset[hunkIndex]);

                    for (var i = 8; i < HunkBytes; i++)
                    {
                        destination[i] = destination[i - 8];
                    }

                    break;
                }

                default:
                    Array.Clear(destination, 0, HunkBytes);
                    break;
            }
        }

        private void ReadHeader()
        {
            var prefix = new byte[16];
            _stream.Position = 0;
            ReadOrThrow(prefix, prefix.Length);

            for (var i = 0; i < MagicTag.Length; i++)
            {
                if (prefix[i] != MagicTag[i])
                {
                    throw new InvalidChdException("The file is not a CHD image");
                }
            }

            var length = (int)ReadBigEndianUInt32(prefix, 8);
            Version = (int)ReadBigEndianUInt32(prefix, 12);

            if (Version < 3 || Version > 5)
            {
                throw new InvalidChdException(
                    $"CHD version {Version} is not supported; only versions 3 to 5 can be read");
            }

            if (length < 16 || length > 4096)
            {
                throw new InvalidChdException("The CHD header length is out of range");
            }

            var header = new byte[length];
            _stream.Position = 0;
            ReadOrThrow(header, length);

            if (Version == 5)
            {
                ReadHeaderV5(header);
            }
            else
            {
                ReadHeaderV34(header);
            }

            if (HunkBytes <= 0 || UnitBytes <= 0 || HunkCount <= 0)
            {
                throw new InvalidChdException("The CHD header describes an empty or invalid image");
            }
        }

        private void ReadHeaderV5(byte[] header)
        {
            for (var i = 0; i < 4; i++)
            {
                var compression = ReadBigEndianUInt32(header, 16 + i * 4);

                if (compression != 0)
                {
                    _codecs[i] = ChdCodec.Create(compression, (int)ReadBigEndianUInt32(header, 56));
                }
            }

            LogicalBytes = (long)ReadBigEndianUInt64(header, 32);
            var mapOffset = (long)ReadBigEndianUInt64(header, 40);
            MetadataOffset = (long)ReadBigEndianUInt64(header, 48);
            HunkBytes = (int)ReadBigEndianUInt32(header, 56);
            UnitBytes = (int)ReadBigEndianUInt32(header, 60);

            if (HunkBytes <= 0)
            {
                throw new InvalidChdException("The CHD header declares a zero hunk size");
            }

            HunkCount = (int)((LogicalBytes + HunkBytes - 1) / HunkBytes);

            if (!IsParentSha1Empty(header, 104))
            {
                throw new InvalidChdException(
                    "The CHD was created against a parent CHD, which this reader cannot resolve");
            }

            var compressed = _codecs[0] != null;

            if (compressed)
            {
                ReadCompressedMapV5(mapOffset);
            }
            else
            {
                ReadUncompressedMapV5(mapOffset);
            }
        }

        private void ReadHeaderV34(byte[] header)
        {
            var flags = ReadBigEndianUInt32(header, 16);
            var compression = ReadBigEndianUInt32(header, 20);

            HunkCount = (int)ReadBigEndianUInt32(header, 24);
            LogicalBytes = (long)ReadBigEndianUInt64(header, 28);
            MetadataOffset = (long)ReadBigEndianUInt64(header, 36);
            HunkBytes = (int)ReadBigEndianUInt32(header, Version == 3 ? 76 : 44);

            // A CD image always addresses whole frames; v3 and v4 do not record the unit size
            UnitBytes = ChdConstants.FrameSize;

            const uint hasParent = 0x00000002;

            if ((flags & hasParent) != 0)
            {
                throw new InvalidChdException(
                    "The CHD was created against a parent CHD, which this reader cannot resolve");
            }

            if (HunkBytes <= 0)
            {
                throw new InvalidChdException("The CHD header declares a zero hunk size");
            }

            _codecs[0] = ChdCodec.Create(compression, HunkBytes);

            ReadLegacyMap(header.Length);
        }

        /// <summary>
        /// Reads the v3/v4 map: one fixed-size entry per hunk, right after the header.
        /// </summary>
        private void ReadLegacyMap(int headerLength)
        {
            AllocateMap();

            if ((long)HunkCount * LegacyMapEntrySize > int.MaxValue)
            {
                throw new InvalidChdException("The CHD hunk map is too large");
            }

            var raw = new byte[HunkCount * LegacyMapEntrySize];

            _stream.Position = headerLength;
            ReadOrThrow(raw, raw.Length);

            for (var hunk = 0; hunk < HunkCount; hunk++)
            {
                var entry = hunk * LegacyMapEntrySize;

                // 64-bit offset, 32-bit CRC, then a 24-bit length and the entry type
                var offset = (long)ReadBigEndianUInt64(raw, entry);
                var length = (uint)(ReadBigEndianUInt16(raw, entry + 12) | (raw[entry + 14] << 16));
                var kind = raw[entry + 15] & 0x0F;

                switch (kind)
                {
                    case 1: // compressed
                        _hunkKind[hunk] = HunkKind.Compressed;
                        _hunkOffset[hunk] = offset;
                        _hunkLength[hunk] = length;
                        break;

                    case 2: // uncompressed
                        _hunkKind[hunk] = HunkKind.Uncompressed;
                        _hunkOffset[hunk] = offset;
                        break;

                    case 3: // mini
                        _hunkKind[hunk] = HunkKind.Mini;
                        _hunkOffset[hunk] = offset;
                        break;

                    case 4: // same as another hunk in this file
                        _hunkKind[hunk] = HunkKind.Self;
                        _hunkOffset[hunk] = offset;
                        break;

                    case 5: // same as a hunk in the parent file
                        throw new InvalidChdException(
                            "The CHD refers to a parent CHD, which this reader cannot resolve");

                    default:
                        throw new InvalidChdException($"The hunk map holds the unknown entry type {kind}");
                }
            }
        }

        /// <summary>
        /// Reads an uncompressed v5 map, where each entry is simply the hunk's position in the
        /// file measured in hunks.
        /// </summary>
        private void ReadUncompressedMapV5(long mapOffset)
        {
            AllocateMap();

            var raw = new byte[HunkCount * 4];
            _stream.Position = mapOffset;
            ReadOrThrow(raw, raw.Length);

            for (var hunk = 0; hunk < HunkCount; hunk++)
            {
                var block = ReadBigEndianUInt32(raw, hunk * 4);

                if (block == 0)
                {
                    _hunkKind[hunk] = HunkKind.Zero;
                }
                else
                {
                    _hunkKind[hunk] = HunkKind.Uncompressed;
                    _hunkOffset[hunk] = (long)block * HunkBytes;
                }
            }
        }

        /// <summary>
        /// Reads a compressed v5 map. The compression type of every hunk is Huffman coded in one
        /// pass, and the lengths, offsets and back-references follow in a second pass over the
        /// same bit stream.
        /// </summary>
        private void ReadCompressedMapV5(long mapOffset)
        {
            AllocateMap();

            var mapHeader = new byte[16];
            _stream.Position = mapOffset;
            ReadOrThrow(mapHeader, mapHeader.Length);

            var mapBytes = (int)ReadBigEndianUInt32(mapHeader, 0);
            var firstOffset = (long)ReadBigEndianUInt48(mapHeader, 4);
            var lengthBits = mapHeader[12];
            var selfBits = mapHeader[13];
            var parentBits = mapHeader[14];

            if (lengthBits > 32 || selfBits > 32 || parentBits > 32)
            {
                throw new InvalidChdException("The hunk map declares an out of range field width");
            }

            if (mapBytes < 0 || mapOffset + 16 + mapBytes > _stream.Length)
            {
                throw new InvalidChdException("The hunk map runs past the end of the file");
            }

            var compressed = new byte[mapBytes];
            ReadOrThrow(compressed, mapBytes);

            var reader = new ChdBitReader(compressed);
            var decoder = new ChdHuffmanDecoder(16, 8);
            decoder.ImportTreeRle(reader);

            ReadMapTypes(reader, decoder);
            ReadMapEntries(reader, firstOffset, lengthBits, selfBits, parentBits);
        }

        // The compression type codes of a v5 map. Codes 0 to 3 select one of the header's four
        // codecs; the rest describe how to recover the hunk without decompressing anything.
        private const int TypeNone = 4;
        private const int TypeSelf = 5;
        private const int TypeParent = 6;
        private const int TypeRleSmall = 7;
        private const int TypeRleLarge = 8;
        private const int TypeSelf0 = 9;
        private const int TypeSelf1 = 10;
        private const int TypeParentSelf = 11;
        private const int TypeParent0 = 12;
        private const int TypeParent1 = 13;

        private byte[] _mapTypes;

        private void ReadMapTypes(ChdBitReader reader, ChdHuffmanDecoder decoder)
        {
            _mapTypes = new byte[HunkCount];

            var repeat = 0;
            byte last = 0;

            for (var hunk = 0; hunk < HunkCount; hunk++)
            {
                if (repeat > 0)
                {
                    _mapTypes[hunk] = last;
                    repeat--;
                    continue;
                }

                var value = (int)decoder.DecodeOne(reader);

                if (value == TypeRleSmall)
                {
                    _mapTypes[hunk] = last;
                    repeat = 2 + (int)decoder.DecodeOne(reader);
                }
                else if (value == TypeRleLarge)
                {
                    _mapTypes[hunk] = last;
                    repeat = 2 + 16 + ((int)decoder.DecodeOne(reader) << 4);
                    repeat += (int)decoder.DecodeOne(reader);
                }
                else
                {
                    last = (byte)value;
                    _mapTypes[hunk] = last;
                }
            }
        }

        private void ReadMapEntries(ChdBitReader reader, long firstOffset, int lengthBits, int selfBits, int parentBits)
        {
            var current = firstOffset;
            long lastSelf = 0;

            for (var hunk = 0; hunk < HunkCount; hunk++)
            {
                var type = _mapTypes[hunk];
                var offset = current;
                uint length = 0;

                switch (type)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        length = reader.Read(lengthBits);
                        current += length;
                        reader.Read(16); // hunk CRC, which we do not verify
                        _hunkKind[hunk] = HunkKind.Compressed;
                        _hunkCodec[hunk] = type;
                        break;

                    case TypeNone:
                        length = (uint)HunkBytes;
                        current += length;
                        reader.Read(16);
                        _hunkKind[hunk] = HunkKind.Uncompressed;
                        break;

                    case TypeSelf:
                        offset = reader.Read(selfBits);
                        lastSelf = offset;
                        _hunkKind[hunk] = HunkKind.Self;
                        break;

                    case TypeSelf1:
                        lastSelf++;
                        goto case TypeSelf0;

                    case TypeSelf0:
                        offset = lastSelf;
                        _hunkKind[hunk] = HunkKind.Self;
                        break;

                    case TypeParent:
                    case TypeParentSelf:
                    case TypeParent0:
                    case TypeParent1:
                        throw new InvalidChdException(
                            "The CHD refers to a parent CHD, which this reader cannot resolve");

                    default:
                        throw new InvalidChdException($"The hunk map holds the unknown entry type {type}");
                }

                _hunkOffset[hunk] = offset;
                _hunkLength[hunk] = length;
            }

            if (reader.Overflowed)
            {
                throw new InvalidChdException("The hunk map ended before every hunk was described");
            }
        }

        private void AllocateMap()
        {
            _hunkKind = new HunkKind[HunkCount];
            _hunkCodec = new byte[HunkCount];
            _hunkOffset = new long[HunkCount];
            _hunkLength = new uint[HunkCount];
        }

        private long MetadataOffset { get; set; }

        private void ReadMetadata()
        {
            var entries = new List<ChdMetadata>();
            var offset = MetadataOffset;
            var header = new byte[MetadataHeaderSize];

            // A malformed file could chain metadata entries in a loop
            var guard = 0;

            while (offset != 0 && ++guard <= 65536)
            {
                if (offset < 0 || offset + MetadataHeaderSize > _stream.Length) break;

                _stream.Position = offset;
                ReadOrThrow(header, header.Length);

                var tag = ReadBigEndianUInt32(header, 0);
                var lengthAndFlags = ReadBigEndianUInt32(header, 4);
                var next = (long)ReadBigEndianUInt64(header, 8);
                var length = (int)(lengthAndFlags & 0x00FFFFFF);

                if (length < 0 || offset + MetadataHeaderSize + length > _stream.Length) break;

                var data = new byte[length];
                ReadOrThrow(data, length);

                entries.Add(new ChdMetadata(tag, data));

                offset = next;
            }

            Metadata = entries;
        }

        private void Seek(long offset, int length)
        {
            if (offset < 0 || offset + length > _stream.Length)
            {
                throw new InvalidChdException("A hunk lies outside the file");
            }

            _stream.Position = offset;
        }

        private void ReadOrThrow(byte[] buffer, int count)
        {
            if (!ReadFully(_stream, buffer, count))
            {
                throw new InvalidChdException("The CHD file ended unexpectedly");
            }
        }

        private static bool ReadFully(Stream stream, byte[] buffer, int count)
        {
            var read = 0;

            while (read < count)
            {
                var bytes = stream.Read(buffer, read, count - read);
                if (bytes <= 0) return false;
                read += bytes;
            }

            return true;
        }

        private static bool IsParentSha1Empty(byte[] header, int offset)
        {
            for (var i = 0; i < 20; i++)
            {
                if (header[offset + i] != 0) return false;
            }

            return true;
        }

        private static ushort ReadBigEndianUInt16(byte[] buffer, int offset)
        {
            return (ushort)((buffer[offset] << 8) | buffer[offset + 1]);
        }

        private static uint ReadBigEndianUInt32(byte[] buffer, int offset)
        {
            return ((uint)buffer[offset] << 24)
                   | ((uint)buffer[offset + 1] << 16)
                   | ((uint)buffer[offset + 2] << 8)
                   | buffer[offset + 3];
        }

        private static ulong ReadBigEndianUInt48(byte[] buffer, int offset)
        {
            ulong value = 0;

            for (var i = 0; i < 6; i++)
            {
                value = (value << 8) | buffer[offset + i];
            }

            return value;
        }

        private static ulong ReadBigEndianUInt64(byte[] buffer, int offset)
        {
            ulong value = 0;

            for (var i = 0; i < 8; i++)
            {
                value = (value << 8) | buffer[offset + i];
            }

            return value;
        }

        private static void WriteBigEndianUInt64(byte[] buffer, int offset, ulong value)
        {
            for (var i = 0; i < 8; i++)
            {
                buffer[offset + i] = (byte)(value >> (56 - i * 8));
            }
        }

        public void Dispose()
        {
            if (!_leaveOpen)
            {
                _stream?.Dispose();
            }
        }
    }

    /// <summary>
    /// One entry of a CHD's metadata chain. A CD image describes each of its tracks with one.
    /// </summary>
    public sealed class ChdMetadata
    {
        public ChdMetadata(uint tag, byte[] data)
        {
            Tag = tag;
            Data = data;
        }

        public uint Tag { get; }

        public byte[] Data { get; }

        /// <summary>
        /// The entry's payload as text, without the trailing null the writer includes.
        /// </summary>
        public string GetText()
        {
            var length = Data.Length;

            while (length > 0 && Data[length - 1] == 0)
            {
                length--;
            }

            return Encoding.ASCII.GetString(Data, 0, length);
        }
    }
}
