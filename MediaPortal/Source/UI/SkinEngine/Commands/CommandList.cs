#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Markup;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.UI.SkinEngine.MpfElements.Resources;
using MediaPortal.UI.SkinEngine.Xaml;
using MediaPortal.UI.SkinEngine.Xaml.Interfaces;
using MediaPortal.Utilities.DeepCopy;

namespace MediaPortal.UI.SkinEngine.Commands
{
  /// <summary>
  /// Represents a list of <see cref="IExecutableCommand"/> instances to be executed in
  /// order. This class itself implements the <see cref="IExecutableCommand"/> interface, hence it
  /// can be executed as a whole.
  /// </summary>
  [ContentProperty("Commands")]
  public class CommandList : DependencyObject, IAddChild<object>, IEnumerable<object>, IExecutableCommand
  {
    #region Protected fields

    protected List<object> _commands = new List<object>();

    #endregion

    #region Properties

    public List<object> Commands { get { return _commands; } }

    #endregion

    #region Ctor

    public override void DeepCopy(IDeepCopyable source, ICopyManager copyManager)
    {
      base.DeepCopy(source, copyManager);
      CommandList cl = (CommandList) source;
      foreach (object cmd in cl._commands)
        _commands.Add(copyManager.GetCopy(cmd));
    }

    public override void Dispose()
    {
      foreach (object command in _commands)
        MPF.TryCleanupAndDispose(command);
      base.Dispose();
    }

    #endregion

    #region IExecutableCommand implementation

    public void Execute()
    {
      IList<object> convertedCommands = LateBoundValue.ConvertLateBoundValues(_commands);
      foreach (object objectCmd in convertedCommands)
      {
        object o;
        if (!TypeConverter.Convert(objectCmd, typeof(IExecutableCommand), out o))
          throw new ArgumentException(string.Format("CommandList: Command '{0}' cannot be converted to {1}", objectCmd, typeof(IExecutableCommand).Name));
        IExecutableCommand cmd = (IExecutableCommand) o;
        cmd.Execute();
        if (!ReferenceEquals(cmd, objectCmd))
          MPF.TryCleanupAndDispose(cmd);
      }
    }

    #endregion

    #region IAddChild Members

    public void AddChild(object o)
    {
      _commands.Add(o);
    }

    #endregion

    #region IEnumerable<IExecutableCommand> implementation

    IEnumerator<object> IEnumerable<object>.GetEnumerator()
    {
      return _commands.GetEnumerator();
    }

    #endregion

    #region IEnumerable implementation

    public IEnumerator GetEnumerator()
    {
      return ((IEnumerable<object>) this).GetEnumerator();
    }

    #endregion
  }
}
