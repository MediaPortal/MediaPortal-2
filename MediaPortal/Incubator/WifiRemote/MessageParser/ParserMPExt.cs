using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserMPExt
  {
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      string itemId = (string)message["ItemId"];
      int itemType = (int)message["MediaType"];
      int providerId = (int)message["ProviderId"];
      String action = (String)message["Action"];
      Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>(message["PlayInfo"].ToString());
      int startPos = (message["StartPosition"] != null) ? (int)message["StartPosition"] : 0;

      if (action != null)
      {
        if (action.Equals("play"))
        {
          //play the item in MediaPortal
          Logger.Debug("play mediaitem: ItemId: " + itemId + ", itemType: " + itemType + ", providerId: " + providerId);

          Guid mediaItemGuid;
          if (!Guid.TryParse(itemId, out mediaItemGuid))
          {
            ServiceRegistration.Get<ILogger>().Info("ParserMPExt: Couldn't convert fileHandler '{0} to Guid", itemId);
            return false;
          }

          Helper.PlayMediaItem(mediaItemGuid, startPos);
          //MpExtendedHelper.PlayMediaItem(itemId, itemType, providerId, values, startPos);
        }
        else if (action.Equals("show"))
        {
          //show the item details in mediaportal (without starting playback)
          //WifiRemote.LogMessage("show mediaitem: ItemId: " + itemId + ", itemType: " + itemType + ", providerId: " + providerId, WifiRemote.LogType.Debug);

          //MpExtendedHelper.ShowMediaItem(itemId, itemType, providerId, values);
        }
        else if (action.Equals("enqueue"))
        {
          //enqueue the mpextended item to the currently active playlist
          Logger.Debug("enqueue mediaitem: ItemId: " + itemId + ", itemType: " + itemType + ", providerId: " + providerId);

          int startIndex = (message["StartIndex"] != null) ? (int)message["StartIndex"] : -1;

          /*PlayListType playlistType = PlayListType.PLAYLIST_VIDEO;
          List<PlayListItem> items = MpExtendedHelper.CreatePlayListItemItem(itemId, itemType, providerId, values, out playlistType);

          PlaylistHelper.AddPlaylistItems(playlistType, items, startIndex);*/
        }
      }
      else
      {
        Logger.Warn("No MpExtended action defined");
      }

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
