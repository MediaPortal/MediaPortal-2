using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  public class SomePreferenceList : PreferenceList
  {

    public SomePreferenceList()
    {
      base._items = new List<StringId>(5);
      base._items.Add(new StringId("First item"));
      base._items.Add(new StringId("Second item"));
      base._items.Add(new StringId("Thirth item"));
      base._items.Add(new StringId("Fourth item"));
      base._items.Add(new StringId("Fifth item"));
      SampleSettings settings = new SampleSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      base._ranking = new List<int>(settings.PreferenceList.Length);
      foreach (int i in settings.PreferenceList)
        base._ranking.Add(i);
      base.InitializeBase();
    }

    public override void Save()
    {
      SampleSettings settings = new SampleSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      bool save = true;
      if (settings.PreferenceList != null && settings.PreferenceList.Length == base._ranking.Count)
      {
        save = false;
        for (int i = 0; i < settings.PreferenceList.Length; i++)
        {
          if (settings.PreferenceList[i] != base._ranking[i])
          {
            save = true;  // we found a difference, so this setting needs to be saved
            break;
          }
        }
      }
      if (!save) return;
      settings.PreferenceList = new int[base._ranking.Count];
      for (int i = 0; i < settings.PreferenceList.Length; i++)
        settings.PreferenceList[i] = base._ranking[i];
      ServiceScope.Get<ISettingsManager>().Save(settings);
    }

  }
}
