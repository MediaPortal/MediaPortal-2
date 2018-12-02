#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Runtime.InteropServices;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UI.Players.Video.Teletext
{
  internal class TeletextReceiver
  {
    private readonly TeletextEventCallback _eventCallback;
    private readonly TeletextPacketCallback _packetCallback;
    private readonly PESDecoder _pesDecoder;
    private readonly TeletextServiceInfoCallback _serviceInfoCallback;
    private readonly IDVBTeletextDecoder _ttxtDecoder;
    private bool _discardPackets;

    private UInt64 _lastStreamPcr;

    public TeletextReceiver(ITeletextSource source, IDVBTeletextDecoder ttxtDecoder)
    {
      assert(source != null, "Source is null");
      assert(ttxtDecoder != null, "Decoder is null");
      ServiceRegistration.Get<ILogger>().Debug("Setting up teletext receiver ... ");
      _eventCallback = new TeletextEventCallback(OnEvent);
      _packetCallback = new TeletextPacketCallback(OnTSPacket);
      _serviceInfoCallback = new TeletextServiceInfoCallback(OnServiceInfo);

      // tell the tsreader's teletext source interface to deliver ts packets to us
      // and to inform us on resets
      ServiceRegistration.Get<ILogger>().Debug("Setting up callbacks with ITeletextSource");
      source.SetTeletextTSPacketCallback(Marshal.GetFunctionPointerForDelegate(_packetCallback));
      source.SetTeletextEventCallback(Marshal.GetFunctionPointerForDelegate(_eventCallback));
      source.SetTeletextServiceInfoCallback(Marshal.GetFunctionPointerForDelegate(_serviceInfoCallback));

      //tsPackets = new Queue<Packet>();

      ServiceRegistration.Get<ILogger>().Debug("Setting up ttxtdecoder and pes decoder");
      this._ttxtDecoder = ttxtDecoder;
      _pesDecoder = new PESDecoder(OnPesPacket);
      ServiceRegistration.Get<ILogger>().Debug("Done setting up teletext receiver ... ");
    }

    public IDVBTeletextDecoder TtxtDecoder
    {
      get { return _ttxtDecoder; }
    }

    private void assert(bool ok, string msg)
    {
      if (!ok)
      {
        //throw new Exception("Assertion failed! " + msg);
        ServiceRegistration.Get<ILogger>().Error("Assertion failed! " + msg);
      }
    }

    /// <summary>
    /// Called from TsReader when a Ts packet containing teletext data 
    /// is received
    /// </summary>
    /// <param name="pbuf">Pointer to a byte buffer of length len</param>
    /// <param name="len">Length of buffer pointed to by buf</param>
    public void OnTSPacket(IntPtr pbuf, int len)
    {
      lock (this)
      {
        if (_discardPackets) return;
        //ServiceRegistration.Get<ILogger>().Debug("OnTSPacket");
        assert(len == 188, "TS packet length is not 188");
        byte[] buffer = new byte[len];
        Marshal.Copy(pbuf, buffer, 0, len); // copy buffer

        _pesDecoder.OnTsPacket(buffer, _lastStreamPcr);
      }
    }

    private bool IntToBool(int i)
    {
      if (i != 0) return true;
      else return false;
    }

    public void OnEvent(int eventCode, UInt64 eventValue)
    {
      TeletextEvent e = (TeletextEvent) eventCode;

      lock (this)
      {
        switch (e)
        {
          case TeletextEvent.RESET:
            ServiceRegistration.Get<ILogger>().Debug("Teletext: RESET");
            _pesDecoder.Reset();
            _ttxtDecoder.Reset();
            // tsPackets.Clear();
            break;
          case TeletextEvent.SEEK_START:
            ServiceRegistration.Get<ILogger>().Debug("Teletext: SEEK_START");
            _discardPackets = true;
            break;
          case TeletextEvent.SEEK_END:
            ServiceRegistration.Get<ILogger>().Debug("Teletext: SEEK_END");

            _pesDecoder.Reset();
            _ttxtDecoder.Reset();
            // tsPackets.Clear();
            _discardPackets = false;
            break;
          case TeletextEvent.BUFFER_IN_UPDATE:
            ServiceRegistration.Get<ILogger>().Error(
              "TeletextReceiver: Call to OnEvent with obsolete event value (BUFFER_IN_UPDATE)");
            break;
          case TeletextEvent.BUFFER_OUT_UPDATE:
            ServiceRegistration.Get<ILogger>().Error(
              "TeletextReceiver: Call to OnEvent with obsolete event value (BUFFER_OUT_UPDATE)");
            break;
          case TeletextEvent.PACKET_PCR_UPDATE:
            //if(lastStreamPCR != eventValue) ServiceRegistration.Get<ILogger>().Debug("Teletext: Packet PCR : {0}", eventValue);
            _lastStreamPcr = eventValue;
            break;
          case TeletextEvent.COMPENSATION_UPDATE:
            ServiceRegistration.Get<ILogger>().Error(
              "TeletextReceiver: Call to OnEvent with obsolete event value (COMPENSATION_UPDATE)");
            //if (eventValue != lastCompensation)
            //{
            //    lastCompensation = eventValue;
            //    ServiceRegistration.Get<ILogger>().Debug("Teletext: Compensation Update : {0}", eventValue);
            //}
            break;
          default:
            throw new Exception("Unknown event type!");
        }
      }
    }

    public void OnServiceInfo(int page, byte type, byte langb1, byte langb2, byte langb3)
    {
      lock (this)
      {
        ServiceRegistration.Get<ILogger>().Debug("Page {0} is of type {1} and in lang {2}{3}{4}", page, type,
                                                 (char) langb1, (char) langb2, (char) langb3);
        StringBuilder sbuf = new StringBuilder();
        sbuf.Append((char) langb1);
        sbuf.Append((char) langb2);
        sbuf.Append((char) langb3);
        _ttxtDecoder.OnServiceInfo(page, type, sbuf.ToString());
      }
    }


    public UInt64 DecodePTS(byte[] header_PTS, byte PTS_DTS_flag)
    {
      assert(header_PTS.Length == 5, "Input to DecodePTS is of incorrect length!");
      byte mark = (byte) (header_PTS[0] & 0xF0);
      assert(mark == PTS_DTS_flag, "Header extension starts incorrectly! " + mark + " vs. " + PTS_DTS_flag);


      UInt64 PTS = 0;
      ServiceRegistration.Get<ILogger>().Debug("TEST: " + (1 << 60));
      PTS &= ((UInt64) (header_PTS[0] & 0x0E)) << 32;
      return PTS;
    }

    /// <summary>
    /// Decodes a PES packet containing a teletext packet
    /// 
    /// </summary>
    /// <param name="streamid"></param>
    /// <param name="header"></param>
    /// <param name="headerlen"></param>
    /// <param name="data"></param>
    /// <param name="datalen"></param>
    /// <param name="isStart"></param>
    public void OnPesPacket(int streamid, byte[] header, int headerlen, byte[] data, int datalen, bool isStart,
                            UInt64 presentTime)
    {
      // header must start with 0x00 0x00 0x01
      assert(header[0] == 0x00 && header[1] == 0x00 && header[2] == 0x01, "Header start bytes incorrect");
      assert(headerlen == 45, "Header length incorrect"); // header must be 45 bytes

      byte stream_id = header[3];
      assert(stream_id == 0xBD, "Stream id is not 0xBD"); // must be private stream 1

      int PES_PACKET_LEN = (header[4] << 8 | header[5]);
      //LogDebug("PES_PACKET_LEN %i", PES_PACKET_LEN);

      bool data_alignment_indicator = IntToBool((header[6] & 0x04) >> 2);
      // alignment indicator must be set for teletext
      assert(data_alignment_indicator, "Data alignment bit not set");

      assert((header[6] & 0xC0) == 0x80, "First two bits of 6th byte wrong");
        // the first two bits of the 6th header byte MUST be 10

      byte PTS_DTS_flag = (byte) ((header[7] & 0xC0) >> 6);
      assert(PTS_DTS_flag != 0x01, "PTS_DTS_flag != 0x01");

      //ServiceRegistration.Get<ILogger>().Debug("Header len:" + headerlen);
      if (PTS_DTS_flag == 0x02 || PTS_DTS_flag == 0x03)
      {
        /*byte[] header_PTS = new byte[5];
        Array.Copy(header,header_PTS,5);
        UInt64 PTS = DecodePTS(header_PTS, PTS_DTS_flag);
        ServiceRegistration.Get<ILogger>().Debug("PES packet contains PTS = " + PTS);*/
      }
      else
      {
        assert(PTS_DTS_flag == 0x00, "PTS_DTS_flag != 0x00 " + PTS_DTS_flag);
        //ServiceRegistration.Get<ILogger>().Debug("PES PACKET DOES NOT CONTAIN PTS");
      }

      byte PES_HEADER_DATA_LENGTH = header[8];
      assert(PES_HEADER_DATA_LENGTH == 0x24, "PES header length incorrect");

      assert((PES_PACKET_LEN + 6)%184 == 0, "PES PACKET LEN invalid");

      int dataBlockLen = PES_PACKET_LEN + 6 - headerlen;
      assert(dataBlockLen == datalen, "Datalen and datablock len mismatch");

      // PES_PACKET_LEN is number of bytes AFTER PES_PACKET_LEN field.
      // header length is the total number of bytes in the header
      // so the data block at the end must be the PES_PACKET_LEN plus
      // the bytes up to PES_PACKET_LEN minus the header bytes

      //LogDebug("Data block length seems to be : %i", dataBlockLen);
      //return 0;
      // see ETSI EN 300 472
      byte data_identifier = data[0];
      if (!(data_identifier >= 0x10 && data_identifier <= 0x1F))
      {
        ServiceRegistration.Get<ILogger>().Debug("Data identifier not as expected {0}", data_identifier);
      }
      // assert(data_identifier >= 0x10 && data_identifier <= 0x1F);

      // see Table 1 in section 4.3
      int size = 46; // data_unit_id + data_unit_length + data_field()
      int dataLeft = dataBlockLen - 1; // subtract 1 for data_identifier

      int initialDataLeft = dataLeft;

      int offset = -1;


      for (int i = 0; dataLeft >= size; i++)
      {
        //offset = 1 + i * size; 
        offset = dataBlockLen - dataLeft;

        byte data_unit_id = data[offset];

        //ServiceRegistration.Get<ILogger>().Debug("Data unit id " + data_unit_id);

        if (!(data_unit_id == 0xFF || data_unit_id == 0x02 || data_unit_id == 0x03))
        {
          if (data_unit_id >= 0x80 && data_unit_id <= 0xFE)
          {
            // custom data. Can have other data field length, so skip it.
            byte data_unit_length = data[offset + 1];
            dataLeft -= data_unit_length + 2; // +2 for id and length
            continue;
          }
          ServiceRegistration.Get<ILogger>().Debug("Data unit id incorrect: " + data_unit_id);
          if (data_unit_id == 0x2C && data[offset + 2] == 0xE4 && data_identifier == 0x02)
          {
            ServiceRegistration.Get<ILogger>().Debug(
              "Data starts without data_identifier! data_identifier has value of data_unit_id, data_unit_id of data_unit_length etc..!!!");
          }

          assert(data_unit_id == 0xFF || data_unit_id == 0x02 || data_unit_id == 0x03, "Data unit id invalid value");
          return;
        }

        // does the decoder wants this type of teletext data?
        if (_ttxtDecoder == null)
        {
          //ServiceRegistration.Get<ILogger>().Debug("Ignoring PES packet (decoder == null)");
        }
        else if (!_ttxtDecoder.AcceptsDataUnitID(data_unit_id))
        {
          //ServiceRegistration.Get<ILogger>().Debug("Ignoring PES packet (unit id " + data_unit_id + " not accepted)");
        }
        else if (data_unit_id == 0x03)
        {
          byte data_unit_length = data[offset + 1];

          // always the same length for teletext data (see section 4.4)
          if (data_unit_length != 0x2C)
          {
            ServiceRegistration.Get<ILogger>().Debug("EBU teletext sub has wrong length field! " + data_unit_length,
                                                     "Wrong length field");
          }

          //WAS: byte* teletextPacketData = &data[offset + 2]; // skip past data_unit_id and data_unit_length
          byte[] teletextPacketData = new byte[size - 2];
          Array.Copy(data, offset + 2, teletextPacketData, 0, size - 2); // pass data_field to decoder

          _ttxtDecoder.OnTeletextPacket(teletextPacketData, presentTime);
        }
        else if (data_unit_id == 0x02)
        {
          //EBU teletext non-subtitle data
          //ServiceRegistration.Get<ILogger>().Debug("EBU Teletext non-subtitle data");
          byte data_unit_length = data[offset + 1];

          // always the same length for teletext data (see section 4.4)
          if (data_unit_length != 0x2C)
          {
            ServiceRegistration.Get<ILogger>().Debug("EBU teletext sub has wrong length field! %X", data_unit_length,
                                                     "Wrong length field (non sub");
          }

          //WAS: byte* teletextPacketData = &data[offset + 2]; // skip past data_unit_id and data_unit_length
          byte[] teletextPacketData = new byte[size - 2];
          Array.Copy(data, offset + 2, teletextPacketData, 0, size - 2); // pass data_field to decoder

          _ttxtDecoder.OnTeletextPacket(teletextPacketData, presentTime);
        }
        dataLeft -= size;
      }

      assert(dataLeft == 0, "Data left is not 0!");
    }

    #region Nested type: Packet

    private struct Packet
    {
      public byte[] buffer;
      public UInt64 releaseTime;

      public Packet(byte[] buf, UInt64 release)
      {
        buffer = buf;
        releaseTime = release;
      }
    }

    #endregion

    #region Nested type: TeletextEventCallback

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void TeletextEventCallback(int eventCode, UInt64 eventValue);

    #endregion

    #region Nested type: TeletextPacketCallback

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void TeletextPacketCallback(IntPtr pbuf, int len);

    #endregion

    #region Nested type: TeletextServiceInfoCallback

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate void TeletextServiceInfoCallback(int page, byte type, byte langb1, byte langb2, byte langb3);

    #endregion
  }
}