#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using System.Runtime.InteropServices;
using System.Text;

namespace MediaPortal.Utilities.Win32
{
  /// <summary>
  /// This class contains definitions for the Win32 API
  /// </summary>
  [Obsolete("Direct usages in the code should be replaced by usages of more abstract classes")]
  public class Win32API
  {
    #region Interop declarations

    #region Constants
    public const int SW_HIDE = 0;
    public const int SW_SHOWNORMAL = 1;
    public const int SW_SHOWMINIMIZED = 2;
    public const int SW_SHOWMAXIMIZED = 3;
    public const int SW_RESTORE = 9;
    public const int WPF_RESTORETOMAXIMIZED = 2;
    public const int WM_SHOWWINDOW = 0x0018;
    public const int SHGFP_TYPE_CURRENT = 0;
    public const int CSIDL_MYMUSIC = 0x000d;     // "My Music" folder
    public const int CSIDL_MYVIDEO = 0x000e;     // "My Videos" folder
    public const int CSIDL_MYPICTURES = 0x0027;  // "My Pictures" folder

    // OSVERSIONINFOEX specific constants
    public const int VER_NT_WORKSTATION = 1;
    public const int VER_NT_DOMAIN_CONTROLLER = 2;
    public const int VER_NT_SERVER = 3;
    public const int VER_SUITE_SMALLBUSINESS = 1;  // Microsoft Small Business Server was once installed on the system, but may have been upgraded to another version of Windows.
    public const int VER_SUITE_ENTERPRISE = 2;     // Windows Server 2008 Enterprise, Windows Server 2003, Enterprise Edition, or Windows 2000 Advanced Server is installed.
    public const int VER_SUITE_BACKOFFICE = 4;     // Microsoft BackOffice components are installed.
    public const int VER_SUITE_TERMINAL = 16;      // Terminal Services is installed. This value is always set.
    public const int VER_SUITE_SMALLBUSINES_RESTRICTED = 32;      // Microsoft Small Business Server is installed with the restrictive client license in force.
    public const int VER_SUITE_EMBEDDEDNT = 64;      // Windows XP Embedded is installed.
    public const int VER_SUITE_DATACENTER = 128;   // Windows Server 2008 Datacenter, Windows Server 2003, Datacenter Edition, or Windows 2000 Datacenter Server is installed.
    public const int VER_SUITE_SINGLEUSERTS = 256; // Remote Desktop is supported, but only one interactive session is supported. This value is set unless the system is running in application server mode.
    public const int VER_SUITE_PERSONAL = 512;     // Windows Vista Home Premium, Windows Vista Home Basic, or Windows XP Home Edition is installed.
    public const int VER_SUITE_BLADE = 1024;       // Windows Server 2003, Web Edition is installed.
    public const int VER_SUITE_STORAGE_SERVER = 8192;       // Windows Storage Server 2003 R2 or Windows Storage Server 2003is installed.
    public const int VER_SUITE_COMPUTE_SERVER = 16384;      // Windows Server 2003, Compute Cluster Edition is installed.
    public const int VER_SUITE_WH_SERVER = 32768;      // Windows Home Server is installed.

    #endregion

    #region Methods
    #region Kernel32
    [DllImport("kernel32.dll")]
    public extern static bool GetDiskFreeSpaceEx(string lpDirectoryName, out UInt64 lpFreeBytesAvailable, out UInt64 lpTotalNumberOfBytes, out UInt64 lpTotalNumberOfFreeBytes);

