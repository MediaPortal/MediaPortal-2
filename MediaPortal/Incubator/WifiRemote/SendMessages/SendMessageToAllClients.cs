using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Plugins.WifiRemote.Messages;
using WifiRemote;

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
