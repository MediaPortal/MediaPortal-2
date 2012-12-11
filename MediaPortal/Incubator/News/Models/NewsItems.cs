using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
