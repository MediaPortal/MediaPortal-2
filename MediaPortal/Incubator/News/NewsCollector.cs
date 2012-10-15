using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Threading;
using MediaPortal.UiComponents.News.Models;

namespace MediaPortal.UiComponents.News
{
  class NewsCollector : INewsCollector
  {
    /// <summary>
    /// Update interval for refreshing news feeds from the internet.
    /// </summary>
    const long NEWSFEEDS_UPDATE_INTERVAL = 10 * 60 * 1000;

    protected readonly string[] SampleFeeds = new string[] 
    { 
      "http://www.team-mediaportal.com/rss-feeds",
      "http://www.spiegel.de/schlagzeilen/tops/index.rss", 
      "http://www.heise.de/newsticker/heise-atom.xml", 
      "http://feeds.betanews.com/bn" 
    };

    protected Random random = new Random();
    protected object syncObj = new object();
    protected DateTime lastWebRefresh = DateTime.MinValue;
    protected NewsItem lastRandomNewsItem = null;
    protected List<NewsFeed> feeds = new List<NewsFeed>();
    protected bool refeshInProgress = false;
    IntervalWork work = null;

    public NewsCollector()
    {
      work = new IntervalWork(RefreshFeeds, TimeSpan.FromMilliseconds(NEWSFEEDS_UPDATE_INTERVAL));
      ServiceRegistration.Get<IThreadPool>().AddIntervalWork(work, true);
    }

    public void Dispose()
    {
      ServiceRegistration.Get<IThreadPool>().RemoveIntervalWork(work);
    }

    public NewsItem GetRandomNewsItem()
    {
      var items = feeds.SelectMany(f => f.Items).Where(i => i != lastRandomNewsItem).ToList();
      if (items.Count == 0) return null;
      if (items.Count == 1) return (NewsItem)items.First();
      return (NewsItem)items[random.Next(items.Count)];
    }

    public List<NewsFeed> GetAllFeeds()
    {
      return feeds;
    }

    public bool IsRefeshing { get { return refeshInProgress; } }
    public event Action RefeshStarted;
    public event Action<INewsCollector> RefeshFinished;

    void RefreshFeeds()
    {
      if (refeshInProgress) return;
      lock (syncObj)
      {
        refeshInProgress = true;
        var refreshStartedEvent = RefeshStarted;
        if (refreshStartedEvent != null) refreshStartedEvent();
        try
        {
          List<NewsFeed> freshFeeds = new List<NewsFeed>();
          foreach (var url in SampleFeeds)
          {
            try
            {
              freshFeeds.Add(TransformFeed(ReadFeed(url)));
            }
            catch (Exception error)
            {
              ServiceRegistration.Get<ILogger>().Warn("Error reading News Feed Data from '{0}': {1}", url, error);
            }
          }
          feeds.Clear();
          feeds.AddRange(freshFeeds);
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("Error refreshing News Data: {0}", ex);
        }
        finally
        {
          refeshInProgress = false;
          var refeshFinishedEvent = RefeshFinished;
          if (refeshFinishedEvent != null) refeshFinishedEvent(this);
        }
      }
    }

    SyndicationFeed ReadFeed(string feedUrl)
    {
      return SyndicationFeed.Load(XmlReader.Create(feedUrl));
    }

    NewsFeed TransformFeed(SyndicationFeed feed)
    {
      var newFeed = new NewsFeed() 
      {
        Title = feed.Title != null ? feed.Title.Text : string.Empty,
        Description = feed.Description != null ? feed.Description.Text : string.Empty,
        LastUpdated = feed.LastUpdatedTime.LocalDateTime,
        Icon = GetCachedOrDownloadImage(feed.ImageUrl)
      };
      
      foreach (var item in feed.Items)
      {
        newFeed.Items.Add(TransformItem(item, newFeed));
      }
      return newFeed;
    }

