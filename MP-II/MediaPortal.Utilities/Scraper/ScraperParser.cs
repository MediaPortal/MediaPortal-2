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
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Web;
using System.Text.RegularExpressions;

namespace MediaPortal.Utilities.Scraper
{
  public class ScraperParser
  {

    public string[] m_param = new string[9];
    public List<ScraperSetting> _scarapersettings = new List<ScraperSetting>();

    XmlDocument m_document;
    XmlElement m_pRootElement;

    string m_name;
    string m_content;


    /// <summary>
    /// Loads the specified XML file.
    /// </summary>
    /// <param name="xmlFile">The XML file.</param>
    /// <returns></returns>
    public bool Load(string xmlFile)
    {
      m_document = new XmlDocument();
      m_document.Load(xmlFile);
      //if (!m_document)
      //  return false;
      m_pRootElement = m_document.DocumentElement;
      string strValue = m_pRootElement.Name;
      if (strValue != "scraper")
      {
        //delete m_document;
        m_document = null;
        m_pRootElement = null;
        return false;
      }
      m_name = m_pRootElement.Attributes["name"].Value;
      m_content = m_pRootElement.Attributes["content"].Value;
      if (string.IsNullOrEmpty(m_name) || string.IsNullOrEmpty(m_content))
      {
        //delete m_document;
        m_document = null;
        m_pRootElement = null;
        return false;
      }

      //// check for known content
      //if (stricmp(m_content, "tvshows") && stricmp(m_content, "movies"))
      //{
      //  delete m_document;
      //  m_document = NULL;
      //  m_pRootElement = NULL;
      //  return false;
      //} 
      LoadSettings();
      return true;
    }

    public void LoadSettings()
    {
      string xml = Parse("GetSettings");
      _scarapersettings.Clear();
      if (!string.IsNullOrEmpty(xml))
      {
        XmlDocument doc = new XmlDocument();
        doc.LoadXml(xml);
        XmlNodeList nodes = doc.SelectNodes("settings/setting");
        foreach (XmlNode node in nodes)
        {
          _scarapersettings.Add(new ScraperSetting(node));
        }
      }
    }

    /// <summary>
    /// Gets the name.
    /// </summary>
    /// <returns></returns>
    public string GetName()
    {
      return m_name;
    }

    /// <summary>
    /// Parses the specified STR tag.
    /// </summary>
    /// <param name="strTag">The STR tag.</param>
    /// <returns></returns>
    public string Parse(string strTag)
    {
      XmlNodeList pChildElement = m_pRootElement.GetElementsByTagName(strTag);

      if (pChildElement == null || pChildElement.Count < 1)
        return string.Empty;
      int iResult = 1; // default to param 1
      if (pChildElement[0].Attributes["dest"] != null)
        int.TryParse(pChildElement[0].Attributes["dest"].Value, out iResult);

      XmlNode pChildStart = pChildElement[0].SelectSingleNode("RegExp");
      ParseNext(ref pChildStart);

      string tmp = m_param[iResult - 1];
      ApplySettings(ref tmp);
      ClearBuffers();
      return tmp;
    }

