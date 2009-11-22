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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core;
using MediaPortal.Core.Logging;

namespace Ui.Players.Video.Subtitles
{
  public struct LineContent
  {
    public bool doubleHeight;
    public string line;
  }

  class TeletextMagazine
  {

    private byte MSB3_NP(byte x) { return (byte)(x & 0x70); } // 3 most significant bits, removing parity bit
    private byte LSB4(byte x) { return (byte)(x & 0x0F); } // 4 less significant bits (no parity)

    public const int TELETEXT_LINES = 25;
    public const int TELETEXT_WIDTH = 40;
    public const int DATA_FIELD_SIZE = 44;
    public const byte TELETEXT_BLANK = 0x20;
    public const byte SPACE_ATTRIB_BOX_START = 0x0B;
    public const byte SPACE_ATTRIB_BOX_END = 0x0A;

    public void assert(bool ok, string msg)
    {
      if (!ok) throw new Exception("Assertion failed in TeletextMagazine! : " + msg);
    }
    public TeletextMagazine()
    {
      ServiceScope.Get<ILogger>().Debug("Magazine ctor");
      pageContent = new byte[TELETEXT_LINES * TELETEXT_WIDTH];
      pageNumInProgress = -1;
      language = -1;
      magID = -1;
    }

    public static void OnServiceInfo(int page, byte type, string iso_lang)
    {
      if (type != 0x02 && type != 0x03) return; // we only want subtitle language info
      else
      {
        lock (langInfo)
        {
          if (!langInfo.ContainsKey(page))
          {
            langInfo.Add(page, iso_lang);
          }
        }
      }
    }

    public void StartPage(TeletextPageHeader header, UInt64 presentTime)
    {
      this.presentTime = presentTime;
      int mag = header.Magazine();
      if (mag != magID)
      {
        ServiceScope.Get<ILogger>().Debug("Magazine magid mag: {0}, {1}", magID, mag);
      }
      assert(mag == magID, "Inconsistent magazine id");
      assert(pageNumInProgress == -1 || (pageNumInProgress >= 100 && pageNumInProgress <= 966), "PageNumInProgress out of range");

      if (header.isTimeFiller() || !header.isSubtitle())
      { // time filling header to indicate end of page
        if (pageNumInProgress != -1)
        { // if we were working on a previous page its finished now
          EndPage();
        }
        //LogDebug("Mag %i FILLER ends page %i", magID, pageNumInProgress);
        Clear();
        pageNumInProgress = -1;
        return;
      }

      if (header.isSerial() && !this.isSerial)
      {
        ServiceScope.Get<ILogger>().Debug("MagID {0} is in serial mode", magID);
        this.isSerial = true;
      }
      int new_page_num = header.PageNumber();
      language = header.Language();

      if (pageNumInProgress != new_page_num)
      {
        //LogDebug("Mag %i, Page %i finished by new page %i", magID, pageNumInProgress, new_page_num);
        if (pageNumInProgress != -1)
        { // if we were working on a previous page its finished now
          EndPage();
        }
        Clear();
        pageNumInProgress = new_page_num;
      }

      if (header.eraseBit())
      {
        Clear();
      }
      assert(pageNumInProgress >= 100 && pageNumInProgress <= 966, "StartPage: pageNumInProgress out of range");
    }

    public void SetLine(int l, byte[] line_data)
    {
      assert(line_data.Length == TELETEXT_WIDTH, "Line data length not equal to TELETEXT_WIDTH : " + line_data.Length);
      Array.Copy(line_data, 0, pageContent, Math.Max(l - 1, 0) * TELETEXT_WIDTH, TELETEXT_WIDTH);
    }

