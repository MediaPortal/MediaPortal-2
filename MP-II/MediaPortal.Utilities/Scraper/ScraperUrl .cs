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

using System.Xml;

namespace MediaPortal.Utilities.Scraper
{
  class ScraperUrl
  {

    public ScraperUrl(string strUrl)
    {
      m_post = false;
      ParseString(strUrl);
 
    }

    public ScraperUrl(XmlElement element)
    {
      m_post = false;
      ParseElement(element); 
    }

    public ScraperUrl()
    {
      m_post = false; 
    }

     ~ScraperUrl()
    {
    }

    public void ParseString(string strUrl)
    {
      if (string.IsNullOrEmpty(strUrl))
        return;

      // ok, now parse the xml file
      /*  if (strUrl.Find("encoding=\"utf-8\"") < 0)
          g_charsetConverter.stringCharsetToUtf8(strUrl);
        */
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(strUrl);

      //if (doc.FirstChild.)
      //{
        m_url = doc.FirstChild.Value;
        m_spoof = doc.FirstChild.Attributes["spoof"].Value;
        m_post = doc.FirstChild.Attributes["post"].Specified;
      //}
      //else
       // m_url = strUrl; 
    }

    public void ParseElement(XmlElement element)
    {
      m_url = element.FirstChild.Value;
      m_spoof = element.FirstChild.Attributes["spoof"].Value;
//      if (element.HasAttribute("post"))
      m_post = false; ;
    }

    string m_spoof;
    string m_url;
    bool m_post;

  }
}
