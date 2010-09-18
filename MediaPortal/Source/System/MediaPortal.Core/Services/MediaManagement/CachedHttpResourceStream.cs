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

using System;
using System.IO;

namespace MediaPortal.Core.Services.MediaManagement
{
  public class CachedHttpResourceStream : Stream
  {
    protected readonly long _length;
    protected long _position = 0;
    protected string _tempFilePath;
    protected FileStream _fileStream;
    protected BackgroundHttpDataTransfer _transferWorker;

    public CachedHttpResourceStream(string resourceURL, long length)
    {
      _length = length;
      _tempFilePath = Path.GetTempFileName();
      _fileStream = new FileStream(_tempFilePath, FileMode.Create);
      _fileStream.SetLength(length);
      _transferWorker = new BackgroundHttpDataTransfer(resourceURL, _fileStream);
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (_transferWorker != null)
          _transferWorker.Dispose();
        if (_fileStream != null)
          _fileStream.Close();
        if (!string.IsNullOrEmpty(_tempFilePath))
          File.Delete(_tempFilePath);
      }
      _transferWorker = null;
      _fileStream = null;
      _tempFilePath = null;
      base.Dispose(disposing);
    }

    public object SyncObj
    {
      get { return _transferWorker.SyncObj; }
    }

    public string ResourceURL
    {
      get { return _transferWorker.ResourceURL; }
    }

    public override bool CanRead
    {
      get { return true; }
    }

    public override bool CanWrite
    {
      get { return false; }
    }

    public override bool CanSeek
    {
      get { return true; }
    }

    public override long Position
    {
      get
      {
        lock (SyncObj)
          return _position;
      }
      set { Seek(value, SeekOrigin.Begin); }
    }

    public override long Length
    {
      get { return _length; }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      return _transferWorker.ReadData(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      long position;
      switch (origin)
      {
        case SeekOrigin.Begin:
          position = offset;
          break;
        case SeekOrigin.Current:
          lock (SyncObj)
            position = _position + offset;
          break;
        case SeekOrigin.End:
          position = _length + offset;
          break;
        default:
          position = 0;
          break;
      }
      _transferWorker.Seek(position);
      return offset;
    }

    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    public override void Flush() { }
  }
}