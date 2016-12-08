using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Settings.Configuration
{
  public class ShowVirtual : MultipleSelectionList
  {
    public ShowVirtual()
    {
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Media.ShowVirtual.Audio]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Media.ShowVirtual.Movie]"));
      _items.Add(LocalizationHelper.CreateResourceString("[Settings.Media.ShowVirtual.Series]"));
    }

    public override void Load()
    {
      base.Load();
      ViewSettings settings = SettingsManager.Load<ViewSettings>();
      if (settings.ShowVirtualAudioMedia)
        _selected.Add(0);
      if (settings.ShowVirtualMovieMedia)
        _selected.Add(1);
      if (settings.ShowVirtualSeriesMedia)
        _selected.Add(2);
    }

    public override void Save()
    {
      base.Save();
      ViewSettings settings = SettingsManager.Load<ViewSettings>();
      settings.ShowVirtualAudioMedia = _selected.Contains(0);
      settings.ShowVirtualMovieMedia = _selected.Contains(1);
      settings.ShowVirtualSeriesMedia = _selected.Contains(2);
      SettingsManager.Save(settings);
    }
  }
}
