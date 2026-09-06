using System;
using System.Buffers.Binary;
using System.IO;
using PSXPackager.Common.Iso;

namespace PSXPackager.Common.Chd
{
    /// <summary>
    /// Presents the CD image inside a CHD as one continuous stream of raw 2352-byte sectors, which
    /// is the layout the PBP writer and the TOC expect.
    /// </summary>
    /// <remarks>
    /// Three things separate a CHD's stored frames from the disc they describe. Tracks are padded
    /// out to a 4 frame boundary in the file, so the padding has to be skipped. A pregap that was
    /// not stored still occupies disc addresses, so those sectors are generated here. And audio is
    /// held big-endian, the reverse of the byte order a .bin file uses, so audio sectors are
    /// swapped on the way out.
    /// </remarks>
    public sealed class ChdDiscStream : Stream
    {
        private const int SyncSize = 12;
        private const int HeaderOffset = 12;
        private const int SubHeaderOffset = 16;
        private const int UserDataOffset = 24;

        /// <summary>The 2 second lead-in that separates the first sector from address 00:00:00.</summary>
        private const int LeadInFrames = 150;

        private readonly ChdFile _chd;
        private readonly bool _leaveOpen;
        private readonly int _framesPerHunk;
        private readonly byte[] _sector = new byte[ChdConstants.SectorDataSize];

        private long _cachedLba = -1;
        private long _position;
        private int _lastTrack;
        private bool _disposed;

        public ChdDiscStream(ChdFile chd, bool leaveOpen = false)
        {
            _chd = chd ?? throw new ArgumentNullException(nameof(chd));
            _leaveOpen = leaveOpen;

            if (chd.UnitBytes != ChdConstants.FrameSize || chd.HunkBytes % ChdConstants.FrameSize != 0)
            {
                throw new InvalidChdException("The CHD does not hold a CD image");
            }

            _framesPerHunk = chd.HunkBytes / ChdConstants.FrameSize;

            Toc = ChdCdToc.Parse(chd);
        }

        public static ChdDiscStream Open(string path)
        {
            var chd = ChdFile.Open(path);

            try
            {
                return new ChdDiscStream(chd);
            }
            catch
            {
                chd.Dispose();
                throw;
            }
        }

        public ChdCdToc Toc { get; }

