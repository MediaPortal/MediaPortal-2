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
using System.Windows.Input;
using System.Windows.Markup;

namespace MediaPortal.ServiceMonitor.Commands
{
  /// <summary>
  /// Basic implementation of the <see cref="ICommand"/>
  /// interface along with a few helper methods and
  /// properties.
  /// </summary>
  public abstract class CommandExtension<T> : MarkupExtension, ICommand where T : class, ICommand, new()
  {
    /// <summary>
    /// A singleton instance.
    /// </summary>
    private static T _command;

    /// <summary>
    /// Gets a shared command instance.
    /// </summary>
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
      return _command ?? (_command = new T());
    }

    /// <summary>
    /// Occurs when changes occur that affect whether
    /// or not the command should execute.
    /// </summary>
    public event EventHandler CanExecuteChanged
    {
      add { CommandManager.RequerySuggested += value; }
      remove { CommandManager.RequerySuggested -= value; }
    }

    /// <summary>
    /// Defines the method to be called when the command is invoked.
    /// </summary>
    /// <param name="parameter">Data used by the command.
    /// If the command does not require data to be passed,
    /// this object can be set to null.
    /// </param>
    public abstract void Execute(object parameter);

    /// <summary>
    /// Defines the method that determines whether the command
    /// can execute in its current state.
    /// </summary>
    /// <returns>
    /// This default implementation always returns true.
    /// </returns>
    /// <param name="parameter">Data used by the command.  
    /// If the command does not require data to be passed,
    /// this object can be set to null.
    /// </param>
    public virtual bool CanExecute(object parameter)
    {
      return true;
    }
  }
}