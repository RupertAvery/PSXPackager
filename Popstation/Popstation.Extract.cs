using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Popstation
{
    public partial class Popstation
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
        const int ISO_BLOCK_SIZE = 0x930;

        public Task Extract(ExtractIsoInfo extractInfo, CancellationToken cancellationToken)
        {
            return Task.Run(() => ExtractIso(extractInfo, cancellationToken));
        }

        private void ExtractIso(ExtractIsoInfo extractInfo, CancellationToken cancellationToken)
        {

            using (var iso_stream = new FileStream(extractInfo.DestinationIso, FileMode.Create, FileAccess.Write))
            {
                List<INDEX> iso_index = ReadIsoIndexes(extractInfo.SourcePbp);

                if (iso_index.Count == 0) throw new Exception("No iso index was found.");

                uint isoSize = (uint)GetIsoSize(extractInfo.SourcePbp, iso_index);

                OnEvent?.Invoke(PopstationEventEnum.GetIsoSize, isoSize);

                OnEvent?.Invoke(PopstationEventEnum.ExtractStart, null);

                uint totSize = 0;
                int i;
                for (i = 0; i < iso_index.Count; i++)
                {
                    byte[] buffer;
                    buffer = ReadBlock(extractInfo.SourcePbp, iso_index, i, out uint bufferSize);

                    totSize += bufferSize;

                    if (totSize > isoSize)
                    {
                        bufferSize = bufferSize - (totSize - isoSize);
                        totSize = isoSize;
                    }

                    iso_stream.Write(buffer, 0, (int)bufferSize);

                    OnEvent?.Invoke(PopstationEventEnum.ExtractProgress, totSize);

                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }

                OnEvent?.Invoke(PopstationEventEnum.ExtractComplete, null);

            }
        }

        private List<INDEX> ReadIsoIndexes(string pbpFile) 
        {
            int psar_offset;
            int this_offset;
            int count = 0;
            int offset;
            int length;
            int[] dummy = new int[6];

            var iso_index = new List<INDEX>();

            // Open the PBP file
            using (var pbp_stream = new FileStream(pbpFile, FileMode.Open, FileAccess.Read))
            {
                // Read in the offset of the PSAR file
                pbp_stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
                psar_offset = pbp_stream.ReadInteger();

                // Go to the location of the ISO indexes in the PSAR
                pbp_stream.Seek(psar_offset + PSAR_INDEX_OFFSET, SeekOrigin.Begin);

                // Store the current location in the PBP
                this_offset = (int)pbp_stream.Position;

                // Reset the counter variable
                count = 0;

                // Read indexes until the start of the ISO file
                while (this_offset < psar_offset + PSAR_ISO_OFFSET)
                {
                    offset = pbp_stream.ReadInteger();
                    length = pbp_stream.ReadInteger();
                    pbp_stream.Read(dummy, 6);

                    // Record our current location in the PBP
                    this_offset = (int)pbp_stream.Position;

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

            }

            return iso_index;
        }

        private byte[] ReadBlock(string pbpFile, List<INDEX> iso_index, int blockNo, out uint datalength)
        {
            byte[] in_buffer;
            byte[] out_buffer;
            int psar_offset;
            int this_offset;
            int out_length;

            using (var pbp_stream = new FileStream(pbpFile, FileMode.Open, FileAccess.Read))
            {
                // Read in the offset of the PSAR file
                pbp_stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
                psar_offset = pbp_stream.ReadInteger();

                // Go to the offset specified in the index
                this_offset = psar_offset + PSAR_ISO_OFFSET + iso_index[blockNo].Offset;
                pbp_stream.Seek(this_offset, SeekOrigin.Begin);

                // Allocate memory for our output buffer
                out_buffer = new byte[16 * ISO_BLOCK_SIZE];

                // Check if this block isn't compressed
                if (iso_index[blockNo].Length == 16 * ISO_BLOCK_SIZE)
                {

                    // It's not compressed, make an exact copy
                    pbp_stream.Read(out_buffer, 0, 16 * ISO_BLOCK_SIZE);

                    // Output size is a full block
                    out_length = 16 * ISO_BLOCK_SIZE;
                }
                else
                {
                    in_buffer = new byte[iso_index[blockNo].Length];
                    pbp_stream.Read(in_buffer, 0, iso_index[blockNo].Length);
                    var totalBytes = in_buffer.Length;

                    //out_buffer = Decompress(in_buffer);
                    var bufferSize = Compression.Decompress(in_buffer, out_buffer);

                    //out_length = out_buffer.Length;
                    out_length = bufferSize;
                }

                datalength = (uint)out_length;

                return out_buffer;
            }
        }

        private int GetIsoSize(string pbpFile, List<INDEX> iso_index)
        {
            byte[] out_buffer;
            int iso_length;
            uint bufferSize;

            // The ISO size is contained in the data referenced in index #2
            // If we've just read in index #2, grab the ISO size from the output buffer
            out_buffer = ReadBlock(pbpFile, iso_index, 1, out bufferSize);
            iso_length = (out_buffer[104] + (out_buffer[105] << 8) + (out_buffer[106] << 16) + (out_buffer[107] << 24)) * ISO_BLOCK_SIZE;

            return iso_length;
        }

    }
}
