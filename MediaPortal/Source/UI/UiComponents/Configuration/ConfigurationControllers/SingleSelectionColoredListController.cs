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
using MediaPortal.Common.Configuration.ConfigurationClasses;
using MediaPortal.Common.General;
using MediaPortal.UI.Presentation.DataObjects;
using System.Drawing;
using MediaPortal.Common.Configuration;

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  /// <summary>
  /// Configuration controller for the <see cref="SingleSelectionList"/> configuration setting.
  /// </summary>
  public class SingleSelectionColoredListController : SelectionListController
  {
    #region Constants

    public const string KEY_NAME = "Name";
    public const string KEY_COLOR = "Color";

    #endregion

    #region Protected fields

    protected int _selectedIndex = -1;
    protected AbstractProperty _isSelectionValidProperty = new WProperty(typeof(bool), false);

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
      IsSelectionValid = _selectedIndex >= 0;
    }

    protected override void SettingChanged()
    {
      SingleSelectionColoredList ssl = (SingleSelectionColoredList)_setting;
      _selectedIndex = ssl.Selected;
      IsSelectionValid = _selectedIndex >= 0;
      base.SettingChanged();
    }

    protected override void UpdateItemsList()
    {
      _items.Clear();
      if (_setting != null)
      {
        SingleSelectionColoredList ssl = (SingleSelectionColoredList)_setting;
        int current = 0;
        _selectedIndex = ssl.Selected;
        IsSelectionValid = _selectedIndex >= 0;
        foreach (ColoredSelectionItem item in ssl.Items)
        {
          ListItem listItem = new ListItem(KEY_NAME, item.ResourceString)
          {
            Selected = (current == _selectedIndex)
          };
          if (item.BackgroundColor != Color.Empty)
            listItem.SetLabel(KEY_COLOR, Common.Localization.LocalizationHelper.CreateStaticString(ColorTranslator.ToHtml(item.BackgroundColor)));
          else
            listItem.SetLabel(KEY_COLOR, Common.Localization.LocalizationHelper.CreateStaticString(""));
          listItem.SelectedProperty.Attach(OnSelectionChanged);
          _items.Add(listItem);
          current++;
        }
      }
      _items.FireChange();
    }

    protected override void UpdateSetting()
    {
      SingleSelectionColoredList ssl = (SingleSelectionColoredList)_setting;
      ssl.Selected = _selectedIndex;
      base.UpdateSetting();
    }

    public override Type ConfigSettingType
    {
      get { return typeof(SingleSelectionColoredList); }
    }

    public AbstractProperty IsSelectionValidProperty
    {
      get { return _isSelectionValidProperty; }
    }

    public bool IsSelectionValid
    {
      get { return (bool)_isSelectionValidProperty.GetValue(); }
      protected set { _isSelectionValidProperty.SetValue(value); }
    }

    protected override string DialogScreen
    {
      get { return "dialog_configuration_coloredsingleselectionlist"; }
    }
  }
}