        /// <summary>
        /// The sector size of the data track, which tells the caller whether the image carries
        /// full sectors or only user data.
        /// </summary>
        public int DataTrackSectorSize
        {
            get
            {
                foreach (var track in Toc.Tracks)
                {
                    if (!track.IsAudio) return track.DataSize;
                }

                return ChdConstants.SectorDataSize;
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => Toc.TotalFrames * ChdConstants.SectorDataSize;

        public override long Position
        {
            get => _position;
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
                _position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || count < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var remaining = (int)Math.Min(count, Math.Max(0, Length - _position));
            var total = 0;

            while (remaining > 0)
            {
                var lba = _position / ChdConstants.SectorDataSize;
                var within = (int)(_position % ChdConstants.SectorDataSize);
                var chunk = Math.Min(ChdConstants.SectorDataSize - within, remaining);

                EnsureSector(lba);

                Buffer.BlockCopy(_sector, within, buffer, offset + total, chunk);

                _position += chunk;
                total += chunk;
                remaining -= chunk;
            }

            return total;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            var target = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => _position + offset,
                SeekOrigin.End => Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

            if (target < 0) throw new IOException("Cannot seek before the start of the stream");

            _position = target;

            return _position;
        }

        public override void Flush()
        {
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed)
            {
                _disposed = true;

                if (!_leaveOpen)
                {
                    _chd.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Builds the sector at a disc address into <see cref="_sector"/>, unless it is already there.
        /// </summary>
        private void EnsureSector(long lba)
        {
            if (_cachedLba == lba) return;

            var track = FindTrack(lba);

            if (track == null)
            {
                // A gap the file does not store: a pregap that was never in the image, or a
                // postgap. Both read back as silence on a real disc.
                Array.Clear(_sector, 0, _sector.Length);
            }
            else
            {
                var fileFrame = track.FileFrame + (lba - track.StartFrame);
                var hunk = fileFrame / _framesPerHunk;
                var frameInHunk = (int)(fileFrame % _framesPerHunk);

                if (hunk < 0 || hunk >= _chd.HunkCount)
                {
                    throw new InvalidChdException("A track runs past the end of the CHD's data");
                }

                var data = _chd.GetHunk((int)hunk);

                ExpandSector(data, frameInHunk * ChdConstants.FrameSize, track, lba);

                if (track.IsAudio)
                {
                    SwapSampleBytes(_sector);
                }
            }

            _cachedLba = lba;
        }

        /// <summary>
        /// Returns the track holding a disc address, or null if the address falls in a gap that
        /// the file does not store.
        /// </summary>
        private ChdTrack FindTrack(long lba)
        {
            var tracks = Toc.Tracks;

            // Reads walk the disc in order, so the previous answer is nearly always right
            if (_lastTrack < tracks.Count && Contains(tracks[_lastTrack], lba))
            {
                return tracks[_lastTrack];
            }

            for (var i = 0; i < tracks.Count; i++)
            {
                if (Contains(tracks[i], lba))
                {
                    _lastTrack = i;
                    return tracks[i];
                }
            }

            return null;
        }

        private static bool Contains(ChdTrack track, long lba)
        {
            return lba >= track.StartFrame && lba < track.StartFrame + track.Frames;
        }

        /// <summary>
        /// Copies a stored frame into <see cref="_sector"/>, rebuilding whatever framing the
        /// track's layout leaves out.
        /// </summary>
        private void ExpandSector(byte[] hunk, int offset, ChdTrack track, long lba)
        {
            if (track.DataSize == ChdConstants.SectorDataSize)
            {
                Buffer.BlockCopy(hunk, offset, _sector, 0, ChdConstants.SectorDataSize);
                return;
            }

            Array.Clear(_sector, 0, _sector.Length);
            Array.Copy(ChdConstants.SyncHeader, 0, _sector, 0, SyncSize);

            switch (track.Type)
            {
                case ChdTrackType.Mode1:
                    // 2048 bytes of user data, with the header, EDC and ECC all left out
                    WriteHeader(lba, 0x01);
                    Buffer.BlockCopy(hunk, offset, _sector, SubHeaderOffset, 2048);
                    WriteEdc(0, 2064, 2064);
                    EccEdc.WriteEcc(_sector, false);
                    break;

                case ChdTrackType.Mode2Form1:
                    // 2048 bytes of user data; the subheader is regenerated as a Form 1 data sector
                    WriteHeader(lba, 0x02);
                    WriteSubHeader(0x08);
                    Buffer.BlockCopy(hunk, offset, _sector, UserDataOffset, 2048);
                    WriteEdc(SubHeaderOffset, 8 + 2048, 2072);
                    EccEdc.WriteEcc(_sector, true);
                    break;

                case ChdTrackType.Mode2Form2:
                    // 2324 bytes of user data, with a Form 2 subheader; Form 2 carries no ECC
                    WriteHeader(lba, 0x02);
                    WriteSubHeader(0x20);
                    Buffer.BlockCopy(hunk, offset, _sector, UserDataOffset, 2324);
                    WriteEdc(SubHeaderOffset, 8 + 2324, 2348);
                    break;

                default:
                    // 2336 bytes: everything but the sync pattern and header is stored as-is
                    WriteHeader(lba, 0x02);
                    Buffer.BlockCopy(hunk, offset, _sector, SubHeaderOffset, 2336);
                    break;
            }
        }

        private void WriteHeader(long lba, byte mode)
        {
            var frame = lba + LeadInFrames;

            _sector[HeaderOffset + 0] = ToBcd((int)(frame / 75 / 60 % 100));
            _sector[HeaderOffset + 1] = ToBcd((int)(frame / 75 % 60));
            _sector[HeaderOffset + 2] = ToBcd((int)(frame % 75));
            _sector[HeaderOffset + 3] = mode;
        }

        /// <summary>
        /// Writes the 4-byte subheader twice, as a Mode 2 sector stores it.
        /// </summary>
        private void WriteSubHeader(byte subMode)
        {
            for (var copy = 0; copy < 2; copy++)
            {
                var offset = SubHeaderOffset + copy * 4;

                _sector[offset + 0] = 0x00;
                _sector[offset + 1] = 0x00;
                _sector[offset + 2] = subMode;
                _sector[offset + 3] = 0x00;
            }
        }

        private void WriteEdc(int start, int count, int destination)
        {
            var edc = EccEdc.ComputeEdc(_sector, start, count);

            BinaryPrimitives.WriteUInt32LittleEndian(_sector.AsSpan(destination, sizeof(uint)), edc);
        }

        /// <summary>
        /// Converts the big-endian samples a CHD stores into the little-endian order a .bin file
        /// and the PBP writer expect.
        /// </summary>
        private static void SwapSampleBytes(byte[] sector)
        {
            for (var i = 0; i + 1 < sector.Length; i += 2)
            {
                (sector[i], sector[i + 1]) = (sector[i + 1], sector[i]);
            }
        }

        private static byte ToBcd(int value) => (byte)(((value / 10) << 4) | (value % 10));
    }
}
