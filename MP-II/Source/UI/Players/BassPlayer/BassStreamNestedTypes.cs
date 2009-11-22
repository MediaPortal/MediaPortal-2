#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;

namespace Media.Players.BassPlayer
{
  public partial class BassStream
  {
    class SyncFifoBuffer
    {
      SyncWord _SyncWord;
      UInt16[] _Buffer;

      public SyncFifoBuffer(SyncWord syncWord)
      {
        _SyncWord = syncWord;
        _Buffer = new UInt16[syncWord.WordLength];
      }

      public void Write(UInt16 value)
      {
        for (int i = 1; i < _Buffer.Length; i++)
        {
          _Buffer[i - 1] = _Buffer[i];
        }
        _Buffer[_Buffer.Length - 1] = value;
      }

      public bool IsMatch()
      {
        bool isSyncWord = true;
        int index = 0;
        while (isSyncWord && index < _SyncWord.WordLength)
        {
          if (_Buffer[index] < _SyncWord.Word[index, 0] || _Buffer[index] > _SyncWord.Word[index, 1])
            isSyncWord = false;
          index++;
        }
        return isSyncWord;
      }
    }

    abstract class SyncWord
    {
      protected UInt16[,] _Word;
      protected int _WordLength;

      protected int _MaxFrameSize;

      public UInt16[,] Word
      {
        get { return _Word; }
      }

      public int WordLength
      {
        get
        {
          if (_WordLength == 0)
            _WordLength = _Word.GetLength(0);
          return _WordLength;
        }
      }

      public int MaxFrameSize
      {
        get { return _MaxFrameSize; }
      }

      public SyncWord()
      {
      }
    }

    class IECSyncWord : SyncWord
    {
      public IECSyncWord()
      {
        // IEC 61937 (S/PDIF compressed audio) sync word:
        // 0xF8724E1F

        _MaxFrameSize = 8192;
        _Word = new UInt16[2, 2];
        _Word[0, 0] = 0xF872;
        _Word[0, 1] = 0xF872;
        _Word[1, 0] = 0x4E1F;
        _Word[1, 1] = 0x4E1F;
      }
    }

    class DDSyncWord : SyncWord
    {
      public DDSyncWord()
      {
        // DD sync word:
        // 0x0B77

        _MaxFrameSize = 8192;
        _Word = new UInt16[1, 2];
        _Word[0, 0] = 0x0B77;
        _Word[0, 1] = 0x0B77;
      }
    }

    class DTS14bitSyncWord : SyncWord
    {
      public DTS14bitSyncWord()
      {
        // DTS Sync word plus extension in 14 bit format:
        // 0x1FFF              0xE800              0x07F
        // 0001 1111 1111 1111 1110 1000 0000 0000 0000 0111 1111

        _MaxFrameSize = 8192;
        _Word = new UInt16[3, 2];
        _Word[0, 0] = 0x1FFF;
        _Word[0, 1] = 0x1FFF;
        _Word[1, 0] = 0xE800;
        _Word[1, 1] = 0xE800;
        _Word[2, 0] = 0x07F0;
        _Word[2, 1] = 0x07FF;
      }
    }

    class DTSSyncWord : SyncWord
    {
      public DTSSyncWord()
      {
        // DTS Sync word:
        // 0x7FFE8001
        // 0111 1111 1111 1110 1000 0000 0000 0001

        _MaxFrameSize = 8192;
        _Word = new UInt16[2, 2];
        _Word[0, 0] = 0x7FFE;
        _Word[0, 1] = 0x7FFE;
        _Word[1, 0] = 0x8001;
        _Word[1, 1] = 0x8001;
      }
    }
  }
}
