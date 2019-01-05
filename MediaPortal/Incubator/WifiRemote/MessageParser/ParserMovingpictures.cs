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

using System;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserMovingpictures
  {
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string action = (string)message["Action"];

      if (!string.IsNullOrEmpty(action))
      {
        // Show movie details for this movie
        if (action == "moviedetails")
        {
          // TODO: implement?
        }
        // Play a movie
        else if (action == "playmovie")
        {
          // we use the FileHandler as MediaItem id
          string movieName = (string)message["MovieName"];
          string id = (string)message["MovieId"];
          int startPos = (message["StartPosition"] != null) ? (int)message["StartPosition"] : 0;

          ServiceRegistration.Get<ILogger>().Debug("WifiRemote Play Movie: MovieName: {0}, MovieId: {1}, StartPos: {2}", movieName, id, startPos);

          if (!string.IsNullOrEmpty(movieName) && string.IsNullOrEmpty(id))
          {
            var item = await Helper.GetMediaItemByMovieNameAsync(movieName);
            if (item != null)
              id = item.MediaItemId.ToString();
          }

          Guid mediaItemGuid;
          if (!Guid.TryParse(id, out mediaItemGuid))
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Movie: Couldn't convert MovieId '{0}' to Guid", id);
            return false;
          }

          await Helper.PlayMediaItemAsync(mediaItemGuid, startPos);
        }
      }

      return true;
    }
  }
}
