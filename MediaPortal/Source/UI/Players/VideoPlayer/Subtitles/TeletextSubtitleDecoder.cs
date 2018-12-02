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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.UI.Players.Video.Teletext;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  internal class TeletextSubtitleDecoder : IDVBTeletextDecoder
  {
    #region Delegates

    public delegate void PageInfoCallback(TeletextPageEntry entry);

    #endregion

    private const int DATA_FIELD_SIZE = 44;
    private readonly SubtitleRenderer _subRender;

    private readonly byte[] _invtab =
      {
        0x00, 0x80, 0x40, 0xc0, 0x20, 0xa0, 0x60, 0xe0,
        0x10, 0x90, 0x50, 0xd0, 0x30, 0xb0, 0x70, 0xf0,
        0x08, 0x88, 0x48, 0xc8, 0x28, 0xa8, 0x68, 0xe8,
        0x18, 0x98, 0x58, 0xd8, 0x38, 0xb8, 0x78, 0xf8,
        0x04, 0x84, 0x44, 0xc4, 0x24, 0xa4, 0x64, 0xe4,
        0x14, 0x94, 0x54, 0xd4, 0x34, 0xb4, 0x74, 0xf4,
        0x0c, 0x8c, 0x4c, 0xcc, 0x2c, 0xac, 0x6c, 0xec,
        0x1c, 0x9c, 0x5c, 0xdc, 0x3c, 0xbc, 0x7c, 0xfc,
        0x02, 0x82, 0x42, 0xc2, 0x22, 0xa2, 0x62, 0xe2,
        0x12, 0x92, 0x52, 0xd2, 0x32, 0xb2, 0x72, 0xf2,
        0x0a, 0x8a, 0x4a, 0xca, 0x2a, 0xaa, 0x6a, 0xea,
        0x1a, 0x9a, 0x5a, 0xda, 0x3a, 0xba, 0x7a, 0xfa,
        0x06, 0x86, 0x46, 0xc6, 0x26, 0xa6, 0x66, 0xe6,
        0x16, 0x96, 0x56, 0xd6, 0x36, 0xb6, 0x76, 0xf6,
        0x0e, 0x8e, 0x4e, 0xce, 0x2e, 0xae, 0x6e, 0xee,
        0x1e, 0x9e, 0x5e, 0xde, 0x3e, 0xbe, 0x7e, 0xfe,
        0x01, 0x81, 0x41, 0xc1, 0x21, 0xa1, 0x61, 0xe1,
        0x11, 0x91, 0x51, 0xd1, 0x31, 0xb1, 0x71, 0xf1,
        0x09, 0x89, 0x49, 0xc9, 0x29, 0xa9, 0x69, 0xe9,
        0x19, 0x99, 0x59, 0xd9, 0x39, 0xb9, 0x79, 0xf9,
        0x05, 0x85, 0x45, 0xc5, 0x25, 0xa5, 0x65, 0xe5,
        0x15, 0x95, 0x55, 0xd5, 0x35, 0xb5, 0x75, 0xf5,
        0x0d, 0x8d, 0x4d, 0xcd, 0x2d, 0xad, 0x6d, 0xed,
        0x1d, 0x9d, 0x5d, 0xdd, 0x3d, 0xbd, 0x7d, 0xfd,
        0x03, 0x83, 0x43, 0xc3, 0x23, 0xa3, 0x63, 0xe3,
        0x13, 0x93, 0x53, 0xd3, 0x33, 0xb3, 0x73, 0xf3,
        0x0b, 0x8b, 0x4b, 0xcb, 0x2b, 0xab, 0x6b, 0xeb,
        0x1b, 0x9b, 0x5b, 0xdb, 0x3b, 0xbb, 0x7b, 0xfb,
        0x07, 0x87, 0x47, 0xc7, 0x27, 0xa7, 0x67, 0xe7,
        0x17, 0x97, 0x57, 0xd7, 0x37, 0xb7, 0x77, 0xf7,
        0x0f, 0x8f, 0x4f, 0xcf, 0x2f, 0xaf, 0x6f, 0xef,
        0x1f, 0x9f, 0x5f, 0xdf, 0x3f, 0xbf, 0x7f, 0xff,
      };

    private readonly TeletextMagazine[] _magazines;
    private PageInfoCallback _pageInfoCallback;

    public TeletextSubtitleDecoder(SubtitleRenderer subRender)
    {
      Assert(subRender != null, "SubtitleRender is null!");
      _subRender = subRender;
      _magazines = new TeletextMagazine[8];
      for (int i = 0; i < 8; i++)
      {
        _magazines[i] = new TeletextMagazine();
        _magazines[i].SetMag(i + 1);
        _magazines[i].Clear();
        _magazines[i].SetOwner(this);
      }
    }

    public SubtitleRenderer SubtitleRender
    {
      get { return _subRender; }
    }

    public PageInfoCallback SubPageInfoCallback
    {
      get { return _pageInfoCallback; }
    }

    #region IDVBTeletextDecoder Members

    public void OnServiceInfo(int page, byte type, string isoLang)
    {
      TeletextMagazine.OnServiceInfo(page, type, isoLang);
    }

    public void OnTeletextPacket(byte[] data, UInt64 presentTime)
    {
      Assert(data.Length == DATA_FIELD_SIZE, "Data length not as expected! " + data.Length);
      // data_field
      byte reservedParityOffset = data[0]; // parity/offset etc

      //LogDebug("first data_field byte: %s", ToBinary(reserved_parity_offset).c_str()); 
      byte reservedFutureUse = (byte) (data[0] & 0xC0); // first two bits
      Assert(reservedFutureUse == 0xC0, "Reserved future use unexpected value");

      byte fieldParity = (byte) ((data[0] & 0x20) >> 5); // 3rd bit
      //LogDebug("field parity %i", field_parity);

      byte lineOffset = (byte) (data[0] & 0x1F); // last 5 bits
      Assert(lineOffset == 0x00 || (lineOffset >= 0x07 && lineOffset <= 0x16), "Line offset wrong!");

      byte framingCode = data[1];
      Assert(framingCode == 0xE4, "Framing code wrong " + framingCode);

      // what is this for? (A: reverse bit ordering)
      for (int j = 2; j < DATA_FIELD_SIZE; j++)
      {
        data[j] = _invtab[data[j]];
      }

      byte magazineAndPacketAddress1 = data[2];
      byte magazineAndPacketAddress2 = data[3];
      byte magazineAndPacketAddress = Hamming.Unham(magazineAndPacketAddress1, magazineAndPacketAddress2);

      byte mag = (byte) (magazineAndPacketAddress & 7);

      // mag == 0 means page is 8nn
      if (mag == 0) mag = 8;

      int magIndex = mag - 1;

      Assert(magIndex >= 0 && magIndex <= 7, "Magindex out of range " + magIndex);

      byte y = (byte) ((magazineAndPacketAddress >> 3) & 0x1f); // Y is the packet number

      int offset = 4; // start of data differs between packet types

      if (y == 0)
      {
        // teletext packet header
        //ServiceRegistration.Get<ILogger>().Debug("Header package : Data length is {0} , offset is {1}", data.Length, offset);
        byte[] offsetdata = new byte[data.Length - offset];
        Array.Copy(data, offset, offsetdata, 0, offsetdata.Length);

        TeletextPageHeader header = new TeletextPageHeader(mag, offsetdata);

        if (header.IsSerial())
        {
          // to support serial mode, just end all pages in progress (there should be only one)
          int inProgress = 0;
          for (int i = 0; i < 8; i++)
          {
            if (_magazines[i].PageInProgress())
            {
              inProgress++;
            }
            _magazines[i].EndPage();
          }
          Assert(inProgress <= 1, "Serial mode: too many pages in progress : " + inProgress);
            // at most one page should be in progress
          if (inProgress > 1)
            ServiceRegistration.Get<ILogger>().Debug("Pages in progress at same time exceeds one ! (%i)", inProgress);
        }

        /*if (header.isSubtitle())
        {*/
        _magazines[magIndex].StartPage(header, presentTime);
        //}
      }
      else if (y >= 1 && y <= 25)
      {
        // display content
        //ServiceRegistration.Get<ILogger>().Debug("Content package : Data length is {0} , offset is {1}", data.Length, offset);
        byte[] offsetdata = new byte[data.Length - offset];
        Array.Copy(data, offset, offsetdata, 0, offsetdata.Length);
        _magazines[magIndex].SetLine(y, offsetdata);
        //WAS: magazines[magIndex].SetLine(Y,&data[offset]);
      }
      else
      {
        //LogDebug("Packet %i for magazine %i (discarded)", Y, mag);
      }
    }

    public bool AcceptsDataUnitID(byte id)
    {
      //ServiceRegistration.Get<ILogger>().Debug("Decoder: Asked about accepting unit id {0}", id);
      // We need to accept both EMU Teletext subs and non-subs
      // because some providers transmit the subs in non-sub PES packets :)
      return id == 0x03 || id == 0x02;
    }

    public void Reset()
    {
      foreach (TeletextMagazine magazine in _magazines)
      {
        magazine.Clear();
      }
    }

    #endregion

    private static void Assert(bool ok, string msg)
    {
      if (!ok) ServiceRegistration.Get<ILogger>().Error("Assertion failed in TeletextSubtitleDecoder : " + msg);
    }

    public void SetPageInfoCallback(PageInfoCallback cb)
    {
      _pageInfoCallback = cb;
    }

    public void OnSubPageInfo()
    {
    }

    public void OnInitialPageInfo()
    {
    }
  }
}
