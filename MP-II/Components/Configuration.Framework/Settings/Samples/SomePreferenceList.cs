using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  public class SomePreferenceList : PreferenceList
  {

    #region Constructors

    public SomePreferenceList()
    {
      base.SetSettingsObject(new SampleSettings());
    }

    #endregion

    #region Public Methods

    public override void Load(object settingsObject)
    {
      base._items = new List<StringId>(5);
      base._items.Add(new StringId("First item"));
      base._items.Add(new StringId("Second item"));
      base._items.Add(new StringId("Thirth item"));
      base._items.Add(new StringId("Fourth item"));
      base._items.Add(new StringId("Fifth item"));
      SampleSettings settings = (SampleSettings)settingsObject;
      base._ranking = new List<int>(settings.PreferenceList.Length);
      foreach (int i in settings.PreferenceList)
        base._ranking.Add(i);
      base.InitializeBase();
    }

    public override void Save(object settingsObject)
    {
      SampleSettings settings = (SampleSettings)settingsObject;
      settings.PreferenceList = new int[base._ranking.Count];
      for (int i = 0; i < settings.PreferenceList.Length; i++)
        settings.PreferenceList[i] = base._ranking[i];
    }

    #endregion

  }
}
