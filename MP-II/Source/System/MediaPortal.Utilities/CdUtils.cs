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
using MediaPortal.Utilities.Win32;

namespace MediaPortal.Utilities
{
  public class CdUtils
  {
    /// <summary>
    /// Eject the given CD Drive
    /// </summary>
    /// <param name="strDrive"></param>
    /// <returns></returns>
    public static bool EjectCDROM(string strDrive)
    {
      bool result = false;
      strDrive = @"\\.\" + strDrive;

      try
      {
        IntPtr fHandle = Win32API.CreateFile(strDrive, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite, 0, System.IO.FileMode.Open, 0x80, IntPtr.Zero);
        if (fHandle.ToInt64() != -1) //INVALID_HANDLE_VALUE)
        {
          uint Result;
          if (Win32API.DeviceIoControl(fHandle, 0x002d4808, IntPtr.Zero, 0, IntPtr.Zero, 0, out Result, IntPtr.Zero) == true)
          {
            result = true;
          }
          Win32API.CloseHandle(fHandle);
        }
      }
      catch (Exception)
      {
      }

      return result;
    }

    /// <summary>
    /// Ejects the CD Drive
    /// </summary>
    public static void EjectCDROM()
    {
      Win32API.mciSendString("set cdaudio door open", null, 0, IntPtr.Zero);
    }
  }
}