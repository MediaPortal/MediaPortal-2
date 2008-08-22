using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  class SomeMultiEntry : MultipleEntryList
  {

    public SomeMultiEntry()
    {
      SampleSettings settings = new SampleSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      List<string>  lines = new List<string>(settings.MultiEntry.Length);
      lines.AddRange(settings.MultiEntry);
      base._lines = lines;
    }

    public override void Save()
    {
      SampleSettings settings = new SampleSettings();
      ISettingsManager manager = ServiceScope.Get<ISettingsManager>();
      manager.Load(settings);
      settings.MultiEntry = new string[base._lines.Count];
      for (int i = 0; i < settings.MultiEntry.Length; i++)
        settings.MultiEntry[i] = base._lines[i];
      manager.Save(settings);
    }

  }
}
