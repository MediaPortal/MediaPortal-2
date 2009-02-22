#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using Ui.Players.VideoPlayer.Teletext;

namespace Ui.Players.VideoPlayer.Subtitles
{
  public class TeletextPageHeader
  {
    public TeletextPageHeader(short mag, byte[] data)
    {
      int offset = 0;
      magazine = mag;
      byte pageByte = Hamming.unham(data[offset], data[offset + 1]); // The lower two (hex) numbers of page
      timefiller = (pageByte == 0xFF);

      if (!timefiller)
      {
        pageNum = (mag * 100 + 10 * (pageByte >> 4) + (pageByte & 0x0F));
        if (pageNum < 100 || pageNum > 966)
        {
          throw new Exception("PageNumber out of range " + pageNum);
        }
      }
      else pageNum = -1;

      //Log.Debug("TeletextPageHeader page: {0}", pageNum);
      //int subpage = ((unham(data[offset + 4], data[offset+5]) << 8) | unham(data[offset+2], data[offset+3])) & 0x3F7F;

      language = ((Hamming.unham(data[offset + 6], data[offset + 7]) >> 5) & 0x07);

      erasePage = (data[offset + 3] & 0x80) == 0x80; // Byte 9,  bit 8
      newsflash = (data[offset + 5] & 0x20) == 0x20; // Byte 11, bit 6
      subtitle = (data[offset + 5] & 0x80) == 0x80; // Byte 11, bit 8

      supressHeader = (data[offset + 6] & 0x02) == 0x02; // Byte 12, bit 2
      updateIndicator = (data[offset + 6] & 0x08) == 0x08; // Byte 12, bit 4

      interruptedSequence = (data[offset + 6] & 0x20) == 0x20; // Byte 12, bit 6
      inhibitDisplay = (data[offset + 6] & 0x80) == 0x80; // Byte 12, bit 8
      magazineSerial = (data[offset + 7] & 0x02) == 0x02; // Byte 13, bit 2

      if (magazineSerial)
      {
        // Log.Debug("Magazine {0} is in serial mode", mag);
      }
    }

    public bool eraseBit()
    {
      return erasePage;
    }

    public bool isSubtitle()
    {
      return this.subtitle;
    }

    public bool isSerial()
    {
      return this.magazineSerial;
    }

    public bool isTimeFiller()
    {
      return this.timefiller;
    }

    public int PageNumber()
    {
      if (timefiller)
      {
        throw new Exception("PageNumber query not allowed on time filler header!");
      }
      return this.pageNum;
    }

    public int Magazine()
    {
      return this.magazine;
    }

    public int Language()
    {
      if (timefiller)
      {
        throw new Exception("Language query not allowed on time filler header!");
      }
      return this.language;
    }

    private int pageNum;
    private int language;
    private int magazine;
    private bool timefiller;
    private bool erasePage;
    private bool newsflash;
    private bool subtitle;
    private bool supressHeader;
    private bool updateIndicator;
    private bool interruptedSequence;
    private bool inhibitDisplay;
    private bool magazineSerial;

  }
}
