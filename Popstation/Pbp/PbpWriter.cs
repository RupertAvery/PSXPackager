using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Popstation.Iso;
using PSXPackager.Common;
using PSXPackager.Common.Cue;

namespace Popstation.Pbp
{
    public abstract class PbpWriter
    {
        /// <summary>
        /// 16 sectors/block * 2352 bytes/sector 
        /// </summary>
        protected const int BLOCK_SIZE = 0x9300;
        /// <summary>
        /// 1MB read buffer
        /// </summary>
        protected const int BUFFER_SIZE = 1048576;

        protected ConvertOptions convertInfo;

        public Action<PopstationEventEnum, object> Notify { get; set; }
        public Func<string, ActionIfFileExistsEnum> ActionIfFileExists { get; set; }
        public List<string> TempFiles { get; set; }


        public PbpWriter(ConvertOptions convertInfo)
        {
            this.convertInfo = convertInfo;
        }


        public void Write(Stream outputStream, CancellationToken cancellationToken)
        {
            try
            {
                EnsureRequiredResourcesExist();

                ProcessTOCs();

                var sfo = convertInfo.SFOEntries is { Count: > 0 }
                    ? BuildSFO(convertInfo.SFOEntries)
                    : BuildDefaultSFO();

                var header = BuildHeader(sfo);

                var psarOffset = header[9];

                outputStream.Write(header, 0, 0x28);

                Notify?.Invoke(PopstationEventEnum.WriteSfo, null);
                outputStream.WriteSFO(sfo);

                Notify?.Invoke(PopstationEventEnum.WriteIcon0Png, null);
                outputStream.WriteResource(convertInfo.Icon0);

                if (convertInfo.Icon1.Exists)
                {
                    Notify?.Invoke(PopstationEventEnum.WriteIcon1Pmf, null);
                    outputStream.WriteResource(convertInfo.Icon1);
                }

                Notify?.Invoke(PopstationEventEnum.WritePic0Png, null);
                outputStream.WriteResource(convertInfo.Pic0);

                Notify?.Invoke(PopstationEventEnum.WritePic1Png, null);
                outputStream.WriteResource(convertInfo.Pic1);

                if (convertInfo.Snd0.Exists)
                {
                    Notify?.Invoke(PopstationEventEnum.WriteSnd0At3, null);
                    outputStream.WriteResource(convertInfo.Snd0);
                }

                Notify?.Invoke(PopstationEventEnum.WriteDataPsp, null);
                outputStream.WriteResource(convertInfo.DataPsp);

                var offset = (uint)outputStream.Position;

                // fill with NULL
                for (var i = 0; i < psarOffset - offset; i++)
                {
                    outputStream.WriteByte(0);
                }

                uint totSize = 0;

                foreach (var disc in convertInfo.DiscInfos)
                {
                    if (File.Exists(disc.SourceIso))
                    {
                        var t = new FileInfo(disc.SourceIso);
                        var isosize = (uint)t.Length;
                        disc.IsoSize = isosize;
                        totSize += isosize;
                    }
                }

                WritePSAR(outputStream, psarOffset, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                WriteSTARTDAT(outputStream);
            }
            finally
            {
                convertInfo.Icon0.Dispose();
                convertInfo.Pic1.Dispose();
                convertInfo.Pic0.Dispose();
                convertInfo.Boot.Dispose();
                convertInfo.Snd0.Dispose();
                convertInfo.Icon1.Dispose();
                convertInfo.DataPsp.Dispose();
            }
        }

        public abstract void WritePSAR(Stream outputStream, uint psarOffset, CancellationToken cancellationToken);

        int nextPatchPos;

        protected void PatchData(ConvertOptions convertInfo, byte[] buffer, int size, int pos)
        {
            while (true)
            {
                if (nextPatchPos >= convertInfo.Patches.Count) break;
                if ((pos <= convertInfo.Patches[nextPatchPos].Position) && ((pos + size) >= convertInfo.Patches[nextPatchPos].Position))
                {
                    buffer[convertInfo.Patches[nextPatchPos].Position - pos] = convertInfo.Patches[nextPatchPos].Data;
                    nextPatchPos++;
                }
                else break;
            }
        }

        protected void WriteSTARTDAT(Stream outputStream)
        {
            Notify?.Invoke(PopstationEventEnum.WriteSpecialData, null);

            using (var basePbp = new FileStream(convertInfo.BasePbp, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                uint[] base_header = new uint[10];
                byte[] buffer = new byte[1 * 1048576];

                basePbp.Read(base_header, 10);

                if (base_header[0] != PBPMAGIC)
                {
                    throw new Exception($"{convertInfo.BasePbp} is not a PBP file.");
                }

                basePbp.Seek(base_header[9] + 12, SeekOrigin.Begin);
                var temp = new byte[sizeof(uint)];
                basePbp.Read(temp, 0, 4);
                var x = BitConverter.ToUInt32(temp, 0);

                x += 0x50000;

                basePbp.Seek(x, SeekOrigin.Begin);
                basePbp.Read(buffer, 0, 8);

                var tempstr = Encoding.ASCII.GetString(buffer, 0, 8);

                if (tempstr != "STARTDAT")
                {
                    throw new Exception($"Cannot find STARTDAT in {convertInfo.BasePbp}. Not a valid PSX eboot.pbp");
                }

                var header = new uint[2];

                basePbp.Seek(x + 16, SeekOrigin.Begin);
                basePbp.Read(header, 2);  // Read 2 ints into header

                // header[0] - the size of the header (always 0x50)
                // header[1] - the size of boot.png

                // Go back and copy 0x50 bytes starting from STARTDAT
                basePbp.Seek(x, SeekOrigin.Begin);
                basePbp.Read(buffer, 0, (int)header[0]);

                if (convertInfo.Boot.Exists)
                {
                    // Update boot size in header
                    var temp_buffer = BitConverter.GetBytes(convertInfo.Boot.Size);
                    for (var j = 0; j < sizeof(uint); j++)
                    {
                        buffer[16 + 4 + j] = temp_buffer[j];
                    }
                }

                // Write the header
                outputStream.Write(buffer, 0, (int)header[0]);


                if (!convertInfo.Boot.Exists)
                {
                    // Copy boot.png from basePbp
                    basePbp.Read(buffer, 0, (int)header[1]);
                    outputStream.Write(buffer, 0, (int)header[1]);
                }
                else
                {
                    Notify?.Invoke(PopstationEventEnum.WriteBootPng, null);
                    outputStream.WriteResource(convertInfo.Boot);

                    // Skip boot.png in basePbp
                    basePbp.Read(buffer, 0, (int)header[1]);
                    //basePbp.Seek((int)header[1], SeekOrigin.Current);
                }

                // Copy the rest of the STARTDAT (encrypted PGD?)
                while ((x = (uint)basePbp.Read(buffer, 0, 1048576)) > 0)
                {
                    outputStream.Write(buffer, 0, (int)x);
                }
            }
        }


        protected void WriteDisc(Stream outputStream, DiscInfo disc, uint psarOffset, bool isMultiDisc, CancellationToken cancellationToken)
        {
            var isoPosition = outputStream.Position - psarOffset;

            var t = new FileInfo(disc.SourceIso);
            var isoSize = (uint)t.Length;
            var actualIsoSize = isoSize;
            uint curSize;
            uint totSize;
            uint p1_offset = 0;
            uint p2_offset = 0;

            // align isoSize with block boundary
            if ((isoSize % BLOCK_SIZE) != 0)
            {
                isoSize = isoSize + (BLOCK_SIZE - (isoSize % BLOCK_SIZE));
            }

            uint x;
            Notify?.Invoke(PopstationEventEnum.WriteIsoHeader, null);

            // Write DATA.PSAR
            outputStream.Write("PSISOIMG0000", 0, 12);

            //if (!isMultiDisc)
            //{
            //    // Pad to psarOffset + 0x400
            //    outputStream.WriteInt32(0, 0xFD);
            //}
            //else
            //{
            //    // Write padded disc size?
            //    p1_offset = (uint)outputStream.Position;
            //    outputStream.WriteUInt32(isoSize + 0x100000, 1);
            //    // Pad to psarOffset + 0x400
            //    outputStream.WriteInt32(0, 0xFC);
            //}

            // Write padded disc size?
            p1_offset = (uint)outputStream.Position;
            outputStream.WriteUInt32(isoSize + 0x100000, 1);
            // Pad to psarOffset + 0x400
            outputStream.WriteInt32(0, 0xFC);


            var data1Length = Popstation.data1.Length;
            var data1 = ArrayPool<byte>.Shared.Rent(data1Length);
            Array.Copy(Popstation.data1, data1, data1Length);
            // Overlay the GameID onto the data1 template
            var titleBytes = Encoding.ASCII.GetBytes(disc.GameID);
            Array.Copy(titleBytes, 0, data1, 1, 4);
            Array.Copy(titleBytes, 4, data1, 6, 5);

            if (disc.TocData == null || disc.TocData.Length == 0)
            {
                throw new Exception("Invalid TOC");
            }

            Notify?.Invoke(PopstationEventEnum.WriteTOC, null);

            // Overlay the TOC data onto the data1 template
            Array.Copy(disc.TocData, 0, data1, 1024, disc.TocData.Length);
            outputStream.Write(data1, 0, data1Length);
            ArrayPool<byte>.Shared.Return(data1);

            if (isMultiDisc)
            {
                outputStream.WriteInt32(0, 1);
            }
            else
            {
                p2_offset = (uint)outputStream.Position;
                outputStream.WriteUInt32(isoSize + 0x100000 + 0x2d31, 1);
            }

            var data2Length = Popstation.data2.Length;
            var data2 = ArrayPool<byte>.Shared.Rent(data2Length);
            Array.Copy(Popstation.data2, data2, data2Length);

            // Overlay the title onto the data2 template
            titleBytes = Encoding.ASCII.GetBytes(disc.GameTitle);
            Array.Copy(titleBytes, 0, data2, 8, disc.GameTitle.Length);
            outputStream.Write(data2, 0, data2Length);
            ArrayPool<byte>.Shared.Return(data2);

            var index_offset = (uint)outputStream.Position;

            //Notify?.Invoke(PopstationEventEnum.WriteIndex, null);

            //memset(dummy, 0, sizeof(dummy));

            uint offset = 0;


            if (convertInfo.CompressionLevel == 0)
            {
                x = BLOCK_SIZE;
            }
            else
            {
                x = 0;
            }

            var dummy = new uint[6];

            for (var i = 0; i < isoSize / BLOCK_SIZE; i++)
            {
                outputStream.WriteUInt32(offset, 1);
                outputStream.WriteUInt32(x, 1);
                outputStream.Write(dummy, 0, sizeof(uint) * dummy.Length);

                if (convertInfo.CompressionLevel == 0)
                    offset += BLOCK_SIZE;
            }

            offset = (uint)outputStream.Position;

            for (var i = 0; i < (isoPosition + psarOffset + 0x100000) - offset; i++)
            {
                outputStream.WriteByte(0);
            }

            //Console.WriteLine("Writing iso #%d (%s)...\n", ciso + 1, inputs[ciso]);
            curSize = 0;
            totSize = 0;

            Notify?.Invoke(PopstationEventEnum.WriteIso, null);
            Notify?.Invoke(PopstationEventEnum.WriteSize, disc.IsoSize);


            using (var inputStream = new FileStream(disc.SourceIso, FileMode.Open, FileAccess.Read))
            {
                if (convertInfo.CompressionLevel == 0)
                {
                    // 1MB buffer
                    byte[] buffer = new byte[BUFFER_SIZE];
                    uint bytesRead;

                    while ((bytesRead = (uint)inputStream.Read(buffer, 0, BUFFER_SIZE)) > 0)
                    {
                        outputStream.Write(buffer, 0, (int)bytesRead);
                        totSize += bytesRead;
                        curSize += bytesRead;
                        // PostMessage(convertInfo.callback, WM_CONVERT_PROGRESS, 0, totSize);
                        Notify?.Invoke(PopstationEventEnum.ConvertProgress, curSize);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                    }

                    for (var i = 0; i < (isoSize - actualIsoSize); i++)
                    {
                        outputStream.WriteByte(0);
                    }
                }
                else
                {
                    var indexes = new IsoIndex[(isoSize / BLOCK_SIZE)];

                    var i = 0;
                    offset = 0;
                    uint bytesRead;
                    byte[] readBuffer = new byte[BLOCK_SIZE];
                    byte[] compressedBuffer = new byte[BUFFER_SIZE];

                    while ((bytesRead = (uint)inputStream.Read(readBuffer, 0, BLOCK_SIZE)) > 0)
                    {
                        totSize += bytesRead;
                        curSize += bytesRead;

                        if (bytesRead < BLOCK_SIZE)
                        {
                            // Clear out the rest of the buffer if we didn't read enough
                            for (var j = 0; j < BLOCK_SIZE - bytesRead; j++)
                            {
                                readBuffer[j + bytesRead] = 0;
                            }
                        }

                        var bufferSize = (uint)Compression.Compress(readBuffer, compressedBuffer, convertInfo.CompressionLevel);

                        bytesRead = bufferSize;

                        indexes[i] = new IsoIndex();
                        indexes[i].Offset = offset;

                        if (bytesRead >= BLOCK_SIZE) /* Block didn't compress */
                        {
                            indexes[i].Length = BLOCK_SIZE;
                            outputStream.Write(readBuffer, 0, BLOCK_SIZE);
                            offset += BLOCK_SIZE;
                        }
                        else
                        {
                            indexes[i].Length = bytesRead;
                            outputStream.Write(compressedBuffer, 0, (int)bytesRead);
                            offset += bytesRead;
                        }

                        Notify?.Invoke(PopstationEventEnum.WriteProgress, curSize);

                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        i++;
                    }

                    if (i != (isoSize / BLOCK_SIZE))
                    {
                        throw new Exception("Some error happened.\n");
                    }

                    offset = (uint)outputStream.Position;

                    uint end_offset = 0;

                    if (!isMultiDisc)
                    {
                        if ((offset % 0x10) != 0)
                        {
                            end_offset = offset + (0x10 - (offset % 0x10));

                            for (var block = 0; block < (end_offset - offset); block++)
                            {
                                outputStream.Write('0');
                            }
                        }
                        else
                        {
                            end_offset = offset;
                        }

                        end_offset -= psarOffset;
                    }

                    offset = (uint)outputStream.Position;

                    //Console.WriteLine($"Updating compressed indexes (iso {ciso + 1})...");
                    Notify?.Invoke(PopstationEventEnum.UpdateIndex, null);


                    if (!isMultiDisc)
                    {
                        outputStream.Seek(p1_offset, SeekOrigin.Begin);
                        outputStream.WriteUInt32(end_offset, 1);

                        end_offset += 0x2d31;
                        outputStream.Seek(p2_offset, SeekOrigin.Begin);
                        outputStream.WriteUInt32(end_offset, 1);
                    }

                    outputStream.Seek(index_offset, SeekOrigin.Begin);
                    outputStream.Write(indexes, 0, (int)(4 + 4 + (6 * 4) * (isoSize / BLOCK_SIZE)));

                    outputStream.Seek(offset, SeekOrigin.Begin);
                }
            }
        }

        private SFOData BuildDefaultSFO()
        {
            var sfoBuilder = new SFOBuilder();

            sfoBuilder.AddEntry(SFOKeys.BOOTABLE, 0x01);
            sfoBuilder.AddEntry(SFOKeys.CATEGORY, SFOValues.PS1Category);
            sfoBuilder.AddEntry(SFOKeys.DISC_ID, convertInfo.MainGameID);
            sfoBuilder.AddEntry(SFOKeys.DISC_VERSION, "1.00");
            sfoBuilder.AddEntry(SFOKeys.LICENSE, SFOValues.License);
            sfoBuilder.AddEntry(SFOKeys.PARENTAL_LEVEL, SFOValues.ParentalLevel);
            sfoBuilder.AddEntry(SFOKeys.PSP_SYSTEM_VER, SFOValues.PSPSystemVersion);
            sfoBuilder.AddEntry(SFOKeys.REGION, 0x8000);
            sfoBuilder.AddEntry(SFOKeys.TITLE, convertInfo.MainGameTitle);

            return sfoBuilder.Build();
        }

        private SFOData BuildSFO(IEnumerable<SFOEntry> entries)
        {
            var sfoBuilder = new SFOBuilder(entries);
            return sfoBuilder.Build();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sfo"></param>
        /// <returns></returns>
        private uint[] BuildHeader(SFOData sfo)
        {
            // point to the end of the header
            uint currentOffset = 0x28;
            uint[] header = new uint[0x28 / 4];

            header[0] = PBPMAGIC;  // Header: PBP<null>
            header[1] = 0x10000;

            header[2] = currentOffset; // Start of SFO

            currentOffset += sfo.Size;
            header[3] = currentOffset; // Start of ICON0

            currentOffset += convertInfo.Icon0.Size;
            header[4] = currentOffset; // Start of ICON1

            currentOffset += convertInfo.Icon1.Size;
            header[5] = currentOffset; // Start of PIC0

            currentOffset += convertInfo.Pic0.Size;
            header[6] = currentOffset; // Start of PIC1

            currentOffset += convertInfo.Pic1.Size;
            header[7] = currentOffset; // Start of SND0

            currentOffset += convertInfo.Snd0.Size;
            header[8] = currentOffset; // Start of DATA.PSP

            var psarOffset = header[8] + convertInfo.DataPsp.Size;

            if ((psarOffset % 0x10000) != 0)
            {
                psarOffset += (0x10000 - (psarOffset % 0x10000));
            }

            header[9] = psarOffset;  // Start of DATA.PSAR

            return header;
        }

        const uint PBPMAGIC = 0x50425000;

        private void EnsureRequiredResourcesExist()
        {
            byte[] buffer = new byte[1 * 1048576];
            uint[] base_header = new uint[10];

            using (var basePbp = new FileStream(convertInfo.BasePbp, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Read the header from the PSP
                basePbp.Read(base_header, 10);

                if (base_header[0] != PBPMAGIC)
                {
                    throw new Exception($"{convertInfo.BasePbp} is not a PBP file.");
                }

                if (!convertInfo.Icon0.Exists)
                {
                    // Grab ICON0 from BASE.PBP
                    var icon0_size = base_header[4] - base_header[3];
                    var icon0_buffer = new byte[icon0_size];

                    basePbp.Seek(base_header[3], SeekOrigin.Begin);
                    basePbp.Read(buffer, 0, (int)icon0_size);

                    convertInfo.Icon0 = new Resource(ResourceType.ICON0, icon0_buffer, icon0_size);
                }

                if (convertInfo.DataPsp is not { Exists: true })
                {
                    uint[] psp_header = new uint[12];
                    // Go to the offset for DATA.PSAR
                    basePbp.Seek(base_header[8], SeekOrigin.Begin);
                    basePbp.Read(psp_header, 12);

                    var prx_size = psp_header[11];

                    basePbp.Seek(base_header[8], SeekOrigin.Begin);
                    basePbp.Read(buffer, 0, (int)prx_size);

                    convertInfo.DataPsp = new Resource(ResourceType.PSP, buffer, prx_size);
                }
            }
        }

        private void ProcessTOCs()
        {
            foreach (var disc in convertInfo.DiscInfos)
            {
                var t = new FileInfo(disc.SourceIso);
                var isosize = (uint)t.Length;
                if (!string.IsNullOrEmpty(disc.SourceToc))
                {
                    if (File.Exists(disc.SourceToc))
                    {
                        var cue = CueFileReader.Read(disc.SourceToc);
                        disc.TocData = cue.GetTOCData(isosize);
                    }
                    else
                    {
                        Notify?.Invoke(PopstationEventEnum.Warning, $"{disc.SourceToc} not found, using default");
                        var cue = CueFileExtensions.GetDummyCueFile();
                        disc.TocData = cue.GetTOCData(isosize);
                    }
                }
                else
                {
                    Notify?.Invoke(PopstationEventEnum.Warning, $"TOC not specified, using default");
                    var cue = CueFileExtensions.GetDummyCueFile();
                    disc.TocData = cue.GetTOCData(isosize);
                }
            }
        }


    }
}
