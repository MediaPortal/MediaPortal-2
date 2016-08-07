using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Settings.Configuration
{
  public class EnableWatchedFlagsConfiguration : MultipleSelectionList, IDisposable
  {
    public EnableWatchedFlagsConfiguration()
    {
      SkinChangeMonitor.Instance.RegisterConfiguration(this);
      _items.Add(LocalizationHelper.CreateResourceString("[WMC.Configuration.EnableWatchedFlags.ListView]"));
      _items.Add(LocalizationHelper.CreateResourceString("[WMC.Configuration.EnableWatchedFlags.GridView]"));
      _items.Add(LocalizationHelper.CreateResourceString("[WMC.Configuration.EnableWatchedFlags.CoverView]"));
    }

    public override void Load()
    {
      base.Load();
      var settings = SettingsManager.Load<WMCSkinSettings>();
      if (settings.EnableListWatchedFlags)
        _selected.Add(0);
      if (settings.EnableGridWatchedFlags)
        _selected.Add(1);
      if (settings.EnableCoverWatchedFlags)
        _selected.Add(2);
    }

    public override void Save()
    {
      base.Save();
      var settings = SettingsManager.Load<WMCSkinSettings>();
      settings.EnableListWatchedFlags = _selected.Contains(0);
      settings.EnableGridWatchedFlags = _selected.Contains(1);
      settings.EnableCoverWatchedFlags = _selected.Contains(2);
      SettingsManager.Save(settings);
    }

    public void Dispose()
    {
      SkinChangeMonitor.Instance.UnregisterConfiguration(this);
    }
  }
}
