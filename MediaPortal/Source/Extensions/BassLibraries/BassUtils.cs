#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using Un4seen.Bass.AddOn.Cd;

namespace MediaPortal.Extensions.BassLibraries
{
  public class BassUtils
  {
    /// <summary>
    /// Checks, if the media in the given drive Letter is a Red Book (Audio) CD.
    /// </summary>
    /// <param name="drive">Drive path or drive letter (<c>"F:"</c> or <c>"F"</c>).</param>
    /// <returns><c>true</c>, if the media in the given <paramref name="drive"/> is a Red Book CD.</returns>
    public static bool isARedBookCD(string drive)
    {
      return GetNumTracks(drive) > 0;
    }

    public static int GetNumTracks(string drive)
    {
      try
      {
        if (string.IsNullOrEmpty(drive))
          return 0;
        char driveLetter = System.IO.Path.GetFullPath(drive).ToCharArray()[0];
        return BassCd.BASS_CD_GetTracks(Drive2BassID(driveLetter));
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("BassUtils: Error examining CD in drive '{0}'", e, drive);
        return 0;
      }
    }

    /// <summary>
    /// Converts the given CD/DVD/BD drive letter to a number suiteable for BASS.
    /// </summary>
    /// <param name="driveLetter">Drive letter to convert.</param>
    /// <returns>Bass id of the given <paramref name="driveLetter"/>.</returns>
    public static int Drive2BassID(char driveLetter)
    {
      BASS_CD_INFO cdinfo = new BASS_CD_INFO();
      for (int i = 0; i < 25; i++)
      {
        if (BassCd.BASS_CD_GetInfo(i, cdinfo))
        {
          if (cdinfo.DriveLetter == driveLetter)
            return i;
        }
      }
      return -1;
    }
  }
}
