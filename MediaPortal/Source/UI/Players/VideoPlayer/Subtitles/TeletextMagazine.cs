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
using System.Collections.Generic;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UI.Players.Video.Subtitles
{
  public struct LineContent
  {
    public bool doubleHeight;
    public string line;
  }

  internal class TeletextMagazine
  {
    // 4 less significant bits (no parity)

    public const int TELETEXT_LINES = 25;
    public const int TELETEXT_WIDTH = 40;
    public const int DATA_FIELD_SIZE = 44;
    public const byte TELETEXT_BLANK = 0x20;
    public const byte SPACE_ATTRIB_BOX_START = 0x0B;
    public const byte SPACE_ATTRIB_BOX_END = 0x0A;

    private static readonly Dictionary<int, string> LangInfo = new Dictionary<int, string>();
                                                    // DVB SI language info for sub pages

    private readonly byte[] _pageContent; // indexed by line and character (col)
    private bool _isSerial;
    private int _language; // encoding language
    private int _magId;
    private TeletextSubtitleDecoder _owner;
    private int _pageNumInProgress;
    private UInt64 _presentTime;

    public TeletextMagazine()
    {
      ServiceRegistration.Get<ILogger>().Debug("Magazine ctor");
      _pageContent = new byte[TELETEXT_LINES*TELETEXT_WIDTH];
      _pageNumInProgress = -1;
      _language = -1;
      _magId = -1;
    }

    private static byte MSB3_NP(byte x)
    {
      return (byte) (x & 0x70);
    }

    // 3 most significant bits, removing parity bit
    private static byte LSB4(byte x)
    {
      return (byte) (x & 0x0F);
    }

    public void Assert(bool ok, string msg)
    {
      if (!ok) throw new Exception("Assertion failed in TeletextMagazine! : " + msg);
    }

    public static void OnServiceInfo(int page, byte type, string isoLang)
    {
      if (type != 0x02 && type != 0x03) return; // we only want subtitle language info
      lock (LangInfo)
      {
        if (!LangInfo.ContainsKey(page))
        {
          LangInfo.Add(page, isoLang);
        }
      }
    }

    public void StartPage(TeletextPageHeader header, UInt64 presentTime)
    {
      _presentTime = presentTime;
      int mag = header.Magazine();
      if (mag != _magId)
      {
        ServiceRegistration.Get<ILogger>().Debug("Magazine magid mag: {0}, {1}", _magId, mag);
      }
      Assert(mag == _magId, "Inconsistent magazine id");
      Assert(_pageNumInProgress == -1 || (_pageNumInProgress >= 100 && _pageNumInProgress <= 966),
             "PageNumInProgress out of range");

      if (header.IsTimeFiller() || !header.IsSubtitle())
      {
        // time filling header to indicate end of page
        if (_pageNumInProgress != -1)
        {
          // if we were working on a previous page its finished now
          EndPage();
        }
        //LogDebug("Mag %i FILLER ends page %i", magID, pageNumInProgress);
        Clear();
        _pageNumInProgress = -1;
        return;
      }

      if (header.IsSerial() && !_isSerial)
      {
        ServiceRegistration.Get<ILogger>().Debug("MagID {0} is in serial mode", _magId);
        _isSerial = true;
      }
      int newPageNum = header.PageNumber();
      _language = header.Language();

      if (_pageNumInProgress != newPageNum)
      {
        //LogDebug("Mag %i, Page %i finished by new page %i", magID, pageNumInProgress, new_page_num);
        if (_pageNumInProgress != -1)
        {
          // if we were working on a previous page its finished now
          EndPage();
        }
        Clear();
        _pageNumInProgress = newPageNum;
      }

      if (header.EraseBit())
      {
        Clear();
      }
      Assert(_pageNumInProgress >= 100 && _pageNumInProgress <= 966, "StartPage: pageNumInProgress out of range");
    }

    public void SetLine(int l, byte[] lineData)
    {
      Assert(lineData.Length == TELETEXT_WIDTH, "Line data length not equal to TELETEXT_WIDTH : " + lineData.Length);
      Array.Copy(lineData, 0, _pageContent, Math.Max(l - 1, 0)*TELETEXT_WIDTH, TELETEXT_WIDTH);
    }

    public void EndPage()
    {
      if (_pageNumInProgress == -1) return; // no page in progress
      if ((_pageNumInProgress < 0 || _pageNumInProgress >= 966))
      {
        ServiceRegistration.Get<ILogger>().Debug("DANGER DANGER!, endpage with pageNumInProgress = %i",
                                                 _pageNumInProgress);
        return;
      }

      ServiceRegistration.Get<ILogger>().Debug("Finished Page {0}", _pageNumInProgress);
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

          // �s this content a space attribute?
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
            Assert(true, "EndPage: Boxed not set as expected");
          }
        }
        SetLine(i, lineContent);
      }


      /*if(!hasContent) {
        ServiceRegistration.Get<ILogger>().Debug("(BLANK PAGE)");
      }*/

      byte[] text = new byte[TELETEXT_WIDTH*TELETEXT_LINES];
      Array.Copy(_pageContent, text, TELETEXT_LINES*TELETEXT_WIDTH);
      TextConversion.Convert(_language, text);

      LineContent[] lc = new LineContent[TELETEXT_LINES];

      string realLang = "";

      lock (LangInfo)
      {
        if (LangInfo.ContainsKey(_pageNumInProgress))
        {
          realLang = LangInfo[_pageNumInProgress];
        }
      }

      for (int line = 0; line < TELETEXT_LINES; line++)
      {
        StringBuilder lineBuilder = new StringBuilder();
        for (int c = 0; c < TELETEXT_WIDTH; c++)
        {
          lineBuilder.Append((char) text[line*TELETEXT_WIDTH + c]);
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
        textBuilder.Append((char) text[i]);

        //sbuf.Append("" + ((int)pageContent[i]) + " ");
        if (((i + 1)%40) == 0)
        {
          textBuilder.Append('\n');
        }
      }

      // prepare subtitle
      TextSubtitle sub = new TextSubtitle();
      sub.Encoding = _language;
      sub.Page = _pageNumInProgress;

      sub.Language = realLang;

      sub.Text = textBuilder.ToString();
      sub.LineContents = lc;
      sub.TimeOut = ulong.MaxValue; // never timeout (will be replaced by other page)
      sub.TimeStamp = _presentTime;
      Assert(String.IsNullOrEmpty(sub.Text), "Sub.text == null!");

      if (_owner.SubPageInfoCallback != null)
      {
        TeletextPageEntry pageEntry = new TeletextPageEntry
                                        {
                                          Language = String.Copy(sub.Language),
                                          Encoding = (TeletextCharTable) sub.Encoding,
                                          Page = sub.Page
                                        };

        _owner.SubPageInfoCallback(pageEntry);
      }

      _owner.SubtitleRender.OnTextSubtitle(ref sub);
      _pageNumInProgress = -1;
    }


    /// <summary>
    /// Retrieve line l of the current page in progress
    /// </summary>
    /// <param name="l"></param>
    /// <returns>A byte array containing the data</returns>
    private byte[] GetLine(int l)
    {
      SanityCheck();
      byte[] lineData = new byte[TELETEXT_WIDTH];
      Array.Copy(_pageContent, Math.Max(l - 1, 0)*TELETEXT_WIDTH, lineData, 0, TELETEXT_WIDTH);
      return lineData;
    }

    public void Clear()
    {
      SanityCheck();
      for (int i = 0; i < _pageContent.Length; i++)
      {
        _pageContent[i] = TELETEXT_BLANK;
      }
    }

    private void SanityCheck()
    {
      Assert(_magId == -1 || (_magId <= 8 && _magId >= 0), "SanityCheck: mag id out of range");
    }

    public bool PageInProgress()
    {
      //LogDebug("Mag %i in progress: %i", magID, (pageNumInProgress != -1));
      return _pageNumInProgress != -1;
    }

    public void SetMag(int mag)
    {
      //assert(pageNumInProgress == -1 && language == -1);
      //assert(mag >= 1 && mag <= 8);
      _magId = mag;
      SanityCheck();
    }

    public void SetOwner(TeletextSubtitleDecoder owner)
    {
      Assert(owner != null, "TeletextSubtitleDecoder must not be null!");
      _owner = owner;
    }
  }
}
