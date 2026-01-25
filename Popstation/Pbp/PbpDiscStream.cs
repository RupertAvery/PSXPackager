using System;
using System.IO;

namespace Popstation.Pbp;

/// <summary>
/// Exposes a possibly compressed disc image embedded in an EBOOT as a decompressed stream
/// </summary>
public class PbpDiscStream : Stream
{
    private readonly bool _dispose;
    private readonly PbpDiscEntry _pbpDiscEntry;

    private readonly byte[] _buffer =
        new byte[16 * PbpReader.ISO_BLOCK_SIZE];

    private int _bufPos;
    private int _bufLen;

    private long _position;
    private int _blockIndex;

    /// <summary>
    /// Creates a new <see cref="PbpDiscStream"/> from a <see cref="PbpDiscEntry"/>
    /// </summary>
    /// <param name="pbpDiscEntry">The <see cref="PbpDiscEntry"/> that contains the disc image</param>
    public PbpDiscStream(PbpDiscEntry pbpDiscEntry)
    {
        _pbpDiscEntry = pbpDiscEntry;
    }


    /// <summary>
    /// Creates a new <see cref="PbpDiscStream"/> from a <see cref="PbpDiscEntry"/>
    /// </summary>
    /// <param name="pbpDiscEntry">The <see cref="PbpDiscEntry"/> that contains the disc image</param>
    /// <param name="dispose">Specifies whether the pbpDiscEntry should be disposed when dispose is called on this stream</param>
    public PbpDiscStream(PbpDiscEntry pbpDiscEntry, bool dispose) : this(pbpDiscEntry)
    {
        _dispose = dispose;
    }


    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _pbpDiscEntry.IsoSize)
            return 0; // EOF

        int totalRead = 0;

        while (count > 0)
        {
            // If buffer empty, refill
            if (_bufPos >= _bufLen)
            {
                _bufLen = (int)_pbpDiscEntry.ReadBlock(_blockIndex++, _buffer);
                _bufPos = 0;

                if (_bufLen == 0)
                    break; // EOF
            }

            int available = _bufLen - _bufPos;
            int toCopy = Math.Min(available, count);

            if (_position + toCopy > _pbpDiscEntry.IsoSize)
            {
                toCopy = (int)(_pbpDiscEntry.IsoSize - _position);
            }

            Array.Copy(_buffer, _bufPos, buffer, offset, toCopy);

            _bufPos += toCopy;
            offset += toCopy;
            count -= toCopy;

            totalRead += toCopy;
            _position += toCopy;

            if (_position >= _pbpDiscEntry.IsoSize)
                break;
        }

        return totalRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long newPos = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => _pbpDiscEntry.IsoSize + offset,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (newPos < 0 || newPos > _pbpDiscEntry.IsoSize)
            throw new IOException("Seek out of range");

        _position = newPos;

        // Reset buffer and compute block index
        _blockIndex = (int)(_position / _buffer.Length);
        _bufPos = _bufLen = 0;

        return _position;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length => _pbpDiscEntry.IsoSize;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (_dispose)
        {
            _pbpDiscEntry.Dispose();
        }
        base.Dispose(disposing);
    }
}