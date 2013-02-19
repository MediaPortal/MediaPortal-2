#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.News.Settings
{
  public class NewsSettings
  {
    public NewsSettings()
    {
      FeedsList = new List<FeedBookmark>();
    }

    [Setting(SettingScope.User, HasDefault=false)]
    public List<FeedBookmark> FeedsList { get; set; }

    [Setting(SettingScope.User, 15)]
    public int RefreshInterval { get; set; }

    static Dictionary<string, List<FeedBookmark>> DefaultFeeds;

    /// <summary>
    /// Gets a default list of feeds for the current user's region, with a fall back to English.
    /// </summary>
    /// <returns></returns>
    public static List<FeedBookmark> GetDefaultRegionalFeeds()
    {
      if (DefaultFeeds == null)
      {
        // if the default feeds haven't been loaded yet, deserialize them from xml file
        var path = Path.Combine(Path.GetDirectoryName(typeof(NewsSettings).Assembly.Location), "DefaultFeeds.xml");
        var serializer = new XmlSerializer(typeof(RegionalFeedBookmarksCollection));
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
          var loadedFeeds = serializer.Deserialize(fs) as RegionalFeedBookmarksCollection;
          DefaultFeeds = new Dictionary<string, List<FeedBookmark>>();
          foreach (var region in loadedFeeds)
            DefaultFeeds[region.RegionCode] = region.FeedBookmarks;
        }
      }
      // find the best matching list of feeds for the user's culture
      List<FeedBookmark> result = null;
      var culture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      // first try to get feeds for this language and region
      if (DefaultFeeds.TryGetValue(culture.Name, out result))
        return result.ToList();
      // then try to get feeds for this language
      if (DefaultFeeds.TryGetValue(culture.TwoLetterISOLanguageName, out result))
        return result.ToList();
      // fallback is always the generic english feeds
      return DefaultFeeds["en"].ToList();
    }
  }
}
