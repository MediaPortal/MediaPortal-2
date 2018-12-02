#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Common.Localization;

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="MultipleSelectionList"/> configuration setting.
  /// </summary>
  public class MultiSelectionListController : SelectionListController
  {
    #region Constants

    public const string KEY_NAME = "Name";

    #endregion

    #region Protected fields

    protected IList<int> _selectedIndices = new List<int>();

    #endregion

    protected IList<int> FindSelectedIndices()
    {
      IList<int> result = new List<int>();
      int current = 0;
      foreach (ListItem item in _items)
      {
        if (item.Selected)
          result.Add(current);
        current++;
      }
      return result;
    }

    protected void OnSelectionChanged(AbstractProperty property, object oldValue)
    {
      _selectedIndices = FindSelectedIndices();
    }

    protected override void SettingChanged()
    {
        MultipleSelectionList msl = (MultipleSelectionList) _setting;
      _selectedIndices = msl.SelectedIndices;
      base.SettingChanged();
    }

    protected override void UpdateItemsList()
    {
      _items.Clear();
      if (_setting != null)
      {
        MultipleSelectionList msl = (MultipleSelectionList) _setting;
        int current = 0;
        foreach (IResourceString item in msl.Items)
        {
          ListItem listItem = new ListItem(KEY_NAME, item)
            {
                Selected = _selectedIndices.Contains(current)
            };
          listItem.SelectedProperty.Attach(OnSelectionChanged);
          _items.Add(listItem);
          current++;
        }
      }
      _items.FireChange();
    }

    protected override void UpdateSetting()
    {
      MultipleSelectionList msl = (MultipleSelectionList) _setting;
      msl.SelectedIndices = FindSelectedIndices();
      base.UpdateSetting();
    }

    public override Type ConfigSettingType
    {
      get { return typeof(MultipleSelectionList); }
    }

    protected override string DialogScreen
    {
      get { return "dialog_configuration_multiselectionlist"; }
    }
  }
}
