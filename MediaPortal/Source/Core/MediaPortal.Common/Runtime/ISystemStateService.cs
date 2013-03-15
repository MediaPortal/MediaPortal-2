#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
    /// The system is being suspended. This enum is never set as current state; it is just used to inform the system about the
    /// suspending process.
    /// </summary>
    Suspending,

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
    /// <param name="force">
    /// If force is set to <c>true</c>, applications which requested to block the shutdown are ignored.
    /// This can cause applications to lose data. Therefore, force should be set to <c>true</c> only in an emergency.
    /// </param>
    void Shutdown(bool force = false);

    /// <summary>
    /// Restarts the PC.
    /// </summary>
    /// <param name="force">
    /// If force is set to <c>true</c>, applications which requested to block the restart are ignored.
    /// This can cause applications to lose data. Therefore, force should be set to <c>true</c> only in an emergency.
    /// </param>
    void Restart(bool force = false);

    /// <summary>
    /// Suspends the current Windows session to memory.
    /// </summary>
    void Suspend();

    /// <summary>
    /// Suspends the current Windows session to disk (Hibernate).
    /// </summary>
    void Hibernate();

    /// <summary>
    /// Logs the current user off.
    /// </summary>
    /// <param name="force">
    /// If force is set to <c>true</c>, applications which requested to block the logoff are ignored.
    /// This can cause applications to lose data. Therefore, force should be set to <c>true</c> only in an emergency.
    /// </param>
    void Logoff(bool force = false);
  }
}