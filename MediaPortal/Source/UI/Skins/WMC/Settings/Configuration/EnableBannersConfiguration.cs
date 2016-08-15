using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using SkinSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Settings.Configuration
{
  public class EnableBannersConfiguration : MultipleSelectionList, IDisposable
  {
    public EnableBannersConfiguration()
    {
      SkinChangeMonitor.Instance.RegisterConfiguration(WMCSkinSettings.SKIN_NAME, this);
      _items.Add(LocalizationHelper.CreateResourceString("[WMC.Configuration.EnableBanners.Movies]"));
      _items.Add(LocalizationHelper.CreateResourceString("[WMC.Configuration.EnableBanners.Series]"));
      _items.Add(LocalizationHelper.CreateResourceString("[WMC.Configuration.EnableBanners.Seasons]"));
    }

    public override void Load()
    {
      base.Load();
      var settings = SettingsManager.Load<WMCSkinSettings>();
      if (settings.EnableMovieGridBanners)
        _selected.Add(0);
      if (settings.EnableSeriesGridBanners)
        _selected.Add(1);
      if (settings.EnableSeasonGridBanners)
        _selected.Add(2);
    }

    public override void Save()
    {
      base.Save();
      var settings = SettingsManager.Load<WMCSkinSettings>();
      settings.EnableMovieGridBanners = _selected.Contains(0);
      settings.EnableSeriesGridBanners = _selected.Contains(1);
      settings.EnableSeasonGridBanners = _selected.Contains(2);
      SettingsManager.Save(settings);
    }

    public void Dispose()
    {
      SkinChangeMonitor.Instance.UnregisterConfiguration(WMCSkinSettings.SKIN_NAME, this);
    }
  }
}
