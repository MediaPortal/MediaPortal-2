#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using System;
using System.IO;

namespace MediaPortal.Core.Services.MediaManagement
{
  /// <summary>
  /// Stream facade to a HTTP data transfer. This class is not multithreading-safe.
  /// </summary>
  public class CachedHttpResourceStream : Stream
  {
    protected static int FILE_BUF_SIZE = 16384;

    protected readonly long _length;
    protected readonly string _resourceURL;
    protected string _tempFilePath;
    protected FileStream _fileBufferStream;
    protected Stream _underlayingStream;

    public CachedHttpResourceStream(string resourceURL, long length)
    {
      _length = length;
      _resourceURL = resourceURL;
      _tempFilePath = Path.GetTempFileName();
      _fileBufferStream = new FileStream(_tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
          FILE_BUF_SIZE, FileOptions.Asynchronous | FileOptions.RandomAccess | FileOptions.DeleteOnClose);
      _fileBufferStream.SetLength(length);
      ForceEagerAllocation(_fileBufferStream);
      _underlayingStream = new BackgroundHttpDataTransfer(_resourceURL, _fileBufferStream);
    }

    /// <summary>
    /// When dealing with large files whose path is created by calling <see cref="Path.GetTempFileName"/>, writing to that
    /// file at the end takes a very long time (here: 1 GB file, writing to a position at the end takes about 19 sec).
    /// This method does an eager allocation by writing a single byte at the end of the file.
    /// </summary>
    /// <param name="stream">File stream pointing to a file in temp file folder.</param>
    private static void ForceEagerAllocation(Stream stream)
    {
      // This is a hack to force Windows to allocate the file at once
      if (stream.Length > 0)
      {
        stream.Position = stream.Length - 1;
        stream.WriteByte(0xFF);
        stream.Flush();
      }
      stream.Position = 0;
    }

    protected override void Dispose(bool disposing)
    {
      if (disposing)
      {
        if (_fileBufferStream != null)
          _fileBufferStream.Close();
        if (_underlayingStream != null)
          _underlayingStream.Dispose();
        if (!string.IsNullOrEmpty(_tempFilePath))
          File.Delete(_tempFilePath);
      }
      _underlayingStream = null;
      _fileBufferStream = null;
      _tempFilePath = null;
      base.Dispose(disposing);
    }

    public string ResourceURL
    {
      get { return _resourceURL; }
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
      get { return _underlayingStream.Position; }
      set { _underlayingStream.Position = value; }
    }

    public override long Length
    {
      get { return _length; }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
      BackgroundHttpDataTransfer bhdt = _underlayingStream as BackgroundHttpDataTransfer;
      if (bhdt != null && bhdt.TransferComplete)
      { // Transfer is complete - exchange our HTTP background worker with actual file stream
        _fileBufferStream.Seek(bhdt.Position, SeekOrigin.Begin);
        _underlayingStream = _fileBufferStream;
        bhdt.Dispose();
      }
      return _underlayingStream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      return _underlayingStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
      throw new NotSupportedException();
    }

    public override void Flush() { }

    public override string ToString()
    {
      return string.Format("Cached http resource string for resource '{0}'", _resourceURL);
    }
  }
}