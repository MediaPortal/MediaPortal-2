#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.Transcoding.Interfaces.SlimTv
{
  public class FileReader : IDisposable
  {
    string _fileName = "";
    FileStream _file = null;
    object _accessLock = new object();
    bool _isStopping = false;
    ILogger _logger = null;

    public FileReader()
    {
      _logger = ServiceRegistration.Get<ILogger>();
    }

    public void Dispose()
    {
      _isStopping = true;
      if (_file != null)
      {
        _file.Close();
      }
    }

    public string GetFileName()
    {
      lock (_accessLock)
      {
        return _fileName;
      }
    }

    public bool SetFileName(string FileName)
    {
      lock (_accessLock)
      {
        _fileName = FileName;
      }
      return true;
    }

    public bool OpenFile()
    {
      int tmo = 14;

      lock (_accessLock)
      {
        if (_file != null)
        {
          _logger.Debug("FileReader: OpenFile() file already open");
          return true;
        }

        if (string.IsNullOrEmpty(_fileName) == true)
        {
          _logger.Debug("FileReader: OpenFile() no filename");
          return false;
        }

        _isStopping = false;

        while (tmo-- > 0)
        {
          if (_isStopping)
            return false;

          try
          {
            _file = File.Open(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            break;
          }
          catch { }

          Thread.Sleep(250);
        }

        if (tmo <= 0)
        {
          _logger.Debug("FileReader: OpenFile(), open file failed. Filename = {0}", _fileName);
          return false;
        }
        _logger.Debug("FileReader: OpenFile(), {0} tries to succeed opening {1}.", 15 - tmo, _fileName);

        _file.Seek(0, SeekOrigin.Begin);

        return true;
      }

    }

    public bool CloseFile()
    {
      // Must lock this section to prevent problems related to
      // closing the file while still receiving data in Receive()
      _isStopping = true;
      lock (_accessLock)
      {
        if (_file == null)
        {
          _logger.Debug("FileReader: CloseFile() no open file");
          return true;
        }
        _file.Close();

        _file = null; // Invalidate the file

        return true;
      }
    }

    public bool IsFileInvalid()
    {
      lock (_accessLock)
      {
        return (_file == null);
      }
    }

    public bool GetFileSize(out long StartPosition, out long Length)
    {
      StartPosition = 0;
      Length = 0;

      if (_file.Length == 0)
      {
        return false;
      }

      Length = _file.Length;
      return true;
    }

    public long SetFilePointer(long DistanceToMove, SeekOrigin MoveMethod)
    {
      lock (_accessLock)
      {
        try
        {
          return _file.Seek(DistanceToMove, MoveMethod);
        }
        catch
        {
          return -1;
        }
      }
    }

    public long GetFilePointer()
    {
      lock (_accessLock)
      {
        return _file.Position;
      }
    }

    public bool Read(byte[] Data, int Offset, int DataLength, out int ReadBytes)
    {
      lock (_accessLock)
      {
        if (_isStopping)
        {
          ReadBytes = 0;
          return false;
        }

        // If the file has already been closed, don't continue
        if (_file == null)
        {
          _logger.Debug("FileReader: Read() no open file");
          ReadBytes = 0;
          return false;
        }

        try
        {
          ReadBytes = _file.Read(Data, Offset, DataLength);//Read file data into buffer
        }
        catch (Exception ex)
        {
          if (!_isStopping)
          {
            _logger.Debug("FileReader: Read() read failed, Error = {0}, filename = {}", ex.Message, _fileName);
          }
          ReadBytes = 0;
          return false;
        }

        if (ReadBytes < DataLength)
        {
          _logger.Debug("FileReader: Read() read to less bytes");
          return false;
        }
        return true;
      }
    }

    public bool Read(byte[] Data, int Offset, int DataLength, out int ReadBytes, long DistanceToMove, SeekOrigin MoveMethod)
    {
      //If end method then we want llDistanceToMove to be the end of the buffer that we read.
      if (MoveMethod == SeekOrigin.End)
        DistanceToMove = 0 - DistanceToMove - DataLength;

      _file.Seek(DistanceToMove, MoveMethod);

      return Read(Data, Offset, DataLength, out ReadBytes);
    }

    public long GetFileSize()
    {
      lock (_accessLock)
      {
        long pStartPosition = 0;
        long pLength = 0;
        GetFileSize(out pStartPosition, out pLength);
        return pLength;
      }
    }

    public void SetStopping(bool IsStopping)
    {
      _isStopping = IsStopping;
    }
  }
}
