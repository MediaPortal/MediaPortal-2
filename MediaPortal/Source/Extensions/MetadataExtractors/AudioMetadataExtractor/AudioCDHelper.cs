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
using MediaPortal.Extensions.OnlineLibraries.Matches;
using Un4seen.Bass.AddOn.Cd;

namespace MediaPortal.Extensions.MetadataExtractors.AudioMetadataExtractor
{
  class AudioCDHelper
  {
    public static DiscIdMatch GetDiscMatch(char driveLetter)
    {
      DiscIdMatch retval = new DiscIdMatch();
      int drive = BassUtils.Drive2BassID(driveLetter);
      if (drive > -1)
      {
        string id = BassCd.BASS_CD_GetID(drive, BASSCDId.BASS_CDID_CDDB);
        retval.CdDbId = id.Substring(0, 8);
        retval.Id = BassCd.BASS_CD_GetID(drive, BASSCDId.BASS_CDID_MUSICBRAINZ);
        retval.ItemName = "Audio CD";
        BassCd.BASS_CD_Release(drive);
      }
      return retval;
    }
  }
}
