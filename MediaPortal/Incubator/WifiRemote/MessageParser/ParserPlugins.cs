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

using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.Messages.Plugins;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPlugins : BaseParser
  {
    public static Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      MessagePlugins msg = new MessagePlugins();
      msg.Plugins = new List<PluginEntry>();
      foreach (var plugin in ServiceRegistration.Get<IPluginManager>().AvailablePlugins)
      {
        msg.Plugins.Add(new PluginEntry
        {
          Id = plugin.Key.ToString(),
          Name = plugin.Value.Metadata.Name,
          Version = plugin.Value.Metadata.PluginVersion
        });
      }
      SendMessageToClient.Send(msg, sender, true);

      return Task.FromResult(true);
    }
  }
}
