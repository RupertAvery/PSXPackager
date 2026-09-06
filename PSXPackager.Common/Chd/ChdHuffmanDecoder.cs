using System;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// The canonical Huffman decoder CHD uses to encode the per-hunk compression types in a
    /// compressed v5 map.
    /// </summary>
    /// <remarks>
    /// Code lengths are stored run-length encoded; the codes themselves are then assigned
    /// canonically from those lengths, so only the lengths travel in the file.
    /// </remarks>
    internal sealed class ChdHuffmanDecoder
    {
        private readonly int _numCodes;
        private readonly int _maxBits;
        private readonly byte[] _codeLengths;
        private readonly uint[] _codes;

        /// <summary>Maps a <see cref="_maxBits"/>-wide window to (symbol &lt;&lt; 5) | code length.</summary>
        private readonly uint[] _lookup;

        public ChdHuffmanDecoder(int numCodes, int maxBits)
        {
            _numCodes = numCodes;
            _maxBits = maxBits;
            _codeLengths = new byte[numCodes];
            _codes = new uint[numCodes];
            _lookup = new uint[1 << maxBits];
        }

        /// <summary>
        /// Reads the run-length encoded code lengths and builds the decoding table.
        /// </summary>
        public void ImportTreeRle(ChdBitReader reader)
        {
            // The width of a length field is driven by the longest code the decoder allows
            var numBits = _maxBits >= 16 ? 5 : _maxBits >= 8 ? 4 : 3;

            var current = 0;

            while (current < _numCodes)
            {
                var nodeBits = (int)reader.Read(numBits);

                if (nodeBits != 1)
                {
                    // Any value but 1 is a literal length
                    _codeLengths[current++] = (byte)nodeBits;
                    continue;
                }

                // A 1 escapes: either a doubled 1 for a literal 1, or a length plus a repeat count
                nodeBits = (int)reader.Read(numBits);

                if (nodeBits == 1)
                {
                    _codeLengths[current++] = 1;
                    continue;
                }

                var repeat = (int)reader.Read(numBits) + 3;

                if (current + repeat > _numCodes)
                {
                    throw new InvalidChdException("Huffman tree run length runs past the end of the code table");
                }

                while (repeat-- > 0)
                {
                    _codeLengths[current++] = (byte)nodeBits;
                }
            }

            AssignCanonicalCodes();
            BuildLookupTable();
        }

        public uint DecodeOne(ChdBitReader reader)
        {
            var entry = _lookup[reader.Peek(_maxBits)];
            var length = (int)(entry & 0x1F);

            if (length == 0)
            {
                throw new InvalidChdException("Encountered an undefined Huffman code");
            }

            reader.Skip(length);

            return entry >> 5;
        }

        private void AssignCanonicalCodes()
        {
            var histogram = new uint[33];

            foreach (var length in _codeLengths)
            {
                if (length > _maxBits)
                {
                    throw new InvalidChdException("Huffman code is longer than the decoder allows");
                }

                histogram[length]++;
            }

            // Walk from the longest code to the shortest, halving as we go, to find where each
            // length's block of codes starts
            uint start = 0;

            for (var length = 32; length > 0; length--)
            {
                var next = (start + histogram[length]) >> 1;

                if (length != 1 && next * 2 != start + histogram[length])
                {
                    throw new InvalidChdException("Huffman code lengths do not describe a valid tree");
                }

                histogram[length] = start;
                start = next;
            }

            for (var code = 0; code < _numCodes; code++)
            {
                if (_codeLengths[code] > 0)
                {
                    _codes[code] = histogram[_codeLengths[code]]++;
                }
            }
        }

        private void BuildLookupTable()
        {
            Array.Clear(_lookup, 0, _lookup.Length);

            for (var code = 0; code < _numCodes; code++)
            {
                var length = _codeLengths[code];
                if (length == 0) continue;

                // Every window whose leading bits are this code decodes to it
                var value = ((uint)code << 5) | length;
                var shift = _maxBits - length;
                var first = _codes[code] << shift;
                var last = ((_codes[code] + 1) << shift) - 1;

                for (var index = first; index <= last; index++)
                {
                    _lookup[index] = value;
                }
            }
        }
    }
}
