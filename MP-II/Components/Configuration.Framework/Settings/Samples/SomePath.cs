using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  public class SomePath : Path
  {

    public SomePath()
    {
      SampleSettings settings = new SampleSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      base._path = settings.Path;
      base._pathType = PathType.FILE;
      //base._pathType = PathType.FOLDER;
    }

    public override void Save()
    {
      SampleSettings settings = new SampleSettings();
      ISettingsManager manager = ServiceScope.Get<ISettingsManager>();
      manager.Load(settings);
      if (settings.Path != base._path)
        return;
      settings.Path = base._path;
      manager.Save(settings);
    }

  }
}
