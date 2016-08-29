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
  public class MpcHcSubtitleEngine : YesNo
  {
    public override void Load()
    {
      _yes = SettingsManager.Load<VideoSettings>().EnableMpcHcSubtitleEngine;
    }

    public override void Save()
    {
      base.Save();
      VideoSettings settings = SettingsManager.Load<VideoSettings>();
      settings.EnableMpcHcSubtitleEngine = _yes;
      SettingsManager.Save(settings);
    }
  }
}
