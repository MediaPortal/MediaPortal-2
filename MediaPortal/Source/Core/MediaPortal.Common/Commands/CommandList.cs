#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.Collections.Generic;
using MediaPortal.Common.Logging;

namespace MediaPortal.Common.Commands
{
  public class CommandList : ICommand
  {
    #region Protected fields

    protected IList<ICommand> _commands;

    #endregion

    public CommandList(IList<ICommand> commands)
    {
      _commands = commands;
    }

    #region ICommand implementation

    public void Execute()
    {
      int i = 0;
      ICommand currentCommand = null;
      try
      {
        foreach (ICommand command in _commands)
        {
          currentCommand = command;
          currentCommand.Execute();
          i++;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("CommandList: Error executing command {0}: {1}", ex, i, currentCommand);
      }
    }

    #endregion
  }
}