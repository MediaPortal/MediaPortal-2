using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Plugins.WifiRemote.Messages;

namespace MediaPortal.Plugins.WifiRemote.SendMessages
{
  internal class SendMessageOverviewInformation
  {
    public static void Send(AsyncSocket client)
    {
      SendMessageToClient.Send(WifiRemote.MessageStatus, client);
      SendMessageToClient.Send(new MessageVolume(), client);

      // If we are playing a file send detailed information about it
      /*if (g_Player.Playing)
      {
        SendMessageToClient.Send(this.nowPlayingMessage, client);
      }*/

      // TODO: reimplement
      // Send facade info to client
      /*SendListViewStatusToClient(client);

      // Inform client about open dialogs
      if (MpDialogsHelper.IsDialogShown)
      {
        MessageDialog msg = MpDialogsHelper.GetDialogMessage(MpDialogsHelper.CurrentDialog);
        SendMessageToClient(msg, client);
      }*/
    }
  }
}
