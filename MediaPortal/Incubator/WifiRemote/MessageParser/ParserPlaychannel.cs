#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Plugins.WifiRemote.Utils;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPlaychannel : BaseParser
  {
    private static ImageHelper _imageHelper;
    private static ConcurrentDictionary<AsyncSocket, int> _socketsWaitingForScreenshot;
    
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      int channelId = GetMessageValue<int>(message, "ChannelId");
      bool startFullscreen = GetMessageValue<bool>(message, "StartFullscreen");

      return await PlayChannelAsync(channelId);
    }
  }
}
