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

using MediaPortal.Presentation.DataObjects;
using MediaPortal.Control.InputManager;
using MediaPortal.SkinEngine.Commands;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Controls.Visuals
{
  public class CheckBox : Button
  {
    #region Private fields

    Property _isCheckedProperty;
    IExecutableCommand _checkedCommand;
    IExecutableCommand _unCheckedCommand;

    #endregion

    #region Ctor

    public CheckBox()
    {
      Init();
    }

    void Init()
    {
      _isCheckedProperty = new Property(typeof(bool), false);
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CheckBox cb = (CheckBox) source;
      IsChecked = copyManager.GetCopy(cb.IsChecked);
      Checked = copyManager.GetCopy(cb.Checked);
      Unchecked = copyManager.GetCopy(cb.Unchecked);
    }

    #endregion

    public Property IsCheckedProperty
    {
      get { return _isCheckedProperty; }
    }

    public bool IsChecked
    {
      get { return (bool) _isCheckedProperty.GetValue(); }
      set { _isCheckedProperty.SetValue(value); }
    }

    public IExecutableCommand Checked
    {
      get { return _checkedCommand; }
      set { _checkedCommand = value; }
    }

    public IExecutableCommand Unchecked
    {
      get { return _unCheckedCommand; }
      set { _unCheckedCommand = value; }
    }

    public override void OnKeyPreview(ref Key key)
    {
      bool checkedChanged = false;
      if (HasFocus && key == Key.Ok)
      {
        checkedChanged = true;
        IsChecked = !IsChecked; // First toggle the state, then execute the base handler
      }

      base.OnKeyPreview(ref key);
      if (checkedChanged)
      {
        key = Key.None;
        if (IsChecked)
        {
          if (Checked != null)
            Checked.Execute();
        }
        else
        {
          if (Unchecked != null)
            Unchecked.Execute();
        }
      }
    }
  }
}
