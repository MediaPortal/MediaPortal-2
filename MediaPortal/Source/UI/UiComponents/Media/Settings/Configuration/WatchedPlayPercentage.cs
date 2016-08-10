using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.UI.Services.Players.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Settings.Configuration
{
  public class WatchedPlayPercentage : LimitedNumberSelect
  {
    public override void Load()
    {
      base.Load();
      _lowerLimit = 0;
      _upperLimit = 100;
      _type = NumberType.Integer;
      _step = 1;
      _value = SettingsManager.Load<PlayerManagerSettings>().WatchedPlayPercentage;
    }

    public override void Save()
    {
      base.Save();
      PlayerManagerSettings settings = SettingsManager.Load<PlayerManagerSettings>();
      settings.WatchedPlayPercentage = (int)_value;
      SettingsManager.Save(settings);
    }
  }
}
