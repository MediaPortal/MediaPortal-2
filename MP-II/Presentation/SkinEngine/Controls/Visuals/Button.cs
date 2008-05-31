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
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Visuals
{
  public class Button : ContentControl
  {
    #region Private fields

    Property _isPressedProperty;

    Property _commandParameter;
    Command _command;
    Command _contextMenuCommand;
    Property _contextMenuCommandParameterProperty;

    #endregion

    #region Ctor

    public Button()
    {
      Init();
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

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      Button b = source as Button;
      IsPressed = copyManager.GetCopy(b.IsPressed);
      Command = copyManager.GetCopy(b.Command);
      CommandParameter = copyManager.GetCopy(b.CommandParameter);
      ContextMenuCommand = copyManager.GetCopy(b.ContextMenuCommand);
      ContextMenuCommandParameter = copyManager.GetCopy(b.ContextMenuCommandParameter);
    }

    #endregion

    #region Public properties

    public override bool HasFocus
    {
      get { return base.HasFocus; }
      set
      {
        base.HasFocus = value;
        if (value == false)
          IsPressed = false;
        //Trace.WriteLine(String.Format("{0} focus:{1}", Name, value));
      }
    }

    public Property IsPressedProperty
    {
      get { return _isPressedProperty; }
    }

    public bool IsPressed
    {
      get { return (bool)_isPressedProperty.GetValue(); }
      set { _isPressedProperty.SetValue(value); }
    }

    #region Command properties

    public Command Command
    {
      get { return _command; }
      set { _command = value; }
    }

    public Property CommandParameterProperty
    {
      get { return _commandParameter; }
      set { _commandParameter = value; }
    }

    public object CommandParameter
    {
      get { return _commandParameter.GetValue(); }
      set { _commandParameter.SetValue(value); }
    }

    public Command ContextMenuCommand
    {
      get { return _contextMenuCommand; }
      set { _contextMenuCommand = value; }
    }

    public Property ContextMenuCommandParameterProperty
    {
      get { return _contextMenuCommandParameterProperty; }
    }

    public object ContextMenuCommandParameter
    {
      get { return _contextMenuCommandParameterProperty.GetValue(); }
      set { _contextMenuCommandParameterProperty.SetValue(value); }
    }

    #endregion

    #endregion
    
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
        // FIXME: Replace this with a TemplateBinding associating the Button's Command and parameter with the
        // ListItem's Command and parameter
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
