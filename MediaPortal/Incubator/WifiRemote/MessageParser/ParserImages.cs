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
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserImages : BaseParser
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
        string imagePath = GetMessageValue<string>(message, "ImagePath");
        string id = GetMessageValue<string>(message, "ImageId");

        // Search for image
        if (action.Equals("imagesearch", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<ImageInfo>(client, search, Convert.ToUInt32(count), null, Helper.GetImagesByImageSearchAsync);
          SendMessageToClient.Send(new MessageImages { Images = list }, sender, true);
        }
        // Show image list
        else if (action.Equals("imagelist", StringComparison.InvariantCultureIgnoreCase))
        {
          var list = await GetItemListAsync<ImageInfo>(client, null, Convert.ToUInt32(count), Convert.ToUInt32(offset), Helper.GetImagesByImageSearchAsync);
          SendMessageToClient.Send(new MessageImages { Images = list }, sender, true);
        }
        else // Show image
        if (action.Equals("playimage", StringComparison.InvariantCultureIgnoreCase))
        {
          ServiceRegistration.Get<ILogger>().Debug("WifiRemote Play Image: ImageId: {0}, ImagePath: {1}, ", id, imagePath);

          var mediaItemGuid = await GetIdFromNameAsync(client, imagePath, id, Helper.GetMediaItemByFileNameAsync);
          if (mediaItemGuid == null)
          {
            ServiceRegistration.Get<ILogger>().Error("WifiRemote Play Image: Couldn't convert ImageId '{0} to Guid", id);
            return false;
          }

          await Helper.PlayMediaItemAsync(mediaItemGuid.Value, 0);
        }
      }

      return true;
    }
  }
}
