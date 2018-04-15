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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.News.Models;
using MediaPortal.UiComponents.News.Settings;

namespace MediaPortal.UiComponents.News
{
  class NewsCollector : INewsCollector
  {
    protected Random _random = new Random();
    protected object _refreshingSyncObj = new object();
    protected bool _forceRefresh = false;
    protected NewsItem _lastRandomNewsItem = null;
    protected List<NewsFeed> _feeds = new List<NewsFeed>();
    protected bool _refeshInProgress = false;
    IntervalWork _work = null;

    public NewsCollector()
    {
      NewsSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NewsSettings>();
      _work = new IntervalWork(RefreshFeeds, TimeSpan.FromMinutes(settings.RefreshInterval));
      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(_work, true);
    }

    public void Dispose()
    {
      ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(_work);
    }

    public NewsItem GetRandomNewsItem()
    {
      List<ListItem> items;
      lock (_feeds)
      {
        items = _feeds.SelectMany(f => f.Items).Where(i => i != _lastRandomNewsItem).ToList();
      }
      if (items.Count == 0) return null;
      if (items.Count == 1) return (NewsItem) items.First();
      return (NewsItem) items[_random.Next(items.Count)];
    }

    public List<NewsFeed> GetAllFeeds()
    {
      // lock the feed list and return a copy of the list, as the background refesh thread can modify our list
      lock (_feeds)
      {
        return _feeds.ToList();
      }
    }

    public bool IsRefeshing { get { return _refeshInProgress; } }
    public event Action RefeshStarted;
    public event Action<INewsCollector> RefeshFinished;

    public void RefreshNow()
    {
      RefreshFeeds();
    }

    public void ChangeRefreshInterval(int minutes)
    {
      ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(_work);
      _work = new IntervalWork(RefreshFeeds, TimeSpan.FromMinutes(minutes));
      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(_work, true);
    }

    List<string> GetConfiguredFeedUrls()
    {
      NewsSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NewsSettings>();
      lock (settings.FeedsList)
      {
        if (settings.FeedsList.Count == 0)
          return NewsSettings.GetDefaultRegionalFeeds().Select(f => f.Url).ToList();
        return settings.FeedsList.Select(f => f.Url).ToList();
      }
    }

    void RefreshFeeds()
    {
      // if the flag for force refresh is set, some thread already came here while a refresh was in progress
      if (_forceRefresh) return;

      // try to get an exclusive lock on the refreshingSyncObj
      if (Monitor.TryEnter(_refreshingSyncObj))
      {
        try
        {
          _refeshInProgress = true;
          ServiceRegistration.Get<ILogger>().Info("Started refeshing News Feeds ...");
          var refreshStartedEvent = RefeshStarted;
          if (refreshStartedEvent != null) refreshStartedEvent();
          _forceRefresh = false; // reset the flag for a forced refresh as we are currently refreshing and next step is getting the configured feeds
          List<NewsFeed> freshFeeds = new List<NewsFeed>();
          foreach (var url in GetConfiguredFeedUrls())
          {
            try
            {
              freshFeeds.Add(SyndicationFeedReader.ReadFeed(url));
            }
            catch (Exception error)
            {
              ServiceRegistration.Get<ILogger>().Warn("Error reading News Feed Data from '{0}'", error, url);
            }
          }
          lock (_feeds)
          {
            _feeds.Clear();
            _feeds.AddRange(freshFeeds);
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("Error refreshing News Data", ex);
        }
        finally
        {
          _refeshInProgress = false;
          ServiceRegistration.Get<ILogger>().Info("Finished refeshing News Feeds ...");
          var refeshFinishedEvent = RefeshFinished;
          if (refeshFinishedEvent != null) refeshFinishedEvent(this);
        }
        Monitor.Exit(_refreshingSyncObj); // release the exclusive lock
      }
      else
      {
        // we couldn't get the lock on the refreshingSyncObj, so set the flag for a forced refresh
        _forceRefresh = true;
      }
      // if the flag for a forced refresh is set, call this method again (we came here either because the refreshing finished or a lock couldn't be aquired)
      if (_forceRefresh)
        RefreshFeeds();
    }
  }
}
