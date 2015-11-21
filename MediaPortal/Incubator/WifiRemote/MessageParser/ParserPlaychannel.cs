using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Client.Player;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.WifiRemote.Messages;
using MediaPortal.Plugins.WifiRemote.SendMessages;
using MediaPortal.Plugins.WifiRemote.Utils;
using MediaPortal.UI.Presentation;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPlaychannel
  {
    private static ImageHelper _imageHelper;
    private static ConcurrentDictionary<AsyncSocket, int> _socketsWaitingForScreenshot;
    
    public static bool Parse(JObject message, SocketServer server, AsyncSocket sender)
    {
      int channelId = (int)message["ChannelId"];
      bool startFullscreen = (message["StartFullscreen"] != null) && (bool)message["StartFullscreen"];

      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Info("WifiRemote: playchannel - no tv handler");
        return false;
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      IChannel channel;
      if (!tvHandler.ChannelAndGroupInfo.GetChannel(channelId, out channel))
      {
        Logger.Info("WifiRemote: playchannel - Channel with id '{0}' not found", channelId);
        return false;
      }

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      SlimTvClientModel model = workflowManager.GetModel(SlimTvClientModel.MODEL_ID) as SlimTvClientModel;
      if (model != null)
        model.Tune(channel);

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
