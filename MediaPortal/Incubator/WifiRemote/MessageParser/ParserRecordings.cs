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
  internal class ParserRecordings : BaseParser
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
        string recordingName = GetMessageValue<string>(message, "RecordingName");
        string id = GetMessageValue<string>(message, "RecordingId");
        int startPos = GetMessageValue<int>(message, "StartPosition");

        // Search for recordings
        if (action.Equals("recordingsearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<RecordingInfo>(client, search, Convert.ToUInt32(count), null, Helper.GetRecordingsByRecordingSearchAsync);
          SendMessageToClient.Send(new MessageRecordings { Recordings = list }, sender, true);
        }
        // Show recording list
        else if (action.Equals("recordinglist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<RecordingInfo>(client, null, Convert.ToUInt32(offset), Convert.ToUInt32(count), Helper.GetRecordingsByRecordingSearchAsync);
          SendMessageToClient.Send(new MessageRecordings { Recordings = list }, sender, true);
        }
        // Show details for this recording
        else if (action.Equals("recordingdetails", StringComparison.InvariantCultureIgnoreCase))
        {
          // TODO: implementation possible?
        }
        // Play a recording
        else if (action.Equals("playrecording", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote: Play Recording: RecordingName: {0}, RecordingId: {1}, StartPos: {2}", recordingName, id, startPos);

          var mediaItemGuid = await GetIdFromNameAsync(client, recordingName, id, Helper.GetRecordingByRecordingNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote: Play Recording: Couldn't convert RecordingId '{0} to Guid", id);
            return false;
          }

          await Helper.PlayMediaItemAsync(mediaItemGuid.Value, startPos);
        }
      }

      return true;
    }
  }
}
