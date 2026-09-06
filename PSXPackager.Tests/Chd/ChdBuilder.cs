using System.IO.Compression;
using System.Text;

namespace UnitTestProject.Chd
{
    /// <summary>
    /// Builds a CHD v5 file in memory, so the reader can be tested against files that really carry
    /// the structures it parses rather than against mocks of them.
    /// </summary>
    internal sealed class ChdBuilder
    {
        public const int HeaderLength = 124;

        /// <summary>Type codes of a compressed map entry, as the format defines them.</summary>
        public const byte TypeCodec0 = 0;
        public const byte TypeCodec1 = 1;
        public const byte TypeNone = 4;
        public const byte TypeSelf = 5;
        public const byte TypeRleSmall = 7;

        private readonly List<byte[]> _hunks = new();
        private readonly List<byte> _types = new();
        private readonly List<(uint Tag, string Text)> _metadata = new();

        public uint[] Compressors { get; } = new uint[4];

        public int HunkBytes { get; set; } = 8 * 2448;

        public int UnitBytes { get; set; } = 2448;

        public long LogicalBytes { get; set; }

        /// <summary>Bits used for the length field of a compressed map entry.</summary>
        public int LengthBits { get; set; } = 24;

        /// <summary>Bits used for the hunk number a self-referencing entry names.</summary>
        public int SelfBits { get; set; } = 16;

        /// <summary>
        /// When set, hunks after the first are written as one run-length coded repeat of the first
        /// hunk's compression type instead of one code each. Every hunk must share a type.
        /// </summary>
        public bool UseRunLengthEncoding { get; set; }

        /// <summary>
        /// Adds a hunk stored with the codec in the given slot.
        /// </summary>
        public void AddHunk(byte type, byte[] data)
        {
            _types.Add(type);
            _hunks.Add(data);
        }

        /// <summary>
        /// Adds an entry that is not backed by data of its own, such as a self-reference.
        /// </summary>
        public void AddReference(byte type, int target)
        {
            _types.Add(type);
            _hunks.Add(BitConverter.GetBytes(target));
        }

        public void AddMetadata(uint tag, string text) => _metadata.Add((tag, text));

        /// <summary>Adds a CHT2 track entry, the form the CHD tools write today.</summary>
        public void AddTrack(int number, string type, int frames, int pregap = 0, string pregapType = "NONE", int postgap = 0)
        {
            AddMetadata(0x43485432,
                $"TRACK:{number} TYPE:{type} SUBTYPE:NONE FRAMES:{frames} PREGAP:{pregap} " +
                $"PGTYPE:{pregapType} PGSUB:NONE POSTGAP:{postgap}");
        }

        public byte[] Build()
        {
            var compressed = Compressors[0] != 0;
            var file = new MemoryStream();

            file.Write(new byte[HeaderLength]);

            // Lay the hunk payloads down first, then the map, then the metadata chain
            var offsets = new long[_hunks.Count];

            for (var i = 0; i < _hunks.Count; i++)
            {
                // Without a map to record byte offsets, a hunk's position is measured in whole
                // hunks, so each one has to start on a hunk boundary
                if (!compressed)
                {
                    var padding = (HunkBytes - file.Position % HunkBytes) % HunkBytes;
                    file.Write(new byte[padding]);
                }

                offsets[i] = file.Position;

                if (_types[i] != TypeSelf)
                {
                    file.Write(_hunks[i]);
                }
            }

            var mapOffset = file.Position;

            if (compressed)
            {
                WriteCompressedMap(file, offsets);
            }
            else
            {
                WriteUncompressedMap(file, offsets);
            }

            var metadataOffset = _metadata.Count > 0 ? file.Position : 0;

            WriteMetadata(file);

            var image = file.ToArray();

            WriteHeader(image, mapOffset, metadataOffset);

            return image;
        }

        private void WriteHeader(byte[] image, long mapOffset, long metadataOffset)
        {
            Encoding.ASCII.GetBytes("MComprHD").CopyTo(image, 0);

            WriteBigEndian(image, 8, (uint)HeaderLength);
            WriteBigEndian(image, 12, 5u);

            for (var i = 0; i < 4; i++)
            {
                WriteBigEndian(image, 16 + i * 4, Compressors[i]);
            }

            var logicalBytes = LogicalBytes != 0 ? LogicalBytes : (long)_hunks.Count * HunkBytes;

            WriteBigEndian(image, 32, (ulong)logicalBytes);
            WriteBigEndian(image, 40, (ulong)mapOffset);
            WriteBigEndian(image, 48, (ulong)metadataOffset);
            WriteBigEndian(image, 56, (uint)HunkBytes);
            WriteBigEndian(image, 60, (uint)UnitBytes);
        }

        /// <summary>
        /// Writes the map of a file with no compression: one 32-bit hunk position per hunk.
        /// </summary>
        private void WriteUncompressedMap(Stream file, long[] offsets)
        {
            for (var i = 0; i < offsets.Length; i++)
            {
                var entry = new byte[4];
                WriteBigEndian(entry, 0, (uint)(offsets[i] / HunkBytes));
                file.Write(entry);
            }
        }

