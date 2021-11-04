using System;
using System.Collections.Generic;
using System.IO;
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

                var sfo = BuildSFO();

                var header = BuildHeader(sfo);

                var psarOffset = header[9];

                outputStream.Write(header, 0, 0x28);

                Notify?.Invoke(PopstationEventEnum.WriteSfo, null);
                outputStream.Write(sfo);

                Notify?.Invoke(PopstationEventEnum.WriteIcon0Png, null);
                convertInfo.Icon0.Write(outputStream);

                if (convertInfo.Icon1.Exists)
                {
                    Notify?.Invoke(PopstationEventEnum.WriteIcon1Pmf, null);
                    convertInfo.Icon1.Write(outputStream);
                }

                Notify?.Invoke(PopstationEventEnum.WritePic0Png, null);
                convertInfo.Pic0.Write(outputStream);

                Notify?.Invoke(PopstationEventEnum.WritePic1Png, null);
                convertInfo.Pic1.Write(outputStream);

                if (convertInfo.Snd0.Exists)
                {
                    Notify?.Invoke(PopstationEventEnum.WriteSnd0At3, null);
                    convertInfo.Snd0.Write(outputStream);
                }

                Notify?.Invoke(PopstationEventEnum.WriteDataPsp, null);
                convertInfo.DataPsp.Write(outputStream);

                var offset = (uint)outputStream.Position;

                for (var i = 0; i < psarOffset - offset; i++)
                {
                    outputStream.WriteByte(0);
                }

                uint totSize = 0;

                for (var ciso = 0; ciso < convertInfo.DiscInfos.Count; ciso++)
                {
                    var disc = convertInfo.DiscInfos[ciso];
                    if (File.Exists(disc.SourceIso))
                    {
                        var t = new FileInfo(convertInfo.DiscInfos[ciso].SourceIso);
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
            catch (Exception e)
            {
                Notify?.Invoke(PopstationEventEnum.Error, e.Message);
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
                uint[] base_header = new uint[0x28 / 4];
                byte[] buffer = new byte[1 * 1048576];

                basePbp.Read(base_header, 1, 0x28);

                if (base_header[0] != 0x50425000)
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
                    throw new Exception($"Cannot find STARTDAT _in {convertInfo.BasePbp}. Not a valid PSX eboot.pbp");
                }

                var header = new uint[2];

                basePbp.Seek(x + 16, SeekOrigin.Begin);
                basePbp.Read(header, 0, 8);
                basePbp.Seek(x, SeekOrigin.Begin);
                basePbp.Read(buffer, 0, (int)header[0]);

                var boot = false;


                if (!boot)
                {
                    outputStream.Write(buffer, 0, (int)header[0]);
                    basePbp.Read(buffer, 0, (int)header[1]);
                    outputStream.Write(buffer, 0, (int)header[1]);
                }
                else
                {
                    Console.WriteLine("Writing boot.png...\n");
                    Notify?.Invoke(PopstationEventEnum.WriteBootPng, null);

                    //ib[5] = boot_size;
                    //var temp_buffer = BitConverter.GetBytes(boot_size);
                    //for (var j = 0; j < sizeof(uint); j++)
                    //{
                    //    buffer[5 + j] = temp_buffer[i];
                    //}

                    //outputStream.Write(buffer, 0, (int)header[0]);

                    //using (var t = new FileStream(convertInfo.Boot, FileMode.Open, FileAccess.Read, FileShare.Read))
                    //{
                    //    t.Read(buffer, 0, (int)boot_size);
                    //    outputStream.Write(buffer, 0, (int)boot_size);
                    //}

                    //basePbp.Read(buffer, 0, (int)header[1]);
                }

                //_base.Seek(x, SeekOrigin.Begin);

                while ((x = (uint)basePbp.Read(buffer, 0, 1048576)) > 0)
                {
                    outputStream.Write(buffer, 0, (int)x);
                }
            }
        }

        protected void WriteDisc(DiscInfo disc, uint iso_position, uint psarOffset, bool isMultiDisc, Stream outputStream, CancellationToken cancellationToken)
        {
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

            if (isMultiDisc)
            {
                outputStream.WriteInteger(0, 0xFD);
            }
            else
            {
                p1_offset = (uint)outputStream.Position;
                outputStream.WriteInteger(isoSize + 0x100000, 1);
                outputStream.WriteInteger(0, 0xFC);
            }

            // Overlay the GameID onto the data1 template
            var titleBytes = Encoding.ASCII.GetBytes(disc.GameID);
            Array.Copy(titleBytes, 0, Popstation.data1, 1, 4);
            Array.Copy(titleBytes, 4, Popstation.data1, 6, 5);

            if (disc.TocData == null || disc.TocData.Length == 0)
            {
                throw new Exception("Invalid TOC");
            }

            Notify?.Invoke(PopstationEventEnum.WriteToc, null);

            // Overlay the TOC data onto the data1 template
            Array.Copy(disc.TocData, 0, Popstation.data1, 1024, disc.TocData.Length);

            outputStream.Write(Popstation.data1, 0, Popstation.data1.Length);

            if (isMultiDisc)
            {
                outputStream.WriteInteger(0, 1);
            }
            else
            {
                p2_offset = (uint)outputStream.Position;
                outputStream.WriteInteger(isoSize + 0x100000 + 0x2d31, 1);
            }

            // Overlay the title onto the data2 template
            titleBytes = Encoding.ASCII.GetBytes(disc.GameTitle);
            Array.Copy(titleBytes, 0, Popstation.data2, 8, disc.GameTitle.Length);
            outputStream.Write(Popstation.data2, 0, Popstation.data2.Length);

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
                outputStream.WriteInteger(offset, 1);
                outputStream.WriteInteger(x, 1);
                outputStream.Write(dummy, 0, sizeof(uint) * dummy.Length);

                if (convertInfo.CompressionLevel == 0)
                    offset += BLOCK_SIZE;
            }

            offset = (uint)outputStream.Position;

            for (var i = 0; i < (iso_position + psarOffset + 0x100000) - offset; i++)
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
                    byte[] buffer2 = new byte[BLOCK_SIZE];
                    byte[] buffer = new byte[BUFFER_SIZE];

                    while ((bytesRead = (uint)inputStream.Read(buffer2, 0, BLOCK_SIZE)) > 0)
                    {
                        totSize += bytesRead;
                        curSize += bytesRead;

                        if (bytesRead < BLOCK_SIZE)
                        {
                            // Clear out the rest of the buffer if we didn't read enough
                            for (var j = 0; j < BLOCK_SIZE - bytesRead; j++)
                            {
                                buffer2[j + bytesRead] = 0;
                            }
                        }

                        var bufferSize = (uint)Compression.Compress(buffer2, buffer, convertInfo.CompressionLevel);

                        bytesRead = bufferSize;

                        indexes[i] = new IsoIndex();
                        indexes[i].Offset = offset;

                        if (bytesRead >= BLOCK_SIZE) /* Block didn't compress */
                        {
                            indexes[i].Length = BLOCK_SIZE;
                            outputStream.Write(buffer2, 0, BLOCK_SIZE);
                            offset += BLOCK_SIZE;
                        }
                        else
                        {
                            indexes[i].Length = bytesRead;
                            outputStream.Write(buffer, 0, (int)bytesRead);
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
                        outputStream.WriteInteger(end_offset, 1);

                        end_offset += 0x2d31;
                        outputStream.Seek(p2_offset, SeekOrigin.Begin);
                        outputStream.WriteInteger(end_offset, 1);
                    }

                    outputStream.Seek(index_offset, SeekOrigin.Begin);
                    outputStream.Write(indexes, 0, (int)(4 + 4 + (6 * 4) * (isoSize / BLOCK_SIZE)));

                    outputStream.Seek(offset, SeekOrigin.Begin);
                }
            }
        }

        private SFOData BuildSFO()
        {
            var sfoBuilder = new SFOBuilder();

            sfoBuilder.AddEntry(SFOKeys.BOOTABLE, 0x01);
            sfoBuilder.AddEntry(SFOKeys.CATEGORY, SFOValues.PS1Category);
            sfoBuilder.AddEntry(SFOKeys.DISC_ID, convertInfo.MainGameID);
            sfoBuilder.AddEntry(SFOKeys.DISC_VERSION, "1.00");
            sfoBuilder.AddEntry(SFOKeys.LICENSE, SFOValues.License);
            sfoBuilder.AddEntry(SFOKeys.PARENTAL_LEVEL, 0x01);
            sfoBuilder.AddEntry(SFOKeys.PSP_SYSTEM_VER, "3.01");
            sfoBuilder.AddEntry(SFOKeys.REGION, 0x8000);
            sfoBuilder.AddEntry(SFOKeys.TITLE, convertInfo.MainGameTitle);

            return sfoBuilder.Build();
        }


        private uint[] BuildHeader(SFOData sfo)
        {
            uint curoffs = 0x28;
            uint[] header = new uint[0x28 / 4];

            header[0] = 0x50425000;
            header[1] = 0x10000;

            header[2] = curoffs;

            curoffs += sfo.Size;
            header[3] = curoffs;

            curoffs += convertInfo.Icon0.Size;
            header[4] = curoffs;

            curoffs += convertInfo.Icon1.Size;
            header[5] = curoffs;

            curoffs += convertInfo.Pic0.Size;
            header[6] = curoffs;

            curoffs += convertInfo.Pic1.Size;
            header[7] = curoffs;

            curoffs += convertInfo.Snd0.Size;
            header[8] = curoffs;

            var psarOffset = header[8] + convertInfo.DataPsp.Size;

            if ((psarOffset % 0x10000) != 0)
            {
                psarOffset = psarOffset + (0x10000 - (psarOffset % 0x10000));
            }

            header[9] = psarOffset;

            return header;
        }

        private void EnsureRequiredResourcesExist()
        {
            byte[] buffer = new byte[1 * 1048576];
            uint[] base_header = new uint[0x28 / 4];

            using (var basePbp = new FileStream(convertInfo.BasePbp, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                basePbp.Read(base_header, 1, 0x28);

                if (base_header[0] != 0x50425000)
                {
                    throw new Exception($"{convertInfo.BasePbp} is not a PBP file.");
                }

                if (!convertInfo.Icon0.Exists)
                {
                    var icon0_size = base_header[4] - base_header[3];
                    var icon0_buffer = new byte[icon0_size];

                    basePbp.Seek(base_header[3], SeekOrigin.Begin);
                    basePbp.Read(buffer, 0, (int)icon0_size);

                    convertInfo.Icon0 = new Resource(ResourceType.ICON0, icon0_buffer, icon0_size);
                }

                if (convertInfo.DataPsp == null || !convertInfo.DataPsp.Exists)
                {
                    uint[] psp_header = new uint[0x30 / 4];
                    basePbp.Seek(base_header[8], SeekOrigin.Begin);
                    basePbp.Read(psp_header, 1, 0x30);

                    var prx_size = psp_header[0x2C / 4];

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
