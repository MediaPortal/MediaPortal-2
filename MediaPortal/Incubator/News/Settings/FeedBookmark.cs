using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System.Xml.Serialization;

namespace MediaPortal.UiComponents.News.Settings
{
  #region Serialization Helpers for Default Feeds

  [XmlRoot("RegionalFeeds")]
  public class RegionalFeedBookmarksCollection : List<RegionalFeedBookmarks>
  {
  }

  [XmlType("Region")]
  public class RegionalFeedBookmarks
  {
    [XmlAttribute("Code")]
    public string RegionCode { get; set; }

    [XmlArray("Feeds"), XmlArrayItem("Feed")]
    public List<FeedBookmark> FeedBookmarks { get; set; }
  }

  #endregion

  public class FeedBookmark
  {
    [XmlAttribute("Name")]
    public string Name { get; set; }

    [XmlText]
    public string Url { get; set; }
  }

  public class FeedBookmarkItem : ListItem
  {
    protected readonly AbstractProperty _url = new WProperty(typeof(string), string.Empty);
    protected readonly AbstractProperty _name = new WProperty(typeof(string), string.Empty);

    public AbstractProperty UrlProperty
    {
      get { return _url; }
    }

    public string Url
    {
      get { return (string)_url.GetValue(); }
      set { _url.SetValue(value); }
    }

    public AbstractProperty NameProperty
    {
      get { return _name; }
    }

    public string Name
    {
      get { return (string)_name.GetValue(); }
      set { _name.SetValue(value); }
    }
  }
}
