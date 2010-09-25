#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System.IO;

namespace MediaPortal.Core.Services.MediaManagement
{
  /// <summary>
  /// Provides a client facade on an underlaying stream, forwarding all calls to the underlaying "master" stream
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
      get { return _underlayingStream.CanRead; }
    }

    public override bool CanSeek
    {
      get { return _underlayingStream.CanSeek; }
    }

    public override bool CanWrite
    {
      get { return _underlayingStream.CanWrite; }
    }

    public override long Length
    {
      get { return _underlayingStream.Length; }
    }

    public override long Position
    {
      get { return _position; }
      set
      {
        _position = value;
        _underlayingStream.Position = value;
      }
    }

    public override void Flush()
    {
      _underlayingStream.Flush();
    }

    public override long Seek(long offset, SeekOrigin origin)
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

    public override void SetLength(long value)
    {
      _underlayingStream.SetLength(value);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      lock (_syncObj)
      {
        Seek(_position, SeekOrigin.Begin);
        int result = _underlayingStream.Read(buffer, offset, count);
        _position += result;
        return result;
      }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      lock (_syncObj)
      {
        Seek(_position, SeekOrigin.Begin);
        _underlayingStream.Write(buffer, offset, count);
      }
    }

    #endregion
  }
}