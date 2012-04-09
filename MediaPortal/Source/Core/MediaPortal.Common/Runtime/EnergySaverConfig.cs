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

using MediaPortal.Utilities.SystemAPI;

namespace MediaPortal.Common.Runtime
{
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
    /// All energy saver actions allowed.
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
  /// Allows control of the system's energy saver. Be careful: Methods of this class only affect the current thread!
  /// </summary>
  /// <remarks>
  /// Each thread can request its required maximum suspension level. The system won't advance the suspension further than the suspension
  /// level of all threads. If for example one thread sets suspension level <see cref="SuspendLevel.AvoidSuspend"/> and another thread sets
  /// suspension level <see cref="SuspendLevel.DisplayRequired"/>, the system won't switch off the display and won't automatically
  /// suspend/shut down the system. Only if no thread has set the suspension level <see cref="SuspendLevel.DisplayRequired"/> but there are
  /// threads which have <see cref="SuspendLevel.AvoidSuspend"/> set, the system might switch off the display but won't suspend/shut down
  /// the system.
  /// The Windows API function <c>SetThreadExecutionState</c> in <c>kernel32.dll</c> is used to provide that functionality.
  /// </remarks>
  public class EnergySaverConfig
  {
    public static void SetCurrentSuspendLevel(SuspendLevel level)
    {
      WindowsAPI.EXECUTION_STATE requestedState = 0;
      switch (level)
      {
        case SuspendLevel.AvoidSuspend:
          requestedState = WindowsAPI.EXECUTION_STATE.ES_SYSTEM_REQUIRED;
          break;
        case SuspendLevel.DisplayRequired:
          requestedState = WindowsAPI.EXECUTION_STATE.ES_DISPLAY_REQUIRED;
          break;
      }
      WindowsAPI.SetThreadExecutionState(WindowsAPI.EXECUTION_STATE.ES_CONTINUOUS | requestedState);
    }
  }
}