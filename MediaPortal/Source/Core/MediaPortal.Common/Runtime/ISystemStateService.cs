#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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

namespace MediaPortal.Common.Runtime
{
  public enum SystemState
  {
    /// <summary>
    /// Not in a defined state yet.
    /// </summary>
    Starting,

    /// <summary>
    /// The system is initializing. All services and components are being configured and started.
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

    /// <summary>
    /// The system is being hibernated. This enum is never set as current state; it is just used to inform the system about the
    /// hibernation process.
    /// </summary>
    Hibernating,
  }

  public interface ISystemStateService
  {
    SystemState CurrentState { get; }

    /// <summary>
    /// Shuts the current Windows session down.
    /// </summary>
    void Shutdown(bool force = false);

    /// <summary>
    /// Restarts the PC.
    /// </summary>
    void Restart(bool force = false);

    /// <summary>
    /// Suspends the current Windows session to memory or to disc.
    /// </summary>
    void Suspend();

    /// <summary>
    /// Suspends the current Windows session to memory or to disc.
    /// Note: Currently the system does not distinguish between
    /// </summary>
    //void Hibernate();

    /// <summary>
    /// Logs the current user off.
    /// </summary>
    void Logoff(bool force = false);
  }
}