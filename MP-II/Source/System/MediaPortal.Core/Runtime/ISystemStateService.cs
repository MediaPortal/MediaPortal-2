#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

namespace MediaPortal.Core.Runtime
{
  public enum SystemState
  {
    /// <summary>
    /// Not in a defined state yet.
    /// </summary>
    None,

    /// <summary>
    /// The system is initializing. All services and components are configured and started.
    /// </summary>
    Initializing,

    /// <summary>
    /// All services and components have been started and the main application is processing its window message loop.
    /// </summary>
    Running,

    /// <summary>
    /// The system received the signal to shut down and all services and components are shutting down.
    /// </summary>
    ShuttingDown,

    /// <summary>
    /// All services and components have been shut down and the system is exiting.
    /// </summary>
    Ending,
  }

  public interface ISystemStateService
  {
    SystemState CurrentState { get; }
  }
}