using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Services.Settings;

namespace MediaPortal.UI.Players.Video.Settings.Configuration
{
  public class SubtitlesEngine : SingleSelectionList
  {
    public override void Load()
    {

    }

    public override void Save()
    {
      base.Save();
      VideoSettings settings = SettingsManager.Load<VideoSettings>();
      
    }
  }
}
