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

using System.Collections.Generic;
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
