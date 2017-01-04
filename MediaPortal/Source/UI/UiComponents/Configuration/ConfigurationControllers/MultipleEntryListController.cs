#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

namespace MediaPortal.UiComponents.Configuration.ConfigurationControllers
{
  //TODO: use IPAddressBindingsSetting.DisplayLength and AddressBindingsSetting.DisplayHeight

  /// <summary>
  /// Configuration controller for the <see cref="MultipleEntryList"/> configuration setting.
  /// </summary>
  public class MultipleEntryListController : SelectionListController
  {
    #region Protected fields

    protected AbstractProperty _valueProperty;
    protected AbstractProperty _isEntrySelectedProperty;
 
    #endregion

    public MultipleEntryListController()
    {
      _valueProperty = new WProperty(typeof(string), string.Empty);
      _valueProperty.Attach(OnValueChanged);
      _isEntrySelectedProperty = new WProperty(typeof(bool), false);
    }

    private void OnValueChanged(AbstractProperty property, object oldValue)
    {
      var selectedIndex = FindSelectedIndex();
      if (selectedIndex >= 0 && !String.Equals(Value, (string)oldValue))
      {
        var item = _items[selectedIndex];
        item.SetLabel(KEY_NAME, Value);
        item.FireChange();
      }
    }

    #region Constants

    public const string KEY_NAME = "Name";

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
      var selectedIndex = FindSelectedIndex();
      if (selectedIndex >= 0)
      {
        Value = _items[selectedIndex].Labels[KEY_NAME].ToString();
        IsEntrySelected = true;
      }
      else
      {
        Value = String.Empty;
        IsEntrySelected = false;
      }
    }

    protected override void UpdateItemsList()
    {
      _items.Clear();
      if (_setting != null)
      {
        var mel = (MultipleEntryList)_setting;
        if (mel.Lines != null)
        {
          foreach (var item in mel.Lines)
          {
            ListItem listItem = new ListItem(KEY_NAME, item);
            listItem.SelectedProperty.Attach(OnSelectionChanged);
            _items.Add(listItem);
          }
        }
      }
      if (_items.Count > 0)
      {
        _items[0].Selected = true;
      }
      else
      {
        Value = string.Empty;
        IsEntrySelected = false;
      }
      _items.FireChange();
    }

    protected override void UpdateSetting()
    {
      var lines = new List<string>();
      foreach (ListItem item in _items)
      {
        lines.Add(item.Labels[KEY_NAME].ToString());
      }
      var mel = (MultipleEntryList)_setting;
      mel.Lines = lines;

      base.UpdateSetting();
    }

    public override Type ConfigSettingType
    {
      get { return typeof(MultipleEntryList); }
    }

    protected override string DialogScreen
    {
      get { return "dialog_configuration_multipleentrylist"; }
    }

    /// <summary>
    /// Property object for the <see cref="Value"/> property
    /// </summary>
    public AbstractProperty ValueProperty
    {
      get { return _valueProperty; }
    }

    /// <summary>
    /// Property for binding the selected entry text to the UI
    /// </summary>
    public string Value
    {
      get { return (string)_valueProperty.GetValue(); }
      set { _valueProperty.SetValue(value); }
    }

    /// <summary>
    /// Property object for the <see cref="IsEntrySelected"/> property
    /// </summary>
    public AbstractProperty IsEntrySelectedProperty
    {
      get { return _isEntrySelectedProperty; }
    }

    /// <summary>
    /// Gets if any entry is currently selected
    /// </summary>
    public bool IsEntrySelected
    {
      get { return (bool)_isEntrySelectedProperty.GetValue(); }
      private set { _isEntrySelectedProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets if add/remove of entries is allowed
    /// </summary>
    public bool IsAddRemoveEnabled
    {
      //TODO: should be defined by the setting object
      get { return true; }
    }

    /// <summary>
    /// Gets if reordering of the entries is allowed
    /// </summary>
    public bool IsUpDownEnabled
    {
      //TODO: should be defined by the setting object
      get { return true; }
    }

    /// <summary>
    /// Adds a new empty entry
    /// </summary>
    public void Add()
    {
      ListItem listItem = new ListItem(KEY_NAME, String.Empty);
      listItem.SelectedProperty.Attach(OnSelectionChanged);
      _items.Add(listItem);
      listItem.Selected = true;
      _items.FireChange();
    }

    /// <summary>
    /// Removes the selected entry
    /// </summary>
    /// <remarks>If no entry is selected, nothing happens.</remarks>
    public void Remove()
    {
      int selectedIndex = FindSelectedIndex();
      if (selectedIndex >= 0)
      {
        _items.RemoveAt(selectedIndex);
        --selectedIndex;
        if (selectedIndex < 0 && _items.Count > 0)
        {
          selectedIndex = 0;
        }
        if (selectedIndex >= 0)
        {
          _items[selectedIndex].Selected = true;
        }
        else
        {
          Value = string.Empty;
          IsEntrySelected = false;
        }
        _items.FireChange();
      }
    }

    /// <summary>
    /// Moves the selected entry up by one.
    /// </summary>
    /// <remarks>If no or the 1st entry is selected, nothing happens.</remarks>
    public void Up()
    {
      int selectedIndex = FindSelectedIndex();
      if (selectedIndex > 0)
      {
        var item = _items[selectedIndex];
        _items.RemoveAt(selectedIndex);
        _items.Insert(selectedIndex - 1, item);
        _items.FireChange();
      }
    }

    /// <summary>
    /// Moves the selected entry down by one.
    /// </summary>
    /// <remarks>If no or the last entry is selected, nothing happens.</remarks>
    public void Down()
    {
      int selectedIndex = FindSelectedIndex();
      if (selectedIndex >= 0 && selectedIndex < _items.Count - 1)
      {
        var item = _items[selectedIndex];
        _items.RemoveAt(selectedIndex);
        _items.Insert(selectedIndex + 1, item);
        _items.FireChange();
      }
    }
  }
}
