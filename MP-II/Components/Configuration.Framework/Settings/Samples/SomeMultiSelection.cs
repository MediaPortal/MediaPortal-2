using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  public class SomeMultiSelection : MultipleSelectionList
  {

    #region Constructors

    public SomeMultiSelection()
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
      base._selected = new List<int>(((SampleSettings)settingsObject).MultiSelection);
    }

    public override void Save(object settingsObject)
    {
      ((SampleSettings)settingsObject).MultiSelection = base._selected.ToArray();
    }

    #endregion

  }
}
