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

using System;
using System.Runtime.InteropServices;
using Hardcodet.Wpf.TaskbarNotification.Interop;

namespace MediaPortal.ServiceMonitor.Utilities
{
  /// <summary>
  /// Callback delegate which is used by the Windows API to submit window messages.
  /// </summary>
  public delegate long WindowProcedureHandler(IntPtr hwnd, uint uMsg, uint wparam, uint lparam);

  /// <summary>
  /// Win API WNDCLASS struct - represents a single window. Used to receive window messages.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  public struct WindowClass
  {
    public uint style;
    public WindowProcedureHandler lpfnWndProc;
    public int cbClsExtra;
    public int cbWndExtra;
    public IntPtr hInstance;
    public IntPtr hIcon;
    public IntPtr hCursor;
    public IntPtr hbrBackground;
    [MarshalAs(UnmanagedType.LPWStr)] public string lpszMenuName;
    [MarshalAs(UnmanagedType.LPWStr)] public string lpszClassName;
  }

  /// <summary>
  /// Methods and constants to communicate with the Windows API.
  /// </summary>
  internal static class WinApi
  {
    /// <summary>
    /// Creates, updates or deletes the taskbar icon.
    /// </summary>
    [DllImport("shell32.Dll")]
    public static extern bool Shell_NotifyIcon(NotifyCommand cmd, [In] ref NotifyIconData data);

    /// <summary>
    /// Processes a default windows procedure.
    /// </summary>
    [DllImport("user32.dll")]
    public static extern long DefWindowProc(IntPtr hWnd, uint msg, uint wparam, uint lparam);

    /// <summary>
    /// Registers the helper window class.
    /// </summary>
    [DllImport("user32.dll", EntryPoint = "RegisterClassW", SetLastError = true)]
    public static extern short RegisterClass(ref WindowClass lpWndClass);
  }
}