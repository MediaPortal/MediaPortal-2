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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using MediaPortal.Presentation.Properties;
using SkinEngine.Controls.Visuals;
using SkinEngine.Controls.Visuals.Triggers;

namespace SkinEngine.Controls.Bindings
{
  public class InvokeCommand : TriggerAction, IBindingCollection
  {
    Property _commandParameter;
    Command _command;
    BindingCollection _bindings;
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

      foreach (Binding binding in c._bindings)
      {
        _bindings.Add((Binding)binding.Clone());
      }

    }
    public object Clone()
    {
      return new InvokeCommand(this);
    }

    void Init()
    {
      _commandParameter = new Property(null);
      _command = null;
      _bindings = new BindingCollection();
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
      InitializeBindings(element);
      if (Command != null)
      {
        Command.Execute(CommandParameter, false);
      }
    }

    #region IBindingCollection Members

    public void Add(Binding binding)
    {
      _bindings.Add(binding);
    }

    public virtual void InitializeBindings(UIElement element)
    {
      if (_bindings.Count == 0) return;
      foreach (Binding binding in _bindings)
      {
        binding.Initialize(this, element);
      }
    }

    public override  void Execute(UIElement element, Trigger trigger)
    {
      Execute(element);
    }

    #endregion
  }
}
