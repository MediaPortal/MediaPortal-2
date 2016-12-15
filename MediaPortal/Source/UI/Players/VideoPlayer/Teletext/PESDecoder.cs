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

using System;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UI.Players.Video.Teletext
{
  internal delegate void PESCallback(int streamid, byte[] header, int headerlen,
                                     byte[] data, int datalen, bool isStart, UInt64 presentTime);

  internal class PESDecoder
  {
    public const byte TS_PACKET_SYNC = 0x47;
    public const int MAX_PES_PACKET = 65535;

    private readonly PESCallback _cb;
    private readonly byte[] _pesBuffer;
    private readonly byte[] _pesHeader = new byte[256];
    private bool _hasPayloadStart; // do we have a packet in progress
    private bool _bStart;
    private int _iPesHeaderLen;
    private int _iPesLength;
    private int _iStreamId;
    private int _iWritePos; // next free position in buffer (and therefore also length of already written data)
    private int _pid; // we dont need this i think..

    public PESDecoder(PESCallback cb)
    {
      ServiceRegistration.Get<ILogger>().Debug("PESDecoder ctor");
      _cb = cb;
      _pid = -1;
      _pesBuffer = new byte[MAX_PES_PACKET];
      _iWritePos = 0;
      _iStreamId = -1;
      _iPesHeaderLen = 0;
      _iPesLength = 0;
      _hasPayloadStart = false;
    }

    public void Reset()
    {
      ServiceRegistration.Get<ILogger>().Debug("PESDecoder.Reset");
      _iWritePos = 0;
      _iPesHeaderLen = 0;
      _hasPayloadStart = false;
    }


    public void SetPid(int pid)
    {
      _pid = pid;
    }

    public void SetStreamId(int streamId)
    {
      _iStreamId = streamId;
    }

    private bool SanityCheck(TSHeader header, byte[] tsPacket)
    {
      //LogDebug("PesDecoder::OnTsPacket %i", tsPacketCount++);
      if (tsPacket == null)
      {
        ServiceRegistration.Get<ILogger>().Debug("tsPacket null!");
        return false;
      }


      // Assume that correct pid is passed!
      /*if (header.Pid != m_pid) 
      {
          ServiceRegistration.Get<ILogger>().Debug("Header Pid is %i, expected %i", header.Pid, m_pid);
          return false;
      }*/

      if (header.SyncByte != TS_PACKET_SYNC)
      {
        ServiceRegistration.Get<ILogger>().Debug("pesdecoder pid:%x sync error", _pid);
        return false;
      }

      if (header.TransportError)
      {
        _bStart = false;
        _iWritePos = 0;
        _iPesLength = 0;
        ServiceRegistration.Get<ILogger>().Debug("pesdecoder pid:%x transport error", _pid);
        return false;
      }

      bool scrambled = (header.TScrambling != 0);
      if (scrambled)
      {
        ServiceRegistration.Get<ILogger>().Debug("pesdecoder scrambled!");
        return false;
      }
      if (header.AdaptionFieldOnly())
      {
        ServiceRegistration.Get<ILogger>().Debug("pesdecoder AdaptionFieldOnly!");
        return false;
      }
      return true;
    }

    public void Assert(bool b, string msg)
    {
      if (!b)
      {
        ServiceRegistration.Get<ILogger>().Error("Assertion failed in PESDecoder: " + msg);
      }
    }

    public void OnTsPacket(byte[] tsPacket, UInt64 presentTime)
    {
      // ServiceRegistration.Get<ILogger>().Debug("PESDECODER ONTSPACKET");
      TSHeader header = new TSHeader(tsPacket);
      if (!SanityCheck(header, tsPacket)) return;

      int pos = header.PayLoadStart; // where in the pes packet does the payload data start?

      if (header.PayloadUnitStart) // if this header starts a new PES packet
      {
        //ServiceRegistration.Get<ILogger>().Debug("PESDECODER: PayLoadUnitStart");
        _hasPayloadStart = true;
        if (tsPacket[pos + 0] == 0 && tsPacket[pos + 1] == 0 && tsPacket[pos + 2] == 1)
        {
          if (_iStreamId < 0)
          {
            //if stream id not set yet, get it from this 
            _iStreamId = tsPacket[pos + 3];
            if (_iStreamId < 0)
            {
              throw new Exception("Stream id less than zero :" + _iStreamId);
            }
          }
          else Assert(_iStreamId == tsPacket[pos + 3], "Stream id changed!"); // stream id should not change!

          if (_iWritePos != 0)
          {
            //throw new Exception("Buffer is not empty, but new packet is being received!");
            ServiceRegistration.Get<ILogger>().Warn("PESDECODER: Buffer is not empty, but new packet is being received!");
          }
          _iWritePos = 0;

          _iPesHeaderLen = tsPacket[pos + 8] + 9;

          if (_pesHeader.Length < _iPesHeaderLen)
          {
            ServiceRegistration.Get<ILogger>().Error(
              "PESDecoder: Reported header length is bigger than header buffer! : {0} vs {1}", _pesHeader.Length,
              _iPesHeaderLen);
          }
          Array.Copy(tsPacket, pos, _pesHeader, 0, _iPesHeaderLen);
          //above replaces -> memcpy(m_pesHeader,&tsPacket[pos],m_iPesHeaderLen);

          pos += (_iPesHeaderLen);
          _bStart = true;

          int a = _pesHeader[4];
          int b = _pesHeader[5];

          _iPesLength = (a << 8) + b - (_iPesHeaderLen - 6); // calculate expected actual payload length
        }
      }
      else if (!_hasPayloadStart)
      {
        //ServiceRegistration.Get<ILogger>().Debug("PACKET DISCARDED: END OF PACKET FOR WHICH WE DONT HAVE START");
        return;
      }

      if (_iWritePos < 0)
      {
        ServiceRegistration.Get<ILogger>().Debug("m_iWritePos < 0");
        return;
      }
      if (_iStreamId <= 0)
      {
        ServiceRegistration.Get<ILogger>().Debug("m_iStreamId <= 0");
        return;
      }

      Assert(pos > 0 && pos < 188, "Pos error : " + pos);
      Assert(_iWritePos + 188 - pos <= MAX_PES_PACKET, "About to exceed buffer size!");
        // check that the buffer is not overrunning

      int bytesToWrite = 188 - pos;
      Assert(bytesToWrite < 188, "Bytes to write too big : " + bytesToWrite);
      Array.Copy(tsPacket, pos, _pesBuffer, _iWritePos, bytesToWrite);
      _iWritePos += bytesToWrite;

      if (_iPesLength == _iWritePos) // we have the expected data
      {
        // ServiceRegistration.Get<ILogger>().Debug("PESDECODER: GOT COMPLETE PACKET");

        // assert(cb != null, "cb is null!");
        if (_iWritePos > 0 && _cb != null)
        {
          //ServiceRegistration.Get<ILogger>().Debug("PESDECODER: CALLING CALLBACK");
          _cb(_iStreamId, _pesHeader, _iPesHeaderLen, _pesBuffer, _iWritePos, _bStart, presentTime);

          _bStart = false;
          _iWritePos = 0;
          _hasPayloadStart = false;
        }
      }
    }
  }
}
