using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  class SomeEntry : Entry
  {

    public SomeEntry()
    {
      SampleSettings settings = new SampleSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      base._value = settings.Entry;
    }

    public override void Save()
    {
      SampleSettings settings = new SampleSettings();
      ISettingsManager manager = ServiceScope.Get<ISettingsManager>();
      manager.Load(settings);
      if (settings.Entry == base._value)
        return;
      settings.Entry = base._value;
      manager.Save(settings);
    }

  }
}
