#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class RadioButton : Button
  {
    #region Protected fields

    protected ICollection<RadioButton> _radioButtonGroup = null;
    protected Property _isCheckedProperty;
    protected Property _groupNameProperty;

    #endregion

    #region Ctor

    public RadioButton()
    {
      Init();
      Attach();
    }

    void Init()
    {
      _groupNameProperty = new Property(typeof(string), null);
      _isCheckedProperty = new Property(typeof(bool), false);
    }

    void Attach()
    {
      _isCheckedProperty.Attach(OnCheckChanged);
      _groupNameProperty.Attach(OnGroupNameChanged);
      _isPressedProperty.Attach(OnButtonPressed);
    }

    void Detach()
    {
      _isCheckedProperty.Detach(OnCheckChanged);
      _groupNameProperty.Detach(OnGroupNameChanged);
      _isPressedProperty.Detach(OnButtonPressed);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      Detach();
      base.DeepCopy(source, copyManager);
      RadioButton rb = (RadioButton) source;
      IsChecked = copyManager.GetCopy(rb.IsChecked);
      GroupName = copyManager.GetCopy(rb.GroupName);
      _radioButtonGroup = copyManager.GetCopy(rb._radioButtonGroup);
      Attach();
    }

    #endregion

    #region Private/protected methods

    void OnButtonPressed(Property property)
    {
      // Copy the "IsPressed" status to "IsChecked". This will automatically clear the
      // "IsChecked" status at other radio buttons.
      // This event will be triggered before the button executes its command.
      IsChecked = IsPressed;
    }

    void OnGroupNameChanged(Property property)
    {
      InitializeGroup();
    }

    void OnCheckChanged(Property property)
    {
      if (IsChecked)
      {
        if (_radioButtonGroup != null)
          foreach (RadioButton radioButton in _radioButtonGroup)
            if (!ReferenceEquals(this, radioButton))
              radioButton.IsChecked = false;
      }
    }

    protected void InitializeGroup()
    {
      if (_radioButtonGroup != null)
        _radioButtonGroup.Remove(this);
      _radioButtonGroup = null;
      if (!string.IsNullOrEmpty(GroupName))
      {
        INameScope ns = FindNameScope();
        if (ns == null)
          return;
        _radioButtonGroup = ns.FindName(GroupName) as ICollection<RadioButton>;
        if (_radioButtonGroup == null)
        {
          _radioButtonGroup = new List<RadioButton>();
          ns.RegisterName(GroupName, _radioButtonGroup);
        }
        _radioButtonGroup.Add(this);
      }
    }

    #endregion

    #region Public properties

    public Property GroupNameProperty
    {
      get { return _groupNameProperty; }
    }

    public string GroupName
    {
      get { return (string) _groupNameProperty.GetValue(); }
      set { _groupNameProperty.SetValue(value); }
    }

    public Property IsCheckedProperty
    {
      get { return _isCheckedProperty; }
    }

    public bool IsChecked
    {
      get { return (bool) _isCheckedProperty.GetValue(); }
      set { _isCheckedProperty.SetValue(value); }
    }

    #endregion

    #region Base overrides

    public override void FireEvent(string eventName)
    {
      if (eventName == LOADED_EVENT)
        InitializeGroup();
      base.FireEvent(eventName);
    }

    #endregion
  }
}
