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
  internal class ParserSchedules : BaseParser
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
        int id = GetMessageValue<int>(message, "ScheduleId");

        // Search for schedule
        if (action.Equals("schedulesearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetSchedulesAsync();
          SendMessageToClient.Send(new MessageSchedules { Schedules = list.Where(p => p.Name.Contains(search)).OrderBy(p => p.StartTime).Select(p => new ScheduleInfo(p)).ToList() }, sender, true);
        }
        // Show schedule list
        else if (action.Equals("schedulelist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetSchedulesAsync();
          SendMessageToClient.Send(new MessageSchedules { Schedules = list.OrderBy(p => p.StartTime).Select(p => new ScheduleInfo(p)).ToList() }, sender, true);
        }
        // Show details for this schedule
        else if (action.Equals("scheduledetails", StringComparison.InvariantCultureIgnoreCase))
        {
          // TODO: implementation possible?
        }
        // Play a movie
        else if (action.Equals("deleteschedule", StringComparison.InvariantCultureIgnoreCase))
        {
          await RemoveSchedulesAsync(id);
        }
      }

      return true;
    }
  }
}
