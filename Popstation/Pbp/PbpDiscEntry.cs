using System;
using System.Collections.Generic;
using System.IO;
using Popstation.Iso;

namespace Popstation.Pbp
{
    public class PbpDiscEntry
    {
        // The maximum possible number of ISO indexes
        const int MAX_INDEXES = 0x7E00;

        // The location of the ISO indexes in the PSAR
        const int PSAR_GAMEID_OFFSET = 0x400;
        const int PSAR_TOC_OFFSET = 0x800;

        const int PSAR_INDEX_OFFSET = 0x4000;
        // The location of the ISO data in the PSAR
        const int PSAR_ISO_OFFSET = 0x100000;
        // The size of one "block" of the ISO
        public const int ISO_BLOCK_SIZE = 0x930;

        private readonly Stream stream;
        private readonly int psar_offset;
        public List<IsoIndexLite> IsoIndex { get; set; }
        public List<TOCEntry> TOC { get; set; }
        public uint IsoSize { get; }
        public int Index { get; set; }

        public PbpDiscEntry(Stream stream, int psarOffset, int index)
        {
            this.stream = stream;
            Index = index;
            psar_offset = psarOffset;
            TOC = ReadTOC();
            IsoIndex = ReadIsoIndexes();
            IsoSize = GetIsoSize();
        }

        private List<TOCEntry> ReadTOC()
        {
            byte[] buffer = new byte[0xA];

            var entries = new List<TOCEntry>();

            // Read in the offset of the PSAR file
            //stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
            //var psar_offset = stream.ReadInteger();


            stream.Seek(psar_offset + PSAR_TOC_OFFSET, SeekOrigin.Begin);

            stream.Read(buffer, 0, 0xA);
            if (buffer[2] != 0xA0) throw new Exception("Invalid TOC!");
            int startTrack = Helper.FromBinaryDecimal(buffer[7]);
            stream.Read(buffer, 0, 0xA);
            if (buffer[2] != 0xA1) throw new Exception("Invalid TOC!");
            int endTrack = Helper.FromBinaryDecimal(buffer[7]);
            stream.Read(buffer, 0, 0xA);
            if (buffer[2] != 0xA2) throw new Exception("Invalid TOC!");
            int mm = Helper.FromBinaryDecimal(buffer[7]);
            int ss = Helper.FromBinaryDecimal(buffer[8]);
            int ff = Helper.FromBinaryDecimal(buffer[9]);
            //var frames = mm * 60 * 75 + ss * 75 + ff;
            //var size = 2352 * frames;

            for (var c = startTrack; c <= endTrack; c++)
            {
                stream.Read(buffer, 0, 0xA);
                var trackNo = Helper.FromBinaryDecimal(buffer[2]);
                if (trackNo != c) throw new Exception("Invalid TOC!");

                var entry = new TOCEntry
                {
                    TrackType = (TrackTypeEnum)buffer[0],
                    TrackNo = trackNo,
                    Minutes = Helper.FromBinaryDecimal(buffer[3]),
                    Seconds = Helper.FromBinaryDecimal(buffer[4]),
                    Frames = Helper.FromBinaryDecimal(buffer[5])
                };

                entries.Add(entry);

            }

            return entries;
        }

        private List<IsoIndexLite> ReadIsoIndexes()
        {
            //int psar_offset;
            int this_offset;
            int count = 0;
            int offset;
            int length;
            int[] dummy = new int[6];

            var iso_index = new List<IsoIndexLite>();

            // Read in the offset of the PSAR file
            //stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
            //psar_offset = stream.ReadInteger();



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
                    var index = new IsoIndexLite
                    {
                        Offset = offset,
                        Length = length
                    };

                    iso_index.Add(index);

                    count++;

                    if (count >= MAX_INDEXES)
                    {
                        throw new Exception("Number of indexes exceeds maximum allowed");
                    }
                }
            }

            if (iso_index.Count == 0) throw new Exception("No iso index was found.");

            return iso_index;
        }

        public uint ReadBlock(int blockNo, byte[] buffer)
        {
            byte[] in_buffer;
            //int psar_offset;
            int this_offset;
            uint out_length;

            //// Read in the offset of the PSAR file
            //stream.Seek(HEADER_PSAR_OFFSET, SeekOrigin.Begin);
            //psar_offset = stream.ReadInteger();

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
    }
}