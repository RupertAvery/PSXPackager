using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Popstation.Cue;
using Popstation.Iso;
using Popstation.Pbp;

namespace Popstation
{
    public class CancellationException : Exception
    {
        public CancellationException(string message) : base(message)
        {

        }
    }

    public enum ActionIfFileExistsEnum
    {
        Overwrite,
        OverwriteAll,
        Skip,
        Abort
    }

    public partial class Popstation
    {
        // 2352 bytes / sector * 16 sectors / block
        const int BLOCK_SIZE = 0x9300;

        public Action<PopstationEventEnum, object> OnEvent { get; set; }
        public Func<string, ActionIfFileExistsEnum> ActionIfFileExists { get; set; }

        int nextPatchPos;

        public bool Convert(ConvertIsoInfo convertInfo, CancellationToken cancellationToken)
        {
            if (convertInfo.Patches?.Count > 0)
            {
                nextPatchPos = 0;
            }

            if (convertInfo.DiscInfos.Count == 1)
            {
                return ConvertIso(convertInfo, cancellationToken);
            }
            else
            {
                return ConvertMultiIso(convertInfo, cancellationToken);
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


        private bool ConvertMultiIso(ConvertIsoInfo convertInfo, CancellationToken cancellationToken)
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
            end_offset = 0;

            //string output;
            //output = convertInfo.DestinationPbp;
            var title = convertInfo.MainGameTitle;
            var code = convertInfo.MainGameID;

            foreach (var disc in convertInfo.DiscInfos)
            {
                if (File.Exists(disc.SourceIso))
                {
                    var t = new FileInfo(disc.SourceIso);
                    isosize = (uint)t.Length;
                    if (!string.IsNullOrEmpty(disc.SourceToc))
                    {
                        if (File.Exists(disc.SourceToc))
                        {
                            disc.TocData = ProcessToc(disc, isosize);
                        }
                        else
                        {
                            OnEvent?.Invoke(PopstationEventEnum.Warning, $"{disc.SourceToc} not found");
                        }
                    }
                }
            }

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
                if (convertInfo.CheckIfFileExists && File.Exists(convertInfo.DestinationPbp))
                {
                    var response = ActionIfFileExists(convertInfo.DestinationPbp);
                    if (response == ActionIfFileExistsEnum.OverwriteAll)
                    {
                        convertInfo.CheckIfFileExists = false;
                    }
                    else if (response == ActionIfFileExistsEnum.Skip)
                    {
                        return false;
                    }
                    else if (response == ActionIfFileExistsEnum.Abort)
                    {
                        throw new CancellationException("Operation was aborted");
                    }
                }

                using (var _out = new FileStream(convertInfo.DestinationPbp, FileMode.Create, FileAccess.Write, FileShare.Write))
                {
                    OnEvent?.Invoke(PopstationEventEnum.Info, $"Writing {convertInfo.DestinationPbp}...");

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

                    int ciso;
                    for (ciso = 0; ciso < convertInfo.DiscInfos.Count; ciso++)
                    {
                        var disc = convertInfo.DiscInfos[ciso];
                        if (File.Exists(disc.SourceIso))
                        {
                            var t = new FileInfo(convertInfo.DiscInfos[ciso].SourceIso);
                            isosize = (uint)t.Length;
                            disc.IsoSize = isosize;
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
                        uint curSize = 0;

                        OnEvent?.Invoke(PopstationEventEnum.WriteStart, ciso + 1);
                        OnEvent?.Invoke(PopstationEventEnum.WriteSize, disc.IsoSize);

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
                            //memcpy(data1, 1, codes[ciso], 4);
                            var titleBytes = Encoding.ASCII.GetBytes(disc.GameID);
                            Array.Copy(titleBytes, 0, data1, 1, 4);

                            //memcpy(data1, 6, codes[ciso] + 4, 5);
                            titleBytes = Encoding.ASCII.GetBytes(disc.GameID);
                            Array.Copy(titleBytes, 4, data1, 6, 5);

                            if (disc.TocData?.Length > 0)
                            {
                                OnEvent?.Invoke(PopstationEventEnum.WriteToc, null);
                                // memcpy(data1 + 1024, convertInfo.tocData, convertInfo.tocSize);
                                Array.Copy(disc.TocData, 0, data1, 1024, disc.TocData.Length);
                            }

                            _out.Write(data1, 0, data1.Length);

                            _out.WriteInteger(0, 1);

                            //TODO:
                            titleBytes = Encoding.ASCII.GetBytes(disc.GameTitle);
                            Array.Copy(titleBytes, 0, data2, 8, disc.GameTitle.Length);
                            //strcpy((char*)(data2 + 8), titles[ciso]);
                            _out.Write(data2, 0, data2.Length);

                            index_offset = (uint)_out.Position;

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
                                    curSize += x;
                                    // PostMessage(convertInfo.callback, WM_CONVERT_PROGRESS, 0, totSize);
                                    OnEvent?.Invoke(PopstationEventEnum.ConvertProgress, curSize);

                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        return false;
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
                                    curSize += x;

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

                                    OnEvent?.Invoke(PopstationEventEnum.WriteProgress, curSize);

                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        return false;
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

                        OnEvent?.Invoke(PopstationEventEnum.WriteComplete, null);
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

            return true;
        }

        private byte[] ProcessToc(DiscInfo disc, uint isosize)
        {
            var cue = CueFileReader.Read(disc.SourceToc);
            var tracks = cue.FileEntries.SelectMany(cf => cf.Tracks).ToList();

            byte[] tocData = new byte[0xA * (tracks.Count + 3)];

            var trackBuffer = new byte[0xA];

            var frames = isosize / 2352;
            var position = Helper.PositionFromFrames(frames);

            var ctr = 0;

            trackBuffer[0] = (byte)Helper.GetTrackType(tracks.First().DataType);
            trackBuffer[1] = 0x00;
            trackBuffer[2] = 0xA0;
            trackBuffer[3] = 0x00;
            trackBuffer[4] = 0x00;
            trackBuffer[5] = 0x00;
            trackBuffer[6] = 0x00;
            trackBuffer[7] = Helper.ToBinaryDecimal(tracks.First().Number);
            trackBuffer[8] = Helper.ToBinaryDecimal(0x20);
            trackBuffer[9] = 0x00;

            Array.Copy(trackBuffer, 0, tocData, ctr, 0xA);
            ctr += 0xA;

            trackBuffer[0] = (byte)Helper.GetTrackType(tracks.Last().DataType);
            trackBuffer[2] = 0xA1;
            trackBuffer[7] = Helper.ToBinaryDecimal(tracks.Last().Number);
            trackBuffer[8] = 0x00;

            Array.Copy(trackBuffer, 0, tocData, ctr, 0xA);
            ctr += 0xA;

            trackBuffer[0] = 0x01;
            trackBuffer[2] = 0xA2;
            trackBuffer[7] = Helper.ToBinaryDecimal(position.Minutes);
            trackBuffer[8] = Helper.ToBinaryDecimal(position.Seconds);
            trackBuffer[9] = Helper.ToBinaryDecimal(position.Frames);

            Array.Copy(trackBuffer, 0, tocData, ctr, 0xA);
            ctr += 0xA;

            foreach (var track in tracks)
            {
                trackBuffer[0] = (byte)Helper.GetTrackType(track.DataType);
                trackBuffer[1] = 0x00;
                trackBuffer[2] = Helper.ToBinaryDecimal(track.Number);
                var pos = track.Indexes.First(idx => idx.Number == 1).Position;
                trackBuffer[3] = Helper.ToBinaryDecimal(pos.Minutes);
                trackBuffer[4] = Helper.ToBinaryDecimal(pos.Seconds);
                trackBuffer[5] = Helper.ToBinaryDecimal(pos.Frames);
                trackBuffer[6] = 0x00;
                pos = pos + (2 * 75); // add 2 seconds for lead in (75 frames / second)
                trackBuffer[7] = Helper.ToBinaryDecimal(pos.Minutes);
                trackBuffer[8] = Helper.ToBinaryDecimal(pos.Seconds);
                trackBuffer[9] = Helper.ToBinaryDecimal(pos.Frames);

                Array.Copy(trackBuffer, 0, tocData, ctr, 0xA);
                ctr += 0xA;
            }

            //0x00    1 byte Track type - 0x41 = data track, 0x01 = audio track
            //0x01    1 byte Always null
            //0x02    1 byte The track number in "binary decimal"
            //0x03    3 bytes The absolute track start address in "binary decimal" - first byte is minutes, second is seconds, third is frames
            //0x06    1 byte Always null
            //0x07    3 bytes The "relative" track address -same as before, and uses MM: SS: FF format

            return tocData;
        }

        private bool ConvertIso(ConvertIsoInfo convertInfo, CancellationToken cancellationToken)
        {
            uint j, offset, isosize, isorealsize;
            uint index_offset, p1_offset, p2_offset, end_offset;
            IsoIndex[] indexes = null;
            uint curoffs = 0x28;

            end_offset = 0;

            byte[] buffer = new byte[1 * 1048576];


            bool pic0 = false, pic1 = false, icon0 = false, icon1 = false, snd = false, toc = false, boot = false, prx = false;
            uint pic0_size, pic1_size, icon0_size, icon1_size, snd_size, toc_size, boot_size, prx_size;

            boot_size = 0;

            uint[] psp_header = new uint[0x30 / 4];
            uint[] base_header = new uint[0x28 / 4];
            uint[] header = new uint[0x28 / 4];
            uint[] dummy = new uint[6];


            //uint i, offset, isosize, isorealsize, x;
            //uint index_offset, p1_offset, p2_offset, m_offset, end_offset;

            var disc = convertInfo.DiscInfos[0];

            var iso_index = new List<IsoIndexLite>();

            using (var _in = new FileStream(disc.SourceIso, FileMode.Open, FileAccess.Read))
            {

                //Check if input is pbp
                if (Path.GetExtension(disc.SourceIso).ToLower() == ".pbp")
                {
                    using (var pbp = new PbpStream(disc.SourceIso, FileMode.Open, FileAccess.Read))
                    {
                        // TODO: Multi-disc support
                        isosize = pbp.Discs[0].IsoSize;
                    }
                }
                else
                {
                    _in.Seek(0, SeekOrigin.End);
                    isosize = (uint)_in.Position;
                    _in.Seek(0, SeekOrigin.Begin);
                }

                isorealsize = isosize;

                if (!string.IsNullOrEmpty(disc.SourceToc) && File.Exists(disc.SourceToc))
                {
                    disc.TocData = ProcessToc(disc, isosize);
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
                    if (convertInfo.CheckIfFileExists && File.Exists(convertInfo.DestinationPbp))
                    {
                        var response = ActionIfFileExists(convertInfo.DestinationPbp);
                        if (response == ActionIfFileExistsEnum.OverwriteAll)
                        {
                            convertInfo.CheckIfFileExists = false;
                        }
                        else if (response == ActionIfFileExistsEnum.Skip)
                        {
                            return false;
                        }
                        else if (response == ActionIfFileExistsEnum.Abort)
                        {
                            throw new CancellationException("Operation was aborted");
                        }
                    }

                    OnEvent?.Invoke(PopstationEventEnum.Info, $"Writing {convertInfo.DestinationPbp}...");

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

                        var psarOffset = header[8] + prx_size;

                        if ((psarOffset % 0x10000) != 0)
                        {
                            psarOffset = psarOffset + (0x10000 - (psarOffset % 0x10000));
                        }

                        header[9] = psarOffset;

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

                        for (var i = 0; i < header[9] - offset; i++)
                        {
                            _out.WriteByte(0);
                        }

                        OnEvent?.Invoke(PopstationEventEnum.WriteIsoHeader, null);

                        _out.Write("PSISOIMG0000", 0, 12);

                        p1_offset = (uint)_out.Position;

                        _out.WriteInteger(isosize + 0x100000, 1);

                        for (var i = 0; i < 0xFC; i++)
                        {
                            _out.WriteInteger(0, 1);
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
                        if (disc.TocData?.Length > 0)
                        {
                            OnEvent?.Invoke(PopstationEventEnum.WriteToc, null);
                            // TODO?
                            Array.Copy(disc.TocData, 0, data1, 1024, disc.TocData.Length);
                            // memcpy(data1 + 1024, convertInfo.tocData, convertInfo.tocSize);
                        }

                        _out.Write(data1, 0, data1.Length);

                        p2_offset = (uint)_out.Position;

                        _out.WriteInteger(isosize + 0x100000 + 0x2d31, 1);


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
                        var size = 0;

                        if (convertInfo.CompressionLevel == 0)
                        {
                            size = BLOCK_SIZE;
                        }
                        else
                        {
                            size = 0;
                        }

                        for (var i = 0; i < isosize / BLOCK_SIZE; i++)
                        {
                            _out.WriteInteger(offset, 1);
                            _out.WriteInteger(size, 1);
                            _out.Write(dummy, 0, sizeof(uint) * dummy.Length);

                            if (convertInfo.CompressionLevel == 0)
                                offset += BLOCK_SIZE;
                        }

                        offset = (uint)_out.Position;

                        for (var i = 0; i < (header[9] + 0x100000) - offset; i++)
                        {
                            _out.WriteByte(0);
                        }

                        OnEvent?.Invoke(PopstationEventEnum.WriteIso, null);

                        OnEvent?.Invoke(PopstationEventEnum.ConvertStart, null);


                        if (convertInfo.CompressionLevel == 0)
                        {
                            if (Path.GetExtension(disc.SourceIso).ToLower() == ".pbp")
                            {
                                byte[] buffer2 = new byte[BLOCK_SIZE];
                                uint bufferSize;
                                uint totSize = 0;

                                using (var pbpStream = new PbpStream(disc.SourceIso, FileMode.Open, FileAccess.Read))
                                {
                                    foreach (var iso_disc in pbpStream.Discs)
                                    {

                                        for (var i = 0; i < iso_disc.IsoIndex.Count; i++)
                                        {
                                            bufferSize = iso_disc.ReadBlock(i, buffer2);

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
                                                return false;
                                            }
                                        }

                                    }

                                }
                            }
                            else
                            {
                                uint i = 0;
                                uint bytesRead = 0;

                                while ((bytesRead = (uint)_in.Read(buffer, 0, 1048576)) > 0)
                                {
                                    if (convertInfo.Patches?.Count > 0) PatchData(convertInfo, buffer, (int)bytesRead, (int)i);

                                    _out.Write(buffer, 0, (int)bytesRead);

                                    i += bytesRead;

                                    OnEvent?.Invoke(PopstationEventEnum.ConvertProgress, i);

                                    if (cancellationToken.IsCancellationRequested)
                                    {
                                        return false;
                                    }
                                }
                            }

                            for (var i = 0; i < (isosize - isorealsize); i++)
                            {
                                _out.WriteByte(0);
                            }
                        }
                        else
                        {
                            indexes = new IsoIndex[isosize / BLOCK_SIZE];

                            var block = 0;
                            uint bytesRead = 0;
                            uint totalBytes = 0;
                            offset = 0;

                            byte[] buffer2 = new byte[BLOCK_SIZE];
                            uint bufferSize;
                            uint totSize = 0;

                            while (true)
                            {

                                if (Path.GetExtension(disc.SourceIso).ToLower() == ".pbp")
                                {
                                    using (var pbpStream = new PbpStream(disc.SourceIso, FileMode.Open, FileAccess.Read))
                                    {
                                        //TODO: Multi-Disc support
                                        if (block >= pbpStream.Discs[0].IsoIndex.Count) break;
                                        bufferSize = pbpStream.Discs[0].ReadBlock((int)block, buffer2);

                                        totSize += bufferSize;
                                        if (totSize > isorealsize)
                                        {
                                            bufferSize = bufferSize - (totSize - isorealsize);
                                            totSize = isorealsize;
                                        }

                                        bytesRead = bufferSize;
                                    }
                                }
                                else
                                {
                                    bytesRead = (uint)_in.Read(buffer2, 0, BLOCK_SIZE);
                                }

                                if (bytesRead == 0) break;

                                if (convertInfo.Patches?.Count > 0) PatchData(convertInfo, buffer2, (int)bytesRead, (int)totalBytes);

                                totalBytes += bytesRead;

                                OnEvent?.Invoke(PopstationEventEnum.ConvertProgress, totalBytes);

                                if (cancellationToken.IsCancellationRequested)
                                {
                                    //if (convertInfo.srcIsPbp) popstripFinal(&iso_index);
                                    OnEvent?.Invoke(PopstationEventEnum.ConvertComplete, null);
                                    return false;
                                }

                                if (bytesRead < BLOCK_SIZE)
                                {
                                    Array.Clear(buffer2, (int)bytesRead, (int)(BLOCK_SIZE - bytesRead));
                                    //memset(buffer2 + x, 0, BlockSize - x);
                                }

                                //var cbuffer = Compress(buffer2, complevel);
                                bufferSize = (uint)Compression.Compress(buffer2, buffer, convertInfo.CompressionLevel);

                                //if (x < 0)
                                //{
                                //    //if (convertInfo.srcIsPbp) popstripFinal(&iso_index);
                                //    throw new Exception("Error _in compression!\n");
                                //}

                                //x = (uint)cbuffer.Length;
                                bytesRead = bufferSize;

                                indexes[block] = new IsoIndex();
                                indexes[block].Offset = offset;

                                if (bytesRead >= BLOCK_SIZE) /* Block didn't compress */
                                {
                                    indexes[block].Length = BLOCK_SIZE;
                                    _out.Write(buffer2, 0, BLOCK_SIZE);
                                    offset += BLOCK_SIZE;
                                }
                                else
                                {
                                    indexes[block].Length = bytesRead;
                                    _out.Write(buffer, 0, (int)bytesRead);
                                    offset += bytesRead;
                                }

                                block++;
                            }

                            if (block != (isosize / BLOCK_SIZE))
                            {
                                throw new Exception("Some error happened.\n");
                            }

                            bytesRead = (uint)_out.Position;

                            if ((bytesRead % 0x10) != 0)
                            {
                                end_offset = bytesRead + (0x10 - (bytesRead % 0x10));

                                for (block = 0; block < (end_offset - bytesRead); block++)
                                {
                                    _out.Write('0');
                                }
                            }
                            else
                            {
                                end_offset = bytesRead;
                            }

                            end_offset -= header[9];
                        }

                        OnEvent?.Invoke(PopstationEventEnum.ConvertComplete, null);

                        OnEvent?.Invoke(PopstationEventEnum.WriteSpecialData, null);

                        _base.Seek(base_header[9] + 12, SeekOrigin.Begin);

                        var temp = new byte[sizeof(uint)];
                        _base.Read(temp, 0, 4);
                        var tempValue = BitConverter.ToUInt32(temp, 0);

                        tempValue += 0x50000;

                        _base.Seek(tempValue, SeekOrigin.Begin);
                        _base.Read(buffer, 0, 8);

                        var tempstr = Encoding.ASCII.GetString(buffer, 0, 8);

                        if (tempstr != "STARTDAT")
                        {
                            throw new Exception($"Cannot find STARTDAT _in {convertInfo.BasePbp}. Not a valid PSX eboot.pbp");
                        }

                        _base.Seek(tempValue + 16, SeekOrigin.Begin);
                        _base.Read(header, 0, 8);
                        _base.Seek(tempValue, SeekOrigin.Begin);
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

                        var bytesRead2 = 0;
                        while ((bytesRead2 = _base.Read(buffer, 0, 1048576)) > 0)
                        {
                            _out.Write(buffer, 0, bytesRead2);
                        }

                        if (convertInfo.CompressionLevel != 0)
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

            return true;
        }
    }
}