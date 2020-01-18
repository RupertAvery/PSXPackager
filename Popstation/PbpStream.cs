using System;
using System.Collections.Generic;
using System.IO;

namespace Popstation
{
    public class PbpStream : IDisposable
    {
        // The maximum possible number of ISO indexes
        const int MAX_INDEXES = 0x7E00;
        //The location of the PSAR offset in the PBP header
        const int HEADER_PSAR_OFFSET = 0x24;
        // The location of the ISO indexes in the PSAR
        const int PSAR_INDEX_OFFSET = 0x4000;
        // The location of the ISO data in the PSAR
        const int PSAR_ISO_OFFSET = 0x100000;
        // The size of one "block" of the ISO
        public const int ISO_BLOCK_SIZE = 0x930;

        private readonly Stream stream;

        public uint IsoSize { get; }

        public List<INDEX> IsoIndex { get; private set; }

        public PbpStream(string path, FileMode mode, FileAccess access)
        {
            stream = new FileStream(path, mode, access);
            IsoIndex = ReadIsoIndexes();

            if (IsoIndex.Count == 0) throw new Exception("No iso index was found.");

            IsoSize = GetIsoSize();
        }

        private List<INDEX> ReadIsoIndexes()
        {
            int psar_offset;
            int this_offset;
            int count = 0;
            int offset;
            int length;
            int[] dummy = new int[6];

            var iso_index = new List<INDEX>();

            // Read in the offset of the PSAR file
            stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
            psar_offset = stream.ReadInteger();

            // Go to the location of the ISO indexes in the PSAR
            stream.Seek(psar_offset + PSAR_INDEX_OFFSET, SeekOrigin.Begin);

            // Store the current location in the PBP
            this_offset = (int)stream.Position;

            // Reset the counter variable
            count = 0;

            // Read indexes until the start of the ISO file
            while (this_offset < psar_offset + PSAR_ISO_OFFSET)
            {
                offset = stream.ReadInteger();
                length = stream.ReadInteger();
                stream.Read(dummy, 6);

                // Record our current location in the PBP
                this_offset = (int)stream.Position;

                // Check if this looks like a valid offset
                if (offset != 0 || length != 0)
                {
                    var index = new INDEX();
                    // Store the block offset
                    index.Offset = offset;
                    // Store the block length
                    index.Length = length;
                    iso_index.Add(index);
                    count++;
                    if (count >= MAX_INDEXES)
                    {
                        throw new Exception("Number of indexes exceeds maximum allowed");
                    }
                }
            }

            return iso_index;
        }

        public uint ReadBlock(int blockNo, byte[] buffer)
        {
            byte[] in_buffer;
            int psar_offset;
            int this_offset;
            uint out_length;

            // Read in the offset of the PSAR file
            stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
            psar_offset = stream.ReadInteger();

            // Go to the offset specified in the index
            this_offset = psar_offset + PSAR_ISO_OFFSET + IsoIndex[blockNo].Offset;
            stream.Seek(this_offset, SeekOrigin.Begin);

            // Check if this block isn't compressed
            if (IsoIndex[blockNo].Length == 16 * ISO_BLOCK_SIZE)
            {

                // It's not compressed, make an exact copy
                stream.Read(buffer, 0, 16 * ISO_BLOCK_SIZE);

                // Output size is a full block
                out_length = 16 * ISO_BLOCK_SIZE;
            }
            else
            {
                in_buffer = new byte[IsoIndex[blockNo].Length];
                stream.Read(in_buffer, 0, IsoIndex[blockNo].Length);
                var totalBytes = in_buffer.Length;

                //out_buffer = Decompress(in_buffer);
                var bufferSize = Compression.Decompress(in_buffer, buffer);

                //out_length = out_buffer.Length;
                out_length = (uint)bufferSize;
            }

            return out_length;
        }

        private uint GetIsoSize()
        {
            byte[] out_buffer = new byte[16 * ISO_BLOCK_SIZE];

            // The ISO size is contained in the data referenced in index #2
            // If we've just read in index #2, grab the ISO size from the output buffer
            ReadBlock(1, out_buffer);

            return (uint)((out_buffer[104] + (out_buffer[105] << 8) + (out_buffer[106] << 16) + (out_buffer[107] << 24)) * ISO_BLOCK_SIZE);
        }

        public void Dispose()
        {
            stream?.Dispose();
        }
    }
}
