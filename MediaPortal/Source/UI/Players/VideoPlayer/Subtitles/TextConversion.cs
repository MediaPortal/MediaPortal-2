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
  class TextConversion
  {
    private static Dictionary<string, Dictionary<char, char>> langSpecificMap;


    static TextConversion()
    {
      langSpecificMap = new Dictionary<string, Dictionary<char, char>>();
      langSpecificMap["dan"] = new Dictionary<char, char>();
      langSpecificMap["dan"]['\u00C4'] = '\u00C6';
      langSpecificMap["dan"]['\u00D6'] = '\u00D8';
      langSpecificMap["dan"]['\u00E4'] = '\u00E6';
      langSpecificMap["dan"]['\u00F6'] = '\u00F8';
      langSpecificMap["nor"] = new Dictionary<char, char>();
      langSpecificMap["nor"]['\u00C4'] = '\u00C6';
      langSpecificMap["nor"]['\u00D6'] = '\u00D8';
      langSpecificMap["nor"]['\u00E4'] = '\u00E6';
      langSpecificMap["nor"]['\u00F6'] = '\u00F8';
    }

    public static string ConvertLineLangSpecific(string lang, string line)
    {
      ServiceRegistration.Get<ILogger>().Debug("ConvertLineLangSpecific {0} {1}", lang, line);

      if (!langSpecificMap.ContainsKey(lang))
      {
        return line;
      }

      StringBuilder lineBuilder = new StringBuilder();
      for (int i = 0; i < line.Length; i++)
      {
        char c = line[i];
        if (langSpecificMap[lang].ContainsKey(c))
        {
          lineBuilder.Append(langSpecificMap[lang][c]);
        }
        else
        {
          lineBuilder.Append(c);
        }
      }
      return lineBuilder.ToString();
    }

    #region character and other tables for multi-language support. Referring the bits C12-C14 in the header

    private static char[,] m_charTableA = new char[,]
    {
      {'#', '\u016F'}, {'\u00A3', '$'},
      {'#', '\u00F5'}, {'\u00E9', '\u00EF'},
      {'#', '$'}, {'\u00A3', '$'},
      {'#', '$'}, {'#', '\u0149'},
      {'\u00E7', '$'}, {'#', '\u00A4'},
      {'#', '\u00CB'}, {'#', '\u00A4'},
      {'\u00A3', '\u011F'}, {'#', '\u00A4'}
    };

    private static char[] m_charTableB = new char[]
    {
      '\u010D', '@', '\u0160', '\u00E0', '\u00A7', '\u00E9', '\u0160',
      '\u0105', '\u00A1', '\u0162', '\u010C', '\u00C9', '\u0130', '\u00C9'
    };

    private static char[,] m_charTableC = new char[,]
    {
      {'\u0165', '\u017E', '\u00FD', '\u00ED', '\u0159', '\u00E9'},
      {'\u2190', '\u00BD', '\u2192', '\u2191', '#', '\u0336'},
      {'\u00C4', '\u00D6', '\u017D', '\u00DC', '\u00D5', '\u0161'},
      {'\u00EB', '\u00EA', '\u00F9', '\u00EE', '#', '\u00E8'},
      {'\u00C4', '\u00D6', '\u00DC', '^', '_', '\u00B0'},
      {'\u00B0', '\u00E7', '\u2192', '\u2191', '#', '\u00F9'},
      {'\u00E9', '\u0229', '\u017D', '\u010D', '\u016B', '\u0161'},
      {'\u01B5', '\u015A', '\u0141', '\u0107', '\u00F3', '\u0119'},
      {'\u00E1', '\u00E9', '\u00ED', '\u00F3', '\u00FA', '\u00BF'},
      {'\u00C2', '\u015E', '\u01CD', '\u00CE', '\u0131', '\u0163'},
      {'\u0106', '\u017D', '\u0110', '\u0160', '\u00EB', '\u010D'},
      {'\u00C4', '\u00D6', '\u00C5', '\u00DC', '_', '\u00E9'},
      {'\u015E', '\u00D6', '\u00C7', '\u00DC', '\u01E6', '\u0131'},
      {'\u00C6', '\u00D8', '\u00C5', '\u00DC', '_', '\u00E9'}
    };

    private static char[,] m_charTableD = new char[,]
    {
      {'\u00E1', '\u011B', '\u00FA', '\u0161'},
      {'\u00BC', '\u2016', '\u00BE', '\u00F7'},
      {'\u00E4', '\u00F6', '\u017E', '\u00FC'},
      {'\u00E2', '\u00F4', '\u00FB', '\u00E7'},
      {'\u00E4', '\u00F6', '\u00FC', '\u00DF'},
      {'\u00E0', '\u00F2', '\u00E8', '\u00EC'},
      {'\u0105', '\u0173', '\u017E', '\u012F'},
      {'\u017C', '\u015B', '\u0142', '\u017A'},
      {'\u00FC', '\u00F1', '\u00E8', '\u00E0'},
      {'\u00E2', '\u015F', '\u01CE', '\u00EE'},
      {'\u0107', '\u017E', '\u0111', '\u0161'},
      {'\u00E4', '\u00F6', '\u00E5', '\u00FC'},
      {'\u015F', '\u00F6', '\u00E7', '\u00FC'},
      {'\u00E6', '\u00F8', '\u00E5', '\u00FC'}
    };

    private static char[] m_charTableE = new char[]
    {
      '\u2190', '\u2192', '\u2191', '\u2193', 'O', 'K', '\u2190', '\u2190',
      '\u2190'
    };

    #endregion

    public static char[] Convert(int languageCode, byte[] teletext)
    {
      Assert(languageCode >= 0 && languageCode <= 7, "Convert: Lang outside range!");
      ServiceRegistration.Get<ILogger>().Debug("TextConversion.Convert: Input data length {0} teletext", teletext.Length);
      char[] text = new char[teletext.Length];

      for (int i = 0; i < teletext.Length; i++)
      {
        byte chr = teletext[i]; // input teletext character
        char chr2 = ' '; // output unicode character


        int txtLanguage;
        switch (languageCode)
        {
          case 0:
            txtLanguage = 1;
            break;
          case 1:
            txtLanguage = 4;
            break;
          case 2:
            txtLanguage = 11;
            break;
          case 3:
            txtLanguage = 5;
            break;
          case 4:
            txtLanguage = 3;
            break;
          case 5:
            txtLanguage = 8;
            break;
          case 6:
            txtLanguage = 0;
            break;
          default:
            txtLanguage = 1;
            break;
        }

        chr &= 0x7f; // strip parity bit

        switch (chr)
        {
          //case 0x00:
          //    //throw new Exception("Invalid character! 0x00");
          //    break;
          case 0x20:
            chr2 = ' ';
            break;
          case 0x23:
          case 0x24:
            chr2 = m_charTableA[txtLanguage, chr - 0x23];
            break;
          case 0x40:
            chr2 = m_charTableB[txtLanguage];
            break;
          case 0x5B:
          case 0x5C:
          case 0x5D:
          case 0x5E:
          case 0x5F:
          case 0x60:
            chr2 = m_charTableC[txtLanguage, chr - 0x5B];
            break;
          case 0x7B:
          case 0x7C:
          case 0x7D:
          case 0x7E:
            chr2 = m_charTableD[txtLanguage, chr - 0x7B];
            break;
          //case 0x7F:
          //    //throw new Exception("Invalid character! " + chr );
          //case 0xE0:
          //    //throw new Exception("Invalid character! " + chr);
          //case 0xE1:
          //    //throw new Exception("Invalid character! " + chr);
          //case 0xE2:
          //   // throw new Exception("Invalid character! " + chr);
          //case 0xE3:
          //  //  throw new Exception("Invalid character! " + chr);
          //case 0xE4:
          //  //  throw new Exception("Invalid character! " + chr);
          //case 0xE5:
          // //   throw new Exception("Invalid character! " + chr);
          //case 0xE6:
          ////    throw new Exception("Invalid character! " + chr);
          //case 0xE7:
          ////    throw new Exception("Invalid character! " + chr);
          //case 0xE8:
          ////    throw new Exception("Invalid character! " + chr);
          //case 0xE9:
          ////    throw new Exception("Invalid character! " + chr);
          //case 0xEA:
          ////    throw new Exception("Invalid character! " + chr);
          //case 0xEB:
          // //   throw new Exception("Invalid character! " + chr);
          //case 0xEC:
          ////    throw new Exception("Invalid character! " + chr);
          //    break;
          case 0xED:
          case 0xEE:
          case 0xEF:
          case 0xF0:
          case 0xF1:
          case 0xF2:
          case 0xF3:
          case 0xF4:
          case 0xF5:
            //case 0xF6:
            chr2 = m_charTableE[chr - 0xED];
            break;
          default:
            chr2 = (char)chr;
            break;
        }
        text[i] = chr2;
      }
      return text;
    }

    private static void Assert(bool ok, string msg)
    {
      if (!ok) throw new Exception("Assertion failed in TextConversion! " + msg);
    }
  }
}
