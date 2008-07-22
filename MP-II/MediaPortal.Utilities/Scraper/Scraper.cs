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
using System.Xml;
using System.Net;

namespace MediaPortal.Utilities.Scraper
{
  public class Scraper
  {
    private ScraperParser parser;
    private string _searchUrl;
    private bool _isLoaded;
    private List<ScraperSearchResult> _searchResults;
    private WebClient client;
    private Dictionary<string, string> _metadata;
    private string _xmldetails;

#region properties

    /// <summary>
    /// Gets or sets a value indicating whether this instance is loaded.
    /// </summary>
    /// <value><c>true</c> if this instance is loaded; otherwise, <c>false</c>.</value>
    public bool IsLoaded
    {
      get
      {
        return _isLoaded;
      }
      set
      {
        _isLoaded = value;
      }

    }

    /// <summary>
    /// Gets or sets the search URL.
    /// </summary>
    /// <value>The search URL.</value>
    public string SearchUrl
    {
      get
      {
        return _searchUrl;
      }
      set
      {
        _searchUrl = value;
      }
    }

    /// <summary>
    /// Gets or sets the search results.
    /// </summary>
    /// <value>The search results.</value>
    public List<ScraperSearchResult> SearchResults
    {
      get
      {
        return _searchResults;
      }
      set
      {
        _searchResults = value;
      }
    }

    /// <summary>
    /// Gets or sets the metadata.
    /// </summary>
    /// <value>The metadata.</value>
    public Dictionary<string, string> Metadata
    {
      get
      {
        return _metadata;
      }
      set
      {
        _metadata = value;
      }
    }

    /// <summary>
    /// Gets or sets the scraper settings.
    /// </summary>
    /// <value>The scraper settings.</value>
    public List<ScraperSetting> ScraperSettings
    {
      get
      {
        return parser._scarapersettings;
      }
      set
      {
        parser._scarapersettings = value;
      }
    }

    /// <summary>
    /// Gets or sets the XML details.
    /// </summary>
    /// <value>The XML details.</value>
    public string XmlDetails
    {
      get
      {
        return _xmldetails;
      }
      set
      {
        _xmldetails = value;
      }
    }

#endregion
    
    public Scraper()
    {
      parser = new ScraperParser();
      client = new WebClient();
      _metadata = new Dictionary<string, string>();
      _searchResults = new List<ScraperSearchResult>();
    }

    /// <summary>
    /// Loads the specified xmlfile.
    /// </summary>
    /// <param name="xmlfile">The xmlfile.</param>
    public void Load(string xmlfile)
    {
      parser.Load(xmlfile);
      IsLoaded = true;
    }

    /// <summary>
    /// Creates the search URL.
    /// </summary>
    /// <param name="searchString">The search string.</param>
    /// <returns></returns>
    public string CreateSearchUrl(string searchString)
    {
      SetBuffer(1, searchString);
      string xml = parser.Parse("CreateSearchUrl");
      if (!string.IsNullOrEmpty(xml))
      {
        try
        {
          XmlDocument doc = new XmlDocument();
          doc.LoadXml(xml);
          if (doc.SelectSingleNode("url") != null)
          {
            SearchUrl = doc.InnerText;
            return SearchUrl;
          }
        }
        catch (Exception)
        {
          SearchUrl = xml;
          return xml;
        }
      }
      return string.Empty;
    }

    public void GetSearchResults()
    {
      SearchResults.Clear();
      if (!string.IsNullOrEmpty(SearchUrl))
      {
        string web = client.DownloadString(SearchUrl);
        SetBuffer(1, web);
        SetBuffer(2, SearchUrl);
        string xml = parser.Parse("GetSearchResults");
        if (!string.IsNullOrEmpty(xml))
        {
          XmlDocument doc = new XmlDocument();
          doc.LoadXml(xml);
          foreach (XmlNode node in doc.SelectNodes("results/entity"))
          {
            SearchResults.Add(new ScraperSearchResult(node));
          }
        }
      }
    }

    /// <summary>
    /// Gets the details.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="id">The id.</param>
    public void GetDetails(string url, string id)
    {
      string web = client.DownloadString(url);
      SetBuffer(1, web);
      SetBuffer(2, id);
      string xml = ParseCustomFunction(parser.Parse("GetDetails"));
      XmlDocument doc = new XmlDocument();
      XmlDetails = xml;
      doc.LoadXml(xml);
      Metadata.Clear();
 
      XmlNode node;
      XmlNodeList nodelist;

      node = doc.SelectSingleNode("details/title");
      if (node != null)
        Metadata.Add("title", node.InnerText);

      node = doc.SelectSingleNode("details/year");
      if (node != null)
        Metadata.Add("year", node.InnerText);

      node = doc.SelectSingleNode("details/rating");
      if (node != null)
        Metadata.Add("rating", node.InnerText);

      nodelist = doc.SelectNodes("details/genre");
      foreach (XmlNode n in nodelist)
      {
        AddMetadata("genre", n.InnerText);
      }

      node = doc.SelectSingleNode("details/details/thumbs/thumb");
      if (node != null)
        Metadata.Add("thumb", node.InnerText);

      nodelist = doc.SelectNodes("details/details/actor/name");
      foreach (XmlNode n in nodelist)
      {
        AddMetadata("actors", n.InnerText);
      }

    }

    /// <summary>
    /// Adds the metadata.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    private void AddMetadata(string key, string value)
    {
      if (!Metadata.ContainsKey(key))
      {
        Metadata.Add(key, value);
      }
      else
      {
        Metadata[key] += "|" + value;
      }

    }

    public string  ParseCustomFunction(string str)
    {
      int i = 0;
      string strBuffer;
      while ((i = str.IndexOf("<url", i)) > 0)
      {
        int i2;
        if ((i2 = str.IndexOf("</url>", i + 11)) > 0)
        {
          strBuffer = str.Substring(i, i2 - i + 6);
          //strBuffer = ConvertHTMLToAnsi(strBuffer);
          str = str.Remove(i, i2 - i + 6);
          string result = ParseCustomFunctionXml(strBuffer);
          str = str.Insert(i, result);
       //   i += strBuffer.Length;
        }
      }
      return str;
    }

    public string ParseCustomFunctionXml(string xml)
    {
      XmlDocument doc = new XmlDocument();
      doc.LoadXml(xml);
      XmlNode node = doc.SelectSingleNode("url");
      if (node.Attributes["function"] != null)
      {
        string web = client.DownloadString(node.InnerText);
        SetBuffer(1, web);
        string str = parser.Parse(node.Attributes["function"].Value);
        return str;
      }
      else
      {
        return node.InnerText;
      }
    }

    public void SetBuffer(int index, string value)
    {
      if (index > 0 && index < 10)
      {
        parser.m_param[index - 1] = value;
      }
    }
  }
}
