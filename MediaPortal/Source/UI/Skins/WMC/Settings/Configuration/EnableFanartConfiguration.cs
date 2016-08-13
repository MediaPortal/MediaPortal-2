using MediaPortal.Common.Configuration.ConfigurationClasses;
using SkinSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Settings.Configuration
{
  public class EnableFanartConfiguration : YesNo, IDisposable
  {
    public EnableFanartConfiguration()
    {
      SkinChangeMonitor.Instance.RegisterConfiguration(WMCSkinSettings.SKIN_NAME, this);
    }

    public override void Load()
    {
      base.Load();
      _yes = SettingsManager.Load<WMCSkinSettings>().EnableFanart;
    }

    public override void Save()
    {
      base.Save();
      var settings = SettingsManager.Load<WMCSkinSettings>();
      settings.EnableFanart = _yes;
      SettingsManager.Save(settings);
    }

    public void Dispose()
    {
      SkinChangeMonitor.Instance.UnregisterConfiguration(WMCSkinSettings.SKIN_NAME, this);
    }
  }
}