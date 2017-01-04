#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.IO;

namespace MediaPortal.Common.Services.ResourceAccess
{
  /// <summary>
  /// Provides a synchronized client facade on an underlaying stream, forwarding all calls to the underlaying "master" stream
  /// but not disposing it. Provides a private position which is independent from other stream clients.
  /// </summary>
  public class SynchronizedMasterStreamClient : Stream
  {
    protected readonly Stream _underlayingStream;
    protected long _position = 0;
    protected object _syncObj;

    public SynchronizedMasterStreamClient(Stream underlayingStream, object sharedSyncObj)
    {
      _underlayingStream = underlayingStream;
      _syncObj = sharedSyncObj;
    }

    #region Base overrides

    public override bool CanRead
    {
      get
      {
        lock (_syncObj)
          return _underlayingStream.CanRead;
      }
    }

    public override bool CanSeek
    {
      get
      {
        lock (_syncObj)
          return _underlayingStream.CanSeek;
      }
    }

    public override bool CanWrite
    {
      get
      {
        lock (_syncObj)
          return _underlayingStream.CanWrite;
      }
    }

    public override long Length
    {
      get
      {
        lock (_syncObj)
          return _underlayingStream.Length;
      }
    }

    public override long Position
    {
      get
      {
        lock (_syncObj)
          return _position;
      }
      set
      {
        lock (_syncObj)
          _position = value;
      }
    }

    public override void Flush()
    {
      lock (_syncObj)
        _underlayingStream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      lock (_syncObj)
      {
        switch (origin)
        {
          case SeekOrigin.Begin:
            _position = offset;
            break;
          case SeekOrigin.Current:
            _position += offset;
            break;
          case SeekOrigin.End:
            _position = Length + offset;
            break;
        }
        return _underlayingStream.Seek(_position, SeekOrigin.Begin);
      }
    }

    public override void SetLength(long value)
    {
      lock (_syncObj)
        _underlayingStream.SetLength(value);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      lock (_syncObj)
      {
        if (_underlayingStream.Position != _position)
          _underlayingStream.Seek(_position, SeekOrigin.Begin);
        int result = _underlayingStream.Read(buffer, offset, count);
        _position += result;
        return result;
      }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      lock (_syncObj)
      {
        if (_underlayingStream.Position != _position)
          _underlayingStream.Seek(_position, SeekOrigin.Begin);
        _underlayingStream.Write(buffer, offset, count);
        _position += count;
      }
    }

    #endregion
  }
}