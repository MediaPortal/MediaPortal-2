using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  class SomeEntry : Entry
  {

    #region Constructors

    public SomeEntry()
    {
      base.SetSettingsObject(new SampleSettings());
    }

    #endregion

    #region Public Methods

    public override void Load(object settingsObject)
    {
      base._value = ((SampleSettings)settingsObject).Entry;
    }

    public override void Save(object settingsObject)
    {
      ((SampleSettings)settingsObject).Entry = base._value;
    }

    #endregion

  }
}
