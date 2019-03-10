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
using System.Linq;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserChannels : BaseParser
  {
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender, bool isTV)
    {
      string action = GetMessageValue<string>(message, "Action");
      var client = sender.GetRemoteClient();

      if (!string.IsNullOrEmpty(action))
      {
        int channelGroupId = GetMessageValue<int>(message, "ChannelGroupId");
        int channelId = GetMessageValue<int>(message, "ChannelId");
        string search = GetMessageValue<string>(message, "Search");
        int hours = GetMessageValue<int>(message, "Hours", 1);
        int count = GetMessageValue<int>(message, "Count", 10);
        int offset = GetMessageValue<int>(message, "Offset");
        bool startFullscreen = GetMessageValue<bool>(message, "StartFullscreen", true);

        // Show channel group list
        if (action.Equals("grouplist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetChannelGroupsAsync(isTV);
          SendMessageToClient.Send(new MessageChannelGroups { ChannelGroups = list.Select(g => new ChannelGroupInfo(g)).ToList() }, sender, true);
        }
        // Search group EPG
        else if (action.Equals("groupepgsearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetEpgAsync(channelGroupId, hours, isTV);
          SendMessageToClient.Send(new MessagePrograms { Programs = list.Where(p => p.Title.Contains(search)).OrderBy(p => p.StartTime).Select(p => new ProgramInfo(p)).ToList() }, sender, true);
        }
        // Show group EPG
        else if (action.Equals("groupepglist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetEpgAsync(channelGroupId, hours, isTV);
          SendMessageToClient.Send(new MessagePrograms { Programs = list.OrderBy(p => p.StartTime).Select(p => new ProgramInfo(p)).ToList() }, sender, true);
        }
        // Search for channel
        else if (action.Equals("channelsearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetChannelsAsync(channelGroupId, isTV);
          SendMessageToClient.Send(new MessageChannels { Channels = list.Where(c => c.Name.StartsWith(search)).OrderBy(c => c.Name).Select(c => new ChannelInfo(c)).ToList() }, sender, true);
        }
        // Show channel list
        else if (action.Equals("channellist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetChannelsAsync(channelGroupId, isTV);
          SendMessageToClient.Send(new MessageChannels { Channels = list.OrderBy(c => c.Name).Skip(offset).Take(count).Select(c => new ChannelInfo(c)).ToList() }, sender, true);
        }
        //Search channel EPG
        else if (action.Equals("channelepgsearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<VideoInfo>(client, search, Convert.ToUInt32(count), null, Helper.GetVideosByVideoSearchAsync);
          SendMessageToClient.Send(new MessageVideos { Videos = list }, sender, true);
        }
        // Show channel EPG
        else if (action.Equals("channelepglist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetChannelEpgAsync(channelId, hours, isTV);
          SendMessageToClient.Send(new MessagePrograms { Programs = list.Where(p => p.Title.Contains(search)).OrderBy(p => p.StartTime).Select(p => new ProgramInfo(p)).ToList() }, sender, true);
        }
        // Show details for this channel
        else if (action.Equals("channeldetails", StringComparison.InvariantCultureIgnoreCase))
        {
          // TODO: implementation possible?
        }
        // Play a channel
        else if (action.Equals("playchannel", StringComparison.InvariantCultureIgnoreCase))
        {         
          return await PlayChannelAsync(channelId);
        }
      }

      return true;
    }
  }
}