    public void EndPage()
    {
      if (pageNumInProgress == -1) return; // no page in progress
      else if ((pageNumInProgress < 0 || pageNumInProgress >= 966))
      {
        ServiceScope.Get<ILogger>().Debug("DANGER DANGER!, endpage with pageNumInProgress = %i", pageNumInProgress);
        return;
      }

      ServiceScope.Get<ILogger>().Debug("Finished Page {0}", pageNumInProgress);
      //bool hasContent = false;

      for (int i = 0; i < 25; i++)
      {
        bool boxed = false;
        byte[] lineContent = GetLine(i);

        for (int j = 0; j < 40; j++)
        {
          // Remove spacing attributes ( see 12.2 of the draft)
          // FIXME: Some subtitles will have the attributed 'double height'
          // and therefore have an empty line between subs.

          // ís this content a space attribute?
          if (MSB3_NP(lineContent[j]) == 0)
          {
            if (LSB4(lineContent[j]) == SPACE_ATTRIB_BOX_START)
            {
              //LogDebug("BS - boxed is true");
              boxed = true;
              //hasContent = true;
            }
            else if (LSB4(lineContent[j]) == SPACE_ATTRIB_BOX_END)
            {
              //LogDebug("BE - boxed is false");
              boxed = false;
            }
            // remove spacing attribute
            lineContent[j] = TELETEXT_BLANK;
          }
          else if (!boxed)
          {
            // if we are not in boxed mode,
            // we dont want to keep the content
            lineContent[j] = TELETEXT_BLANK;
            assert(!boxed, "EndPage: Boxed not set as expected");
          }
        }
        SetLine(i, lineContent);
      }


      /*if(!hasContent) {
        ServiceScope.Get<ILogger>().Debug("(BLANK PAGE)");
      }*/

      byte[] text = new byte[TELETEXT_WIDTH * TELETEXT_LINES];
      Array.Copy(pageContent, text, TELETEXT_LINES * TELETEXT_WIDTH);
      TextConversion.Convert(language, text);

      LineContent[] lc = new LineContent[TELETEXT_LINES];

      string realLang = "";

      lock (langInfo)
      {
        if (langInfo.ContainsKey(pageNumInProgress))
        {
          realLang = langInfo[pageNumInProgress];
        }
      }

      for (int line = 0; line < TELETEXT_LINES; line++)
      {
        StringBuilder lineBuilder = new StringBuilder();
        for (int c = 0; c < TELETEXT_WIDTH; c++)
        {
          lineBuilder.Append((char)text[line * TELETEXT_WIDTH + c]);
        }
        lc[line] = new LineContent();
        if (realLang != "")
        {
          lc[line].line = TextConversion.ConvertLineLangSpecific(realLang, lineBuilder.ToString());
        }
        else
        {
          lc[line].line = lineBuilder.ToString();
        }
        lc[line].doubleHeight = true;
      }

      StringBuilder textBuilder = new StringBuilder();
      for (int i = 0; i < text.Length; i++)
      {
        //sbuf.Append((char)text[i]);
        textBuilder.Append((char)text[i]);

        //sbuf.Append("" + ((int)pageContent[i]) + " ");
        if (((i + 1) % 40) == 0)
        {
          textBuilder.Append('\n');
        }
      }

      // prepare subtitle
      TEXT_SUBTITLE sub = new TEXT_SUBTITLE();
      sub.encoding = language;
      sub.page = pageNumInProgress;

      sub.language = realLang;

      sub.text = textBuilder.ToString();
      sub.lc = lc;
      sub.timeOut = ulong.MaxValue; // never timeout (will be replaced by other page)
      sub.timeStamp = presentTime;
      assert(sub.text != null, "Sub.text == null!");

      if (owner.SubPageInfoCallback != null)
      {
        TeletextPageEntry pageEntry = new TeletextPageEntry();
        pageEntry.language = String.Copy(sub.language);
        pageEntry.encoding = (TeletextCharTable)sub.encoding;
        pageEntry.page = sub.page;

        owner.SubPageInfoCallback(pageEntry);
      }

      owner.SubtitleRender.OnTextSubtitle(ref sub);
      pageNumInProgress = -1;
    }


    /// <summary>
    /// Retrieve line l of the current page in progress
    /// </summary>
    /// <param name="l"></param>
    /// <returns>A byte array containing the data</returns>
    byte[] GetLine(int l)
    {
      SanityCheck();
      byte[] line_data = new byte[TELETEXT_WIDTH];
      Array.Copy(pageContent, Math.Max(l - 1, 0) * TELETEXT_WIDTH, line_data, 0, TELETEXT_WIDTH);
      return line_data;
    }

    public void Clear()
    {
      SanityCheck();
      for (int i = 0; i < pageContent.Length; i++)
      {
        pageContent[i] = TELETEXT_BLANK;
      }
    }

    void SanityCheck()
    {
      assert(magID == -1 || (magID <= 8 && magID >= 0), "SanityCheck: mag id out of range");
    }

    public bool PageInProgress()
    {
      //LogDebug("Mag %i in progress: %i", magID, (pageNumInProgress != -1));
      return pageNumInProgress != -1;
    }

    public void SetMag(int mag)
    {
      //assert(pageNumInProgress == -1 && language == -1);
      //assert(mag >= 1 && mag <= 8);
      magID = mag;
      SanityCheck();
    }

    public void SetOwner(TeletextSubtitleDecoder owner)
    {
      assert(owner != null, "TeletextSubtitleDecoder must not be null!");
      this.owner = owner;
    }


    private UInt64 presentTime;
    private bool isSerial = false;
    private int pageNumInProgress;
    TeletextSubtitleDecoder owner;
    private int language; // encoding language
    private byte[] pageContent; // indexed by line and character (col)
    private static Dictionary<int, string> langInfo = new Dictionary<int, string>(); // DVB SI language info for sub pages

    int magID;
  }
}
