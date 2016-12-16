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

using System;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.News.Models
{
  public class NewsItem : ListItem
  {
    protected readonly AbstractProperty _title = new WProperty(typeof(string), "No Title");

    public AbstractProperty TitleProperty
    {
      get { return _title; }
    }

    public string Title
    {
      get { return (string)_title.GetValue(); }
      set { _title.SetValue(value); }
    }

    protected readonly AbstractProperty _summary = new WProperty(typeof(string), "No Summary");

    public AbstractProperty SummaryProperty
    {
      get { return _summary; }
    }

    public string Summary
    {
      get { return (string)_summary.GetValue(); }
      set { _summary.SetValue(value); }
    }

    protected readonly AbstractProperty _publishDate = new WProperty(typeof(DateTime), DateTime.MinValue);

    public AbstractProperty PublishDateProperty
    {
      get { return _publishDate; }
    }

    public DateTime PublishDate
    {
      get { return (DateTime)_publishDate.GetValue(); }
      set { _publishDate.SetValue(value); }
    }

    protected readonly AbstractProperty _thumb = new WProperty(typeof(string), string.Empty);

    public AbstractProperty ThumbProperty
    {
      get { return _thumb; }
    }

    public string Thumb
    {
      get { return (string)_thumb.GetValue(); }
      set { _thumb.SetValue(value); }
    }

    public string Id { get; set; }

    public NewsFeed Feed { get; set; }

    public void CopyTo(NewsItem otherItem)
    {
      otherItem.Thumb = Thumb;
      otherItem.Title = Title;
      otherItem.Summary = Summary;
      otherItem.PublishDate = PublishDate;
      otherItem.Id = Id;
      otherItem.Feed = Feed;
    }
  }
}
