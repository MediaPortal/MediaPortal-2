using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.Common;
using MediaPortal.Common.General;

namespace MediaPortal.UiComponents.News.Models
{
  public class CurrentNewsModel : BaseTimerControlledModel
  {
    /// <summary>
    /// Update interval for the current news item.
    /// </summary>
    const long NEWSITEM_UPDATE_INTERVAL = 10 * 1000;

    protected readonly AbstractProperty _currentNewsItemProperty = new WProperty(typeof(NewsItem), new NewsItem());

    /// <summary>
    /// Exposes the current news item to the skin.
    /// </summary>
    public AbstractProperty CurrentNewsItemProperty
    {
      get { return _currentNewsItemProperty; }
    }

    /// <summary>
    /// Current news item
    /// </summary>
    public NewsItem CurrentNewsItem
    {
      get { return (NewsItem)_currentNewsItemProperty.GetValue(); }
      set { _currentNewsItemProperty.SetValue(value); }
    }

    public CurrentNewsModel()
      : base(NEWSITEM_UPDATE_INTERVAL)
    {
    }

    protected override void Update()
    {
      INewsCollector newsCollector = ServiceRegistration.Get<INewsCollector>(false);
      if (newsCollector != null)
      {
        NewsItem newNewsItem = newsCollector.GetRandomNewsItem();
        if (newNewsItem != null)
          newNewsItem.CopyTo(CurrentNewsItem);
      } 
    }
  }
}
