using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common;
using MediaPortal.Common.Localization;

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

    static readonly Dictionary<string, FeedBookmark[]> SampleFeeds = new Dictionary<string, FeedBookmark[]>()
    { 
      { "en", new FeedBookmark[] 
        { 
          new FeedBookmark() { Name = "MediaPortal", Url = "http://www.team-mediaportal.com/rss-feeds"},
          new FeedBookmark() { Name = "Cnet", Url = "http://feeds.feedburner.com/cnet/tcoc"},
          new FeedBookmark() { Name = "BetaNews", Url = "http://feeds.betanews.com/bn"}
        }
      },
      { "de", new FeedBookmark[] 
        { 
          new FeedBookmark() { Name = "MediaPortal", Url = "http://www.team-mediaportal.com/rss-feeds"},
          new FeedBookmark() { Name = "Spiegel", Url = "http://www.spiegel.de/schlagzeilen/tops/index.rss"}, 
          new FeedBookmark() { Name = "Heise", Url = "http://www.heise.de/newsticker/heise-atom.xml"}
        }
      }
    };

    public static FeedBookmark[] GetDefaultRegionalFeeds()
    {
      FeedBookmark[] result = null;
      var culture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      string langCode = culture.Name;
      int regionPartIndex = langCode.IndexOf("-");
      if (regionPartIndex > 0) langCode = langCode.Substring(0, regionPartIndex);
      if (!SampleFeeds.TryGetValue(langCode, out result))
        result = SampleFeeds["en"];
      return result;
    }
  }
}
