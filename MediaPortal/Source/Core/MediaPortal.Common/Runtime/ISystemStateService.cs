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

    /// <summary>
    /// The system is resumed from suspend mode. This enum is never set as current state; it is just used to inform the system about the
    /// resume process.
    /// </summary>
    Resuming,
  }

  /// <summary>
  /// Enumeration of automatic shutdown levels which can be disabled.
  /// </summary>
  /// <remarks>
  /// We order the energy saving levels of a computer in two successive steps. First, the screen is switched off, second, the
  /// computer is suspended/shut down. The application can prevent Windows from doing either of them. If the level
  /// <see cref="AvoidSuspend"/> is used, the automatic suspension is prevented but the display may be switched off. If
  /// the level <see cref="DisplayRequired"/> is used, both automatic suspension/shutdown and display switch off are prevented.
  /// Suspend level <see cref="None"/> is used to reset the suspend state.
  /// </remarks>
  public enum SuspendLevel
  {
    /// <summary>
    /// All energy saving levels allowed.
    /// </summary>
    None,

    /// <summary>
    /// Automatic suspend/shutdown is disabled, automatic display switch off is allowed.
    /// </summary>
    AvoidSuspend,

    /// <summary>
    /// Both automatic suspend/shutdown and automatic display switch off are allowed.
    /// </summary>
    DisplayRequired,
  }

  /// <summary>
  /// <see cref="ISystemStateService"/> provides control over Windows state, i.e. to shutdown or reboot the computer.
  /// Additionally the <see cref="SetCurrentSuspendLevel"/> method allows disabling some of the system's energy saving levels.
  /// Be careful: This methods only affects the current thread!
  /// </summary>
  public interface ISystemStateService
  {
    /// <summary>
    /// Gets the current <see cref="SystemState"/>.
    /// </summary>
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

    /// <summary>
    /// Sets the <see cref="SuspendLevel"/> for the current thread.
    /// </summary>
    /// <param name="level">The <see cref="SuspendLevel"/>, which should be set.</param>
    /// <param name="continuous">
    /// If continuous is set to <c>true</c>, the given <paramref name="level">SuspendLevel</paramref> is valid for the current thread,
    /// until it has been changed again.
    /// ATTENTION: This should be used only by the application's main thread.
    /// To reset the system's idle time only, continuous has to be set to <c>false</c>.
    /// </param>
    /// <remarks>
    /// Each thread can request its required maximum suspension level. The system won't advance the suspension further than the suspension
    /// level of all threads. If for example one thread sets suspension level <see cref="SuspendLevel.AvoidSuspend"/> and another thread sets
    /// suspension level <see cref="SuspendLevel.DisplayRequired"/>, the system won't switch off the display and won't automatically
    /// suspend/shut down the system. Only if no thread has set the suspension level <see cref="SuspendLevel.DisplayRequired"/> but there are
    /// threads which have <see cref="SuspendLevel.AvoidSuspend"/> set, the system might switch off the display but won't suspend/shut down
    /// the system.
    /// The Windows API function <c>SetThreadExecutionState</c> in <c>kernel32.dll</c> is used to provide that functionality.
    /// </remarks>
    void SetCurrentSuspendLevel(SuspendLevel level, bool continuous = false);
  }
}