    [DllImport("Kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    public extern static bool GetVolumeInformation(
      string RootPathName,
      StringBuilder VolumeNameBuffer,
      int VolumeNameSize,
      out uint VolumeSerialNumber,
      out uint MaximumComponentLength,
      out uint FileSystemFlags,
      StringBuilder FileSystemNameBuffer,
      int nFileSystemNameSize);

    [DllImport("kernel32.dll")]
    public static extern long GetDriveType(string driveLetter);

    [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
      IntPtr lpInBuffer, uint nInBufferSize,
      IntPtr lpOutBuffer, uint nOutBufferSize,
      out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern IntPtr CreateFile(
      string filename,
      [MarshalAs(UnmanagedType.U4)] System.IO.FileAccess fileaccess,
      [MarshalAs(UnmanagedType.U4)] System.IO.FileShare fileshare,
      int securityattributes,
      [MarshalAs(UnmanagedType.U4)] System.IO.FileMode creationdisposition,
      int flags, IntPtr template);


    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool CloseHandle(IntPtr hObject);

    #endregion

    #region User32
    [DllImportAttribute("user32", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int CallNextHookEx(int hHook, int nCode, int wParam, ref int lParam);

    [DllImportAttribute("user32", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int UnhookWindowsHookEx(int hHook);

    [DllImportAttribute("user32", EntryPoint = "FindWindowA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern uint FindWindow([MarshalAs(UnmanagedType.VBByRefStr)] ref string lpClassName, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpWindowName);

    [DllImportAttribute("user32", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int GetWindow(int hwnd, int wCmd);

    [DllImport("user32", SetLastError = true)]
    public static extern uint GetWindowPlacement(uint _hwnd, [Out] out WindowPlacement _lpwndpl);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool PeekMessage([In, Out] ref MSG msg, IntPtr hwnd, int msgMin, int msgMax, int remove);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern bool GetMessageW([In, Out] ref MSG msg, IntPtr hWnd, int uMsgFilterMin, int uMsgFilterMax);

    [DllImport("user32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    public static extern bool GetMessageA([In, Out] ref MSG msg, IntPtr hWnd, int uMsgFilterMin, int uMsgFilterMax);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern bool TranslateMessage([In, Out] ref MSG msg);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    public static extern IntPtr DispatchMessageW([In] ref MSG msg);

    [DllImport("user32.dll", CharSet = CharSet.Ansi, ExactSpelling = true)]
    public static extern IntPtr DispatchMessageA([In] ref MSG msg);

    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    public static extern IntPtr GetParent(HandleRef hWnd);

    [DllImport("user32", SetLastError = true)]
    public static extern uint ShowWindow(uint _hwnd, int _showCommand);

    [DllImportAttribute("user32", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int EnableWindow(uint hwnd, int fEnable);

    [DllImport("user32", SetLastError = true)]
    public static extern uint SetForegroundWindow(uint _hwnd);

    [DllImport("user32", SetLastError = true)]
    public static extern bool PostThreadMessage(int idThread, uint Msg, uint wParam, uint lParam);
    #endregion

    #region GDI
    [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC")]
    public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll", EntryPoint = "SelectObject")]
    public static extern IntPtr SelectObject(IntPtr hdc, IntPtr bmp);

    [DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
    public static extern IntPtr DeleteDC(IntPtr hDc);
    #endregion

    #region WinInet
    [DllImport("wininet.dll")]
    public extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
    #endregion

    #region Shell32
    // Takes the CSIDL of a folder and returns the pathname.
    // Will be deprecated in Vista and replaced by SHGetKnownFolderPath
    [DllImport("shell32.dll")]
    public static extern Int32 SHGetFolderPath(
        IntPtr hwndOwner,        // Handle to an owner window.
        Int32 nFolder,           // A CSIDL value that identifies the folder whose path is to be retrieved.
        IntPtr hToken,           // An access token that can be used to represent a particular user.
        UInt32 dwFlags,          // Flags to specify which path is to be returned. It is used for cases where the folder associated with a CSIDL may be moved or renamed by the user. 
        StringBuilder pszPath);  // Pointer to a null-terminated string which will receive the path.
    #endregion

    #region winmm
    [DllImport("winmm.dll", EntryPoint = "mciSendStringA", CharSet = CharSet.Ansi)]
    public static extern int mciSendString(string lpstrCommand, StringBuilder lpstrReturnString, int uReturnLength, IntPtr hwndCallback);

    #endregion
    #endregion

    #region Structures

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
      public IntPtr hwnd;
      public int message;
      public IntPtr wParam;
      public IntPtr lParam;
      public int time;
      public int pt_x;
      public int pt_y;
    }

    /// <summary>
    /// Point struct used for GetWindowPlacement API.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Point
    {
      public int x;
      public int y;

      public Point(int _x, int _y)
      {
        x = _x;
        y = _y;
      }
    }

    /// <summary>
    /// Rect struct used for GetWindowPlacement API.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Rectangle
    {
      public int x;
      public int y;
      public int right;
      public int bottom;

      public Rectangle(int _x, int _y, int _right, int _bottom)
      {
        x = _x;
        y = _y;
        right = _right;
        bottom = _bottom;
      }
    }

    /// <summary>
    /// WindowPlacement struct used for GetWindowPlacement API.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct WindowPlacement
    {
      public uint length;
      public uint flags;
      public uint showCmd;
      public Point minPosition;
      public Point maxPosition;
      public Rectangle normalPosition;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct OSVERSIONINFOEX
    {
      public int dwOSVersionInfoSize;
      public int dwMajorVersion;
      public int dwMinorVersion;
      public int dwBuildNumber;
      public int dwPlatformId;
      [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
      public string szCSDVersion;
      public short wServicePackMajor;
      public short wServicePackMinor;
      public short wSuiteMask;
      public byte wProductType;
      public byte wReserved;
    }
    #endregion
    #endregion
  }
}
