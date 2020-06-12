using MediaPortal.Common.Configuration.ConfigurationClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Settings.Configuration
{
  public class MinimiseOnStart : YesNo
  {
    public override void Load()
    {
      _yes = SettingsManager.Load<EmulatorsSettings>().MinimiseOnGameStart;
    }

    public override void Save()
    {
      base.Save();
      EmulatorsSettings settings = SettingsManager.Load<EmulatorsSettings>();
      settings.MinimiseOnGameStart = _yes;
      SettingsManager.Save(settings);
    }
  }
}