#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using MediaPortal.Core.Configuration.ConfigurationClasses;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.Core.Localization;

namespace UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="SingleSelectionList"/> configuration setting.
  /// </summary>
  public class SingleSelectionListController : SelectionListController
  {
    #region Constants

    public const string KEY_NAME = "Name";

    #endregion

    #region Protected fields

    protected int _selectedIndex = -1;

    #endregion

    protected int FindSelectedIndex()
    {
      int current = 0;
      foreach (ListItem item in _items)
      {
        if (item.Selected)
          return current;
        current++;
      }
      return -1;
    }

    protected void OnSelectionChanged(AbstractProperty property, object oldValue)
    {
      _selectedIndex = FindSelectedIndex();
    }

    protected override void SettingChanged()
    {
        SingleSelectionList ssl = (SingleSelectionList) _setting;
      _selectedIndex = ssl.Selected;
      base.SettingChanged();
    }

    protected override void UpdateItemsList()
    {
      _items.Clear();
      if (_setting != null)
      {
        SingleSelectionList ssl = (SingleSelectionList) _setting;
        int current = 0;
        _selectedIndex = ssl.Selected;
        foreach (IResourceString item in ssl.Items)
        {
          ListItem listItem = new ListItem(KEY_NAME, item)
            {
                Selected = (current == _selectedIndex)
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
      SingleSelectionList ssl = (SingleSelectionList) _setting;
      ssl.Selected = _selectedIndex;
      base.UpdateSetting();
    }

    public override Type ConfigSettingType
    {
      get { return typeof(SingleSelectionList); }
    }

    protected override string DialogScreen
    {
      get { return "dialog_configuration_singleselectionlist"; }
    }
  }
}
