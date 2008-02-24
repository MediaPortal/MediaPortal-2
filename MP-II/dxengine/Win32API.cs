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
using System.Text;
using System.Runtime.InteropServices;

namespace dxEngine
{
  public static class Win32API
  {
    public static void Show(string className, string windowName, bool visible)
    {
      uint i = FindWindow(ref className, ref windowName);
      if (visible)
      {
        ShowWindow(i, 5);
      }
      else
      {
        ShowWindow(i, 0);
      }
    }

    public static void Enable(string className, string windowName, bool enable)
    {
      uint i = FindWindow(ref className, ref windowName);
      if (enable)
      {
        EnableWindow(i, -1);
      }
      else
      {
        EnableWindow(i, 0);
      }
    }

    public static void ShowStartBar(bool visible)
    {
      try
      {
        Show("Shell_TrayWnd", "", visible);
      }
      catch (Exception) { }
    }

    public static void EnableStartBar(bool enable)
    {
      try
      {
        Enable("Shell_TrayWnd", "", enable);
      }
      catch (Exception) { }
    }

    [DllImportAttribute("user32", EntryPoint = "FindWindowA", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern uint FindWindow([MarshalAs(UnmanagedType.VBByRefStr)] ref string lpclassName, [MarshalAs(UnmanagedType.VBByRefStr)] ref string lpwindowName);

    [DllImport("user32", SetLastError = true)]
    private static extern uint ShowWindow(uint _hwnd, int _showCommand);

    [DllImportAttribute("user32", ExactSpelling = true, CharSet = CharSet.Ansi, SetLastError = true)]
    public static extern int EnableWindow(uint hwnd, int fEnable);
  }
}
