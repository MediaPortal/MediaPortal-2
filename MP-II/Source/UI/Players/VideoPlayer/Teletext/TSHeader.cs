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

namespace Ui.Players.Video.Teletext
{
  class TSHeader
  {
    public byte SyncByte;
    public bool TransportError;
    public bool PayloadUnitStart;
    public bool TransportPriority;
    public int Pid;
    public byte TScrambling;
    public byte AdaptionControl;
    public byte ContinuityCounter;
    public byte AdaptionFieldLength;
    public byte PayLoadStart;
    private byte[] m_packet;

    public TSHeader(byte[] tsPacket)
    {
      m_packet = tsPacket;
      Decode(tsPacket);
    }


    public bool AdaptionFieldOnly()
    {
      return (AdaptionControl == 2);
    }

    private void Decode(byte[] data)
    {
      //47 40 d2 10
      //															bits  byteNo		mask
      //SyncByte											:	8			0				
      //TransportError								:	1			1				0x80  10000000
      //PayloadUnitStart							: 1			1				0x40  01000000
      //TransportPriority							: 1			1				0x20  00100000
      //Pid														: 13	 1&2	          00011111 11111111
      //Transport Scrambling Control	: 2			3				0xc0  11000000
      //Adaption Field Control				: 2			3				0x30	00110000
      //ContinuityCounter							: 4			3				0xf   00001111

      //Two adaption field control bits which may take four values:
      // 1. 01 – no adaptation field, payload only				0x10		1
      // 2. 10 – adaptation field only, no payload				0x20		2
      // 3. 11 – adaptation field followed by payload			0x30		3
      // 4. 00 - RESERVED for future use 									0x00

      SyncByte = data[0];
      TransportError = (data[1] & 0x80) > 0 ? true : false;
      PayloadUnitStart = (data[1] & 0x40) > 0 ? true : false;
      TransportPriority = (data[1] & 0x20) > 0 ? true : false;
      Pid = ((data[1] & 0x1F) << 8) + data[2];
      TScrambling = (byte)(data[3] & 0xC0);
      AdaptionControl = (byte)((data[3] >> 4) & 0x3);
      ContinuityCounter = (byte)(data[3] & 0x0F);
      AdaptionFieldLength = 0;
      PayLoadStart = 4;
      if (AdaptionControl >= 2)
      {
        AdaptionFieldLength = data[4];
        PayLoadStart = (byte)(5 + AdaptionFieldLength);
      }
      if (AdaptionControl == 1)
      {
        if (PayloadUnitStart)
        {
          if (data[4] == 0 && data[5] == 0 && data[6] == 1) PayLoadStart = 4;
          else PayLoadStart = (byte)(data[4] + 5);
        }
      }
    }
  }
}