    string GetCachedOrDownloadImage(Uri url)
    {
      if (url == null)
        return string.Empty;

      try
      {
        using (var md5 = new System.Security.Cryptography.MD5CryptoServiceProvider())
        {
          string iconUrlHash = BitConverter.ToString(md5.ComputeHash(UTF8Encoding.UTF8.GetBytes(url.ToString())));
          string thumbsPath = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\News\Thumbs");
          if (!System.IO.Directory.Exists(thumbsPath)) System.IO.Directory.CreateDirectory(thumbsPath);
          string originalExtension = System.IO.Path.GetExtension(url.LocalPath);
          string extension = originalExtension.ToLower() == ".gif" ? ".png" : originalExtension;
          string fileName = System.IO.Path.Combine(thumbsPath, iconUrlHash) + extension;
          if (!System.IO.File.Exists(fileName))
          {
            using (var client = new CompressionWebClient())
            {
              if (extension == originalExtension)
              {
                client.DownloadFile(url, fileName);
              }
              else
              {
                using (var imageData = client.OpenRead(url))
                {
                  System.Drawing.Image image = System.Drawing.Image.FromStream(imageData, true, true);
                  image.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                }
              }
            }
          }
          return fileName;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error downloading image for feed or item '{0}': {1}", url.ToString(), ex);
        return string.Empty;
      }
    }

    NewsItem TransformItem(SyndicationItem item, NewsFeed targetFeed)
    {
      var newItem = new NewsItem()
      {
        Id = GetItemId(item),
        Feed = targetFeed,
        Title = item.Title != null ? item.Title.Text : string.Empty,
        PublishDate = item.PublishDate.LocalDateTime,
        Summary = GetItemSummary(item),
        Thumb = GetItemThumb(item)
      };
      return newItem;
    }

    string GetItemId(SyndicationItem item)
    {
      if (!string.IsNullOrWhiteSpace(item.Id))
      {
        return item.Id;
      }
      else
      {
        var link = item.Links.FirstOrDefault(l => l.Uri != null && l.MediaType == null);
        if (link != null)
          return link.Uri.ToString();
      }
      return string.Empty;
    }

    string GetItemSummary(SyndicationItem item)
    {
      // first try extended content
      string result = GetItemContentFromExtension(item);
      if (!string.IsNullOrWhiteSpace(result))
        return PlainTextFromHtml(result);

      // then check if a content is set
      if (item.Content != null)
      {
        var textContent = item.Content as TextSyndicationContent;
        if (textContent != null)
        {
          return PlainTextFromHtml(textContent.Text);
        }
      }

      // use the summary
      if (item.Summary != null)
        return PlainTextFromHtml(item.Summary.Text);

      return string.Empty;
    }

    string GetItemThumb(SyndicationItem item)
    {
      // check all links
      var link = item.Links.FirstOrDefault(l => l.Uri != null && l.MediaType != null && l.MediaType.StartsWith("image"));
      if (link != null)
      {
        return GetCachedOrDownloadImage(link.Uri);
      }
      // check the Content
      if (item.Content != null)
      {
        var textContent = item.Content as TextSyndicationContent;
        if (textContent != null)
        {
          string image = GetImageFromText(textContent.Text);
          if (!string.IsNullOrWhiteSpace(image)) return image;
        }
      }
      // check extended content
      string result = GetImageFromText(GetItemContentFromExtension(item));
      if (!string.IsNullOrWhiteSpace(result)) return result;

      return string.Empty;
    }

    string GetImageFromText(string text)
    {
      if (!string.IsNullOrWhiteSpace(text))
      {
        var match = Regex.Match(text, @"<img.*?src=""(?<src>[^""]+)""", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
        if (match.Success)
        {
          string src = match.Groups["src"].Value;
          if (!string.IsNullOrWhiteSpace(src))
          {
            Uri imgUrl;
            if (Uri.TryCreate(src, UriKind.Absolute, out imgUrl))
            {
              string result = GetCachedOrDownloadImage(imgUrl);
              if (!string.IsNullOrWhiteSpace(result))
              {
                return result;
              }
            }
          }
          match = match.NextMatch();
        }
      }
      return string.Empty;
    }

    /// <summary>
    /// Rss allows extensions. We are interested in one particular, the Content module.
    /// It allows to define the full content of an item with the encoded element.
    /// Namespace: http://purl.org/rss/1.0/modules/content/
    /// </summary>
    /// <returns></returns>
    string GetItemContentFromExtension(SyndicationItem item)
    {
      if (item.ElementExtensions != null)
      {
        var encoded = item.ElementExtensions.FirstOrDefault(e => e.OuterNamespace == "http://purl.org/rss/1.0/modules/content/" && e.OuterName == "encoded");
        if (encoded != null)
        {
          return encoded.GetReader().ReadElementContentAsString();
        }
      }
      return string.Empty;
    }

    public static string PlainTextFromHtml(string input)
    {
      string result = input;
      if (!string.IsNullOrEmpty(result))
      {
        // decode HTML escape characters
        result = System.Web.HttpUtility.HtmlDecode(result);

        // Replace &nbsp; with space
        result = Regex.Replace(result, @"&nbsp;", " ", RegexOptions.Multiline);

        // Remove double spaces
        result = Regex.Replace(result, @"  +", "", RegexOptions.Multiline);

        // Replace <br/> with \n
        result = Regex.Replace(result, @"< *br */*>", "\n", RegexOptions.IgnoreCase & RegexOptions.Multiline);

        // Remove remaining HTML tags                
        result = Regex.Replace(result, @"<[^>]*>", "", RegexOptions.Multiline);

        // Replace multiple newlines with just one
        result = Regex.Replace(result, @"(\r?\n){3,}", "\n", RegexOptions.IgnoreCase & RegexOptions.Multiline);

        // Remove whitespace at the beginning and end
        result = result.Trim();
      }
      return result;
    }
  }
}
