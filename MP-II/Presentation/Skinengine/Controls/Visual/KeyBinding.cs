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
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Presentation.Properties;
using Presentation.SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Control.InputManager;
using MediaPortal.Presentation.Collections;
using Presentation.SkinEngine;
using Presentation.SkinEngine.Controls.Bindings;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class KeyBinding : FrameworkElement
  {
    Property _commandParameter;
    Property _keyProperty;
    Command _command;
    public KeyBinding()
    {
      Init();
    }

    public KeyBinding(KeyBinding b)
      : base(b)
    {
      Init();

      Command = b.Command;
      CommandParameter = b.CommandParameter;
      KeyPress = b.KeyPress;
    }

    public override object Clone()
    {
      return new KeyBinding(this);
    }

    void Init()
    {
      _commandParameter = new Property(null);
      _command = null;
      _keyProperty = new Property("");
      Focusable = false;
    }

    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    /// <value>The key.</value>
    public string KeyPress
    {
      get
      {
        return _keyProperty.GetValue() as string;
      }
      set
      {
        _keyProperty.SetValue(value);
      }
    }

    /// <summary>
    /// Gets or sets the key property.
    /// </summary>
    /// <value>The key property.</value>
    public Property KeyPressProperty
    {
      get
      {
        return _keyProperty;
      }
      set
      {
        _keyProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the command.
    /// </summary>
    /// <value>The command.</value>
    public Command Command
    {
      get
      {
        return _command;
      }
      set
      {
        _command = value;
      }
    }
    /// <summary>
    /// Gets or sets the command parameter property.
    /// </summary>
    /// <value>The command parameter property.</value>
    public Property CommandParameterProperty
    {
      get
      {
        return _commandParameter;
      }
      set
      {
        _commandParameter = value;
      }
    }

    /// <summary>
    /// Gets or sets the control style.
    /// </summary>
    /// <value>The control style.</value>
    public object CommandParameter
    {
      get
      {
        return _commandParameter.GetValue();
      }
      set
      {
        _commandParameter.SetValue(value);
      }
    }
    public override void DoRender()
    {
    }
    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (key == MediaPortal.Control.InputManager.Key.None)
      {
        return;
      }
      if (key.ToString() == KeyPress)
      {
        if (Command != null)
        {
          if (CommandParameter != null)
            Command.Method.Invoke(Command.Object, new object[] { CommandParameter });
          else
            Command.Method.Invoke(Command.Object, null);
        }
      }
    }
  }
}
