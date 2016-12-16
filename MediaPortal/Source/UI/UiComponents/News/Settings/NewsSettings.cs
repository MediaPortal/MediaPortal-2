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
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.News.Settings
{
  public class NewsSettings
  {
    internal const string DEFAULT_FEEDS_URL = "http://install.team-mediaportal.com/MP2/News/DefaultFeeds.xml";

    public NewsSettings()
    {
      FeedsList = new List<FeedBookmark>();
    }

    [Setting(SettingScope.User, HasDefault = false)]
    public List<FeedBookmark> FeedsList { get; set; }

    [Setting(SettingScope.User, 15)]
    public int RefreshInterval { get; set; }

    static Dictionary<string, List<FeedBookmark>> _defaultFeeds;

    /// <summary>
    /// Gets a default list of feeds for the current user's region, with a fall back to English.
    /// </summary>
    /// <returns></returns>
    public static List<FeedBookmark> GetDefaultRegionalFeeds()
    {
      if (_defaultFeeds == null)
      {
        try
        {
          // if the default feeds haven't been loaded yet, load xml from sever
          using (var client = new CompressionWebClient())
          {
            // use our special client that has a lower timeout and uses compression by default
            string defaultFeedsData = client.DownloadString(DEFAULT_FEEDS_URL);
            // deserialize feeds from xml file
            var serializer = new XmlSerializer(typeof(RegionalFeedBookmarksCollection));
            using (var reader = new StringReader(defaultFeedsData))
            {
              var loadedFeeds = (RegionalFeedBookmarksCollection) serializer.Deserialize(reader);
              _defaultFeeds = new Dictionary<string, List<FeedBookmark>>();
              foreach (var region in loadedFeeds)
                _defaultFeeds[region.RegionCode] = region.FeedBookmarks;
            }
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Warn("Unable to load default news feeds xml from server: {0}", ex.Message);
          // return an empty list, so next time this method is called it will try to download the default feeds again
          return new List<FeedBookmark>();
        }
      }
      // find the best matching list of feeds for the user's culture
      List<FeedBookmark> result;
      var culture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      // first try to get feeds for this language and region
      if (_defaultFeeds.TryGetValue(culture.Name, out result))
        return result.ToList();
      // then try to get feeds for this language
      if (_defaultFeeds.TryGetValue(culture.TwoLetterISOLanguageName, out result))
        return result.ToList();
      // fallback is always the generic english feeds
      return _defaultFeeds["en"].ToList();
    }
  }
}
