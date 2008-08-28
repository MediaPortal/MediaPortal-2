#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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

using System;
using System.Collections.Generic;

namespace MediaPortal.Presentation.Screen
{
  public enum ScreenMode
  {
    NormalWindowed,
    FullScreenWindowed,
    ExclusiveMode
  };

  public enum FPS
  {
    None, // Used setting ScreenMode to NormalWindowed or NormalWindowed
    FPS_24, // Used for 24 FPS film material in ExclusiveMode
    FPS_25, // Used for 25 FPS film material in ExclusiveMode
    FPS_30, // Used for 30 FPS film material in ExclusiveMode
    Default, // Used for unknow FPS film material in ExclusiveMode
    Desktop // Used for GUI in ExclusiveMode
  };

  public interface IScreenControl
  {
    /// <summary>
    /// Switches between diffrent sceen modes.
    /// </summary>
    /// <param name="mode">The requested mode</param>
    /// <param name="fps">FPS of material to show, only valid when mode is ExclusiveMode</param>
    void SwitchMode(ScreenMode mode, FPS fps);

    /// <summary>
    /// returns if application is fullscreen mode or in windowed mode
    /// </summary>
    /// <value>
    /// 	<c>true</c> if this instance is fullscreen mode; otherwise, <c>false</c>.
    /// </value>
    bool IsFullScreen { get; }

    /// <summary>
    /// set / get if refresh rate control is enabled
    /// </summary>
    /// <value>
    /// 	<c>true</c> if enabled; otherwise, <c>false</c>.
    /// </value>
    bool RefreshRateControlEnabled { get; set;}

    /// <summary>
    /// Returns available display modes
    /// </summary>
    IList<string> DisplayModes{ get; }

    /// <summary>
    /// Returns the window handle of the main window.
    /// </summary>
    IntPtr MainWindowHandle { get; }

    /// <summary>
    /// Sets the display mode for a give frame rate
    /// </summary>
    /// <param name="fps">The frame rate to set</param>
    /// <param name="displaymode">The display mode for the frame rate</param>
    void SetDisplayMode(FPS fps, string displaymode);

    /// <summary>
    /// Get the display mode for a give frame rate
    /// </summary>
    /// <param name="fps">The frame rate</param>
    string GetDisplayMode(FPS fps);
  }
}