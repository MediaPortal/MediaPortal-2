using System;
using System.Collections.Generic;
using System.Text;

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
  }
}
