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

using System.Text;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.Messages;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.WifiRemote.SendMessages
{
  internal class SendMessageToClient
  {
    /// <summary>
    /// Send a message (object) to a specific client
    /// </summary>
    /// <param name="message">Message object to send</param>
    /// <param name="client">A connected client socket</param>
    /// <param name="ignoreAuth">False if messages should only be sent to authed clients</param>
    public static void Send(IMessage message, AsyncSocket client, bool ignoreAuth)
    {
      if (message == null)
      {
        ServiceRegistration.Get<ILogger>().Debug("WifiRemote: SendMessageToClient failed: IMessage object is null");
        return;
      }

      string messageString = JsonConvert.SerializeObject(message);
      Send(messageString, client, ignoreAuth);
    }

    /// <summary>
    /// Send a message (object) to a specific authed client
    /// </summary>
    /// <param name="message"></param>
    /// <param name="client"></param>
    public static void Send(IMessage message, AsyncSocket client)
    {
      Send(message, client, false);
    }

    /// <summary>
    /// Send a message to a specific client
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="client">A connected client socket</param>
    /// <param name="ignoreAuth">False if messages should only be sent to authed clients</param>
    public static void Send(string message, AsyncSocket client, bool ignoreAuth)
    {
      if (message == null)
      {
        ServiceRegistration.Get<ILogger>().Debug("WifiRemote: SendMessageToClient failed: Message string is null");
        return;
      }

      byte[] data = Encoding.UTF8.GetBytes(message + "\r\n");
      if (client.GetRemoteClient().IsAuthenticated || ignoreAuth)
      {
        client.Write(data, -1, 0);
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Info("WifiRemote: SendMessageToClient failed: No Auth: {0}, ignoreAuth: {1}", client.GetRemoteClient().IsAuthenticated, ignoreAuth);
      }
    }

    /// <summary>
    /// Send a message to a specific authenticated client
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="client">A connected and authenticated client</param>
    public static void Send(string message, AsyncSocket client)
    {
      Send(message, client, false);
    }
  }
}
