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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Plugins.Transcoding.Interfaces.SlimTv
{
  public class MultiFileReader : IDisposable
  {
    //Maximum time in msec to wait for the buffer file to become available - Needed for DVB radio (this sometimes takes some time)
    const int MAX_BUFFER_TIMEOUT = 1500;

    //block read sizes for SMB2 data cache workaround
    const int NEXT_READ_SIZE = 8192;
    const int NEXT_READ_ROLLOVER = (NEXT_READ_SIZE * 64);

    const int INFO_BUFF_SIZE = 131072;

    FileReader _tsBufferFile = new FileReader();
    long _startPosition;
    long _endPosition;
    long _currentPosition;
    int _filesAdded;
    int _filesRemoved;

    List<MultiFileReaderFile> _tsFiles = new List<MultiFileReaderFile>();

    FileReader _tsFile = new FileReader();
    long _tsFileId;

    FileReader _tsFileNext = new FileReader();
    long _tsFileIdNext;
    DateTime _lastFileNextRead;
    long _currPosnFileNext;
    ILocalFsResourceAccessor _fileAccessor = null;

    FileReader _tsFileGetLength = new FileReader();

    bool _useFileNext;
    bool _isStopping;

    byte[] _fileReadNextBuffer;
    byte[] _infoFileBuffer1;
    byte[] _infoFileBuffer2;
    object _accessLock = new object();

    ILogger _logger = null;


    public MultiFileReader(bool UseFileNext)
    {
      _startPosition = 0;
      _endPosition = 0;
      _currentPosition = 0;
      _filesAdded = 0;
      _filesRemoved = 0;
      _tsFileId = -1;
      _tsFileIdNext = -1;
      _lastFileNextRead = DateTime.Now;
      _currPosnFileNext = 0;
      _useFileNext = UseFileNext;
      _isStopping = false;

      _fileReadNextBuffer = null;
      _infoFileBuffer1 = null;
      _infoFileBuffer2 = null;

      _fileReadNextBuffer = new byte[NEXT_READ_SIZE];
      _infoFileBuffer1 = new byte[INFO_BUFF_SIZE];
      _infoFileBuffer2 = new byte[INFO_BUFF_SIZE];

      _logger = ServiceRegistration.Get<ILogger>();
      _logger.Debug("MultiFileReader: ctor, useFileNext = {0}", _useFileNext);
    }

    public void Dispose()
    {
      SetStopping(true);
      _tsBufferFile.CloseFile();
      _tsFile.CloseFile();
      _tsFileNext.CloseFile();

      if (_fileReadNextBuffer != null)
      {
        _fileReadNextBuffer = null;
      }
      else
      {
        _logger.Debug("MultiFileReader: dtor - ERROR m_pFileReadBuffer is NULL !!");
      }

      if (_infoFileBuffer1 != null)
      {
        _infoFileBuffer1 = null;
      }
      else
      {
        _logger.Debug("MultiFileReader: dtor - ERROR m_pInfoFileBuffer1 is NULL !!");
      }

      if (_infoFileBuffer2 != null)
      {
        _infoFileBuffer2 = null;
      }
      else
      {
        _logger.Debug("MultiFileReader: dtor - ERROR m_pInfoFileBuffer2 is NULL !!");
      }

      _logger.Debug("MultiFileReader: dtor");
    }

    public string GetFileName()
    {
      lock (_accessLock)
      {
        return _tsBufferFile.GetFileName();
      }
    }

    public bool SetFileName(string FileName)
    {
      lock (_accessLock)
      {
        return _tsBufferFile.SetFileName(FileName);
      }
    }

    public ILocalFsResourceAccessor GetFileAccessor()
    {
      lock (_accessLock)
      {
        return _fileAccessor;
      }
    }

    public bool SetFileAccessor(ILocalFsResourceAccessor FileAccessor)
    {
      lock (_accessLock)
      {
        _fileAccessor = FileAccessor;
        return true;
      }
    }

    public bool OpenFile()
    {
      lock (_accessLock)
      {
        SetStopping(false);

        if(string.IsNullOrEmpty(_tsBufferFile.GetFileName()) && _fileAccessor != null)
        {
          _tsBufferFile.SetFileName(_fileAccessor.LocalFileSystemPath);
        }

        bool hr = _tsBufferFile.OpenFile();

        //For radio the buffer sometimes needs some time to become available, so wait try it more than once
        DateTime tc = DateTime.Now;
        while (RefreshTSBufferFile() == false)
        {
          if ((DateTime.Now - tc).TotalMilliseconds > MAX_BUFFER_TIMEOUT)
          {
            _logger.Debug("MultiFileReader: timed out while waiting for buffer file to become available");
            return false;
          }
          Thread.Sleep(1);
        }

        _currentPosition = 0;

        return hr;
      }
    }

    public bool CloseFile()
    {
      SetStopping(true);
      lock (_accessLock)
      {
        bool hr = true;
        hr &= _tsBufferFile.CloseFile();
        hr &= _tsFile.CloseFile();
        _tsFileId = -1;
        hr &= _tsFileNext.CloseFile();
        _tsFileIdNext = -1;
        return hr;
      }
    }

    bool IsFileInvalid()
    {
      lock (_accessLock)
      {
        return _tsBufferFile.IsFileInvalid();
      }
    }

    bool SetFilePointer(long DistanceToMove, SeekOrigin MoveMethod)
    {
      lock (_accessLock)
      {
        RefreshTSBufferFile();

        if (MoveMethod == SeekOrigin.End)
        {
          _currentPosition = _endPosition + DistanceToMove;
        }
        else if (MoveMethod == SeekOrigin.Current)
        {
          _currentPosition += DistanceToMove;
        }
        else
        {
          _currentPosition = _startPosition + DistanceToMove;
        }

        if (_currentPosition < _startPosition)
          _currentPosition = _startPosition;

        if (_currentPosition > _endPosition)
        {
          _logger.Debug("MultiFileReader: Seeking beyond the end position: {0} > {1}", _currentPosition, _endPosition);
          _currentPosition = _endPosition;
        }

        return true;
      }
    }

    public long GetFilePointer()
    {
      lock (_accessLock)
      {
        return _currentPosition;
      }
    }

    public bool Read(byte[] Data, int Offset, int DataLength, out int ReadBytes)
    {
      lock (_accessLock)
      {
        return ReadNoLock(Data, Offset, DataLength, out ReadBytes);
      }
    }

    bool ReadNoLock(byte[] Data, int Offset, int DataLength, out int ReadBytes)
    {
      bool hr = true;

      // If the file has already been closed, don't continue
      if (_tsBufferFile.IsFileInvalid())
      {
        _logger.Debug("MultiFileReader: Read() - IsFileInvalid() failure");
        ReadBytes = 0;
        return false;
      }

      hr = RefreshTSBufferFile();
      if (hr != true)
      {
        ReadBytes = 0;
        return false;
      }

      if (_currentPosition < _startPosition)
        _currentPosition = _startPosition;

      long oldCurrentPosn = _currentPosition;

      // Find out which file the currentPosition is in (and the next file, if it exists)
      MultiFileReaderFile file = null;
      MultiFileReaderFile fileNext = null;
      bool fileFound = false;
      for (int it = 0; it < _tsFiles.Count; it++)
      {
        if (fileFound)
        {
          fileNext = _tsFiles[it]; //This is the next file after the file we need to read
          break;
        }
        file = _tsFiles[it];
        if (_currentPosition < (file.StartPosition + file.Length))
        {
          fileFound = true;
        }
      };

      if (file == null)
      {
        _logger.Debug("MultiFileReader: no file");
        ReadBytes = 0;
        return false;
      }

      if (!fileFound)
      {
        // The current position is past the end of the last file
        ReadBytes = 0;
        return false;
      }

      if (_tsFileId != file.FilePositionId)
      {
        if (!_tsFile.IsFileInvalid())
        {
          _tsFile.CloseFile();
        }
        _tsFile.SetFileName(file.Filename);
        _tsFile.OpenFile();
        _tsFileId = file.FilePositionId;
      }

      //Start of 'file next' SMB data cache workaround processing
      if (fileNext == null && !_tsFileNext.IsFileInvalid())
      {
        _tsFileNext.CloseFile();
        _tsFileIdNext = -1;
      }

      if (fileNext != null && _useFileNext && _fileReadNextBuffer != null)
      {
        if (_tsFileIdNext != fileNext.FilePositionId)
        {
          if (!_tsFileNext.IsFileInvalid())
          {
            _tsFileNext.CloseFile();
          }
          _tsFileNext.SetFileName(fileNext.Filename);
          _tsFileNext.OpenFile();
          _tsFileIdNext = fileNext.FilePositionId;
          _currPosnFileNext = 0;

          if (fileNext.Length >= NEXT_READ_SIZE)
          {
            //Do a dummy read to try and refresh the SMB cache
            int bytesNextRead = 0;
            _tsFileNext.SetFilePointer(_currPosnFileNext, SeekOrigin.Begin);
            _tsFileNext.Read(_fileReadNextBuffer, 0, NEXT_READ_SIZE, out bytesNextRead);
            _lastFileNextRead = DateTime.Now;
            if (bytesNextRead != NEXT_READ_SIZE)
            {
              string url = Encoding.Unicode.GetString(File.ReadAllBytes(fileNext.Filename));
              _logger.Debug("MultiFileReader: FileNext read 1 failed, bytes {0}, posn {1}, file {2}", bytesNextRead, _currPosnFileNext, url);
            }
            _currPosnFileNext += NEXT_READ_SIZE;
            _currPosnFileNext %= NEXT_READ_ROLLOVER;
          }
        }
        else if ((fileNext.Length >= (_currPosnFileNext + NEXT_READ_SIZE)) && (DateTime.Now > _lastFileNextRead.AddMilliseconds(1100)))
        {
          //Do a dummy read to try and refresh the SMB data cache
          int bytesNextRead = 0;
          _tsFileNext.SetFilePointer(_currPosnFileNext, SeekOrigin.Begin);
          _tsFileNext.Read(_fileReadNextBuffer, 0, NEXT_READ_SIZE, out bytesNextRead);
          _lastFileNextRead = DateTime.Now;
          if (bytesNextRead != NEXT_READ_SIZE)
          {
            string url = Encoding.Unicode.GetString(File.ReadAllBytes(fileNext.Filename));
            _logger.Debug("MultiFileReader: FileNext read 2 failed, bytes {0}, posn {1}, file {2}", bytesNextRead, _currPosnFileNext, url);
          }
          _currPosnFileNext += NEXT_READ_SIZE;
          _currPosnFileNext %= NEXT_READ_ROLLOVER;
        }
      }
      //End of 'file next' SMB data cache workaround processing

      long seekPosition = _currentPosition - file.StartPosition;

      _tsFile.SetFilePointer(seekPosition, SeekOrigin.Begin);
      long posSeeked = _tsFile.GetFilePointer();
      if (posSeeked != seekPosition)
      {
        _logger.Debug("MultiFileReader: SEEK FAILED");
        ReadBytes = 0;
        _currentPosition = oldCurrentPosn;
        return false;
      }

      int bytesRead = 0;
      int bytesToRead = Convert.ToInt32(file.Length - seekPosition);
      if (DataLength > bytesToRead)
      {
        hr = _tsFile.Read(Data, Offset, bytesToRead, out bytesRead);
        if (hr == false)
        {
          if (!_isStopping)
          {
            _logger.Debug("MultiFileReader: READ FAILED1");
          }
          ReadBytes = 0;
          _currentPosition = oldCurrentPosn;
          return false;
        }
        _currentPosition += bytesRead;

        if ((bytesRead < bytesToRead) || fileNext == null)
        {
          //We haven't got all of the current file segment (so we can't read the next segment), 
          //or there is no 'next file' to read so just return the data we have...
          ReadBytes = bytesRead;
          return false;
        }

        hr = ReadNoLock(Data, Offset + bytesToRead, DataLength - bytesToRead, out ReadBytes);
        if (hr == false)
        {
          if (!_isStopping)
          {
            _logger.Debug("MultiFileReader: READ FAILED2");
          }
          ReadBytes = 0;
          _currentPosition = oldCurrentPosn;
          return false;
        }
        ReadBytes += bytesRead;
      }
      else
      {
        hr = _tsFile.Read(Data, Offset, DataLength, out ReadBytes);
        if (hr == false)
        {
          if (!_isStopping)
          {
            _logger.Debug("MultiFileReader::READ FAILED3");
          }
          ReadBytes = 0;
          _currentPosition = oldCurrentPosn;
          return false;
        }
        _currentPosition += ReadBytes;
      }

      return hr;
    }

    public bool Read(byte[] Data, int Offset, int DataLength, out int ReadBytes, long DistanceToMove, SeekOrigin MoveMethod)
    {
      //If end method then we want llDistanceToMove to be the end of the buffer that we read.
      if (MoveMethod == SeekOrigin.End)
        DistanceToMove = 0 - DistanceToMove - DataLength;

      SetFilePointer(DistanceToMove, MoveMethod);

      return Read(Data, Offset, DataLength, out ReadBytes);
    }

    bool RefreshTSBufferFile()
    {
      if (_tsBufferFile.IsFileInvalid())
      {
        return false;
      }

      int bytesRead;
      MultiFileReaderFile file;

      bool result;
      long currentPosition = 0;
      int filesAdded, filesRemoved;
      int filesAdded2, filesRemoved2;
      long Error = 0;
      long Loop = 10;
      long fileLength = 0;

      do
      {
        if (_isStopping || _infoFileBuffer1 == null || _infoFileBuffer2 == null)
          return false;

        if (Error > 0) //Handle errors from a previous loop iteration
        {
          if (Loop < 9) //An error on the first loop iteration is quasi-normal, so don't log it
          {
            _logger.Debug("MultiFileReader has error {0} in Loop {1}. Try to clear SMB Cache.", Error, 10 - Loop);
            _logger.Debug("MultiFileReader m_filesAdded: {0}, m_filesRemoved: {1}, m_startPosition: {2}, m_endPosition: {3}, currentPosition = {4}", _filesAdded, _filesRemoved, _startPosition, _endPosition, currentPosition);
          }
          // try to clear local / remote SMB file cache. This should happen when we close the filehandle
          _tsBufferFile.CloseFile();
          _tsBufferFile.OpenFile();
          Thread.Sleep(5);
        }

        Error = 0;
        currentPosition = -1;
        filesAdded = -1;
        filesRemoved = -1;
        filesAdded2 = -2;
        filesRemoved2 = -2;
        Loop--;

        fileLength = _tsBufferFile.GetFileSize();

        // Min file length is Header ( __int64 + long + long ) + filelist ( > 0 ) + Footer ( long + long ) 
        if (fileLength <= (8 + 4 + 4 + 2 + 4 + 4))
          return false;

        if (fileLength % 2 != 0) //Must be an even number of bytes in length
          return false;

        if (fileLength > INFO_BUFF_SIZE)
          return false;

        //int readLength = sizeof(currentPosition) + sizeof(filesAdded) + sizeof(filesRemoved);
        int readLength = 8 + 4 + 4;

        _tsBufferFile.SetFilePointer(0, SeekOrigin.Begin);
        result = _tsBufferFile.Read(_infoFileBuffer1, 0, readLength, out bytesRead);

        if (result == false || bytesRead != readLength)
          Error |= 0x02;

        if (Error == 0)
        {
          currentPosition = BitConverter.ToInt64(_infoFileBuffer1, 0);
          filesAdded = BitConverter.ToInt32(_infoFileBuffer1, 8);
          filesRemoved = BitConverter.ToInt32(_infoFileBuffer1, 8 + 4);
        }

        _tsBufferFile.SetFilePointer(0, SeekOrigin.Begin);
        result = _tsBufferFile.Read(_infoFileBuffer2, 0, readLength, out bytesRead);

        if (result == false || bytesRead != readLength)
          Error |= 0x04;

        if (Error == 0)
        {
          currentPosition = BitConverter.ToInt64(_infoFileBuffer2, 0);
          filesAdded2 = BitConverter.ToInt32(_infoFileBuffer2, 8);
          filesRemoved2 = BitConverter.ToInt32(_infoFileBuffer2, 8 + 4);
        }

        if ((filesAdded2 != filesAdded) || (filesRemoved2 != filesRemoved))
        {
          Error |= 0x08;
          continue;
        }

        // If no files added or removed, break the loop !
        if ((_filesAdded == filesAdded) && (_filesRemoved == filesRemoved))
          break;

        //Now read the full file for processing and comparison
        _tsBufferFile.SetFilePointer(0, SeekOrigin.Begin);
        result = _tsBufferFile.Read(_infoFileBuffer1, 0, Convert.ToInt32(fileLength), out bytesRead);

        if (result == false || bytesRead != fileLength)
          Error |= 0x20;

        Thread.Sleep(1);

        //read it again to a different buffer  
        _tsBufferFile.SetFilePointer(0, SeekOrigin.Begin);
        result = _tsBufferFile.Read(_infoFileBuffer2, 0, Convert.ToInt32(fileLength), out bytesRead);

        if (result == false || bytesRead != fileLength)
          Error |= 0x40;

        //Compare the two buffers (except the 'currentPosition' values), and compare the filesAdded/filesRemoved values 
        //at the beginning and end of the second buffer for integrity checking
        if (
            (Error == 0)
            && (ArrayCompare(_infoFileBuffer1, 8, _infoFileBuffer2, 8, Convert.ToInt32(fileLength - 8)) == true)
            && (ArrayCompare(_infoFileBuffer2, 8, _infoFileBuffer2, Convert.ToInt32(fileLength - (2 * 4)), 2 * 4) == true)
            )
        {
          currentPosition = BitConverter.ToInt64(_infoFileBuffer2, 0);
          filesAdded = BitConverter.ToInt32(_infoFileBuffer2, 8);
          filesRemoved = BitConverter.ToInt32(_infoFileBuffer2, 8 + 4);
        }
        else
        {
          Error |= 0x80;
          continue;
        }

        //Rebuild the file list if files have been added or removed
        if ((_filesAdded != filesAdded) || (_filesRemoved != filesRemoved))
        {
          int filesToRemove = filesRemoved - _filesRemoved;
          int filesToAdd = filesAdded - _filesAdded;
          int fileID = filesRemoved;
          long nextStartPosition = 0;

          // Removed files that aren't present anymore.
          while ((filesToRemove > 0) && (_tsFiles.Count > 0))
          {
            _tsFiles.RemoveAt(0);

            filesToRemove--;
          }

          // Figure out what the start position of the next new file will be
          if (_tsFiles.Count > 0)
          {
            file = _tsFiles[_tsFiles.Count - 1];

            if (filesToAdd > 0)
            {
              // If we're adding files the chances are the one at the back has a partial length
              // so we need update it.
              result = GetFileLength(file.Filename, out file.Length, true);
              if (result == false)
                Error |= 0x10;
            }

            nextStartPosition = file.StartPosition + file.Length;
          }

          //Get the real path of the buffer file
          string wfilename = _tsBufferFile.GetFileName();
          string path = Path.GetDirectoryName(wfilename);

          // Create a list of files in the .tsbuffer file.
          List<string> filenames = new List<string>();
          string fileNames = Encoding.Unicode.GetString(_infoFileBuffer2, 8 + 4 + 4, Convert.ToInt32(fileLength - (4 + 4)));
          foreach (string filename in fileNames.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries))
          {
            string pFilename;
            if (string.IsNullOrEmpty(filename) == false)
            {
              if (filename.IndexOfAny(Path.GetInvalidPathChars()) == -1)
              {
                string temp = Path.GetFileName(filename);
                if (string.IsNullOrEmpty(path) == false && string.IsNullOrEmpty(temp) == false)
                {
                  pFilename = Path.Combine(path, temp);
                }
                else
                {
                  pFilename = filename;
                }
                filenames.Add(pFilename);
              }
            }
          }

          if ((filesAdded - filesRemoved) != filenames.Count)
          {
            _logger.Debug("MultiFileReader: expected file count incorrect");
            Error |= 0x200;
            continue;
          }

          // Go through files
          int itFiles = 0;
          int itFilenames = 0;

          while (itFiles < _tsFiles.Count)
          {
            itFiles++;
            fileID++;

            if (itFilenames < filenames.Count)
            {
              // TODO: Check that the filenames match. ( Ambass : With buffer integrity check, probably no need to do this !)
              itFilenames++;
            }
            else
            {
              _logger.Debug("MultiFileReader: has missing files!!");
              Error |= 0x400;
              continue;
            }
          }

          while (itFilenames < filenames.Count)
          {
            string pFilename = filenames[itFilenames];

            file = new MultiFileReaderFile();
            file.Filename = pFilename;
            file.StartPosition = nextStartPosition;

            fileID++;
            file.FilePositionId = fileID;

            result = GetFileLength(pFilename, out file.Length, false);
            if (result == false)
              Error |= 0x100;

            _tsFiles.Add(file);

            nextStartPosition = file.StartPosition + file.Length;

            itFilenames++;
          }

          if (_tsFiles.Count != filenames.Count)
          {
            _logger.Debug("MultiFileReader: files to filenames mismatch");
            Error |= 0x800;
            continue;
          }

          _filesAdded = filesAdded;
          _filesRemoved = filesRemoved;
        }

      } while (Error > 0 && Loop > 0); // If Error is set, try again...until Loop reaches 0.

      if (Loop < 8)
      {
        _logger.Debug("MultiFileReader: has waited {0} times for TSbuffer integrity.", 10 - Loop);

        if (Error > 0)
        {
          _logger.Debug("MultiFileReader has failed for TSbuffer integrity. Error : {0}", Error);
          return false;
        }
      }

      if (_tsFiles.Count > 0)
      {
        file = _tsFiles[0];
        _startPosition = file.StartPosition;

        file = _tsFiles[_tsFiles.Count - 1];
        file.Length = currentPosition;
        _endPosition = file.StartPosition + currentPosition;
      }
      else
      {
        _startPosition = 0;
        _endPosition = 0;
      }

      return true;
    }

    bool ArrayCompare(byte[] Array1, int Array1Offset, byte[] Array2, int Array2Offset, int CompareLength)
    {
      if (Array1 == Array2)
      {
        return true;
      }
      if ((Array1 != null) && (Array2 != null))
      {
        if ((Array1.Length - Array1Offset) < CompareLength)
        {
          return false;
        }
        if ((Array2.Length - Array2Offset) < CompareLength)
        {
          return false;
        }
        for (int i = 0; i < CompareLength; i++)
        {
          if (Array1[i + Array1Offset] != Array2[i + Array2Offset])
          {
            return false;
          }
        }
        return true;
      }
      return false;
    }


    public long GetFileSize()
    {
      lock (_accessLock)
      {
        RefreshTSBufferFile();
        return _endPosition - _startPosition;
      }
    }

    bool GetFileLength(string pFilename, out long length, bool doubleCheck)
    {
      bool hr = true;

      long Error = 0;
      long Loop = 10;

      do
      {
        if (_isStopping)
        {
          length = 0;
          return false;
        }

        if (Error > 0) //Handle errors from a previous loop iteration
        {
          if (Loop < 3)
          {
            _logger.Debug("MultiFileReader::GetFileLength() has error {0} in Loop {1}. Trying again", Error, 10 - Loop);
          }
          Thread.Sleep(5);
        }

        Error = 0;
        Loop--;

        _tsFileGetLength.SetFileName(pFilename);
        hr = _tsFileGetLength.OpenFile();
        if (hr == false)
        {
          Error |= 0x2;
        }
        length = _tsFileGetLength.GetFileSize();
        if (doubleCheck)
        {
          Thread.Sleep(5);
          if (length != _tsFileGetLength.GetFileSize())
          {
            Error |= 0x4;
          }
        }
        _tsFileGetLength.CloseFile();

      } while (Error > 0 && Loop > 0); // If Error is set, try again...until Loop reaches 0.

      if (Loop < 2)
      {
        _logger.Debug("MultiFileReader: GetFileLength() has waited {0} times for stable length.", 10 - Loop);

        if (Error > 0)
        {
          _logger.Debug("MultiFileReader: GetFileLength() has failed. Error: {0}", Error);
          length = 0;
          return false;
        }
      }

      return hr;
    }

    //Enable 'FileNext' file reads to workaround SMB2/SM3 possible 'data cache' problems
    public void SetFileNext(bool UseFileNext)
    {
      lock (_accessLock)
      {
        _useFileNext = UseFileNext;
      }
    }

    public bool GetFileNext()
    {
      lock (_accessLock)
      {
        return _useFileNext;
      }
    }

    public void SetStopping(bool IsStopping)
    {
      _isStopping = IsStopping;

      _tsBufferFile.SetStopping(IsStopping);
      _tsFile.SetStopping(IsStopping);
      _tsFileGetLength.SetStopping(IsStopping);
      _tsFileNext.SetStopping(IsStopping);
    }
  }

  class MultiFileReaderFile
  {
    public string Filename;
    public long StartPosition;
    public long Length;
    public long FilePositionId;
  };
}
