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

using System.Collections.Generic;
using System.Xml;
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
      }
      strDest = strDest.Replace("\\n", "\n");
    }

    private void ReplaceExpresionBuffers(ref string strDest)
    {
      string temp;
      for (int i = 0; i < 9; ++i)
      {
        temp = string.Format("\\{0:D}", i + 1);
        strDest = strDest.Replace(temp, m_param[i]);
      }
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
        }

        bool[] bTrim = new bool[10];
        for (int iBuf = 0; iBuf < 10; ++iBuf)
          bTrim[iBuf] = false;
        if (pExpression.Attributes["trim"] != null)
        {
          string szTrim = pExpression.Attributes["trim"].Value;
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
            Clean(ref strResult);

            dest += strResult;
          }

        }
      }

    }

    /// <summary>
    /// Parses the next.
    /// </summary>
    /// <param name="element">The element.</param>
    void ParseNext(ref XmlNode element)
    {
      //XmlNode pReg = element;
      XmlNodeList tempnodelist = element.ParentNode.SelectNodes("RegExp");
      foreach (XmlNode pReg in tempnodelist)
      {
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
