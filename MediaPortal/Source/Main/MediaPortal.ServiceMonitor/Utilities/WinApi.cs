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
    [MarshalAs(UnmanagedType.LPWStr)]
    public string lpszMenuName;
    [MarshalAs(UnmanagedType.LPWStr)]
    public string lpszClassName;
  }

  
  
  /// <summary>
  /// Win32 API imports.
  /// </summary>
  internal static class WinApi
  {

    public const int HWND_BROADCAST = 0xffff;
    public static readonly uint MP2_SHOWME = RegisterWindowMessage("MP2_SHOWME");
    
    
    /// <summary>
    /// Creates, updates or deletes the taskbar icon.
    /// </summary>
    [DllImport("shell32.Dll")]
    public static extern bool Shell_NotifyIcon(NotifyCommand cmd, [In]ref NotifyIconData data);


    /// <summary>
    /// Creates the helper window that receives messages from the taskar icon.
    /// </summary>
    [DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true)]
    public static extern IntPtr CreateWindowEx(int dwExStyle, [MarshalAs(UnmanagedType.LPWStr)] string lpClassName,
                           [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName, int dwStyle, int x, int y,
                           int nWidth, int nHeight, uint hWndParent, int hMenu, int hInstance,
                           int lpParam);


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

    /// <summary>
    /// Registers a listener for a window message.
    /// </summary>
    /// <param name="lpString"></param>
    /// <returns></returns>
    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW")]
    public static extern uint RegisterWindowMessage([MarshalAs(UnmanagedType.LPWStr)] string lpString);

    /// <summary>
    /// Used to destroy the hidden helper window that receives messages from the
    /// taskbar icon.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyWindow(IntPtr hWnd);


    /// <summary>
    /// Gives focus to a given window.
    /// </summary>
    /// <param name="hWnd"></param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);


    /// <summary>
    /// Gets the maximum number of milliseconds that can elapse between a
    /// first click and a second click for the OS to consider the
    /// mouse action a double-click.
    /// </summary>
    /// <returns>The maximum amount of time, in milliseconds, that can
    /// elapse between a first click and a second click for the OS to
    /// consider the mouse action a double-click.</returns>
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern int GetDoubleClickTime();


    /// <summary>
    /// Gets the screen coordinates of the current mouse position.
    /// </summary>
    /// <param name="lpPoint"></param>
    /// <returns></returns>
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool GetCursorPos(ref Point lpPoint);
    

    /// <summary>
    /// Places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message.
    /// </summary>
    /// <param name="hwnd">A handle to the window whose window procedure is to receive the message. </param>
    /// <param name="msg">The message to be posted.</param>
    /// <param name="wparam">Additional message-specific information.</param>
    /// <param name="lparam">Additional message-specific information.</param>
    /// <returns></returns>
    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hwnd, uint msg, IntPtr wparam, IntPtr lparam);

  }
}