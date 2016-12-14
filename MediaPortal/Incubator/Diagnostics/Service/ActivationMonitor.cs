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
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UiComponents.Diagnostics.Service
{
  class ActivationMonitor
  {
    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("psapi.dll")]
    private static extern uint GetModuleFileNameEx(IntPtr hWnd, IntPtr hModule, StringBuilder lpFileName, int nSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern uint GetProcessIdOfThread(IntPtr handle);

    [Flags]
    public enum ProcessAccessFlags : uint
    {
      All = 0x001F0FFF,
      Terminate = 0x00000001,
      CreateThread = 0x00000002,
      VirtualMemoryOperation = 0x00000008,
      VirtualMemoryRead = 0x00000010,
      VirtualMemoryWrite = 0x00000020,
      DuplicateHandle = 0x00000040,
      CreateProcess = 0x000000080,
      SetQuota = 0x00000100,
      SetInformation = 0x00000200,
      QueryInformation = 0x00000400,
      QueryLimitedInformation = 0x00001000,
      Synchronize = 0x00100000
    }

    [Flags]
    public enum ThreadAccess
    {
      TERMINATE = 0x0001,
      SUSPEND_RESUME = 0x0002,
      GET_CONTEXT = 0x0008,
      SET_CONTEXT = 0x0010,
      SET_INFORMATION = 0x0020,
      QUERY_INFORMATION = 0x0040,
      SET_THREAD_TOKEN = 0x0080,
      IMPERSONATE = 0x0100,
      DIRECT_IMPERSONATION = 0x0200
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

    public static string GetText(IntPtr hWnd)
    {
      // Allocate correct string length first
      int length = GetWindowTextLength(hWnd);
      StringBuilder sb = new StringBuilder(length + 1);
      GetWindowText(hWnd, sb, sb.Capacity);
      return sb.ToString();
    }

    public static string GetTopWindowName()
    {
      IntPtr hWnd = GetForegroundWindow();
      uint lpdwProcessId;
      GetWindowThreadProcessId(hWnd, out lpdwProcessId);

      var text = GetProcessModuleName(lpdwProcessId);
      return text;
    }

    private static string GetProcessModuleName(uint lpdwProcessId)
    {
      IntPtr hProcess = OpenProcess(ProcessAccessFlags.QueryInformation | ProcessAccessFlags.VirtualMemoryRead, false, lpdwProcessId);

      StringBuilder text = new StringBuilder(1000);
      GetModuleFileNameEx(hProcess, IntPtr.Zero, text, text.Capacity);

      CloseHandle(hProcess);
      return text.ToString();
    }

    public static string GetProcessByThreadId(uint threadId)
    {
      IntPtr threadHandle = IntPtr.Zero;
      try
      {
        threadHandle = OpenThread(ThreadAccess.QUERY_INFORMATION, false, threadId);
        if (threadHandle != IntPtr.Zero)
        {
          var procId = GetProcessIdOfThread(threadHandle);
          if (procId != 0)
          {
            return GetProcessModuleName(procId);
          }
        }
      }
      finally
      {
        if (threadHandle != IntPtr.Zero)
          CloseHandle(threadHandle);
      }
      return string.Empty;
    }

    private static IntPtr GetFocusedHandle()
    {
      var info = new GuiThreadInfo();
      info.cbSize = Marshal.SizeOf(info);
      if (!GetGUIThreadInfo(0, ref info))
        throw new Win32Exception();
      return info.hwndFocus;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GuiThreadInfo
    {
      public int cbSize;
      public uint flags;
      public IntPtr hwndActive;
      public IntPtr hwndFocus;
      public IntPtr hwndCapture;
      public IntPtr hwndMenuOwner;
      public IntPtr hwndMoveSize;
      public IntPtr hwndCaret;
      public Rectangle rcCaret;
    }

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo lpgui);

    const int WM_ACTIVATE = 0x0006;
    const int WM_ACTIVATEAPP = 0x001C;

    internal static void HandleMessage(ref Message m)
    {
      if (m.Msg == WM_ACTIVATEAPP)
      {
        ServiceRegistration.Get<ILogger>().Info("ActivationMonitor: WM_ACTIVATEAPP: {0} / {1}", m.WParam, m.LParam);
        LogActiveProcess(m.LParam.ToInt32());
      }

      if (m.Msg == WM_ACTIVATE) // control activated by clicking or key
      {
        ServiceRegistration.Get<ILogger>().Info("ActivationMonitor: WM_ACTIVATE: {0} / {1}", m.WParam, m.LParam);
        // wparam -> 0 = inactive, 1 = active, 2 = active by click
        //if wparam = inactive, then lparam is a handle to the window being activated
        if (m.WParam.ToInt32() == 0)
        {
          // Deactivation by another Window without Form Handle?
          if (m.LParam.ToInt32() == 0)
          {
            LogActiveWindow();
          }
        }
      }
    }

    private static void LogActiveProcess(int procId)
    {
      var app = GetProcessByThreadId((uint)procId);
      ServiceRegistration.Get<ILogger>().Info("ActivationMonitor: Handle {0}, App: '{1}'", procId, app);
    }

    private static void LogActiveWindow()
    {
      var newHandle = GetFocusedHandle();
      string title = "";
      if (newHandle != IntPtr.Zero)
        title = GetText(newHandle);

      string app = GetTopWindowName();

      ServiceRegistration.Get<ILogger>().Info("ActivationMonitor: Handle {0}, Title '{1}', App: '{2}'", newHandle, title, app);
    }
  }
}
