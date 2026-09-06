using System;
using System.Buffers.Binary;
using System.IO;

namespace PSXPackager.Common.Iso
{
    /// <summary>
    /// Presents a "cooked" image of 2048-byte user-data sectors as a raw image of 2352-byte
    /// Mode 2 Form 1 sectors, which is the layout PlayStation discs use and the only layout
    /// the PBP writer and the TOC understand.
    /// </summary>
    /// <remarks>
    /// Sectors are synthesised on demand: the sync pattern, MSF header and subheader are
    /// generated from the sector number, and the EDC/ECC fields are computed from the user data.
    /// </remarks>
    public class Mode2Form1Stream : Stream
    {
        private const int SyncOffset = 0;
        private const int SyncSize = 12;
        private const int HeaderOffset = 12;
        private const int SubHeaderOffset = 16;
        private const int SubHeaderSize = 8;
        private const int UserDataOffset = 24;
        private const int EdcOffset = UserDataOffset + DiscImage.UserSectorSize;

        /// <summary>
        /// The 2 second lead-in that separates the first sector (LBA 0) from address 00:00:00.
        /// </summary>
        private const int LeadInFrames = 150;

        private static readonly byte[] SyncPattern =
        {
            0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00
        };

        private readonly Stream _source;
        private readonly bool _leaveOpen;
        private readonly long _sectorCount;
        private readonly byte[] _sector = new byte[DiscImage.RawSectorSize];

        private long _cachedSector = -1;
        private long _position;
        private bool _disposed;

        public Mode2Form1Stream(Stream source, bool leaveOpen = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.CanRead) throw new ArgumentException("Source stream must be readable", nameof(source));
            if (!source.CanSeek) throw new ArgumentException("Source stream must be seekable", nameof(source));

            _source = source;
            _leaveOpen = leaveOpen;

            // A trailing partial sector is padded out with zeroes
            _sectorCount = (source.Length + DiscImage.UserSectorSize - 1) / DiscImage.UserSectorSize;

            Array.Copy(SyncPattern, 0, _sector, SyncOffset, SyncSize);
            WriteSubHeader();
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length => _sectorCount * DiscImage.RawSectorSize;

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
            if (offset < 0 || count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));

            var total = 0;

            while (count > 0 && _position < Length)
            {
                var lba = _position / DiscImage.RawSectorSize;
                var offsetInSector = (int)(_position % DiscImage.RawSectorSize);

                EnsureSector(lba);

                var chunk = Math.Min(count, DiscImage.RawSectorSize - offsetInSector);
                Array.Copy(_sector, offsetInSector, buffer, offset, chunk);

                _position += chunk;
                offset += chunk;
                count -= chunk;
                total += chunk;
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
                    _source.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// Builds the requested sector into <see cref="_sector"/>, unless it is already there.
        /// </summary>
        private void EnsureSector(long lba)
        {
            if (_cachedSector == lba) return;

            // Reset first, so a short read at the end of the image leaves the tail zeroed
            Array.Clear(_sector, UserDataOffset, DiscImage.UserSectorSize);

            _source.Position = lba * DiscImage.UserSectorSize;

            var read = 0;
            while (read < DiscImage.UserSectorSize)
            {
                var bytesRead = _source.Read(_sector, UserDataOffset + read, DiscImage.UserSectorSize - read);
                if (bytesRead <= 0) break;
                read += bytesRead;
            }

            WriteHeader(lba);

            // The EDC of a Form 1 sector covers the subheader and the user data
            var edc = EccEdc.ComputeEdc(_sector, SubHeaderOffset, SubHeaderSize + DiscImage.UserSectorSize);
            BinaryPrimitives.WriteUInt32LittleEndian(_sector.AsSpan(EdcOffset, sizeof(uint)), edc);

            EccEdc.WriteEcc(_sector, true);

            _cachedSector = lba;
        }

        private void WriteHeader(long lba)
        {
            var frame = lba + LeadInFrames;

            _sector[HeaderOffset + 0] = ToBcd((int)(frame / 75 / 60 % 100));
            _sector[HeaderOffset + 1] = ToBcd((int)(frame / 75 % 60));
            _sector[HeaderOffset + 2] = ToBcd((int)(frame % 75));
            _sector[HeaderOffset + 3] = 0x02; // Mode 2
        }

        private void WriteSubHeader()
        {
            // File 0, channel 0, submode "data" (Form 1), no coding info.
            // The 4-byte subheader is stored twice.
            for (var copy = 0; copy < 2; copy++)
            {
                var offset = SubHeaderOffset + copy * 4;
                _sector[offset + 0] = 0x00;
                _sector[offset + 1] = 0x00;
                _sector[offset + 2] = 0x08;
                _sector[offset + 3] = 0x00;
            }
        }

        private static byte ToBcd(int value) => (byte)(((value / 10) << 4) | (value % 10));
    }
}