    public void ApplySettings(ref string str)
    {
      foreach (ScraperSetting setting in _scarapersettings)
      {
        string param = string.Format("$INFO[{0}]", setting.Id);
        str = str.Replace(param, setting.Default);
      }
    }
    /// <summary>
    /// Converts the HTML to ANSI.
    /// </summary>
    /// <param name="strHTML">The STR HTML.</param>
    /// <returns></returns>
    public string ConvertHTMLToAnsi(string szHTML)
    {
      //if (szHTML == null) return String.Empty;
      //if (szHTML.Length == 0) return String.Empty;
      //string stripped = Regex.Replace(szHTML, @"<(.|\n)*?>", " ");
      //return stripped.Trim();
      //return szHTML;
      return szHTML; //HttpUtility.HtmlDecode(szHTML);
      //Convert.
      //if (szHTML == null)
      //  return null;

      int i = 0;
      int len = (int)szHTML.Length;
      if (len == 0)
        return null;

      int iAnsiPos = 0;
      //C++ TO C# CONVERTER TODO TASK: The memory management function 'malloc' has no equivalent in C#:
      char[] szAnsi = new char[len * 2 * sizeof(char)];

      while (i < len)
      {
        char kar = szHTML[i];
        if (kar == '&')
        {
          if (szHTML[i + 1] == '#')
          {
            int ipos = 0;
            string szDigit = "";
            i += 2;
            if (szHTML[i + 2] == 'x')
              i++;

            while (ipos < 12 && szHTML[i] != '\0' && char.IsDigit(szHTML[i]))
            {
              szDigit.Insert(ipos, szHTML[i].ToString());
              szDigit = szDigit.Substring(0, ipos + 1);
              ipos++;
              i++;
            }

            // is it a hex or a decimal string?
            if (szHTML[i + 2] == 'x')
              szAnsi[iAnsiPos++] = (char)(Convert.ToInt32(szDigit, 16) & 0xFF);
            else
              szAnsi[iAnsiPos++] = (char)(Convert.ToInt32(szDigit, 10) & 0xFF);
            i++;
          }
          else
          {
            i++;
            int ipos = 0;
            string szKey = "";
            while (szHTML[i] != null && szHTML[i] != ';' && ipos < 12)
            {
              szKey.Insert(ipos, szHTML[i].ToString());
              szKey = szKey.Substring(0, ipos + 1);
              ipos++;
              i++;
            }
            i++;
            if (string.Compare(szKey, "amp") == 0)
              szAnsi[iAnsiPos++] = '&';
            else if (string.Compare(szKey, "quot") == 0)
              szAnsi[iAnsiPos++] = (char)0x22;
            else if (string.Compare(szKey, "frasl") == 0)
              szAnsi[iAnsiPos++] = (char)0x2F;
            else if (string.Compare(szKey, "lt") == 0)
              szAnsi[iAnsiPos++] = (char)0x3C;
            else if (string.Compare(szKey, "gt") == 0)
              szAnsi[iAnsiPos++] = (char)0x3E;
            else if (string.Compare(szKey, "trade") == 0)
              szAnsi[iAnsiPos++] = (char)0x99;
            else if (string.Compare(szKey, "nbsp") == 0)
              szAnsi[iAnsiPos++] = ' ';
            else if (string.Compare(szKey, "iexcl") == 0)
              szAnsi[iAnsiPos++] = (char)0xA1;
            else if (string.Compare(szKey, "cent") == 0)
              szAnsi[iAnsiPos++] = (char)0xA2;
            else if (string.Compare(szKey, "pound") == 0)
              szAnsi[iAnsiPos++] = (char)0xA3;
            else if (string.Compare(szKey, "curren") == 0)
              szAnsi[iAnsiPos++] = (char)0xA4;
            else if (string.Compare(szKey, "yen") == 0)
              szAnsi[iAnsiPos++] = (char)0xA5;
            else if (string.Compare(szKey, "brvbar") == 0)
              szAnsi[iAnsiPos++] = (char)0xA6;
            else if (string.Compare(szKey, "sect") == 0)
              szAnsi[iAnsiPos++] = (char)0xA7;
            else if (string.Compare(szKey, "uml") == 0)
              szAnsi[iAnsiPos++] = (char)0xA8;
            else if (string.Compare(szKey, "copy") == 0)
              szAnsi[iAnsiPos++] = (char)0xA9;
            else if (string.Compare(szKey, "ordf") == 0)
              szAnsi[iAnsiPos++] = (char)0xAA;
            else if (string.Compare(szKey, "laquo") == 0)
              szAnsi[iAnsiPos++] = (char)0xAB;
            else if (string.Compare(szKey, "not") == 0)
              szAnsi[iAnsiPos++] = (char)0xAC;
            else if (string.Compare(szKey, "shy") == 0)
              szAnsi[iAnsiPos++] = (char)0xAD;
            else if (string.Compare(szKey, "reg") == 0)
              szAnsi[iAnsiPos++] = (char)0xAE;
            else if (string.Compare(szKey, "macr") == 0)
              szAnsi[iAnsiPos++] = (char)0xAF;
            else if (string.Compare(szKey, "deg") == 0)
              szAnsi[iAnsiPos++] = (char)0xB0;
            else if (string.Compare(szKey, "plusmn") == 0)
              szAnsi[iAnsiPos++] = (char)0xB1;
            else if (string.Compare(szKey, "sup2") == 0)
              szAnsi[iAnsiPos++] = (char)0xB2;
            else if (string.Compare(szKey, "sup3") == 0)
              szAnsi[iAnsiPos++] = (char)0xB3;
            else if (string.Compare(szKey, "acute") == 0)
              szAnsi[iAnsiPos++] = (char)0xB4;
            else if (string.Compare(szKey, "micro") == 0)
              szAnsi[iAnsiPos++] = (char)0xB5;
            else if (string.Compare(szKey, "para") == 0)
              szAnsi[iAnsiPos++] = (char)0xB6;
            else if (string.Compare(szKey, "middot") == 0)
              szAnsi[iAnsiPos++] = (char)0xB7;
            else if (string.Compare(szKey, "cedil") == 0)
              szAnsi[iAnsiPos++] = (char)0xB8;
            else if (string.Compare(szKey, "sup1") == 0)
              szAnsi[iAnsiPos++] = (char)0xB9;
            else if (string.Compare(szKey, "ordm") == 0)
              szAnsi[iAnsiPos++] = (char)0xBA;
            else if (string.Compare(szKey, "raquo") == 0)
              szAnsi[iAnsiPos++] = (char)0xBB;
            else if (string.Compare(szKey, "frac14") == 0)
              szAnsi[iAnsiPos++] = (char)0xBC;
            else if (string.Compare(szKey, "frac12") == 0)
              szAnsi[iAnsiPos++] = (char)0xBD;
            else if (string.Compare(szKey, "frac34") == 0)
              szAnsi[iAnsiPos++] = (char)0xBE;
            else if (string.Compare(szKey, "iquest") == 0)
              szAnsi[iAnsiPos++] = (char)0xBF;
            else if (string.Compare(szKey, "Agrave") == 0)
              szAnsi[iAnsiPos++] = (char)0xC0;
            else if (string.Compare(szKey, "Aacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xC1;
            else if (string.Compare(szKey, "Acirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xC2;
            else if (string.Compare(szKey, "Atilde") == 0)
              szAnsi[iAnsiPos++] = (char)0xC3;
            else if (string.Compare(szKey, "Auml") == 0)
              szAnsi[iAnsiPos++] = (char)0xC4;
            else if (string.Compare(szKey, "Aring") == 0)
              szAnsi[iAnsiPos++] = (char)0xC5;
            else if (string.Compare(szKey, "AElig") == 0)
              szAnsi[iAnsiPos++] = (char)0xC6;
            else if (string.Compare(szKey, "Ccedil") == 0)
              szAnsi[iAnsiPos++] = (char)0xC7;
            else if (string.Compare(szKey, "Egrave") == 0)
              szAnsi[iAnsiPos++] = (char)0xC8;
            else if (string.Compare(szKey, "Eacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xC9;
            else if (string.Compare(szKey, "Ecirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xCA;
            else if (string.Compare(szKey, "Euml") == 0)
              szAnsi[iAnsiPos++] = (char)0xCB;
            else if (string.Compare(szKey, "Igrave") == 0)
              szAnsi[iAnsiPos++] = (char)0xCC;
            else if (string.Compare(szKey, "Iacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xCD;
            else if (string.Compare(szKey, "Icirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xCE;
            else if (string.Compare(szKey, "Iuml") == 0)
              szAnsi[iAnsiPos++] = (char)0xCF;
            else if (string.Compare(szKey, "ETH") == 0)
              szAnsi[iAnsiPos++] = (char)0xD0;
            else if (string.Compare(szKey, "Ntilde") == 0)
              szAnsi[iAnsiPos++] = (char)0xD1;
            else if (string.Compare(szKey, "Ograve") == 0)
              szAnsi[iAnsiPos++] = (char)0xD2;
            else if (string.Compare(szKey, "Oacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xD3;
            else if (string.Compare(szKey, "Ocirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xD4;
            else if (string.Compare(szKey, "Otilde") == 0)
              szAnsi[iAnsiPos++] = (char)0xD5;
            else if (string.Compare(szKey, "Ouml") == 0)
              szAnsi[iAnsiPos++] = (char)0xD6;
            else if (string.Compare(szKey, "times") == 0)
              szAnsi[iAnsiPos++] = (char)0xD7;
            else if (string.Compare(szKey, "Oslash") == 0)
              szAnsi[iAnsiPos++] = (char)0xD8;
            else if (string.Compare(szKey, "Ugrave") == 0)
              szAnsi[iAnsiPos++] = (char)0xD9;
            else if (string.Compare(szKey, "Uacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xDA;
            else if (string.Compare(szKey, "Ucirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xDB;
            else if (string.Compare(szKey, "Uuml") == 0)
              szAnsi[iAnsiPos++] = (char)0xDC;
            else if (string.Compare(szKey, "Yacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xDD;
            else if (string.Compare(szKey, "THORN") == 0)
              szAnsi[iAnsiPos++] = (char)0xDE;
            else if (string.Compare(szKey, "szlig") == 0)
              szAnsi[iAnsiPos++] = (char)0xDF;
            else if (string.Compare(szKey, "agrave") == 0)
              szAnsi[iAnsiPos++] = (char)0xE0;
            else if (string.Compare(szKey, "aacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xE1;
            else if (string.Compare(szKey, "acirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xE2;
            else if (string.Compare(szKey, "atilde") == 0)
              szAnsi[iAnsiPos++] = (char)0xE3;
            else if (string.Compare(szKey, "auml") == 0)
              szAnsi[iAnsiPos++] = (char)0xE4;
            else if (string.Compare(szKey, "aring") == 0)
              szAnsi[iAnsiPos++] = (char)0xE5;
            else if (string.Compare(szKey, "aelig") == 0)
              szAnsi[iAnsiPos++] = (char)0xE6;
            else if (string.Compare(szKey, "ccedil") == 0)
              szAnsi[iAnsiPos++] = (char)0xE7;
            else if (string.Compare(szKey, "egrave") == 0)
              szAnsi[iAnsiPos++] = (char)0xE8;
            else if (string.Compare(szKey, "eacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xE9;
            else if (string.Compare(szKey, "ecirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xEA;
            else if (string.Compare(szKey, "euml") == 0)
              szAnsi[iAnsiPos++] = (char)0xEB;
            else if (string.Compare(szKey, "igrave") == 0)
              szAnsi[iAnsiPos++] = (char)0xEC;
            else if (string.Compare(szKey, "iacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xED;
            else if (string.Compare(szKey, "icirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xEE;
            else if (string.Compare(szKey, "iuml") == 0)
              szAnsi[iAnsiPos++] = (char)0xEF;
            else if (string.Compare(szKey, "eth") == 0)
              szAnsi[iAnsiPos++] = (char)0xF0;
            else if (string.Compare(szKey, "ntilde") == 0)
              szAnsi[iAnsiPos++] = (char)0xF1;
            else if (string.Compare(szKey, "ograve") == 0)
              szAnsi[iAnsiPos++] = (char)0xF2;
            else if (string.Compare(szKey, "oacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xF3;
            else if (string.Compare(szKey, "ocirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xF4;
            else if (string.Compare(szKey, "otilde") == 0)
              szAnsi[iAnsiPos++] = (char)0xF5;
            else if (string.Compare(szKey, "ouml") == 0)
              szAnsi[iAnsiPos++] = (char)0xF6;
            else if (string.Compare(szKey, "divide") == 0)
              szAnsi[iAnsiPos++] = (char)0xF7;
            else if (string.Compare(szKey, "oslash") == 0)
              szAnsi[iAnsiPos++] = (char)0xF8;
            else if (string.Compare(szKey, "ugrave") == 0)
              szAnsi[iAnsiPos++] = (char)0xF9;
            else if (string.Compare(szKey, "uacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xFA;
            else if (string.Compare(szKey, "ucirc") == 0)
              szAnsi[iAnsiPos++] = (char)0xFB;
            else if (string.Compare(szKey, "uuml") == 0)
              szAnsi[iAnsiPos++] = (char)0xFC;
            else if (string.Compare(szKey, "yacute") == 0)
              szAnsi[iAnsiPos++] = (char)0xFD;
            else if (string.Compare(szKey, "thorn") == 0)
              szAnsi[iAnsiPos++] = (char)0xFE;
            else if (string.Compare(szKey, "yuml") == 0)
              szAnsi[iAnsiPos++] = (char)0xFF;
            else
            {
              // its not an ampersand code, so just copy the contents
              szAnsi[iAnsiPos++] = '&';
              for (int iLen = 0; iLen < szKey.Length; iLen++)
                szAnsi[iAnsiPos++] = szKey[iLen];
            }
          }
        }
        else
        {
          szAnsi[iAnsiPos++] = kar;
          i++;
        }
      }
      szAnsi[iAnsiPos++] = '\0';
      return szAnsi.ToString();
    }

    /// <summary>
    /// Replaces the buffers.
    /// </summary>
    /// <param name="strDest">The STR dest.</param>
    private void ReplaceBuffers(ref string strDest)
    {
      string temp;
      for (int i = 0; i < 9; ++i)
      {
        temp = string.Format("$${0:D}", i + 1);
        strDest = strDest.Replace(temp, m_param[i]);
        //int iIndex = 0;
        //while ((iIndex = strDest.IndexOf(temp, iIndex)) <strDest.Length) // COPIED FROM CStdString WITH THE ADDITION OF $ ESCAPING
        //{
        //  strDest.(strDest.begin() + iIndex, strDest.begin() + iIndex + temp.Length, m_param[i]);
        //  iIndex += m_param[i].length();
        //}
      }
      //int iIndex = 0;
      //while ((iIndex = strDest.find("\\n", iIndex)) != CStdString.npos)
      //  strDest.replace(strDest.begin() + iIndex, strDest.begin() + iIndex + 2, "\n");
      strDest = strDest.Replace("\\n", "\n");
    }

    private void ReplaceExpresionBuffers(ref string strDest)
    {
      string temp;
      for (int i = 0; i < 9; ++i)
      {
        temp = string.Format("\\{0:D}", i + 1);
        strDest = strDest.Replace(temp, m_param[i]);
        //int iIndex = 0;
        //while ((iIndex = strDest.IndexOf(temp, iIndex)) <strDest.Length) // COPIED FROM CStdString WITH THE ADDITION OF $ ESCAPING
        //{
        //  strDest.(strDest.begin() + iIndex, strDest.begin() + iIndex + temp.Length, m_param[i]);
        //  iIndex += m_param[i].length();
        //}
      }
      //int iIndex = 0;
      //while ((iIndex = strDest.find("\\n", iIndex)) != CStdString.npos)
      //  strDest.replace(strDest.begin() + iIndex, strDest.begin() + iIndex + 2, "\n");
      //strDest = strDest.Replace("\\n", "\n");
    }

    private void ParseExpression(string input, ref string dest, XmlNode element, bool bAppend)
    {
      string strOutput = element.Attributes["output"].Value;

      XmlNode pExpression = element.SelectSingleNode("expression");
      if (pExpression != null)
      {
        Regex reg;

        string strExpression;
        if (pExpression.FirstChild != null)
          strExpression = pExpression.FirstChild.Value;
        else
          strExpression = "(.*)";
        ReplaceBuffers(ref strExpression);
        ReplaceBuffers(ref strOutput);

        reg = new Regex(strExpression, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        //if (!reg.RegComp(strExpression.c_str()))
        //{
        //  //std::cout << "error compiling regexp in scraper";
        //  return;
        //}

        bool bRepeat = false;
        if (pExpression.Attributes["repeat"] != null)
        {
          string szRepeat = pExpression.Attributes["repeat"].Value;
          if (string.Compare(szRepeat, "yes", true) == 0)
            bRepeat = true;
        }
        if (pExpression.Attributes["clear"] != null)
        {
          string szClear = pExpression.Attributes["clear"].Value;
          if (string.Compare(szClear, "yes", true) == 0)
            dest = ""; // clear no matter if regexp fails
        }
        bool[] bClean = new bool[10];
        for (int iBuf = 0; iBuf < 10; ++iBuf)
          bClean[iBuf] = true;
        if (pExpression.Attributes["noclean"] != null)
        {
          string szNoClean = pExpression.Attributes["noclean"].Value;
          int iChar = 0;
          string[] temp = new string[9];
          temp = szNoClean.Split(',');
          for (int i = 0; i < bClean.Length; i++)
          {
            bClean[i] = false;
          }
          for (int i = 0; i < temp.Length; i++)
          {
            int j = -1;
            int.TryParse(temp[i], out j);
            if (j > 0 && j < bClean.Length)
            {
              bClean[j] = true;
            }
          }
          //while (iChar > -1 && iChar < (int)szNoClean.Length-1)
          //{
          //char[] temp = new char[3];
          //if (szNoClean[iChar] <= '9' && szNoClean[iChar] >= '0')
          //{
          //  temp[0] = szNoClean.Substring(iChar++,1).ToCharArray()[0];
          //  int j = 1;
          //  if (szNoClean[iChar] <= '9' && szNoClean[iChar] >= '0')
          //    temp[j++] = szNoClean.Substring(iChar++,1).ToCharArray()[0];

          //  temp[j] = ' ';
          //}
          //else
          //  break;

          //int param = 0;
          //int.TryParse(temp.ToString().Trim(),out param);
          //if (param--<0)
          //{
          //  iChar = -1;
          //  break;
          //}
          ////CLog::Log(LOGDEBUG,"not cleaning %i",param+1);
          //bClean[param] = false;
          //if (szNoClean[iChar++] != ',')
          //  iChar = -1;
          //}
        }

        bool[] bTrim = new bool[10];
        for (int iBuf = 0; iBuf < 10; ++iBuf)
          bTrim[iBuf] = false;
        if (pExpression.Attributes["trim"] != null)
        {
          string szTrim = pExpression.Attributes["trim"].Value;
          int iChar = 0;
          string[] temp = new string[9];
          temp = szTrim.Split(',');
          for (int i = 0; i < bTrim.Length; i++)
          {
            bTrim[i] = false;
          }
          for (int i = 0; i < temp.Length; i++)
          {
            int j = -1;
            int.TryParse(temp[i], out j);
            if (j > 0 && j < bTrim.Length)
            {
              bTrim[j] = true;
            }
          }

          //int iChar = 0;
          //while (iChar > -1 && iChar < (int)szTrim.Length)
          //{
          //  char[] temp = new char[3];
          //  if (szTrim[iChar] <= '9' && szTrim[iChar] >= '0')
          //  {
          //    temp[0] = szTrim[iChar++];
          //    int j = 1;
          //    if (szTrim[iChar] <= '9' && szTrim[iChar] >= '0')
          //      temp[j++] = szTrim[iChar++];

          //    temp[j] = '\0';
          //  }
          //  else
          //    break;

          //  int param = Convert.ToInt32(temp);
          //  if (param--!=0)
          //  {
          //    iChar = -1;
          //    break;
          //  }
          //  //CLog::Log(LOGDEBUG,"not cleaning %i",param+1);
          //  bTrim[param] = true;
          //  if (szTrim[iChar++] != ',')
          //    iChar = -1;
          //}
        }

        int iOptional = -1;
        if (pExpression.Attributes["optional"] != null)
          int.TryParse(pExpression.Attributes["optional"].Value, out iOptional);

        int iCompare = -1;
        if (pExpression.Attributes["compare"] != null)
          int.TryParse(pExpression.Attributes["compare"].Value, out iCompare);

        if (iCompare > -1)
          m_param[iCompare - 1].ToLower();
        string curInput = input;
        for (int iBuf = 0; iBuf < 9; ++iBuf)
        {
          if (bClean[iBuf])
          {
            string temp;
            temp = string.Format("\\{0:D}", iBuf + 1);
            int i2 = 0;
            while ((i2 = strOutput.IndexOf(temp, i2)) > 0)
            {
              strOutput = strOutput.Insert(i2, "!!!CLEAN!!!");
              i2 += 11;
              strOutput = strOutput.Insert(i2 + 2, "!!!CLEAN!!!");
              i2 += 2;
            }
          }
          if (bTrim[iBuf])
          {
            string temp;
            temp = string.Format("\\{0:D}", iBuf + 1);
            int i2 = 0;
            while ((i2 = strOutput.IndexOf(temp, i2)) > 0)
            {
              strOutput = strOutput.Insert(i2, "!!!TRIM!!!");
              i2 += 10;
              strOutput = strOutput.Insert(i2 + 2, "!!!TRIM!!!");
              i2 += 2;
            }
          }
        }
        if (reg.IsMatch(curInput))
        {
          if (!bAppend)
          {
            dest = "";
            bAppend = true;
          }
          MatchCollection mtc = reg.Matches(curInput);

          string strResult = strOutput;
          int i = 0;

          for (i = 0; i < mtc.Count; i++)
          {
            strResult = strOutput;
            for (int j = 1; j < mtc[i].Groups.Count; j++) //(Group gr in mtc[i].Groups)
            {
              string temp = string.Format("\\{0:D}", j);
              strResult = strResult.Replace(temp, mtc[i].Groups[j].Value);
            }
            if (!bRepeat)
              i = mtc.Count;
            //HttpUtility.HtmlEncode(
            // bulding output expresion string 
            //strResult.Replace( "&","&amp;");
            //ReplaceExpresionBuffers(ref strResult);
            Clean(ref strResult);

            dest += strResult;
          }

        }
        //strCurOutput.Replace("&", "!!!AMPAMP!!!");
        //int i = strExpression.IndexOf(curInput);
        //int iPos = 0;
        //while (i > -1 && i < (int)curInput.Length)
        //{
        //  if (!bAppend)
        //  {
        //    dest = "";
        //    bAppend = true;
        //  }
        //  string strCurOutput = strOutput;
        //  if (iOptional > -1) // check that required param is there
        //  {
        //  string temp;
        //  temp = string.Format("\\{0:D}", iOptional);
        //  char szParam = reg.GetReplaceString(temp);
        //  Regex reg2;
        //  reg2.RegComp("(.*)(\\\\\\(.*\\\\2.*)\\\\\\)(.*)");
        //  int i2 = reg2.RegFind(strCurOutput.c_str());
        //  while (i2 > -1)
        //  {
        //    char szRemove = reg2.GetReplaceString("\\2");
        //    int iRemove = szRemove.Length;
        //    int i3 = strCurOutput.find(szRemove);
        //    if (szParam && string.Compare(szParam, ""))
        //    {
        //      strCurOutput.erase(i3 + iRemove, 2);
        //      strCurOutput.erase(i3, 2);
        //    }
        //    else
        //      strCurOutput.replace(strCurOutput.begin() + i3, strCurOutput.begin() + i3 + iRemove + 2, "");

        //    i2 = reg2.RegFind(strCurOutput.c_str());
        //  }
      }

      //int iLen = reg.GetFindLen();
      //// nasty hack #1 - & means \0 in a replace string

      //string result = reg.Replace(strCurOutput,);
      //if (result && result.Length)
      //{
      //  CStdString strResult = new CStdString(result);
      //  strResult.Replace("!!!AMPAMP!!!", "&");
      //  Clean(ref strResult);
      //  ReplaceBuffers(ref strResult);
      //  if (iCompare > -1)
      //  {
      //    CStdString strResultNoCase = strResult;
      //    strResultNoCase.ToLower();
      //    if (strResultNoCase.Find(m_param[iCompare - 1]) != CStdString.npos)
      //      dest += strResult;
      //  }
      //  else
      //    dest += strResult;
      //}
      //if (bRepeat)
      //{
      //  curInput.erase(0, i + iLen > (int)curInput.size() ? curInput.size() : i + iLen);
      //  i = reg.RegFind(curInput.c_str());
      //}
      //else
      //  i = -1;
      //}
      //}
    }

    /// <summary>
    /// Parses the next.
    /// </summary>
    /// <param name="element">The element.</param>
    void ParseNext(ref XmlNode element)
    {
      //XmlNode pReg = element;
      XmlNodeList tempnodelist = element.ParentNode.SelectNodes("RegExp");
      //if (tempnodelist.Count < 1)
      //{
      //  tempnodelist = element.ParentNode.SelectNodes("RegExp");
      //}
      foreach (XmlNode pReg in tempnodelist)
      {
        //if (pReg.Name != "RegExp")
        //{
        //  pReg = pReg.NextSibling;
        //  continue;
        //}
        XmlNode pChildReg = pReg.SelectSingleNode("RegExp");
        if (pChildReg != null)
          ParseNext(ref pChildReg);
        else
        {
          pChildReg = pReg.SelectSingleNode("clear");
          if (pChildReg != null)
            ParseNext(ref pChildReg);
        }

        int iDest = 1;
        bool bAppend = false;

        if (pReg.Attributes["dest"] != null)
        {
          string szDest = pReg.Attributes["dest"].Value;
          if (szDest.Length > 0)
          {
            if (szDest.Substring(szDest.Length - 1, 1) == "+")
              bAppend = true;

            int.TryParse(szDest[0].ToString(), out iDest);
          }
        }
        string strInput;
        if (pReg.Attributes["input"] != null)
        {
          string szInput = pReg.Attributes["input"].Value;
          strInput = szInput;
          ReplaceBuffers(ref strInput);
        }
        else
          strInput = m_param[0];

        bool bExecute = true;
        if (pReg.Attributes["conditional"] != null)
        {
          string szConditional = pReg.Attributes["conditional"].Value;

          if (szConditional != null)
          {
            bool bInverse = false;
            if (szConditional[0] == '!')
            {
              bInverse = true;
              szConditional = szConditional.Substring(1);
            }
            ScraperSetting strSetting = GetSetting(szConditional);
            if (strSetting != null)
            {
              bExecute = bInverse ? !strSetting.GetValueAsBool() : strSetting.GetValueAsBool();
            }
            else
            {
              bExecute = !bInverse;
            }
          }
        }
        if (bExecute)
          ParseExpression(strInput, ref m_param[iDest - 1], pReg, bAppend);
        //pReg = pReg.NextSibling;// NextSiblingElement("RegExp");
      }
    }

    void Clean(ref string strDirty)
    {
      int i = 0;
      string strBuffer;
      while ((i = strDirty.IndexOf("!!!CLEAN!!!", i)) > 0)
      {
        int i2;
        if ((i2 = strDirty.IndexOf("!!!CLEAN!!!", i + 11)) > 0)
        {
          strBuffer = strDirty.Substring(i + 11, i2 - i - 11);
          strBuffer = ConvertHTMLToAnsi(strBuffer);
          strDirty = strDirty.Remove(i, i2 - i + 11);
          strDirty = strDirty.Insert(i, strBuffer);
          i += strBuffer.Length;
        }
      }

      i = 0;
      while ((i = strDirty.IndexOf("!!!TRIM!!!", i)) > 0)
      {
        int i2;
        if ((i2 = strDirty.IndexOf("!!!TRIM!!!", i + 10)) > 0)
        {
          strBuffer = strDirty.Substring(i + 10, i2 - i - 10);
          strBuffer = strBuffer.Trim();
          strDirty = strDirty.Remove(i, i2 - i + 10);
          strDirty = strDirty.Insert(i, strBuffer);
          i += strBuffer.Length;
        }
      }


      //while ((i = strDirty.Find("!!!CLEAN!!!", i)) != CStdString.npos)
      //{
      //  int i2;
      //  if ((i2 = strDirty.Find("!!!CLEAN!!!", i + 11)) != CStdString.npos)
      //  {
      //    strBuffer = strDirty.substr(i + 11, i2 - i - 11);
      //    //char* szConverted = ConvertHTMLToAnsi(strBuffer.c_str());
      //    //const char* szTrimmed = RemoveWhiteSpace(szConverted);
      //    CStdString strConverted = new CStdString(strBuffer);
      //    //      HTML::CHTMLUtil::RemoveTags(strConverted);
      //    string szTrimmed = RemoveWhiteSpace(strConverted.c_str());
      //    strDirty.erase(i, i2 - i + 11);
      //    strDirty.Insert(i, szTrimmed);
      //    i += szTrimmed.Length;
      //    //free(szConverted);
      //  }
      //  else
      //    break;
      //}
      //i = 0;
      //while ((i = strDirty.Find("!!!TRIM!!!", i)) != CStdString.npos)
      //{
      //  int i2;
      //  if ((i2 = strDirty.Find("!!!TRIM!!!", i + 10)) != CStdString.npos)
      //  {
      //    strBuffer = strDirty.substr(i + 10, i2 - i - 10);
      //    string szTrimmed = RemoveWhiteSpace(strBuffer.c_str());
      //    strDirty.erase(i, i2 - i + 10);
      //    strDirty.Insert(i, szTrimmed);
      //    i += szTrimmed.Length;
      //  }
      //  else
      //    break;
      //}

    }

    private string RemoveWhiteSpace(string str)
    {
      string strTemp = str.Trim(); ;// = str.Replace(" ", "");
      strTemp = strTemp.Replace("\n", "");
      return str;
    }


    /// <summary>
    /// Clears the buffers.
    /// </summary>
    private void ClearBuffers()
    {
      for (int i = 0; i < 9; ++i)
      {
        m_param[i] = string.Empty; ;
      }

    }

    /// <summary>
    /// Gets the setting.
    /// </summary>
    /// <param name="id">The id.</param>
    /// <returns></returns>
    public ScraperSetting GetSetting(string id)
    {
      foreach (ScraperSetting set in _scarapersettings)
      {
        if (set.Id == id)
          return set;
      }
      return null;
    }
  }
}
