using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Popstation
{
    public static class Helper
    {
        public static byte ToBinaryDecimal(int value)
        {
            var ones = value % 10;
            var tens = value / 10;
            return (byte)(tens * 0x10 + ones);
        }

        public static IndexPosition PositionFromFrames(long frames)
        {
            int totalSeconds = (int)(frames / 75);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            frames = frames % 75;

            var position = new IndexPosition()
            {
                Minutes = minutes,
                Seconds = seconds,
                Frames = (int)frames,
            };

            return position;
        }
    }

    public partial class Popstation
    {
        const int BLOCK_SIZE = 0x9300;

        public Action<PopstationEventEnum, object> OnEvent { get; set; }

        int nextPatchPos;

        public Task Convert(ConvertIsoInfo convertInfo, CancellationToken cancellationToken)
        {
            if (convertInfo.Patches?.Count > 0)
            {
                nextPatchPos = 0;
            }

            if (convertInfo.DiscInfos.Count == 1)
            {
                return Task.Run(() => ConvertIso(convertInfo, cancellationToken));
            }
            else
            {
                return Task.Run(() => ConvertMultiIso(convertInfo, cancellationToken));
            }
        }

        private void PatchData(ConvertIsoInfo convertInfo, byte[] buffer, int size, int pos)
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


        private void ConvertMultiIso(ConvertIsoInfo convertInfo, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[1 * 1048576];
            byte[] buffer2 = new byte[BLOCK_SIZE];
            uint totSize;

            bool pic0 = false, pic1 = false, icon0 = false, icon1 = false, snd = false, toc = false, boot = false, prx = false;
            uint pic0_size, pic1_size, icon0_size, icon1_size, snd_size, toc_size, boot_size, prx_size;

            boot_size = 0;

            uint[] psp_header = new uint[0x30 / 4];
            uint[] base_header = new uint[0x28 / 4];
            uint[] header = new uint[0x28 / 4];
            uint[] dummy = new uint[6];

            uint i, offset, isosize, isorealsize, x;
            uint index_offset, p1_offset, p2_offset, m_offset, end_offset;
            IsoIndex[] indexes = null;
            uint[] iso_positions = new uint[5];
            int ciso;
            //z_stream z;
            end_offset = 0;

            string output;
            string title;

            string code;

            output = convertInfo.DestinationPbp;
            title = convertInfo.MainGameTitle;
            code = convertInfo.MainGameID;

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

            var sfo = sfoBuilder.Build();

            using (var _base = new FileStream(convertInfo.BasePbp, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var _out = new FileStream(output, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    OnEvent?.Invoke(PopstationEventEnum.WritePbpHeader, null);

                    _base.Read(base_header, 1, 0x28);

                    if (base_header[0] != 0x50425000)
                    {
                        throw new Exception($"{convertInfo.BasePbp} is not a PBP file.");
                    }

                    if (File.Exists(convertInfo.Icon0))
                    {
                        var t = new FileInfo(convertInfo.Icon0);
                        icon0_size = (uint)t.Length;
                        icon0 = true;
                    }
                    else
                    {
                        icon0_size = base_header[4] - base_header[3];
                    }

                    if (File.Exists(convertInfo.Icon1))
                    {
                        var t = new FileInfo(convertInfo.Icon1);
                        icon1_size = (uint)t.Length;
                        icon1 = true;
                    }
                    else
                    {
                        icon1_size = 0;
                    }

                    if (File.Exists(convertInfo.Pic0))
                    {
                        var t = new FileInfo(convertInfo.Pic0);
                        pic0_size = (uint)t.Length;
                        pic0 = true;
                    }
                    else
                    {
                        pic0_size = 0; //base_header[6] - base_header[5];
                    }


                    if (File.Exists(convertInfo.Pic1))
                    {
                        var t = new FileInfo(convertInfo.Pic1);
                        pic1_size = (uint)t.Length;
                        pic1 = true;
                    }
                    else
                    {
                        pic1_size = 0; // base_header[7] - base_header[6];
                    }


                    if (File.Exists(convertInfo.Snd0))
                    {
                        var t = new FileInfo(convertInfo.Snd0);
                        snd_size = (uint)t.Length;
                        snd = true;
                    }
                    else
                    {
                        snd_size = 0;
                    }

                    if (File.Exists(convertInfo.Boot))
                    {
                        var t = new FileInfo(convertInfo.Boot);
                        boot_size = (uint)t.Length;
                        boot = true;
                    }
                    else
                    {
                        //boot = false;
                    }

                    if (File.Exists(convertInfo.DataPsp))
                    {
                        var t = new FileInfo(convertInfo.DataPsp);
                        prx_size = (uint)t.Length;
                        prx = true;
                    }
                    else
                    {
                        _base.Seek(base_header[8], SeekOrigin.Begin);
                        _base.Read(psp_header, 1, 0x30);

                        prx_size = psp_header[0x2C / 4];
                    }

                    uint curoffs = 0x28;

                    header[0] = 0x50425000;
                    header[1] = 0x10000;

                    header[2] = curoffs;

                    curoffs += sfo.Size;
                    header[3] = curoffs;

                    curoffs += icon0_size;
                    header[4] = curoffs;

                    curoffs += icon1_size;
                    header[5] = curoffs;

                    curoffs += pic0_size;
                    header[6] = curoffs;

                    curoffs += pic1_size;
                    header[7] = curoffs;

                    curoffs += snd_size;
                    header[8] = curoffs;

                    x = header[8] + prx_size;

                    if ((x % 0x10000) != 0)
                    {
                        x = x + (0x10000 - (x % 0x10000));
                    }

                    header[9] = x;

                    _out.Write(header, 0, 0x28);

                    OnEvent?.Invoke(PopstationEventEnum.WriteSfo, null);

                    _out.Write(sfo);

                    OnEvent?.Invoke(PopstationEventEnum.WriteIcon0Png, null);

                    if (!icon0)
                    {
                        _base.Seek(base_header[3], SeekOrigin.Begin);
                        _base.Read(buffer, 0, (int)icon0_size);
                        _out.Write(buffer, 0, (int)icon0_size);
                    }
                    else
                    {
                        using (var t = new FileStream(convertInfo.Icon0, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            t.Read(buffer, 0, (int)icon0_size);
                            _out.Write(buffer, 0, (int)icon0_size);
                        }
                    }

                    if (icon1)
                    {
                        OnEvent?.Invoke(PopstationEventEnum.WriteIcon1Pmf, null);

                        using (var t = new FileStream(convertInfo.Icon1, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            t.Read(buffer, 0, (int)icon1_size);
                            _out.Write(buffer, 0, (int)icon0_size);
                        }
                    }

                    if (!pic0)
                    {
                        //_base.Seek(base_header[5], SeekOrigin.Begin);
                        //_base.Read(buffer, 1, pic0_size);
                        //_out.Write(buffer, 1, pic0_size);
                    }
                    else
                    {
                        OnEvent?.Invoke(PopstationEventEnum.WritePic0Png, null);

                        using (var t = new FileStream(convertInfo.Pic0, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            t.Read(buffer, 0, (int)pic0_size);
                            _out.Write(buffer, 0, (int)pic0_size);
                        }
                    }

                    if (!pic1)
                    {
                        //_base.Seek(base_header[6], SeekOrigin.Begin);
                        //_base.Read(buffer, 0, pic1_size);
                        //_out.Write(buffer, 0, pic1_size);		
                    }
                    else
                    {
                        OnEvent?.Invoke(PopstationEventEnum.WritePic1Png, null);

                        using (var t = new FileStream(convertInfo.Pic1, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            t.Read(buffer, 0, (int)pic1_size);
                            _out.Write(buffer, 0, (int)pic1_size);
                        }
                    }

                    if (snd)
                    {
                        OnEvent?.Invoke(PopstationEventEnum.WriteSnd0At3, null);

                        using (var t = new FileStream(convertInfo.Snd0, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            t.Read(buffer, 0, (int)snd_size);
                            _out.Write(buffer, 0, (int)snd_size);
                        }
                    }

                    OnEvent?.Invoke(PopstationEventEnum.WriteDataPsp, null);

                    if (prx)
                    {
                        using (var t = new FileStream(convertInfo.DataPsp, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            t.Read(buffer, 0, (int)prx_size);
                            _out.Write(buffer, 0, (int)prx_size);
                        }
                    }
                    else
                    {
                        _base.Seek(base_header[8], SeekOrigin.Begin);
                        _base.Read(buffer, 0, (int)prx_size);
                        _out.Write(buffer, 0, (int)prx_size);
                    }

                    offset = (uint)_out.Position;

                    for (i = 0; i < header[9] - offset; i++)
                    {
                        _out.WriteByte(0);
                    }

                    OnEvent?.Invoke(PopstationEventEnum.WritePsTitle, null);

                    _out.Write("PSTITLEIMG000000", 0, 16);

                    // Save this offset position
                    p1_offset = (uint)_out.Position;

                    _out.WriteInteger(0, 2);
                    _out.WriteInteger(0x2CC9C5BC, 1);
                    _out.WriteInteger(0x33B5A90F, 1);
                    _out.WriteInteger(0x06F6B4B3, 1);
                    _out.WriteInteger(0xB25945BA, 1);
                    _out.WriteInteger(0, 0x76);

                    m_offset = (uint)_out.Position;

                    //memset(iso_positions, 0, sizeof(iso_positions));
                    _out.Write(iso_positions, 1, sizeof(uint) * 5);

                    _out.WriteRandom(12);
                    _out.WriteInteger(0, 8);

                    _out.Write('_');
                    _out.Write(code, 0, 4);
                    _out.Write('_');
                    _out.Write(code, 4, 5);

                    _out.WriteChar(0, 0x15);

                    p2_offset = (uint)_out.Position;
                    _out.WriteInteger(0, 2);

                    _out.Write(data3, 0, data3.Length);
                    _out.Write(title, 0, title.Length);

                    _out.WriteChar(0, 0x80 - title.Length);
                    _out.WriteInteger(7, 1);
                    _out.WriteInteger(0, 0x1C);

                    Stream _in;
                    //Get size of all isos
                    totSize = 0;
                    for (ciso = 0; ciso < convertInfo.DiscInfos.Count; ciso++)
                    {
                        if (File.Exists(convertInfo.DiscInfos[ciso].SourceIso))
                        {
                            var t = new FileInfo(convertInfo.DiscInfos[ciso].SourceIso);
                            isosize = (uint)t.Length;
                            totSize += isosize;
                        }
                    }

                    //TODO: Callback
                    //PostMessage(convertInfo.callback, WM_CONVERT_SIZE, 0, totSize);

                    totSize = 0;
                    var lastTicks = DateTime.Now.Ticks;

                    for (ciso = 0; ciso < convertInfo.DiscInfos.Count; ciso++)
                    {
                        var disc = convertInfo.DiscInfos[ciso];

                        if (!File.Exists(disc.SourceIso))
                        {
                            continue;
                        }

                        using (_in = new FileStream(disc.SourceIso, FileMode.Open, FileAccess.Read))
                        {

                            var t = new FileInfo(disc.SourceIso);
                            isosize = (uint)t.Length;
                            isorealsize = isosize;

                            if ((isosize % BLOCK_SIZE) != 0)
                            {
                                isosize = isosize + (BLOCK_SIZE - (isosize % BLOCK_SIZE));
                            }

                            offset = (uint)_out.Position;

                            if (offset % 0x8000 == 0)
                            {
                                x = 0x8000 - (offset % 0x8000);
                                _out.WriteChar(0, (int)x);
                            }

                            iso_positions[ciso] = (uint)_out.Position - header[9];

                            OnEvent?.Invoke(PopstationEventEnum.WriteIsoHeader, ciso + 1);

                            // Write DATA.PSAR
                            _out.Write("PSISOIMG0000", 0, 12);

                            _out.WriteInteger(0, 0xFD);

                            //TODO??
                            var titleBytes = Encoding.ASCII.GetBytes(disc.GameID);
                            Array.Copy(titleBytes, 0, data1, 1, 4);

                            titleBytes = Encoding.ASCII.GetBytes(disc.GameID);
                            Array.Copy(titleBytes, 4, data1, 6, 5);

                            //memcpy(data1, 1, codes[ciso], 4);
                            //memcpy(data1, 6, codes[ciso] + 4, 5);
                            _out.Write(data1, 0, data1.Length);

                            _out.WriteInteger(0, 1);

                            //TODO:
                            titleBytes = Encoding.ASCII.GetBytes(disc.GameTitle);
                            Array.Copy(titleBytes, 0, data2, 8, disc.GameTitle.Length);
                            //strcpy((char*)(data2 + 8), titles[ciso]);
                            _out.Write(data2, 0, data2.Length);

                            index_offset = (uint)_out.Position;

                            Console.WriteLine("Writing indexes (iso #%d)...\n", ciso + 1);
                            OnEvent?.Invoke(PopstationEventEnum.WriteIndex, ciso + 1);

                            //memset(dummy, 0, sizeof(dummy));

                            offset = 0;

                            if (convertInfo.CompressionLevel == 0)
                            {
                                x = BLOCK_SIZE;
                            }
                            else
                            {
                                x = 0;
                            }

                            for (i = 0; i < isosize / BLOCK_SIZE; i++)
                            {
                                _out.WriteInteger(offset, 1);
                                _out.WriteInteger(x, 1);
                                _out.Write(dummy, 0, sizeof(uint) * dummy.Length);

                                if (convertInfo.CompressionLevel == 0)
                                    offset += BLOCK_SIZE;
                            }

                            offset = (uint)_out.Position;

                            for (i = 0; i < (iso_positions[ciso] + header[9] + 0x100000) - offset; i++)
                            {
                                _out.WriteByte(0);
                            }

                            //Console.WriteLine("Writing iso #%d (%s)...\n", ciso + 1, inputs[ciso]);
                            OnEvent?.Invoke(PopstationEventEnum.WriteIso, ciso + 1);

                            if (convertInfo.CompressionLevel == 0)
                            {
                                while ((x = (uint)_in.Read(buffer, 0, 1048576)) > 0)
                                {
                                    _out.Write(buffer, 0, (int)x);
                                    totSize += x;
                                    // PostMessage(convertInfo.callback, WM_CONVERT_PROGRESS, 0, totSize);
                                    OnEvent?.Invoke(PopstationEventEnum.ConvertProgress, totSize);

                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        return;
                                    }
                                }

                                for (i = 0; i < (isosize - isorealsize); i++)
                                {
                                    _out.WriteByte(0);
                                }
                            }
                            else
                            {
                                indexes = new IsoIndex[(isosize / BLOCK_SIZE)];

                                i = 0;
                                offset = 0;

                                while ((x = (uint)_in.Read(buffer2, 0, BLOCK_SIZE)) > 0)
                                {
                                    totSize += x;

                                    if (x < BLOCK_SIZE)
                                    {
                                        for (var j = 0; j < BLOCK_SIZE - x; j++)
                                        {
                                            buffer2[j + x] = 0;
                                        }
                                        //memset(buffer2 + x, 0, BlockSize - x);
                                    }

                                    var bufferSize = (uint)Compression.Compress(buffer2, buffer, convertInfo.CompressionLevel);

                                    x = bufferSize;

                                    indexes[i] = new IsoIndex();
                                    indexes[i].Offset = offset;

                                    if (x >= BLOCK_SIZE) /* Block didn't compress */
                                    {
                                        indexes[i].Length = BLOCK_SIZE;
                                        _out.Write(buffer2, 0, BLOCK_SIZE);
                                        offset += BLOCK_SIZE;
                                    }
                                    else
                                    {
                                        indexes[i].Length = x;
                                        _out.Write(buffer, 0, (int)x);
                                        offset += x;
                                    }

                                    OnEvent?.Invoke(PopstationEventEnum.WriteProgress, ciso + 1);

                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        return;
                                    }

                                    i++;
                                }

                                if (i != (isosize / BLOCK_SIZE))
                                {
                                    throw new Exception("Some error happened.\n");
                                }

                            }
                        }

                        if (convertInfo.CompressionLevel != 0)
                        {
                            offset = (uint)_out.Position;

                            //Console.WriteLine($"Updating compressed indexes (iso {ciso + 1})...");
                            OnEvent?.Invoke(PopstationEventEnum.UpdateIndex, ciso + 1);

                            _out.Seek(index_offset, SeekOrigin.Begin);
                            //TODO: 
                            _out.Write(indexes, 0, (int)(4 + 4 + (6 * 4) * (isosize / BLOCK_SIZE)));

                            _out.Seek(offset, SeekOrigin.Begin);
                        }
                    }

                    x = (uint)_out.Position;

                    if ((x % 0x10) != 0)
                    {
                        end_offset = x + (0x10 - (x % 0x10));

                        for (i = 0; i < (end_offset - x); i++)
                        {
                            _out.Write('0');
                        }
                    }
                    else
                    {
                        end_offset = x;
                    }

                    end_offset -= header[9];

                    Console.WriteLine("Writing special data...\n");
                    OnEvent?.Invoke(PopstationEventEnum.WriteSpecialData, null);

                    _base.Seek(base_header[9] + 12, SeekOrigin.Begin);
                    var temp = new byte[sizeof(uint)];
                    _base.Read(temp, 0, 4);
                    x = BitConverter.ToUInt32(temp, 0);

                    x += 0x50000;

                    _base.Seek(x, SeekOrigin.Begin);
                    _base.Read(buffer, 0, 8);

                    var tempstr = System.Text.Encoding.ASCII.GetString(buffer, 0, 8);

                    if (tempstr != "STARTDAT")
                    {
                        throw new Exception($"Cannot find STARTDAT _in {convertInfo.BasePbp}. Not a valid PSX eboot.pbp");
                    }

                    _base.Seek(x + 16, SeekOrigin.Begin);
                    _base.Read(header, 0, 8);
                    _base.Seek(x, SeekOrigin.Begin);
                    _base.Read(buffer, 0, (int)header[0]);

                    if (!boot)
                    {
                        _out.Write(buffer, 0, (int)header[0]);
                        _base.Read(buffer, 0, (int)header[1]);
                        _out.Write(buffer, 0, (int)header[1]);
                    }
                    else
                    {
                        Console.WriteLine("Writing boot.png...\n");
                        OnEvent?.Invoke(PopstationEventEnum.WriteBootPng, null);

                        //ib[5] = boot_size;
                        var temp_buffer = BitConverter.GetBytes(boot_size);
                        for (var j = 0; j < sizeof(uint); j++)
                        {
                            buffer[5 + j] = temp_buffer[i];
                        }

                        _out.Write(buffer, 0, (int)header[0]);

                        using (var t = new FileStream(convertInfo.Boot, FileMode.Open, FileAccess.Read, FileShare.Read))
                        {
                            t.Read(buffer, 0, (int)boot_size);
                            _out.Write(buffer, 0, (int)boot_size);
                        }

                        _base.Read(buffer, 0, (int)header[1]);
                    }

                    //_base.Seek(x, SeekOrigin.Begin);

                    while ((x = (uint)_base.Read(buffer, 0, 1048576)) > 0)
                    {
                        _out.Write(buffer, 0, (int)x);
                    }

                    _out.Seek(p1_offset, SeekOrigin.Begin);
                    _out.WriteInteger(end_offset, 1);

                    end_offset += 0x2d31;
                    _out.Seek(p2_offset, SeekOrigin.Begin);
                    _out.WriteInteger(end_offset, 1);

                    _out.Seek(m_offset, SeekOrigin.Begin);
                    _out.Write(iso_positions, 1, sizeof(uint) * iso_positions.Length);

                }
            }

        }

        private byte GetTrackType(string trackType)
        {
            switch (trackType)
            {
                case "MODE2/2352":
                    return 0x41;
                case "AUDIO":
                    return 0x01;
            }
            throw new ArgumentOutOfRangeException();
        }


        private void ConvertIso(ConvertIsoInfo convertInfo, CancellationToken cancellationToken)
        {
            uint i, j, offset, isosize, isorealsize, x;
            uint index_offset, p1_offset, p2_offset, end_offset;
            IsoIndex[] indexes = null;
            uint curoffs = 0x28;

            end_offset = 0;

            int complevel;

            byte[] buffer = new byte[1 * 1048576];
            byte[] buffer2 = new byte[BLOCK_SIZE];
            uint totSize;

            bool pic0 = false, pic1 = false, icon0 = false, icon1 = false, snd = false, toc = false, boot = false, prx = false;
            uint pic0_size, pic1_size, icon0_size, icon1_size, snd_size, toc_size, boot_size, prx_size;

            boot_size = 0;

            uint[] psp_header = new uint[0x30 / 4];
            uint[] base_header = new uint[0x28 / 4];
            uint[] header = new uint[0x28 / 4];
            uint[] dummy = new uint[6];

            int blockCount = 0;
            //uint i, offset, isosize, isorealsize, x;
            //uint index_offset, p1_offset, p2_offset, m_offset, end_offset;

            var disc = convertInfo.DiscInfos[0];

            complevel = convertInfo.CompressionLevel;


            var iso_index = new List<INDEX>();

            using (var _in = new FileStream(disc.SourceIso, FileMode.Open, FileAccess.Read))
            {

                //Check if input is pbp
                if (Path.GetExtension(disc.SourceIso).ToLower() == ".pbp")
                {
                    iso_index = ReadIsoIndexes(disc.SourceIso);
                    blockCount = iso_index.Count;
                    if (iso_index.Count == 0) throw new Exception("No iso index was found.");
                    isosize = (uint)GetIsoSize(disc.SourceIso, iso_index);
                }
                else
                {
                    _in.Seek(0, SeekOrigin.End);
                    isosize = (uint)_in.Position;
                    _in.Seek(0, SeekOrigin.Begin);
                }

                isorealsize = isosize;

                if (!string.IsNullOrEmpty(disc.SourceToc))
                {

                    var reader = new CueReader();
                    var cueFiles = reader.Read(disc.SourceToc);
                    var tracks = cueFiles.SelectMany(cf => cf.Tracks).ToList();

                    convertInfo.TocData = new byte[0xA * (tracks.Count + 3)];

                    var trackBuffer = new byte[0xA];

                    var frames = isosize / 2352;
                    var position = Helper.PositionFromFrames(frames);

                    var ctr = 0;

                    trackBuffer[0] = GetTrackType(tracks.First().DataType);
                    trackBuffer[1] = 0x00;
                    trackBuffer[2] = 0xA0;
                    trackBuffer[3] = 0x00;
                    trackBuffer[4] = 0x00;
                    trackBuffer[5] = 0x00;
                    trackBuffer[6] = 0x00;
                    trackBuffer[7] = Helper.ToBinaryDecimal(tracks.First().Number);
                    trackBuffer[8] = Helper.ToBinaryDecimal(0x20);
                    trackBuffer[9] = 0x00;

                    Array.Copy(trackBuffer, 0, convertInfo.TocData, ctr, 0xA);
                    ctr += 0xA;

                    trackBuffer[0] = GetTrackType(tracks.Last().DataType);
                    trackBuffer[2] = 0xA1;
                    trackBuffer[7] = Helper.ToBinaryDecimal(tracks.Last().Number);
                    trackBuffer[8] = 0x00;

                    Array.Copy(trackBuffer, 0, convertInfo.TocData, ctr, 0xA);
                    ctr += 0xA;

                    trackBuffer[0] = 0x01;
                    trackBuffer[2] = 0xA2;
                    trackBuffer[7] = Helper.ToBinaryDecimal(position.Minutes);
                    trackBuffer[8] = Helper.ToBinaryDecimal(position.Seconds);
                    trackBuffer[9] = Helper.ToBinaryDecimal(position.Frames);

                    Array.Copy(trackBuffer, 0, convertInfo.TocData, ctr, 0xA);
                    ctr += 0xA;

                    foreach (var track in tracks)
                    {
                        trackBuffer[0] = GetTrackType(track.DataType);
                        trackBuffer[1] = 0x00;
                        trackBuffer[2] = Helper.ToBinaryDecimal(track.Number);
                        var pos = track.Indexes.First(idx => idx.Number == 1).Position;
                        trackBuffer[3] = Helper.ToBinaryDecimal(pos.Minutes);
                        trackBuffer[4] = Helper.ToBinaryDecimal(pos.Seconds);
                        trackBuffer[5] = Helper.ToBinaryDecimal(pos.Frames);
                        trackBuffer[6] = 0x00;
                        trackBuffer[7] = Helper.ToBinaryDecimal(pos.Minutes);
                        trackBuffer[8] = Helper.ToBinaryDecimal(pos.Seconds + 2);
                        trackBuffer[9] = Helper.ToBinaryDecimal(pos.Frames);

                        Array.Copy(trackBuffer, 0, convertInfo.TocData, ctr, 0xA);
                        ctr += 0xA;
                    }

                    //0x00    1 byte Track type - 0x41 = data track, 0x01 = audio track
                    //0x01    1 byte Always null
                    //0x02    1 byte The track number in "binary decimal"
                    //0x03    3 bytes The absolute track start address in "binary decimal" - first byte is minutes, second is seconds, third is frames
                    //0x06    1 byte Always null
                    //0x07    3 bytes The "relative" track address -same as before, and uses MM: SS: FF format

                }


                //PostMessage(convertInfo.callback, WM_CONVERT_SIZE, 0, isosize);
                OnEvent?.Invoke(PopstationEventEnum.ConvertSize, isosize);

                if ((isosize % BLOCK_SIZE) != 0)
                {
                    isosize = isosize + (BLOCK_SIZE - (isosize % BLOCK_SIZE));
                }

                //Console.WriteLine("isosize, isorealsize %08X  %08X\n", isosize, isorealsize);
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

                var sfo = sfoBuilder.Build();

                using (var _base = new FileStream(convertInfo.BasePbp, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using (var _out = new FileStream(convertInfo.DestinationPbp, FileMode.Create, FileAccess.Write, FileShare.Write))
                    {
                        OnEvent?.Invoke(PopstationEventEnum.WriteHeader, null);

                        _base.Read(base_header, 1, 0x28);

                        if (base_header[0] != 0x50425000)
                        {
                            throw new Exception($"{convertInfo.BasePbp} is not a PBP file.");
                        }

                        //sfo_size = base_header[3] - base_header[2];

                        if (File.Exists(convertInfo.Icon0))
                        {
                            var t = new FileInfo(convertInfo.Icon0);
                            icon0_size = (uint)t.Length;
                            icon0 = true;
                        }
                        else
                        {
                            icon0_size = base_header[4] - base_header[3];
                        }

                        if (File.Exists(convertInfo.Icon1))
                        {
                            var t = new FileInfo(convertInfo.Icon1);
                            icon1_size = (uint)t.Length;
                            icon1 = true;
                        }
                        else
                        {
                            icon1_size = 0;
                        }

                        if (File.Exists(convertInfo.Pic0))
                        {
                            var t = new FileInfo(convertInfo.Pic0);
                            pic0_size = (uint)t.Length;
                            pic0 = true;
                        }
                        else
                        {
                            pic0_size = 0; //base_header[6] - base_header[5];
                        }


                        if (File.Exists(convertInfo.Pic1))
                        {
                            var t = new FileInfo(convertInfo.Pic1);
                            pic1_size = (uint)t.Length;
                            pic1 = true;
                        }
                        else
                        {
                            pic1_size = 0; // base_header[7] - base_header[6];
                        }


                        if (File.Exists(convertInfo.Snd0))
                        {
                            var t = new FileInfo(convertInfo.Snd0);
                            snd_size = (uint)t.Length;
                            snd = true;
                        }
                        else
                        {
                            snd_size = 0;
                        }

                        if (File.Exists(convertInfo.Boot))
                        {
                            var t = new FileInfo(convertInfo.Boot);
                            boot_size = (uint)t.Length;
                            boot = true;
                        }
                        else
                        {
                            //boot = false;
                        }

                        if (File.Exists(convertInfo.DataPsp))
                        {
                            var t = new FileInfo(convertInfo.DataPsp);
                            prx_size = (uint)t.Length;
                            prx = true;
                        }
                        else
                        {
                            _base.Seek(base_header[8], SeekOrigin.Begin);
                            _base.Read(psp_header, 1, 0x30);

                            prx_size = psp_header[0x2C / 4];
                        }


                        header[0] = 0x50425000;
                        header[1] = 0x10000;

                        header[2] = curoffs;

                        curoffs += sfo.Size;
                        header[3] = curoffs;

                        curoffs += icon0_size;
                        header[4] = curoffs;

                        curoffs += icon1_size;
                        header[5] = curoffs;

                        curoffs += pic0_size;
                        header[6] = curoffs;

                        curoffs += pic1_size;
                        header[7] = curoffs;

                        curoffs += snd_size;
                        header[8] = curoffs;

                        x = header[8] + prx_size;

                        if ((x % 0x10000) != 0)
                        {
                            x = x + (0x10000 - (x % 0x10000));
                        }

                        header[9] = x;

                        _out.Write(header, 0, 0x28);

                        OnEvent?.Invoke(PopstationEventEnum.WriteSfo, null);

                        //_base.Seek(base_header[2], SeekOrigin.Begin);
                        //_base.Read(buffer, 0, (int)sfo_size);

                        //SetSFOTitle(buffer, convertInfo.SaveTitle);
                        //SetSFOCode(buffer, convertInfo.SaveID);

                        _out.Write(sfo);

                        OnEvent?.Invoke(PopstationEventEnum.WriteIcon0Png, null);

                        if (!icon0)
                        {
                            _base.Seek(base_header[3], SeekOrigin.Begin);
                            _base.Read(buffer, 0, (int)icon0_size);
                            _out.Write(buffer, 0, (int)icon0_size);
                        }
                        else
                        {
                            using (var t = new FileStream(convertInfo.Icon0, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                t.Read(buffer, 0, (int)icon0_size);
                                _out.Write(buffer, 0, (int)icon0_size);
                            }
                        }

                        if (icon1)
                        {
                            OnEvent?.Invoke(PopstationEventEnum.WriteIcon1Pmf, null);

                            using (var t = new FileStream(convertInfo.Icon1, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                t.Read(buffer, 0, (int)icon1_size);
                                _out.Write(buffer, 0, (int)icon0_size);
                            }
                        }

                        if (!pic0)
                        {
                            //_base.Seek(base_header[5], SeekOrigin.Begin);
                            //_base.Read(buffer, 1, pic0_size);
                            //_out.Write(buffer, 1, pic0_size);
                        }
                        else
                        {
                            OnEvent?.Invoke(PopstationEventEnum.WritePic0Png, null);

                            using (var t = new FileStream(convertInfo.Pic0, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                t.Read(buffer, 0, (int)pic0_size);
                                _out.Write(buffer, 0, (int)pic0_size);
                            }
                        }

                        if (!pic1)
                        {
                            //_base.Seek(base_header[6], SeekOrigin.Begin);
                            //_base.Read(buffer, 0, pic1_size);
                            //_out.Write(buffer, 0, pic1_size);		
                        }
                        else
                        {
                            OnEvent?.Invoke(PopstationEventEnum.WritePic1Png, null);

                            using (var t = new FileStream(convertInfo.Pic1, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                t.Read(buffer, 0, (int)pic1_size);
                                _out.Write(buffer, 0, (int)pic1_size);
                            }
                        }

                        if (snd)
                        {
                            OnEvent?.Invoke(PopstationEventEnum.WriteSnd0At3, null);

                            using (var t = new FileStream(convertInfo.Snd0, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                t.Read(buffer, 0, (int)snd_size);
                                _out.Write(buffer, 0, (int)snd_size);
                            }
                        }

                        OnEvent?.Invoke(PopstationEventEnum.WriteDataPsp, null);

                        if (prx)
                        {
                            using (var t = new FileStream(convertInfo.DataPsp, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                t.Read(buffer, 0, (int)prx_size);
                                _out.Write(buffer, 0, (int)prx_size);
                            }
                        }
                        else
                        {
                            _base.Seek(base_header[8], SeekOrigin.Begin);
                            _base.Read(buffer, 0, (int)prx_size);
                            _out.Write(buffer, 0, (int)prx_size);
                        }


                        offset = (uint)_out.Position;

                        for (i = 0; i < header[9] - offset; i++)
                        {
                            _out.WriteByte(0);
                        }

                        OnEvent?.Invoke(PopstationEventEnum.WriteIsoHeader, null);

                        _out.Write("PSISOIMG0000", 0, 12);

                        p1_offset = (uint)_out.Position;

                        x = isosize + 0x100000;
                        _out.WriteInteger(x, 1);

                        x = 0;
                        for (i = 0; i < 0xFC; i++)
                        {
                            _out.WriteInteger(x, 1);
                        }

                        // TODO
                        var titleBytes = Encoding.ASCII.GetBytes(convertInfo.MainGameID);
                        Array.Copy(titleBytes, 0, data1, 1, 4);

                        titleBytes = Encoding.ASCII.GetBytes(convertInfo.MainGameID);
                        Array.Copy(titleBytes, 4, data1, 6, 5);

                        //memcpy(data1 + 1, convertInfo.gameID, 4);
                        //memcpy(data1 + 6, convertInfo.gameID + 4, 5);

                        /*
                            offset = isorealsize/2352+150;
                            min = offset/75/60;
                            sec = (offset-min*60*75)/75;
                            frm = offset-(min*60+sec)*75;
                            data1[0x41b] = bcd(min);
                            data1[0x41c] = bcd(sec);
                            data1[0x41d] = bcd(frm);
                        */
                        if (convertInfo.TocData?.Length > 0)
                        {
                            OnEvent?.Invoke(PopstationEventEnum.WriteToc, null);
                            // TODO?
                            Array.Copy(convertInfo.TocData, 0, data1, 1024, convertInfo.TocData.Length);
                            // memcpy(data1 + 1024, convertInfo.tocData, convertInfo.tocSize);

                        }
                        _out.Write(data1, 0, data1.Length);

                        p2_offset = (uint)_out.Position;
                        x = isosize + 0x100000 + 0x2d31;
                        _out.WriteInteger(x, 1);


                        // TODO
                        titleBytes = Encoding.ASCII.GetBytes(convertInfo.MainGameTitle);
                        Array.Copy(titleBytes, 0, data2, 8, titleBytes.Length);
                        _out.Write(data2, 0, data2.Length);
                        //strcpy((char*)(data2 + 8), convertInfo.gameTitle);
                        //fwrite(data2, 1, sizeof(data2), _out);

                        index_offset = (uint)_out.Position;

                        OnEvent?.Invoke(PopstationEventEnum.WriteIndex, null);

                        // TODO
                        // memset(dummy, 0, sizeof(dummy));

                        offset = 0;

                        if (complevel == 0)
                        {
                            x = BLOCK_SIZE;
                        }
                        else
                        {
                            x = 0;
                        }

                        for (i = 0; i < isosize / BLOCK_SIZE; i++)
                        {
                            _out.WriteInteger(offset, 1);
                            _out.WriteInteger(x, 1);
                            _out.Write(dummy, 0, sizeof(uint) * dummy.Length);

                            if (complevel == 0)
                                offset += BLOCK_SIZE;
                        }

                        offset = (uint)_out.Position;

                        for (i = 0; i < (header[9] + 0x100000) - offset; i++)
                        {
                            _out.WriteByte(0);
                        }

                        OnEvent?.Invoke(PopstationEventEnum.WriteIso, null);

                        OnEvent?.Invoke(PopstationEventEnum.ConvertStart, null);

                        totSize = 0;
                        uint bufferSize;

                        if (complevel == 0)
                        {
                            i = 0;
                            if (Path.GetExtension(disc.SourceIso).ToLower() == ".pbp")
                            {
                                for (i = 0; i < blockCount; i++)
                                {
                                    buffer2 = ReadBlock(disc.SourceIso, iso_index, (int)i, out bufferSize);

                                    //bufferSize = (uint)buffer2.Length;

                                    if (convertInfo.Patches?.Count > 0) PatchData(convertInfo, buffer2, (int)bufferSize, (int)totSize);

                                    totSize += bufferSize;

                                    if (totSize > isorealsize)
                                    {
                                        bufferSize = bufferSize - (totSize - isorealsize);
                                        totSize = isorealsize;
                                    }

                                    _out.Write(buffer2, 0, (int)bufferSize);

                                    OnEvent?.Invoke(PopstationEventEnum.ConvertProgress, totSize);

                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        return;
                                    }
                                }
                            }
                            else
                            {
                                while ((x = (uint)_in.Read(buffer, 0, 1048576)) > 0)
                                {
                                    if (convertInfo.Patches?.Count > 0) PatchData(convertInfo, buffer, (int)x, (int)i);

                                    _out.Write(buffer, 0, (int)x);

                                    i += x;

                                    OnEvent?.Invoke(PopstationEventEnum.ConvertProgress, i);

                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        return;
                                    }
                                }
                            }

                            for (i = 0; i < (isosize - isorealsize); i++)
                            {
                                _out.WriteByte(0);
                            }
                        }

                        else
                        {
                            indexes = new IsoIndex[isosize / BLOCK_SIZE];

                            //if (!indexes)
                            //{
                            //    if (convertInfo.srcIsPbp) popstripFinal(&iso_index);

                            //    throw new Exception("Cannot alloc memory for indexes!\n");
                            //}

                            i = 0;
                            j = 0;
                            offset = 0;

                            while (true)
                            {

                                if (Path.GetExtension(disc.SourceIso).ToLower() == ".pbp")
                                {
                                    if (i >= blockCount) break;
                                    buffer2 = ReadBlock(disc.SourceIso, iso_index, (int)i, out bufferSize);

                                    totSize += bufferSize;
                                    if (totSize > isorealsize)
                                    {
                                        bufferSize = bufferSize - (totSize - isorealsize);
                                        totSize = isorealsize;
                                    }
                                    x = bufferSize;
                                }
                                else
                                {
                                    x = (uint)_in.Read(buffer2, 0, BLOCK_SIZE);
                                }
                                if (x == 0) break;
                                if (convertInfo.Patches?.Count > 0) PatchData(convertInfo, buffer2, (int)x, (int)j);

                                j += x;

                                //PostMessage(convertInfo.callback, WM_CONVERT_PROGRESS, 0, j);
                                OnEvent?.Invoke(PopstationEventEnum.ConvertProgress, j);

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    //if (convertInfo.srcIsPbp) popstripFinal(&iso_index);
                                    OnEvent?.Invoke(PopstationEventEnum.ConvertComplete, null);
                                    return;
                                }

                                if (x < BLOCK_SIZE)
                                {
                                    Array.Clear(buffer2, (int)x, (int)(BLOCK_SIZE - x));
                                    //memset(buffer2 + x, 0, BlockSize - x);
                                }

                                //var cbuffer = Compress(buffer2, complevel);
                                bufferSize = (uint)Compression.Compress(buffer2, buffer, complevel);

                                //if (x < 0)
                                //{
                                //    //if (convertInfo.srcIsPbp) popstripFinal(&iso_index);
                                //    throw new Exception("Error _in compression!\n");
                                //}

                                //x = (uint)cbuffer.Length;
                                x = bufferSize;

                                indexes[i] = new IsoIndex();
                                indexes[i].Offset = offset;

                                if (x >= BLOCK_SIZE) /* Block didn't compress */
                                {
                                    indexes[i].Length = BLOCK_SIZE;
                                    _out.Write(buffer2, 0, BLOCK_SIZE);
                                    offset += BLOCK_SIZE;
                                }
                                else
                                {
                                    indexes[i].Length = x;
                                    _out.Write(buffer, 0, (int)x);
                                    offset += x;
                                }

                                i++;
                            }

                            if (i != (isosize / BLOCK_SIZE))
                            {
                                throw new Exception("Some error happened.\n");
                            }

                            x = (uint)_out.Position;

                            if ((x % 0x10) != 0)
                            {
                                end_offset = x + (0x10 - (x % 0x10));

                                for (i = 0; i < (end_offset - x); i++)
                                {
                                    _out.Write('0');
                                }
                            }
                            else
                            {
                                end_offset = x;
                            }

                            end_offset -= header[9];
                        }

                        OnEvent?.Invoke(PopstationEventEnum.ConvertComplete, null);

                        OnEvent?.Invoke(PopstationEventEnum.WriteSpecialData, null);

                        _base.Seek(base_header[9] + 12, SeekOrigin.Begin);

                        var temp = new byte[sizeof(uint)];
                        _base.Read(temp, 0, 4);
                        x = BitConverter.ToUInt32(temp, 0);

                        x += 0x50000;

                        _base.Seek(x, SeekOrigin.Begin);
                        _base.Read(buffer, 0, 8);

                        var tempstr = System.Text.Encoding.ASCII.GetString(buffer, 0, 8);

                        if (tempstr != "STARTDAT")
                        {
                            throw new Exception($"Cannot find STARTDAT _in {convertInfo.BasePbp}. Not a valid PSX eboot.pbp");
                        }

                        _base.Seek(x + 16, SeekOrigin.Begin);
                        _base.Read(header, 0, 8);
                        _base.Seek(x, SeekOrigin.Begin);
                        _base.Read(buffer, 0, (int)header[0]);

                        if (!boot)
                        {
                            _out.Write(buffer, 0, (int)header[0]);
                            _base.Read(buffer, 0, (int)header[1]);
                            _out.Write(buffer, 0, (int)header[1]);
                        }
                        else
                        {
                            OnEvent?.Invoke(PopstationEventEnum.WriteBootPng, null);

                            //ib[5] = boot_size;
                            var temp_buffer = BitConverter.GetBytes(boot_size);
                            for (var k = 0; k < sizeof(uint); k++)
                            {
                                buffer[5 + k] = temp_buffer[k];
                            }

                            _out.Write(buffer, 0, (int)header[0]);

                            using (var t = new FileStream(convertInfo.Boot, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                t.Read(buffer, 0, (int)boot_size);
                                _out.Write(buffer, 0, (int)boot_size);
                            }

                            _base.Read(buffer, 0, (int)header[1]);

                            //ib[5] = boot_size;
                            //fwrite(buffer, 1, header[0], _out);
                            //t = fopen(convertInfo.boot, "rb");
                            //t.Read(buffer, 1, boot_size);
                            //_out.Write(buffer, 1, boot_size);
                            //fclose(t);
                            //fread(buffer, 1, header[1], _base);
                        }

                        while ((x = (uint)_base.Read(buffer, 0, 1048576)) > 0)
                        {
                            _out.Write(buffer, 0, (int)x);
                        }

                        if (complevel != 0)
                        {
                            OnEvent?.Invoke(PopstationEventEnum.UpdateIndex, null);

                            _out.Seek(p1_offset, SeekOrigin.Begin);
                            _out.WriteInteger(end_offset, 1);

                            end_offset += 0x2d31;
                            _out.Seek(p2_offset, SeekOrigin.Begin);
                            _out.WriteInteger(end_offset, 1);

                            _out.Seek(index_offset, SeekOrigin.Begin);
                            _out.Write(indexes, 0, (int)(4 + 4 + (6 * 4) * (isosize / BLOCK_SIZE)));
                        }

                    }
                }

            }
        }
    }
}