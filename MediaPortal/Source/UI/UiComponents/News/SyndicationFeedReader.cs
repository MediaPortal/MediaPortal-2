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
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.UiComponents.News.Models;

namespace MediaPortal.UiComponents.News
{
  public static class SyndicationFeedReader
  {
    static SyndicationFeedReader()
    {
      // Enable newer Tls versions (.NET > 4.5 uses Ssl3 and Tls)
      try
      {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Warn("Error setting supported SecurityProtocolTypes", ex);
      }
    }

    public static NewsFeed ReadFeed(string feedUrl)
    {
      SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create(feedUrl));
      var newFeed = new NewsFeed
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

    static string GetCachedOrDownloadImage(Uri url)
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

    static NewsItem TransformItem(SyndicationItem item, NewsFeed targetFeed)
    {
      var newItem = new NewsItem
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

    static string GetItemId(SyndicationItem item)
    {
      if (!string.IsNullOrWhiteSpace(item.Id))
        return item.Id;

      var link = item.Links.FirstOrDefault(l => l.Uri != null && l.MediaType == null);
      return link != null ? link.Uri.ToString() : string.Empty;
    }

    static string GetItemSummary(SyndicationItem item)
    {
      // First try extended content
      string result = GetItemContentFromExtension(item);
      if (!string.IsNullOrWhiteSpace(result))
        return PlainTextFromHtml(result);

      // Then check if a content is set
      if (item.Content != null)
      {
        var textContent = item.Content as TextSyndicationContent;
        if (textContent != null)
          return PlainTextFromHtml(textContent.Text);
      }

      // Use the summary
      if (item.Summary != null)
        return PlainTextFromHtml(item.Summary.Text);

      return string.Empty;
    }

    static string GetItemThumb(SyndicationItem item)
    {
      // Check all links
      var link = item.Links.FirstOrDefault(l => l.Uri != null && l.MediaType != null && l.MediaType.StartsWith("image"));
      if (link != null)
        return GetCachedOrDownloadImage(link.Uri);

      // Check the Content
      if (item.Content != null)
      {
        var textContent = item.Content as TextSyndicationContent;
        if (textContent != null)
        {
          string image = GetImageFromText(textContent.Text);
          if (!string.IsNullOrWhiteSpace(image)) 
            return image;
        }
      }
      // Check extended content
      string result = GetImageFromText(GetItemContentFromExtension(item));
      if (!string.IsNullOrWhiteSpace(result)) return result;

      return string.Empty;
    }

    static string GetImageFromText(string text)
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
    static string GetItemContentFromExtension(SyndicationItem item)
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

    static string PlainTextFromHtml(string input)
    {
      string result = input;
      if (!string.IsNullOrEmpty(result))
      {
        // Decode HTML escape characters
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
