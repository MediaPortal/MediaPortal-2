using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.News.Settings
{
  public class FeedBookmark
  {
    public string Url { get; set; }
    public string Name { get; set; }
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
