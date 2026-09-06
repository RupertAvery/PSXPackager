namespace UnitTestProject.Chd
{
    /// <summary>
    /// Writes the smallest legal FLAC stream that carries a given set of samples: one frame per
    /// block, each channel stored as a verbatim subframe.
    /// </summary>
    /// <remarks>
    /// This exists so the CHD FLAC path can be tested without an encoder dependency. A CHD stores
    /// only the frames, with no "fLaC" marker and no STREAMINFO block, which is exactly what this
    /// produces. Verbatim subframes compress nothing, which does not matter - the point is to
    /// produce something a decoder will accept.
    /// </remarks>
    internal static class FlacFrameWriter
    {
        /// <summary>
        /// Encodes interleaved 16-bit stereo samples as a sequence of FLAC frames.
        /// </summary>
        /// <param name="samples">Interleaved samples, left channel first.</param>
        /// <param name="blockSize">Samples per channel in each frame.</param>
        public static byte[] Write(short[] samples, int blockSize)
        {
            const int channels = 2;

            var output = new MemoryStream();
            var total = samples.Length / channels;

            for (var start = 0; start < total; start += blockSize)
            {
                var count = Math.Min(blockSize, total - start);

                output.Write(WriteFrame(samples, start, count, start / blockSize));
            }

            return output.ToArray();
        }

        private static byte[] WriteFrame(short[] samples, int start, int blockSize, int frameNumber)
        {
            var bits = new BitWriter();

            // Sync code, a reserved bit, then a zero for the fixed block size strategy
            bits.Write(0x3FFE, 14);
            bits.Write(0, 1);
            bits.Write(0, 1);

            bits.Write(0b0111, 4); // the block size follows the header as a 16 bit value
            bits.Write(0b1001, 4); // 44.1 kHz
            bits.Write(0b0001, 4); // two independently coded channels
            bits.Write(0b100, 3);  // 16 bits per sample
            bits.Write(0, 1);      // reserved

            WriteUtf8(bits, frameNumber);

            bits.Write((uint)(blockSize - 1), 16);

            // The header is covered by a CRC-8 of its own
            bits.Write(Crc8(bits.ToArray()), 8);

            for (var channel = 0; channel < 2; channel++)
            {
                bits.Write(0, 1);       // reserved
                bits.Write(0b000001, 6); // verbatim
                bits.Write(0, 1);       // no wasted bits

                for (var i = 0; i < blockSize; i++)
                {
                    bits.Write((uint)(ushort)samples[(start + i) * 2 + channel], 16);
                }
            }

            bits.AlignToByte();
            bits.Write(Crc16(bits.ToArray()), 16);

            return bits.ToArray();
        }

        /// <summary>
        /// Writes a number in the extended UTF-8 form FLAC uses for frame numbers.
        /// </summary>
        private static void WriteUtf8(BitWriter bits, int value)
        {
            if (value < 0x80)
            {
                bits.Write((uint)value, 8);
                return;
            }

            if (value < 0x800)
            {
                bits.Write(0b110u | (uint)(value >> 6) & 0x1F, 8);
                bits.Write(0b10u << 6 | (uint)value & 0x3F, 8);
                return;
            }

            bits.Write(0b1110_0000u | ((uint)value >> 12) & 0x0F, 8);
            bits.Write(0b1000_0000u | ((uint)value >> 6) & 0x3F, 8);
            bits.Write(0b1000_0000u | (uint)value & 0x3F, 8);
        }

        private static uint Crc8(byte[] data)
        {
            uint crc = 0;

            foreach (var b in data)
            {
                crc ^= b;

                for (var bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 0x80) != 0 ? ((crc << 1) ^ 0x07) & 0xFF : (crc << 1) & 0xFF;
                }
            }

            return crc;
        }

        private static uint Crc16(byte[] data)
        {
            uint crc = 0;

            foreach (var b in data)
            {
                crc ^= (uint)b << 8;

                for (var bit = 0; bit < 8; bit++)
                {
                    crc = (crc & 0x8000) != 0 ? ((crc << 1) ^ 0x8005) & 0xFFFF : (crc << 1) & 0xFFFF;
                }
            }

            return crc;
        }

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

            public void AlignToByte()
            {
                while (_bits != 0)
                {
                    Write(0, 1);
                }
            }

            /// <summary>The bytes written so far; only valid on a byte boundary.</summary>
            public byte[] ToArray() => _bytes.ToArray();
        }
    }
}
