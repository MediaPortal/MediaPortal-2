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
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserVideos : BaseParser
  {
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      string action = GetMessageValue<string>(message, "Action");
      var client = sender.GetRemoteClient();

      if (!string.IsNullOrEmpty(action))
      {
        string search = GetMessageValue<string>(message, "Search");
        int count = GetMessageValue<int>(message, "Count", 10);
        int offset = GetMessageValue<int>(message, "Offset");
        string videoName = GetMessageValue<string>(message, "VideoName");
        string id = GetMessageValue<string>(message, "VideoId");
        int startPos = GetMessageValue<int>(message, "StartPosition");

        // Search for video
        if (action.Equals("videosearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<VideoInfo>(client, search, Convert.ToUInt32(count), null, Helper.GetVideosByVideoSearchAsync);
          SendMessageToClient.Send(new MessageVideos { Videos = list }, sender, true);
        }
        // Show video list
        else if (action.Equals("videolist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<VideoInfo>(client, null, Convert.ToUInt32(count), Convert.ToUInt32(offset), Helper.GetVideosByVideoSearchAsync);
          SendMessageToClient.Send(new MessageVideos { Videos = list }, sender, true);
        }
        // Show details for this video
        else if (action.Equals("videodetails", StringComparison.InvariantCultureIgnoreCase))
        {
          // TODO: implementation possible?
        }
        // Play a movie
        else if (action.Equals("playvideo", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Video: VideoName: {0}, VideoId: {1}, StartPos: {2}", videoName, id, startPos);

          var mediaItemGuid = await GetIdFromNameAsync(client, videoName, id, Helper.GetVideoByVideoNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Video: Couldn't convert VideoId '{0} to Guid", id);
            return false;
          }

          await Helper.PlayMediaItemAsync(mediaItemGuid.Value, startPos);
        }
      }

      return true;
    }
  }
}
