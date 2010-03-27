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

using System;

namespace MediaPortal.Utilities.Win32
{
  /// <summary>
  /// Contains Window Releated Win32 Methods
  /// </summary>
  [Obsolete("Will be replaced by a class in namespace MediaPortal.Utilities.System which can cope with all Windows systems")]
  public class Window
  {
    /// <summary>
    /// Shows the specified window
    /// </summary>
    /// <param name="ClassName"></param>
    /// <param name="WindowName"></param>
    /// <param name="bVisible"></param>
    public static void Show(string ClassName, string WindowName, bool bVisible)
    {
      uint i = Win32API.FindWindow(ref ClassName, ref WindowName);
      if (bVisible)
      {
        Win32API.ShowWindow(i, 5);
      }
      else
      {
        Win32API.ShowWindow(i, 0);
      }
    }

    /// <summary>
    /// Enables the Specified Window
    /// </summary>
    /// <param name="ClassName"></param>
    /// <param name="WindowName"></param>
    /// <param name="bEnable"></param>
    public static void Enable(string ClassName, string WindowName, bool bEnable)
    {
      uint i = Win32API.FindWindow(ref ClassName, ref WindowName);
      if (bEnable)
      {
        Win32API.EnableWindow(i, -1);
      }
      else
      {
        Win32API.EnableWindow(i, 0);
      }
    }

    /// <summary>
    /// Shows the Start Bar
    /// </summary>
    /// <param name="bVisible"></param>
    public static void ShowStartBar(bool bVisible)
    {
      try
      {
        Show("Shell_TrayWnd", "", bVisible);
      }
      catch (Exception) { }
    }

    /// <summary>
    /// Enables / Disables the Start Bar
    /// </summary>
    /// <param name="bEnable"></param>
    public static void EnableStartBar(bool bEnable)
    {
      try
      {
        Enable("Shell_TrayWnd", "", bEnable);
      }
      catch (Exception) { }
    }

    /// <summary> 
    /// Finds the specified window by its Process ID. Then brings it to 
    /// the foreground. 
    /// </summary> 
    /// <param name="_hWnd">Handle to the window to find and activate.</param> 
    public static void ActivateWindowByHandle(uint _hWnd)
    {
      Win32API.WindowPlacement windowPlacement;
      Win32API.GetWindowPlacement(_hWnd, out windowPlacement);

      switch (windowPlacement.showCmd)
      {
        case Win32API.SW_HIDE:           //Window is hidden
          Win32API.ShowWindow(_hWnd, Win32API.SW_RESTORE);
          break;
        case Win32API.SW_SHOWMINIMIZED:  //Window is minimized
          // if the window is minimized, then we need to restore it to its 
          // previous size. we also take into account whether it was 
          // previously maximized. 
          int showCmd = (windowPlacement.flags == Win32API.WPF_RESTORETOMAXIMIZED) ? Win32API.SW_SHOWMAXIMIZED : Win32API.SW_SHOWNORMAL;
          Win32API.ShowWindow(_hWnd, showCmd);
          break;
        default:
          // if it's not minimized, then we just call SetForegroundWindow to 
          // bring it to the front. 
          Win32API.SetForegroundWindow(_hWnd);
          break;
      }
    }
  }
}
