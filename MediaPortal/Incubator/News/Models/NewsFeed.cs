using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Common.General;

namespace MediaPortal.UiComponents.News.Models
{
  public class NewsFeed : ListItem
  {
    public NewsFeed()
    {
      Items = new ItemsList();
    }

    public ItemsList Items { get; protected set; }

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

    protected readonly AbstractProperty _description = new WProperty(typeof(string), "");

    public AbstractProperty DescriptionProperty
    {
      get { return _description; }
    }

    public string Description
    {
      get { return (string)_description.GetValue(); }
      set { _description.SetValue(value); }
    }

    protected readonly AbstractProperty _lastUpdated = new WProperty(typeof(DateTime), DateTime.MinValue);

    public AbstractProperty LastUpdatedProperty
    {
      get { return _lastUpdated; }
    }

    public DateTime LastUpdated
    {
      get { return (DateTime)_lastUpdated.GetValue(); }
      set { _lastUpdated.SetValue(value); }
    }

    protected readonly AbstractProperty _icon = new WProperty(typeof(string), string.Empty);

    public AbstractProperty IconProperty
    {
      get { return _icon; }
    }
    
    public string Icon
    {
      get { return (string)_icon.GetValue(); }
      set { _icon.SetValue(value); }
    }

  }
}
