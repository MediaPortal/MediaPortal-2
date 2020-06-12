using Emulators.LibRetro.Settings;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Settings.Configuration.LibRetro
{
  public class AutoSaveSetting : YesNo
  {
    public override void Load()
    {
      _yes = SettingsManager.Load<LibRetroSettings>().AutoSave;
    }

    public override void Save()
    {
      LibRetroSettings settings = SettingsManager.Load<LibRetroSettings>();
      settings.AutoSave = _yes;
      SettingsManager.Save(settings);
    }
  }
}
