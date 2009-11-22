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

using System.Collections;
using System.Collections.Generic;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Commands
{
  /// <summary>
  /// Represents a list of <see cref="IExecutableCommand"/> instances to be executed in
  /// order. This class itself implements the <see cref="IExecutableCommand"/> interface, hence it
  /// can be executed as a whole.
  /// </summary>
  public class CommandList : DependencyObject, IAddChild<IExecutableCommand>,
      IEnumerable<IExecutableCommand>, IExecutableCommand
  {
    #region Protected fields

    protected IList<IExecutableCommand> _commands = new List<IExecutableCommand>();

    #endregion

    #region Ctor

    public CommandList() { }

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandList cl = (CommandList) source;
      foreach (IExecutableCommand cmd in cl._commands)
        _commands.Add(copyManager.GetCopy(cmd));
    }

    #endregion

    #region IExecutableCommand implementation

    public void Execute()
    {
      foreach (IExecutableCommand cmd in _commands)
        cmd.Execute();
    }

    #endregion

    #region IAddChild Members

    public void AddChild(IExecutableCommand o)
    {
      _commands.Add(o);
    }

    #endregion

    #region IEnumerable<IExecutableCommand> implementation

    IEnumerator<IExecutableCommand> IEnumerable<IExecutableCommand>.GetEnumerator()
    {
      return _commands.GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    public IEnumerator GetEnumerator()
    {
      return ((IEnumerable<IExecutableCommand>) this).GetEnumerator();
    }

    #endregion
  }
}
