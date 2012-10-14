using System;
using System.Collections.Generic;
using MediaPortal.UiComponents.News.Models;

namespace MediaPortal.UiComponents.News
{
  public interface INewsCollector : IDisposable
  {
    NewsItem GetRandomNewsItem();
    List<NewsFeed> GetAllFeeds();
    bool IsRefeshing { get; }
    event Action<INewsCollector> Refeshed;
  }
}
