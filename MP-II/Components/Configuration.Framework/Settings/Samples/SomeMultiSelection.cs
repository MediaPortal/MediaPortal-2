using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Core;
using MediaPortal.Presentation.Localisation;
using MediaPortal.Core.Settings;


namespace MediaPortal.Configuration.Settings
{
  public class SomeMultiSelection : MultipleSelectionList
  {
    public SomeMultiSelection()
    {
      base._items = new List<StringId>(5);
      base._items.Add(new StringId("First item"));
      base._items.Add(new StringId("Second item"));
      base._items.Add(new StringId("Thirth item"));
      base._items.Add(new StringId("Fourth item"));
      base._items.Add(new StringId("Fifth item"));
      SampleSettings settings = new SampleSettings();
      ServiceScope.Get<ISettingsManager>().Load(settings);
      base._selected = new List<int>(settings.MultiSelection);
    }

    public override void Save()
    {
      SampleSettings settings = new SampleSettings();
      ISettingsManager manager = ServiceScope.Get<ISettingsManager>();
      manager.Load(settings);
      if (settings.MultiSelection.Length == base._selected.Count)
      {
        bool changed = false;
        for (int i = 0; i < settings.MultiSelection.Length; i++)
        {
          if (!base._selected.Contains(settings.MultiSelection[i]))
          {
            changed = true;
            break;
          }
        }
        if (!changed) return;
      }
      settings.MultiSelection = base._selected.ToArray();
      manager.Save(settings);
    }

  }
}
