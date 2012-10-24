using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public readonly string[] SampleFeeds = new string[] 
    { 
      "http://www.team-mediaportal.com/rss-feeds",
      "http://www.spiegel.de/schlagzeilen/tops/index.rss", 
      "http://www.heise.de/newsticker/heise-atom.xml", 
      "http://feeds.betanews.com/bn" 
    };
  }
}
