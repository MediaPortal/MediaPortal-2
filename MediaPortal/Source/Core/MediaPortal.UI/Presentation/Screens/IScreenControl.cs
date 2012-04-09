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

using System;
using MediaPortal.Common.Runtime;

namespace MediaPortal.UI.Presentation.Screens
{
  public enum ScreenMode
  {
    NormalWindowed,
    FullScreen
  };

  public interface IScreenControl
  {
    /// <summary>
    /// Returns the information if the MediaPortal 2 internal screen saver is currently active.
    /// </summary>
    bool IsScreenSaverActive { get; }

    /// <summary>
    /// Returns the information if the MediaPortal 2 internal screen saver is enabled.
    /// </summary>
    bool IsScreenSaverEnabled { get; }

    /// <summary>
    /// Returns the timeout minutes which is bided after the last user input before the MediaPortal 2 internal screen saver becomes active.
    /// </summary>
    double ScreenSaverTimeoutMin { get; }

    /// <summary>
    /// Returns the information whether the application is in fullscreen mode or in windowed mode.
    /// </summary>
    /// <value>
    /// <c>true</c> if this instance is fullscreen mode; otherwise, <c>false</c>.
    /// </value>
    bool IsFullScreen { get; }

    /// <summary>
    /// Returns the window handle of the main window.
    /// </summary>
    IntPtr MainWindowHandle { get; }

    /// <summary>
    /// Gets or sets the maximum energy saver level which may be set by Windows.
    /// </summary>
    SuspendLevel ApplicationSuspendLevel { get; set; }

    /// <summary>
    /// Enables or disables the MediaPortal 2 internal screen saver or sets its timeout.
    /// </summary>
    /// <param name="screenSaverEnabled">If set to <c>true</c>, the screen saver becomes active, else it becomes inactive.</param>
    /// <param name="screenSaverTimeoutMin">Sets the timeout in minutes before the screen saver becomes active.</param>
    void ConfigureScreenSaver(bool screenSaverEnabled, double screenSaverTimeoutMin);

    /// <summary>
    /// Shuts MediaPortal down.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Minimizes the MediaPortal application.
    /// </summary>
    void Minimize();

    /// <summary>
    /// Restores the window from minimized state.
    /// </summary>
    void Restore();

    /// <summary>
    /// Suspends the current Windows session to memory or to disc.
    /// </summary>
    void Suspend();

    /// <summary>
    /// Switches between windowed and fullscreen mode.
    /// </summary>
    /// <param name="mode">The requested mode.</param>
    void SwitchMode(ScreenMode mode);
  }
}
