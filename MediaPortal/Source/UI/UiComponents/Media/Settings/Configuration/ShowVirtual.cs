using MediaPortal.Common.Configuration.ConfigurationClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Settings.Configuration
{
  public class ShowVirtual : YesNo
  {
    public override void Load()
    {
      base.Load();
      _yes = SettingsManager.Load<ViewSettings>().ShowVirtual;
    }

    public override void Save()
    {
      base.Save();
      ViewSettings settings = SettingsManager.Load<ViewSettings>();
      settings.ShowVirtual = _yes;
      SettingsManager.Save(settings);
    }
  }
}
