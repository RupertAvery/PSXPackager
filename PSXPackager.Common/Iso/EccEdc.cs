namespace PSXPackager.Common.Iso
{
    /// <summary>
    /// Generates the error detection (EDC) and error correction (ECC) fields of a CD-ROM sector.
    /// </summary>
    /// <remarks>
    /// The ECC is a Reed-Solomon Product Code over GF(2^8) with the primitive polynomial 0x11D,
    /// laid out as two interleaved parity blocks (P and Q) as defined by ECMA-130.
    /// The EDC is a CRC-32 with the reversed polynomial 0xD8018001.
    /// </remarks>
    internal static class EccEdc
    {
        /// <summary>Multiply by x in GF(2^8).</summary>
        private static readonly byte[] EccForward = new byte[256];
        /// <summary>Inverse of <see cref="EccForward"/> XORed with its input, used to solve for the parity byte.</summary>
        private static readonly byte[] EccBackward = new byte[256];
        private static readonly uint[] EdcTable = new uint[256];

        static EccEdc()
        {
            for (var i = 0; i < 256; i++)
            {
                var j = (i << 1) ^ ((i & 0x80) != 0 ? 0x11D : 0);

                EccForward[i] = (byte)j;
                EccBackward[i ^ j] = (byte)i;

                var edc = (uint)i;
                for (var bit = 0; bit < 8; bit++)
                {
                    edc = (edc >> 1) ^ ((edc & 1) != 0 ? 0xD8018001u : 0);
                }
                EdcTable[i] = edc;
            }
        }

        /// <summary>
        /// Computes the EDC checksum over a range of a sector.
        /// </summary>
        public static uint ComputeEdc(byte[] sector, int offset, int count)
        {
            uint edc = 0;

            for (var i = 0; i < count; i++)
            {
                edc = (edc >> 8) ^ EdcTable[(edc ^ sector[offset + i]) & 0xFF];
            }

            return edc;
        }

        /// <summary>
        /// Computes the P and Q parity of a sector in place.
        /// </summary>
        /// <param name="sector">A full 2352-byte sector.</param>
        /// <param name="zeroAddress">
        /// True for Mode 2 sectors, whose parity is computed with the 4-byte header treated as zero.
        /// False for Mode 1 sectors, whose parity covers the header as written.
        /// </param>
        public static void WriteEcc(byte[] sector, bool zeroAddress)
        {
            var address = new byte[4];

            if (zeroAddress)
            {
                System.Array.Copy(sector, 12, address, 0, 4);
                System.Array.Clear(sector, 12, 4);
            }

            // P parity: 86 columns of 24 bytes, written to 0x81C
            ComputeBlock(sector, 0x0C, 86, 24, 2, 86, 0x81C);
            // Q parity: 52 diagonals of 43 bytes (covering the P parity too), written to 0x8C8
            ComputeBlock(sector, 0x0C, 52, 43, 86, 88, 0x8C8);

            if (zeroAddress)
            {
                System.Array.Copy(address, 0, sector, 12, 4);
            }
        }

        private static void ComputeBlock(
            byte[] sector,
            int sourceOffset,
            int majorCount,
            int minorCount,
            int majorMult,
            int minorInc,
            int destinationOffset)
        {
            var size = majorCount * minorCount;

            for (var major = 0; major < majorCount; major++)
            {
                var index = (major >> 1) * majorMult + (major & 1);
                byte eccA = 0;
                byte eccB = 0;

                for (var minor = 0; minor < minorCount; minor++)
                {
                    var value = sector[sourceOffset + index];

                    index += minorInc;
                    if (index >= size) index -= size;

                    eccA ^= value;
                    eccB ^= value;
                    eccA = EccForward[eccA];
                }

                eccA = EccBackward[EccForward[eccA] ^ eccB];

                sector[destinationOffset + major] = eccA;
                sector[destinationOffset + major + majorCount] = (byte)(eccA ^ eccB);
            }
        }
    }
}
