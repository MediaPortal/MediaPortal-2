#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using MediaPortal.UI.Presentation.Models;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System.Linq;
using System;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Common.Commands;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.News.Models
{
  public class CurrentNewsModel : BaseTimerControlledModel
  {
    /// <summary>
    /// Update interval for the current news item.
    /// </summary>
    const long NEWSITEM_UPDATE_INTERVAL = 10 * 1000;
    const int NEWSITEM_TOP_COUNT = 10;

    protected readonly AbstractProperty _currentNewsItemProperty = new WProperty(typeof(NewsItem), new NewsItem());
    protected ItemsList _currentNewsItems = new ItemsList();
    protected ItemsList _currentTopNewsItems = new ItemsList();
    protected ItemsList _currentFeedItems = new ItemsList();
    protected bool _inited = false;

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

    /// <summary>
    /// Current news items ordered by date
    /// </summary>
    public ItemsList CurrentNewsItems
    {
      get { return _currentNewsItems; }
    }

    /// <summary>
    /// Current top news items ordered by date
    /// </summary>
    public ItemsList CurrentTopNewsItems
    {
      get { return _currentTopNewsItems; }
    }

    /// <summary>
    /// Current news feeds ordered by date
    /// </summary>
    public ItemsList CurrentNewsFeeds
    {
      get { return _currentFeedItems; }
    }

    public CurrentNewsModel()
      : base(true, 100)
    {
      _inited = false;
    }

    protected Task ShowNews(ListItem item)
    {
      var model = ServiceRegistration.Get<IWorkflowManager>().GetModel(NewsModel.NEWS_MODEL_ID) as NewsModel;
      if (model != null)
      {
        model.Select(item);
      }
      return Task.CompletedTask;
    }

    public void Refresh()
    {
      INewsCollector newsCollector = ServiceRegistration.Get<INewsCollector>(false);
      if (newsCollector != null)
      {
        NewsItem newNewsItem = newsCollector.GetRandomNewsItem();
        if (newNewsItem != null)
        {
          newNewsItem.CopyTo(CurrentNewsItem);

          var feeds = newsCollector.GetAllFeeds().OrderByDescending(f => f.LastUpdated);
          //Only update if changed
          if (!feeds.Select(f => f.LastUpdated).SequenceEqual(_currentFeedItems.Select(f => (f as NewsFeed)?.LastUpdated ?? DateTime.Now)))
          {
            _currentFeedItems.Clear();
            foreach (var feed in feeds)
            {
              feed.Command = new AsyncMethodDelegateCommand(() => ShowNews(feed));
              _currentFeedItems.Add(feed);
            }
            _currentFeedItems.FireChange();
          }

          var items = newsCollector.GetAllNewsItems().OrderByDescending(i => i.PublishDate);
          //Only update if changed
          if (!items.Select(i => i.PublishDate).SequenceEqual(_currentNewsItems.Select(i => (i as NewsItem)?.PublishDate ?? DateTime.Now)))
          {
            _currentNewsItems.Clear();
            _currentTopNewsItems.Clear();
            foreach (var newsItem in items)
            {
              newsItem.Command = new AsyncMethodDelegateCommand(() => ShowNews(newsItem));
              _currentNewsItems.Add(newsItem);
              if (_currentTopNewsItems.Count < NEWSITEM_TOP_COUNT)
                _currentTopNewsItems.Add(newsItem);
            }
            _currentNewsItems.FireChange();
            _currentTopNewsItems.FireChange();
          }
        }
      }
    }

    protected override void Update()
    {
      Refresh();

      if (!_inited)
      {
        // Decrease interval once we have the first item
        ChangeInterval(NEWSITEM_UPDATE_INTERVAL);
      }
      _inited = true;
    }
  }
}
