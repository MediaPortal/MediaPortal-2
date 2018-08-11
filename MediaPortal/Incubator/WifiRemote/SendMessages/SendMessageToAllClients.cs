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
using System.Collections.Generic;
using Deusty.Net;
using MediaPortal.Plugins.WifiRemote.Messages;

namespace MediaPortal.Plugins.WifiRemote.SendMessages
{
  internal class SendMessageToAllClients
  {
    /// <summary>
    /// Send a message (object) to all connected clients.
    /// </summary>
    /// <param name="message">Message object to send</param>
    /// <param name="connectedSockets">A list of connected Sockets</param>
    /// <param name="ignoreAuth">False if the message should only be sent to authed clients</param>
    public static void Send(IMessage message, ref List<AsyncSocket> connectedSockets, bool ignoreAuth)
    {
      if (message == null) return;

      foreach (AsyncSocket socket in connectedSockets)
      {
        SendMessageToClient.Send(message, socket, ignoreAuth);
      }
    }

    /// <summary>
    /// Send a message (object) to all connected clients.
    /// </summary>
    /// <param name="message">Message object to send</param>
    /// <param name="connectedSockets">A list of connected Sockets</param>
    public static void Send(IMessage message, ref List<AsyncSocket> connectedSockets)
    {
      Send(message, ref connectedSockets, false);
    }

    /// <summary>
    /// Send a message to all connected clients.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="connectedSockets">A list of connected Sockets</param>
    /// <param name="ignoreAuth">False if the message should only be sent to authed clients</param>
    public static void Send(string message, ref List<AsyncSocket> connectedSockets, bool ignoreAuth)
    {
      if (message == null) return;
      lock (connectedSockets)
      {
        foreach (AsyncSocket socket in connectedSockets)
        {
          SendMessageToClient.Send(message, socket, ignoreAuth);
        }
      }
    }

    /// <summary>
    /// Send a message to all connected and authenticated clients.
    /// </summary>
    /// <param name="message">The message</param>
    /// <param name="connectedSockets">A list of connected Sockets</param>
    public static void Send(String message, ref List<AsyncSocket> connectedSockets)
    {
      Send(message, ref connectedSockets, false);
    }
  }
}
