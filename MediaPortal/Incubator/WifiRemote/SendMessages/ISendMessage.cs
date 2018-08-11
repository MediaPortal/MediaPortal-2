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
using MediaPortal.Plugins.WifiRemote.Messages;

namespace MediaPortal.Plugins.WifiRemote.SendMessages
{
  internal interface ISendMessage
  {
    /// <summary>
    /// Send a message (object) to a specific client
    /// </summary>
    /// <param name="message">Message object to send</param>
    /// <param name="client">A connected client socket</param>
    /// <param name="ignoreAuth">False if messages should only be sent to authed clients</param>
    void Send(IMessage message, AsyncSocket client, bool ignoreAuth);

    /// <summary>
    /// Send a message (object) to a specific authed client
    /// </summary>
    /// <param name="message"></param>
    /// <param name="client"></param>
    void Send(IMessage message, AsyncSocket client);

    void Send(string message, AsyncSocket client, bool ignoreAuth);

    void Send(string message, AsyncSocket client);
  }
}
