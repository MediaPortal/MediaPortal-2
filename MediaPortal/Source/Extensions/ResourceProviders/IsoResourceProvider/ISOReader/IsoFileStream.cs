using System;
using System.IO;

namespace ISOReader
{
    internal class IsoFileStream : Stream
    {
        private long _startOffset;
        private long _dataLength;
        private Stream _isoStream;

        public IsoFileStream(Stream isoStream, long startOffset, long dataLength)
        {
          _isoStream = isoStream;
          _startOffset = startOffset;
          _dataLength = dataLength;
          _isoStream.Seek(_startOffset, SeekOrigin.Begin);
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _dataLength; }
        }

        public override long Position
        {
          get { return _isoStream.Position - _startOffset; }
          set { _isoStream.Position = value + _startOffset; }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Position > _dataLength)
                return 0;
            int toRead = (int)Math.Min((uint)count, _dataLength - Position);
            return _isoStream.Read(buffer, offset, toRead);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
          long newPos = _isoStream.Seek(offset + _startOffset, origin);
          return newPos - _startOffset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
