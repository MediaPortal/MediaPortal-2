using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  public class SomePath : Path
  {

    #region Constructors

    public SomePath()
    {
      base.SetSettingsObject(new SampleSettings());
    }

    #endregion

    #region Public Methods

    public override void Load(object settingsObject)
    {
      base._path = ((SampleSettings)settingsObject).Path;
    }

    public override void Save(object settingsObject)
    {
      ((SampleSettings)settingsObject).Path = base._path;
    }

    #endregion

  }
}
