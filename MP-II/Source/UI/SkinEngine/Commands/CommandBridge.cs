#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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

using MediaPortal.Core.Commands;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Commands
{
  /// <summary>
  /// Represents a bridge between the two command frameworks: the <see cref="IExecutableCommand"/> &
  /// <see cref="ICommandStencil"/> related classes and the <see cref="ICommand"/> related classes.
  /// Instances of this class implement <see cref="IExecutableCommand"/> and are able to call
  /// <see cref="ICommand"/> instances.
  /// </summary>
  public class CommandBridge : DependencyObject, IExecutableCommand
  {
    #region Protected fields

    protected ICommand _command;

    #endregion

    #region Ctor

    public CommandBridge() { }

    public CommandBridge(ICommand command)
    {
      _command = command;
    }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandBridge cb = (CommandBridge) source;
      _command = copyManager.GetCopy(cb._command);
    }

    #endregion

    #region Properties

    /// <summary>
    /// The command to be called by this <see cref="IExecutableCommand"/> implementation.
    /// This command will have to implement the MediaPortal Core <see cref="ICommand"/> interface.
    /// </summary>
    public ICommand Command
    {
      get { return _command; }
      set { _command = value; }
    }

    #endregion

    #region IExecutableCommand implementation

    public void Execute()
    {
      if (_command == null)
        return;
      _command.Execute();
    }

    #endregion
  }
}
