using System;
using System.Collections.Generic;
using System.Text;

using MediaPortal.Utilities.Win32;
using Un4seen.Bass.AddOn.Cd;

namespace MediaPortal.Utilities.CD
{
  public class Utils
  {
    /// <summary>
    /// Checks, if the given Drive Letter is a Red Book (Audio) CD
    /// </summary>
    /// <param name="driveLetter"></param>
    /// <returns></returns>
    public static bool isARedBookCD(string drive)
    {
      try
      {
        if (drive.Length < 1) return false;
        char driveLetter = System.IO.Path.GetFullPath(drive).ToCharArray()[0];
        int cddaTracks = BassCd.BASS_CD_GetTracks(Drive2BassID(driveLetter));

        if (cddaTracks > 0)
          return true;
        else
          return false;
      }
      catch (Exception)
      {
        return false;
      }
    }

    /// <summary>
    /// Converts the given CD/DVD Drive Letter to a number suiteable for BASS
    /// </summary>
    /// <param name="driveLetter"></param>
    /// <returns></returns>
    public static int Drive2BassID(char driveLetter)
    {
      for (int i = 0; i < 25; i++)
      {
        if (BassCd.BASS_CD_GetDriveLetterChar(i) == driveLetter)
          return i;
      }
      return -1;
    }

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
