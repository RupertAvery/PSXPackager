using System;
using System.IO;

namespace Popstation.Pbp;

public class PbpDiscStream : Stream
{
    private readonly PbpDiscEntry _reader;

    private readonly byte[] _buffer =
        new byte[16 * PbpReader.ISO_BLOCK_SIZE];

    private int _bufPos;
    private int _bufLen;

    private long _position;
    private int _blockIndex;

    public PbpDiscStream(PbpDiscEntry reader)
    {
        _reader = reader;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _reader.IsoSize)
            return 0; // EOF

        int totalRead = 0;

        while (count > 0)
        {
            // If buffer empty, refill
            if (_bufPos >= _bufLen)
            {
                _bufLen = (int)_reader.ReadBlock(_blockIndex++, _buffer);
                _bufPos = 0;

                if (_bufLen == 0)
                    break; // EOF
            }

            int available = _bufLen - _bufPos;
            int toCopy = Math.Min(available, count);

            if (_position + toCopy > _reader.IsoSize)
            {
                toCopy = (int)(_reader.IsoSize - _position);
            }

            Array.Copy(_buffer, _bufPos, buffer, offset, toCopy);

            _bufPos += toCopy;
            offset += toCopy;
            count -= toCopy;

            totalRead += toCopy;
            _position += toCopy;

            if (_position >= _reader.IsoSize)
                break;
        }

        return totalRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
        //long newPos = origin switch
        //{
        //    SeekOrigin.Begin => offset,
        //    SeekOrigin.Current => _position + offset,
        //    SeekOrigin.End => _reader.IsoSize + offset,
        //    _ => throw new ArgumentOutOfRangeException()
        //};

        //if (newPos < 0 || newPos > _reader.IsoSize)
        //    throw new IOException("Seek out of range");

        //_position = newPos;

        //// Reset buffer and compute block index
        //_blockIndex = (int)(_position / _buffer.Length);
        //_bufPos = _bufLen = 0;

        //return _position;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;

    public override long Length => _reader.IsoSize;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}