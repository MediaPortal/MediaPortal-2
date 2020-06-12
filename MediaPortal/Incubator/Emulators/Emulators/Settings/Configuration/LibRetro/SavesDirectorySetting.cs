using Emulators.LibRetro.Settings;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulators.Settings.Configuration.LibRetro
{
  public class SavesDirectorySetting : PathEntry
  {
    public override void Load()
    {
      _pathSelectionType = PathSelectionType.Folder;
      _path = SettingsManager.Load<LibRetroSettings>().SavesDirectory;
    }

    public override void Save()
    {
      LibRetroSettings settings = SettingsManager.Load<LibRetroSettings>();
      settings.SavesDirectory = _path;
      SettingsManager.Save(settings);
    }
  }
}
