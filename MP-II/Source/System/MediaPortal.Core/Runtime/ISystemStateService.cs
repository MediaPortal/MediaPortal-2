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

namespace MediaPortal.Core.Runtime
{
  public enum SystemState
  {
    /// <summary>
    /// Not in a defined state yet.
    /// </summary>
    None,

    /// <summary>
    /// The system is initializing. This means, all services and components will be configured and started.
    /// </summary>
    Initializing,

    /// <summary>
    /// All services and components have been started and the main application will start to process its window message loop.
    /// </summary>
    Started,

    /// <summary>
    /// The system received the signal to shut down. All services and components will shut down now.
    /// </summary>
    ShuttingDown,

    /// <summary>
    /// All services and components have been shut down and the system will exit now.
    /// </summary>
    Ending,
  }

  public interface ISystemStateService
  {
    SystemState CurrentState { get; }
  }
}