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
using MediaPortal.Utilities.DeepCopy;

namespace Presentation.SkinEngine.Controls.Bindings
{
  public class InvokeCommand : TriggerAction
  {
    #region Private fields

    Property _commandParameter;
    Command _command;

    #endregion

    #region Ctor

    public InvokeCommand()
    {
      Init();
    }

    void Init()
    {
      _commandParameter = new Property(typeof(object), null);
      _command = null;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      InvokeCommand ic = source as InvokeCommand;
      Command = copyManager.GetCopy(ic.Command);
      CommandParameter = copyManager.GetCopy(ic.CommandParameter);
    }

    #endregion

    #region Public properties

    public Command Command
    {
      get { return _command; }
      set { _command = value; }
    }

    public Property CommandParameterProperty
    {
      get { return _commandParameter; }
    }

    public object CommandParameter
    {
      get { return _commandParameter.GetValue(); }
      set { _commandParameter.SetValue(value); }
    }

    public void Execute(UIElement element)
    {
      if (Command != null)
        Command.Execute(CommandParameter, false);
    }

    public override void Execute(UIElement element, Trigger trigger)
    {
      Execute(element);
    }

    #endregion
  }
}
