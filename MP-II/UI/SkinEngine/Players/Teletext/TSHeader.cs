using System;
using System.Collections.Generic;
using System.Text;

namespace MediaPortal.SkinEngine.Players.Teletext
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
