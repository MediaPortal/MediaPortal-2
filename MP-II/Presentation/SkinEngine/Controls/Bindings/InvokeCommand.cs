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
using Presentation.SkinEngine.Controls.Visuals;
using Presentation.SkinEngine.Controls.Visuals.Triggers;
using Presentation.SkinEngine.MarkupExtensions;

namespace Presentation.SkinEngine.Controls.Bindings
{

  public class InvokeCommand : TriggerAction
  {
    Property _commandParameter;
    Command _command;
    public InvokeCommand()
    {
      Init();
    }

    public InvokeCommand(InvokeCommand c)
      : base(c)
    {
      Init();
      Command = c.Command;
      CommandParameter = c._commandParameter;
    }

    public override object Clone()
    {
      InvokeCommand result = new InvokeCommand(this);
      BindingMarkupExtension.CopyBindings(this, result);
      return result;
    }

    void Init()
    {
      _commandParameter = new Property(typeof(object), null);
      _command = null;
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

    public void Execute(UIElement element)
    {
      if (Command != null)
      {
        Command.Execute(CommandParameter, false);
      }
    }

    public override  void Execute(UIElement element, Trigger trigger)
    {
      Execute(element);
    }
  }
}
