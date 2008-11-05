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
  public class ScraperSearchResult
  {
    string _url;
    string _title;
    string _id;

    /// <summary>
    /// Gets or sets the URL.
    /// </summary>
    /// <value>The URL.</value>
    public string Url
    {
      get
      {
        return _url;
      }
      set
      {
        _url = value;
      }
    }

    /// <summary>
    /// Gets or sets the title.
    /// </summary>
    /// <value>The title.</value>
    public string Title
    {
      get
      {
        return _title;
      }
      set
      {
        _title = value;
      }
    }

    /// <summary>
    /// Gets or sets the id.
    /// </summary>
    /// <value>The id.</value>
    public string Id
    {
      get
      {
        return _id;
      }
      set
      {
        _id = value;
      }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScraperSearchResult"/> class.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="title">The title.</param>
    /// <param name="id">The id.</param>
    public ScraperSearchResult(string url,string title,string id)
    {
      Url = url;
      Title = title;
      Id = id;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ScraperSearchResult"/> class.
    /// </summary>
    /// <param name="node">The node.</param>
    public ScraperSearchResult(XmlNode node)
    {
      if (node.SelectSingleNode("title") != null)
        Title = node.SelectSingleNode("title").InnerText;
      if (node.SelectSingleNode("url") != null)
        Url = node.SelectSingleNode("url").InnerText;
      if (node.SelectSingleNode("id") != null)
        Id = node.SelectSingleNode("id").InnerText;
    }

    /// <summary>
    /// Returns a <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
    /// </summary>
    /// <returns>
    /// A <see cref="T:System.String"></see> that represents the current <see cref="T:System.Object"></see>.
    /// </returns>
    public override string ToString()
    {
      return Title;
    }
  }
}
