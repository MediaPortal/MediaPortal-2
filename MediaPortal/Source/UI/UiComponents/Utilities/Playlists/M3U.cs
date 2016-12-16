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

using System.Collections.Generic;
using System.IO;
using System.Text;
using MediaPortal.Common;
using MediaPortal.Common.Logging;

namespace MediaPortal.UiComponents.Utilities.Playlists
{
  public class M3U
  {
    public static IList<string> ExtractFileNamesFromPlaylist(string playlistFilePath)
    {
      ILogger logger = ServiceRegistration.Get<ILogger>();
      logger.Info("M3U: Extracting file names from playlist file '{0}'", playlistFilePath);
      IList<string> result = new List<string>();
      StreamReader reader = new StreamReader(playlistFilePath, playlistFilePath.ToLowerInvariant().EndsWith(".m3u8") ? Encoding.UTF8 : Encoding.Default);
      string line;
      while ((line = reader.ReadLine()) != null)
      {
        if (line.Trim().StartsWith("#"))
        {
          // DH: Should be logged at trace level, if we would support it
          //logger.Debug("M3U: Skipping line '{0}'", line);
          continue;
        }
        // DH: Should be logged at trace level, if we would support it
        //logger.Debug("M3U: Processing playlist entry '{0}'", line);
        result.Add(line);
      }
      return result;
    }
  }
}