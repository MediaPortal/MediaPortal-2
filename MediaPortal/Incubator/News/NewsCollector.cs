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
    protected Random random = new Random();
    protected object refreshingSyncObj = new object();
    protected bool forceRefresh = false;
    protected NewsItem lastRandomNewsItem = null;
    protected List<NewsFeed> feeds = new List<NewsFeed>();
    protected bool refeshInProgress = false;
    IntervalWork work = null;

    public NewsCollector()
    {
      NewsSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NewsSettings>();
      work = new IntervalWork(RefreshFeeds, TimeSpan.FromMinutes(settings.RefreshInterval));
      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(work, true);
    }

    public void Dispose()
    {
      ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(work);
    }

    public NewsItem GetRandomNewsItem()
    {
      List<ListItem> items = null;
      lock (feeds)
      {
        items = feeds.SelectMany(f => f.Items).Where(i => i != lastRandomNewsItem).ToList();
      }
      if (items == null || items.Count == 0) return null;
      if (items.Count == 1) return (NewsItem)items.First();
      return (NewsItem)items[random.Next(items.Count)];
    }

    public List<NewsFeed> GetAllFeeds()
    {
      // lock the feed list and return a copy of the list, as the background refesh thread can modify our list
      lock (feeds)
      {
        return feeds.ToList(); 
      }
    }

    public bool IsRefeshing { get { return refeshInProgress; } }
    public event Action RefeshStarted;
    public event Action<INewsCollector> RefeshFinished;

    public void RefreshNow()
    {
      RefreshFeeds();
    }

    public void ChangeRefreshInterval(int minutes)
    {
      ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(work);
      work = new IntervalWork(RefreshFeeds, TimeSpan.FromMinutes(minutes));
      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(work, true);
    }

    List<string> GetConfiguredFeedUrls()
    {
      NewsSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NewsSettings>();
      lock (settings.FeedsList)
      {
        if (settings.FeedsList.Count == 0)
          return NewsSettings.GetDefaultRegionalFeeds().Select(f => f.Url).ToList();
        else
          return settings.FeedsList.Select(f => f.Url).ToList();
      }
    }

    void RefreshFeeds()
    {
      // if the flag for force refresh is set, some thread already came here while a refresh was in progress
      if (forceRefresh) return;

      // try to get an exclusive lock on the refreshingSyncObj
      if (Monitor.TryEnter(refreshingSyncObj))
      {
        try
        {
          refeshInProgress = true;
          ServiceRegistration.Get<ILogger>().Info("Started refeshing News Feeds ...");
          var refreshStartedEvent = RefeshStarted;
          if (refreshStartedEvent != null) refreshStartedEvent();
          forceRefresh = false; // reset the flag for a forced refresh as we are currently refreshing and next step is getting the configured feeds
          List<NewsFeed> freshFeeds = new List<NewsFeed>();
          foreach (var url in GetConfiguredFeedUrls())
          {
            try
            {
              freshFeeds.Add(SyndicationFeedReader.ReadFeed(url));
            }
            catch (Exception error)
            {
              ServiceRegistration.Get<ILogger>().Warn("Error reading News Feed Data from '{0}': {1}", url, error);
            }
          }
          lock (feeds)
          {
            feeds.Clear();
            feeds.AddRange(freshFeeds);
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("Error refreshing News Data: {0}", ex);
        }
        finally
        {
          refeshInProgress = false;
          ServiceRegistration.Get<ILogger>().Info("Finished refeshing News Feeds ...");
          var refeshFinishedEvent = RefeshFinished;
          if (refeshFinishedEvent != null) refeshFinishedEvent(this);
        }
        Monitor.Exit(refreshingSyncObj); // release the exclusive lock
      }
      else
      {
        // we couldn't get the lock on the refreshingSyncObj, so set the flag for a forced refresh
        forceRefresh = true;
      }
      // if the flag for a forced refresh is set, call this method again (we came here either because the refreshing finished or a lock couldn't be aquired)
      if (forceRefresh) RefreshFeeds();
    }
  }
}
