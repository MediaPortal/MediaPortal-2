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
using MediaPortal.UI.Players.Video.Teletext;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  public class TeletextPageHeader
  {
    private readonly bool _erasePage;
    private readonly int _language;
    private readonly int _magazine;
    private readonly bool _magazineSerial;
    private readonly int _pageNum;
    private readonly bool _subtitle;
    private readonly bool _timefiller;
    private bool _inhibitDisplay;
    private bool _interruptedSequence;
    private bool _newsflash;
    private bool _supressHeader;
    private bool _updateIndicator;

    public TeletextPageHeader(short mag, byte[] data)
    {
      int offset = 0;
      _magazine = mag;
      byte pageByte = Hamming.Unham(data[offset], data[offset + 1]); // The lower two (hex) numbers of page
      _timefiller = (pageByte == 0xFF);

      if (!_timefiller)
      {
        _pageNum = (mag*100 + 10*(pageByte >> 4) + (pageByte & 0x0F));
        if (_pageNum < 100 || _pageNum > 966)
        {
          throw new Exception("PageNumber out of range " + _pageNum);
        }
      }
      else _pageNum = -1;

      //Log.Debug("TeletextPageHeader page: {0}", pageNum);
      //int subpage = ((unham(data[offset + 4], data[offset+5]) << 8) | unham(data[offset+2], data[offset+3])) & 0x3F7F;

      _language = ((Hamming.Unham(data[offset + 6], data[offset + 7]) >> 5) & 0x07);

      _erasePage = (data[offset + 3] & 0x80) == 0x80; // Byte 9,  bit 8
      _newsflash = (data[offset + 5] & 0x20) == 0x20; // Byte 11, bit 6
      _subtitle = (data[offset + 5] & 0x80) == 0x80; // Byte 11, bit 8

      _supressHeader = (data[offset + 6] & 0x02) == 0x02; // Byte 12, bit 2
      _updateIndicator = (data[offset + 6] & 0x08) == 0x08; // Byte 12, bit 4

      _interruptedSequence = (data[offset + 6] & 0x20) == 0x20; // Byte 12, bit 6
      _inhibitDisplay = (data[offset + 6] & 0x80) == 0x80; // Byte 12, bit 8
      _magazineSerial = (data[offset + 7] & 0x02) == 0x02; // Byte 13, bit 2

      if (_magazineSerial)
      {
        // Log.Debug("Magazine {0} is in serial mode", mag);
      }
    }

    public bool EraseBit()
    {
      return _erasePage;
    }

    public bool IsSubtitle()
    {
      return _subtitle;
    }

    public bool IsSerial()
    {
      return _magazineSerial;
    }

    public bool IsTimeFiller()
    {
      return _timefiller;
    }

    public int PageNumber()
    {
      if (_timefiller)
      {
        throw new Exception("PageNumber query not allowed on time filler header!");
      }
      return _pageNum;
    }

    public int Magazine()
    {
      return _magazine;
    }

    public int Language()
    {
      if (_timefiller)
      {
        throw new Exception("Language query not allowed on time filler header!");
      }
      return _language;
    }
  }
}
