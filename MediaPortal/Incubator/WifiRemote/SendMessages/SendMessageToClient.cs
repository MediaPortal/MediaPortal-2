using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.Messages;
using Newtonsoft.Json;
using WifiRemote;

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
        ServiceRegistration.Get<ILogger>().Debug("SendMessageToClient failed: IMessage object is null");
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
        ServiceRegistration.Get<ILogger>().Debug("SendMessageToClient failed: Message string is null");
        return;
      }

      //WifiRemote.LogMessage("Send to " + client.LocalAddress + ": " + message, WifiRemote.LogType.Debug);
      byte[] data = Encoding.UTF8.GetBytes(message + "\r\n");
      if (client.GetRemoteClient().IsAuthenticated || ignoreAuth)
      {
        client.Write(data, -1, 0);
      }
      else
      {
        ServiceRegistration.Get<ILogger>().Info("SendMessageToClient failed: No Auth: {0}, ignoreAuth: {1}", client.GetRemoteClient().IsAuthenticated, ignoreAuth);
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
