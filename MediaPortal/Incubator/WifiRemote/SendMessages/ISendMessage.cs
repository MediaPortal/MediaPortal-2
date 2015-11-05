using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.WifiRemote.Messages;
using WifiRemote;

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
