using System;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// Reads a big-endian, most-significant-bit-first bit stream, the encoding CHD uses for its
    /// compressed hunk map and for the Huffman trees embedded in it.
    /// </summary>
    internal sealed class ChdBitReader
    {
        private readonly byte[] _data;
        private uint _buffer;
        private int _bits;
        private int _offset;

        public ChdBitReader(byte[] data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        /// <summary>
        /// True once more bits have been consumed than the buffer holds, which only a malformed
        /// stream can cause.
        /// </summary>
        public bool Overflowed => _offset - _bits / 8 > _data.Length;

        /// <summary>
        /// Returns the next <paramref name="count"/> bits without consuming them. Reading past the
        /// end of the data yields zeroes, matching the reference decoder.
        /// </summary>
        public uint Peek(int count)
        {
            if (count == 0) return 0;

            if (count > _bits)
            {
                while (_bits <= 24)
                {
                    if (_offset < _data.Length)
                    {
                        _buffer |= (uint)_data[_offset] << (24 - _bits);
                    }

                    _offset++;
                    _bits += 8;
                }
            }

            return _buffer >> (32 - count);
        }

        public void Skip(int count)
        {
            _buffer = count >= 32 ? 0 : _buffer << count;
            _bits -= count;
        }

        public uint Read(int count)
        {
            var value = Peek(count);
            Skip(count);
            return value;
        }
    }
}
