#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using MediaPortal.Extensions.BassLibraries;
using System.Collections.Generic;
using System.IO;
using Un4seen.Bass.AddOn.Cd;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor.Matchers
{
  class AudioCDMatcher
  {
    private static List<char> PHYSICAL_DISK_ROOTS = new List<char>();

    public static bool IsOpticalDisc(string fileName)
    {
      string root = Path.GetPathRoot(fileName);
      return PHYSICAL_DISK_ROOTS.Contains(root[0]) == false;
    }

    public static bool GetDiscMatch(string fileName, out string musicBrainzId, out string cdDbId)
    {
      musicBrainzId = null;
      cdDbId = null;
      string root = Path.GetPathRoot(fileName);
      char rootLetter = root[0];
      if (PHYSICAL_DISK_ROOTS.Contains(rootLetter) == false)
      {
        int drive = BassUtils.Drive2BassID(rootLetter);
        if (drive > -1)
        {
          string id = BassCd.BASS_CD_GetID(drive, BASSCDId.BASS_CDID_CDDB);
          cdDbId = id.Substring(0, 8);
          musicBrainzId = BassCd.BASS_CD_GetID(drive, BASSCDId.BASS_CDID_MUSICBRAINZ);
          BassCd.BASS_CD_Release(drive);

          return true;
        }
        else
        {
          PHYSICAL_DISK_ROOTS.Add(rootLetter);
        }
      }
      return false;
    }
  }
}
