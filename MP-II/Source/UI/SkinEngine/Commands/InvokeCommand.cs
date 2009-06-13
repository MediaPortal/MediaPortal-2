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

using System.Collections.Generic;
using MediaPortal.Core.General;
using MediaPortal.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.SkinEngine.Commands
{
  /// <summary>
  /// <see cref="IExecutableCommand"/> implementation to execute an
  /// <see cref="ICommandStencil"/> with a list of actual parameters.
  /// </summary>
  public class InvokeCommand : DependencyObject, IExecutableCommand
  {
    #region Protected fields

    protected IList<object> _commandParameters = new List<object>();
    protected Property _commandStencilProperty = new Property(typeof(ICommandStencil), null);

    #endregion

    #region Ctor

    public InvokeCommand()
    { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      InvokeCommand ic = (InvokeCommand) source;
      CommandStencil = copyManager.GetCopy(ic.CommandStencil);
      foreach (object o in ic._commandParameters)
        _commandParameters.Add(copyManager.GetCopy(o));
    }

    #endregion

    #region Public properties

    public Property CommandStencilProperty
    {
      get { return _commandStencilProperty; }
    }

    public ICommandStencil CommandStencil
    {
      get { return (ICommandStencil) _commandStencilProperty.GetValue(); }
      set { _commandStencilProperty.SetValue(value); }
    }

    public IList<object> CommandParameters
    {
      get { return _commandParameters; }
    }

    #endregion

    #region IExecutableCommand implementation

    public void Execute()
    {
      if (CommandStencil != null)
        CommandStencil.Execute(CommandParameters);
    }

    #endregion
  }
}
