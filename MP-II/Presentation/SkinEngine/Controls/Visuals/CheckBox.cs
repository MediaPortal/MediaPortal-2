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

using MediaPortal.Presentation.Properties;
using MediaPortal.Control.InputManager;
using Presentation.SkinEngine.Controls.Bindings;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class CheckBox : Button
  {
    Property _isCheckedProperty;
    Command _checkedCommand;
    Command _unCheckedCommand;
    public CheckBox()
    {
      Init();
    }

    public CheckBox(CheckBox box)
      :base(box)
    {
      Init();
      //IsChecked = box.IsChecked;
      Checked = Checked;
      Unchecked = Unchecked;
    }

    public override object Clone()
    {
      CheckBox result = new CheckBox(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _isCheckedProperty = new Property(typeof(bool), false);
      _isCheckedProperty.Attach(new PropertyChangedHandler(onPropChanged));
    }
    void onPropChanged(Property p)
    {
    }
    /// <summary>
    /// Gets or sets the is pressed property.
    /// </summary>
    /// <value>The is pressed property.</value>
    public Property IsCheckedProperty
    {
      get
      {
        return _isCheckedProperty;
      }
      set
      {
        _isCheckedProperty = value;
      }
    }
    public Command Checked
    {
      get
      {
        return _checkedCommand;
      }
      set
      {
        _checkedCommand = value;
      }
    }

    public Command Unchecked
    {
      get
      {
        return _unCheckedCommand;
      }
      set
      {
        _unCheckedCommand = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is pressed.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is pressed; otherwise, <c>false</c>.
    /// </value>
    public bool IsChecked
    {
      get
      {
        return (bool)_isCheckedProperty.GetValue();
      }
      set
      {
        _isCheckedProperty.SetValue(value);
      }
    }

    public override void OnKeyPressed(ref Key key)
    {
      if (!HasFocus) return;

      base.OnKeyPressed(ref key);
      if (key == MediaPortal.Control.InputManager.Key.Enter)
      {
        IsChecked = !IsChecked;
        key = MediaPortal.Control.InputManager.Key.None;
        if (IsChecked)
        {
          if (Checked != null)
          {
            Checked.Execute();
          }
        }
        else
        {
          if (Unchecked != null)
          {
            Unchecked.Execute();
          }
        }
        return;
      }
    }

  }
}
