﻿#region Copyright (C) 2007-2015 Team MediaPortal

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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserProperties : BaseParser
  {
    public static Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      JArray array = GetMessageValue<JArray>(message, "Properties");
      if (array != null)
      {
        var client = SocketServer.Instance.connectedSockets.Single(x => x == sender).GetRemoteClient();
        foreach (JValue v in array)
        {
          String propString = (string)v.Value;
          ServiceRegistration.Get<ILogger>().Info("WifiRemote Properties: Property: {0}", propString);
          client.Properties.Add(propString);
        }

        List<Property> properties = new List<Property>();
        foreach (String s in client.Properties)
        {
          // TODO: Implement?:
          //"#Play.Current.Title",
          //"#Play.Current.File",
          //"#Play.Current.Thumb",
          //"#Play.Current.Plot",
          //"#Play.Current.PlotOutline",
          //"#Play.Current.Channel",
          //"#Play.Current.Genre",
          //"#Play.Current.Title",
          //"#Play.Current.Artist",
          //"#Play.Current.Album",
          //"#Play.Current.Track",
          //"#Play.Current.Year",
          //"#TV.View.channel",
          //"#TV.View.thumb",
          //"#TV.View.start",
          //"#TV.View.stop",
          //"#TV.View.remaining",
          //"#TV.View.genre",
          //"#TV.View.title",
          //"#TV.View.description",
          //"#TV.Next.start",
          //"#TV.Next.stop",
          //"#TV.Next.title",
          //"#TV.Next.description"

          //String value = GUIPropertyManager.GetProperty(s);
          //if (value != null && !value.Equals("") && CheckProperty(s))
          //{
          //properties.Add(new Property(s, value));
          //}
        }

        MessageProperties propertiesMessage = new MessageProperties();
        propertiesMessage.Tags = properties;
        SendMessageToClient.Send(propertiesMessage, sender);
      }

      return Task.FromResult(true);
    }
  }
}
