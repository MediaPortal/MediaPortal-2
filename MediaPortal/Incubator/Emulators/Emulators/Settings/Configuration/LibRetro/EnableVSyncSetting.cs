using Emulators.LibRetro.Render;
using Emulators.LibRetro.Settings;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Settings.Configuration.LibRetro
{
  public class EnableVSyncSetting : YesNo
  {
    public override void Load()
    {
      _yes = SettingsManager.Load<LibRetroSettings>().SynchronizationType == SynchronizationType.VSync;
    }

    public override void Save()
    {
      LibRetroSettings settings = SettingsManager.Load<LibRetroSettings>();
      settings.SynchronizationType = _yes ? SynchronizationType.VSync : SynchronizationType.Audio;
      SettingsManager.Save(settings);
    }
  }
}
