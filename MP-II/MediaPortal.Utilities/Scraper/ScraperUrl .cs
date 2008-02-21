using System;
using System.Collections.Generic;
using System.Text;
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
