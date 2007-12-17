#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using SkinEngine;
using SkinEngine.Controls;
using MediaPortal.Core;
using MediaPortal.Core.Collections;
using MediaPortal.Core.Properties;
using SkinEngine;
using SkinEngine.Properties;

public class Scriptlet : IScriptProperty
{
  public Property Get(IControl control, string param)
  {
    Control c = control as Control;
    if (c == null)
      return new Property(false);
    Window window = (Window)c.Window;
    return new BooleanProperty(window, param);
  }

  public class BooleanProperty : Dependency
  {
    Property _property;
    Window _window;
    Control _control;
    string _controlName;
    public BooleanProperty(Window window, string name)
    {
      _window = window;
      _controlName = name;
      base.DependencyObject = window.ControlCountProperty;
      OnValueChanged(window.ControlCountProperty);
    }

    protected override void OnValueChanged(Property property)
    {
      if (_control == null)
      {
        _control = _window.GetControlByName(_controlName) as Control;
        if (_control == null)
        {
          SetValue(true);
          return;
        }
        _control.HasFocusProperty.Attach(OnValueChanged);
      }
      SetValue(!_control.HasFocus);
    }
  };

}
