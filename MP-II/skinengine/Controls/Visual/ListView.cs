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
using System.Reflection;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MediaPortal.Core.Properties;
using SkinEngine.Controls.Visuals.Styles;
using MediaPortal.Core.InputManager;

using SkinEngine;
using SkinEngine.Controls.Panels;
using SkinEngine.Controls.Bindings;

namespace SkinEngine.Controls.Visuals
{
  public class ListView : ItemsControl
  {
    Property _commandParameter;
    Command _command;
    Property _commands;
    Command _contextMenuCommand;
    Property _contextMenuCommandParameterProperty;
    Command _selectionChanged;

    #region ctor

    public ListView()
    {
      Init();
    }

    public ListView(ListView c)
      : base(c)
    {
      Init();

      Command = c.Command;
      CommandParameter = c._commandParameter;
      SelectionChanged = c.SelectionChanged;

      ContextMenuCommand = c.ContextMenuCommand;
      ContextMenuCommandParameter = c.ContextMenuCommandParameter;
      Commands = (CommandGroup)c.Commands.Clone();
    }

    public override object Clone()
    {
      return new ListView(this);
    }

    void Init()
    {
      _commandParameter = new Property(null);
      _commands = new Property(new CommandGroup());
      _command = null;
      _contextMenuCommandParameterProperty = new Property(null);
      _contextMenuCommand = null;
    }
    #endregion


    #region events
    public Command SelectionChanged
    {
      get
      {
        return _selectionChanged;
      }
      set
      {
        _selectionChanged = value;
      }
    }
    #endregion

    #region command properties
    public Property CommandsProperty
    {
      get
      {
        return _commands;
      }
      set
      {
        _commands = value;
      }
    }
    /// <summary>
    /// Gets or sets the command.s
    /// </summary>
    /// <value>The command.</value>
    public CommandGroup Commands
    {
      get
      {
        return _commands.GetValue() as CommandGroup;
      }
      set
      {
        _commands.SetValue(value);
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


    #region input handling
    /// <summary>
    /// Called when [mouse move].
    /// </summary>
    /// <param name="x">The x.</param>
    /// <param name="y">The y.</param>
    public override void OnMouseMove(float x, float y)
    {
      base.OnMouseMove(x, y);
      UpdateCurrentItem();
    }

    /// <summary>
    /// Handles keypresses
    /// </summary>
    /// <param name="key">The key.</param>
    public override void OnKeyPressed(ref Key key)
    {
      UpdateCurrentItem();
      bool executeCmd = (CurrentItem != null && key == MediaPortal.Core.InputManager.Key.Enter);
      bool executeContextCmd = (CurrentItem != null && key == MediaPortal.Core.InputManager.Key.ContextMenu);
      base.OnKeyPressed(ref key);

      if (executeCmd)
      {
        if (Command != null)
        {
          Command.Execute(CommandParameter, false);
        }
        Commands.Execute(this);
      }
      if (executeContextCmd)
      {
        if (ContextMenuCommand != null)
        {
          ContextMenuCommand.Execute(ContextMenuCommandParameter, false);

        }
      }
    }

    /// <summary>
    /// Updates the current item.
    /// </summary>
    void UpdateCurrentItem()
    {
      UIElement element = FindFocusedItem();
      if (element == null)
      {
        CurrentItem = null;
      }
      else
      {
        while (element.Context == null && element.VisualParent != null)
          element = element.VisualParent;
        CurrentItem = element.Context;
      }
      if (SelectionChanged != null)
      {
        SelectionChanged.Execute(CurrentItem, true);
      }

    }

    #endregion

  }
}
