using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  class SomeMultiEntry : MultipleEntryList
  {

    #region Constructors

    public SomeMultiEntry()
    {
      base.SetSettingsObject(new SampleSettings());
    }

    #endregion

    #region Public Methods

    public override void Load(object settingsObject)
    {
      SampleSettings settings = (SampleSettings)settingsObject;
      List<string> lines = new List<string>(settings.MultiEntry.Length);
      lines.AddRange(settings.MultiEntry);
      base._lines = lines;
    }

    public override void Save(object settingsObject)
    {
      SampleSettings settings = (SampleSettings)settingsObject;
      settings.MultiEntry = new string[base._lines.Count];
      for (int i = 0; i < settings.MultiEntry.Length; i++)
        settings.MultiEntry[i] = base._lines[i];
    }

    #endregion

  }
}