        /// <summary>
        /// Writes a compressed map: a 16-byte header, a Huffman tree for the compression types,
        /// the types themselves, and then the length and offset of every hunk.
        /// </summary>
        private void WriteCompressedMap(Stream file, long[] offsets)
        {
            var bits = new BitWriter();

            // A flat tree: sixteen symbols of four bits each, so symbol n encodes as the value n.
            // Each length is written as a literal, since only the value 1 is an escape.
            for (var symbol = 0; symbol < 16; symbol++)
            {
                bits.Write(4, 4);
            }

            if (UseRunLengthEncoding && _types.Count >= 4)
            {
                // The first hunk names the type; a small run then covers the hunk it appears at
                // plus two more than the count that follows it
                bits.Write(_types[0], 4);
                bits.Write(TypeRleSmall, 4);
                bits.Write((uint)(_types.Count - 1 - 3), 4);
            }
            else
            {
                foreach (var type in _types)
                {
                    bits.Write(type, 4);
                }
            }

            foreach (var (type, index) in _types.Select((type, index) => (type, index)))
            {
                switch (type)
                {
                    case TypeSelf:
                        bits.Write((uint)BitConverter.ToInt32(_hunks[index]), SelfBits);
                        break;

                    case TypeNone:
                        bits.Write(0, 16); // CRC, which the reader does not check
                        break;

                    default:
                        bits.Write((uint)_hunks[index].Length, LengthBits);
                        bits.Write(0, 16);
                        break;
                }
            }

            var map = bits.ToArray();
            var header = new byte[16];

            WriteBigEndian(header, 0, (uint)map.Length);

            // The offset of the first hunk; the rest follow on from it
            var first = offsets.Length > 0 ? offsets[0] : 0;

            for (var i = 0; i < 6; i++)
            {
                header[4 + i] = (byte)(first >> (40 - i * 8));
            }

            header[12] = (byte)LengthBits;
            header[13] = (byte)SelfBits;
            header[14] = 0;

            file.Write(header);
            file.Write(map);
        }

        private void WriteMetadata(Stream file)
        {
            for (var i = 0; i < _metadata.Count; i++)
            {
                var data = Encoding.ASCII.GetBytes(_metadata[i].Text + "\0");
                var header = new byte[16];

                WriteBigEndian(header, 0, _metadata[i].Tag);
                WriteBigEndian(header, 4, (uint)data.Length);

                var next = i + 1 < _metadata.Count
                    ? (ulong)(file.Position + header.Length + data.Length)
                    : 0;

                WriteBigEndian(header, 8, next);

                file.Write(header);
                file.Write(data);
            }
        }

        /// <summary>
        /// Compresses a hunk the way the CD flavour of a codec does: the sector data and the
        /// subcode are compressed separately, behind a flag byte per eight frames saying which
        /// sectors had their sync header and ECC stripped.
        /// </summary>
        public static byte[] CompressCdZlib(byte[] hunk, int hunkBytes, IEnumerable<int>? strippedFrames = null)
        {
            var frames = hunkBytes / 2448;
            var sectors = new byte[frames * 2352];
            var subcode = new byte[frames * 96];

            for (var frame = 0; frame < frames; frame++)
            {
                Array.Copy(hunk, frame * 2448, sectors, frame * 2352, 2352);
                Array.Copy(hunk, frame * 2448 + 2352, subcode, frame * 96, 96);
            }

            var flags = new byte[(frames + 7) / 8];

            foreach (var frame in strippedFrames ?? Enumerable.Empty<int>())
            {
                flags[frame / 8] |= (byte)(1 << (frame % 8));
            }

            var sectorData = Deflate(sectors);
            var subcodeData = Deflate(subcode);

            var output = new MemoryStream();
            output.Write(flags);
            output.WriteByte((byte)(sectorData.Length >> 8));
            output.WriteByte((byte)sectorData.Length);
            output.Write(sectorData);
            output.Write(subcodeData);

            return output.ToArray();
        }

        public static byte[] Deflate(byte[] data)
        {
            var output = new MemoryStream();

            using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, true))
            {
                deflate.Write(data);
            }

            return output.ToArray();
        }

        private static void WriteBigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        private static void WriteBigEndian(byte[] buffer, int offset, ulong value)
        {
            for (var i = 0; i < 8; i++)
            {
                buffer[offset + i] = (byte)(value >> (56 - i * 8));
            }
        }

        /// <summary>
        /// Writes bits most significant first, the order the CHD map is coded in.
        /// </summary>
        private sealed class BitWriter
        {
            private readonly List<byte> _bytes = new();
            private int _current;
            private int _bits;

            public void Write(uint value, int count)
            {
                for (var i = count - 1; i >= 0; i--)
                {
                    _current = (_current << 1) | (int)((value >> i) & 1);

                    if (++_bits != 8) continue;

                    _bytes.Add((byte)_current);
                    _current = 0;
                    _bits = 0;
                }
            }

            public byte[] ToArray()
            {
                if (_bits > 0)
                {
                    _bytes.Add((byte)(_current << (8 - _bits)));
                }

                return _bytes.ToArray();
            }
        }
    }
}
