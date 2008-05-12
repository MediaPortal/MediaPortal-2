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
using MediaPortal.Presentation.Collections;
using Presentation.SkinEngine;
using Presentation.SkinEngine.Controls.Bindings;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class Button : ContentControl
  {
    Property _isPressedProperty;

    Property _commandParameter;
    Command _command;
    Command _contextMenuCommand;
    Property _contextMenuCommandParameterProperty;

    #region ctor
    public Button()
    {
      Init();
    }

    public Button(Button b)
      : base(b)
    {
      Init();
      IsPressed = b.IsPressed;


      Command = b.Command;
      CommandParameter = b.CommandParameter;

      ContextMenuCommand = b.ContextMenuCommand;
      ContextMenuCommandParameter = b.ContextMenuCommandParameter;
      /*if (b.Style != null)
      {
        Style = b.Style;
        OnStyleChanged(StyleProperty);
      }*/
    }

    public override object Clone()
    {
      Button result = new Button(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _isPressedProperty = new Property(typeof(bool), false);
      _commandParameter = new Property(typeof(object), null);
      _command = null;
      _contextMenuCommandParameterProperty = new Property(typeof(object), null);
      _contextMenuCommand = null;
      Focusable = true;
    }

    #endregion

    #region Public properties
    /// <summary>
    /// Gets or sets a value indicating whether this uielement has focus.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this uielement has focus; otherwise, <c>false</c>.
    /// </value>
    public override bool HasFocus
    {
      get
      {
        return base.HasFocus;
      }
      set
      {
        base.HasFocus = value;
        if (value == false)
          IsPressed = false;
        //Trace.WriteLine(String.Format("{0} focus:{1}", Name, value));
      }
    }

    /// <summary>
    /// Gets or sets the is pressed.
    /// </summary>
    /// <value>The is pressed.</value>
    public Property IsPressedProperty
    {
      get
      {
        return _isPressedProperty;
      }
      set
      {
        _isPressedProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is pressed.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is pressed; otherwise, <c>false</c>.
    /// </value>
    public bool IsPressed
    {
      get
      {
        return (bool)_isPressedProperty.GetValue();
      }
      set
      {
        _isPressedProperty.SetValue(value);
      }
    }

    #region Command properties
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

    /// <summary>
    /// Gets or sets the context menu command.
    /// </summary>
    /// <value>The context menu command.</value>
    public Command ContextMenuCommand
    {
      get
      {
        return _contextMenuCommand;
      }
      set
      {
        _contextMenuCommand = value;
      }
    }

    /// <summary>
    /// Gets or sets the context menu command parameter property.
    /// </summary>
    /// <value>The context menu command parameter property.</value>
    public Property ContextMenuCommandParameterProperty
    {
      get
      {
        return _contextMenuCommandParameterProperty;
      }
      set
      {
        _contextMenuCommandParameterProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the context menu command parameter.
    /// </summary>
    /// <value>The context menu command parameter.</value>
    public object ContextMenuCommandParameter
    {
      get
      {
        return _contextMenuCommandParameterProperty.GetValue();
      }
      set
      {
        _contextMenuCommandParameterProperty.SetValue(value);
      }
    }

    #endregion
    #endregion
    
    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      if (!HasFocus) return;
      if (key == MediaPortal.Control.InputManager.Key.None) return;
      if (key == MediaPortal.Control.InputManager.Key.Enter)
      {
        IsPressed = true;
      }
      if (key == MediaPortal.Control.InputManager.Key.Enter)
      {
        if (Command != null)
        {
          if (CommandParameter != null)
            Command.Method.Invoke(Command.Object, new object[] { CommandParameter });
          else if (Command.Parameter != null)
            Command.Method.Invoke(Command.Object, new object[] { Command.Parameter });
          else
            Command.Method.Invoke(Command.Object, null);
        }
        if (Context is ListItem)
        {
          ListItem listItem = Context as ListItem;
          if (listItem.Command != null)
          {
            listItem.Command.Execute(listItem.CommandParameter);
          }
        }
      }

      if (key == MediaPortal.Control.InputManager.Key.ContextMenu)
      {
        if (ContextMenuCommand != null)
        {
          ContextMenuCommand.Method.Invoke(ContextMenuCommand.Object, new object[] { ContextMenuCommandParameter });
        }
      }

      UIElement cntl = FocusManager.PredictFocus(this, ref key);
      if (cntl != null)
      {
        cntl.HasFocus = true;
        key = MediaPortal.Control.InputManager.Key.None;
      }
    }
  }
}
