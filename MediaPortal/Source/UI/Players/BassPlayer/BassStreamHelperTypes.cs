#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.UI.Players.BassPlayer
{
  public class SyncFifoBuffer
  {
    protected readonly SyncWord _syncWord;
    protected readonly UInt16[] _buffer;

    public SyncFifoBuffer(SyncWord syncWord)
    {
      _syncWord = syncWord;
      _buffer = new UInt16[syncWord.WordLength];
    }

    public void Write(UInt16 value)
    {
      for (int i = 1; i < _buffer.Length; i++)
      {
        _buffer[i - 1] = _buffer[i];
      }
      _buffer[_buffer.Length - 1] = value;
    }

    public bool IsMatch()
    {
      bool isSyncWord = true;
      int index = 0;
      while (isSyncWord && index < _syncWord.WordLength)
      {
        if (_buffer[index] < _syncWord.Word[index, 0] || _buffer[index] > _syncWord.Word[index, 1])
          isSyncWord = false;
        index++;
      }
      return isSyncWord;
    }
  }

  public abstract class SyncWord
  {
    protected UInt16[,] _word;
    protected int _wordLength;

    protected int _maxFrameSize;

    public UInt16[,] Word
    {
      get { return _word; }
    }

    public int WordLength
    {
      get
      {
        if (_wordLength == 0)
          _wordLength = _word.GetLength(0);
        return _wordLength;
      }
    }

    public int MaxFrameSize
    {
      get { return _maxFrameSize; }
    }
  }

  public class IECSyncWord : SyncWord
  {
    public IECSyncWord()
    {
      // IEC 61937 (S/PDIF compressed audio) sync word:
      // 0xF8724E1F

      _maxFrameSize = 8192;
      _word = new UInt16[2, 2];
      _word[0, 0] = 0xF872;
      _word[0, 1] = 0xF872;
      _word[1, 0] = 0x4E1F;
      _word[1, 1] = 0x4E1F;
    }
  }

  public class DDSyncWord : SyncWord
  {
    public DDSyncWord()
    {
      // DD sync word:
      // 0x0B77

      _maxFrameSize = 8192;
      _word = new UInt16[1, 2];
      _word[0, 0] = 0x0B77;
      _word[0, 1] = 0x0B77;
    }
  }

  public class DTS14bitSyncWord : SyncWord
  {
    public DTS14bitSyncWord()
    {
      // DTS Sync word plus extension in 14 bit format:
      // 0x1FFF              0xE800              0x07F
      // 0001 1111 1111 1111 1110 1000 0000 0000 0000 0111 1111

      _maxFrameSize = 8192;
      _word = new UInt16[3, 2];
      _word[0, 0] = 0x1FFF;
      _word[0, 1] = 0x1FFF;
      _word[1, 0] = 0xE800;
      _word[1, 1] = 0xE800;
      _word[2, 0] = 0x07F0;
      _word[2, 1] = 0x07FF;
    }
  }

  public class DTSSyncWord : SyncWord
  {
    public DTSSyncWord()
    {
      // DTS Sync word:
      // 0x7FFE8001
      // 0111 1111 1111 1110 1000 0000 0000 0001

      _maxFrameSize = 8192;
      _word = new UInt16[2, 2];
      _word[0, 0] = 0x7FFE;
      _word[0, 1] = 0x7FFE;
      _word[1, 0] = 0x8001;
      _word[1, 1] = 0x8001;
    }
  }
}
