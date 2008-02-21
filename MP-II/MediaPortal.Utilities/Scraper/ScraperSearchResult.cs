using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace MediaPortal.Utilities.Scraper
{
  class ScraperSearchResult
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
