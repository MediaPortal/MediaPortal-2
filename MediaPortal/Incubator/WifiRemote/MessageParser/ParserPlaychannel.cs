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

using System.Collections.Concurrent;
using System.Threading.Tasks;
using Deusty.Net;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.SlimTv.Client.Models;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.WifiRemote.Utils;
using MediaPortal.UI.Presentation.Workflow;
using Newtonsoft.Json.Linq;

namespace MediaPortal.Plugins.WifiRemote.MessageParser
{
  internal class ParserPlaychannel
  {
    private static ImageHelper _imageHelper;
    private static ConcurrentDictionary<AsyncSocket, int> _socketsWaitingForScreenshot;
    
    public static async Task<bool> ParseAsync(JObject message, SocketServer server, AsyncSocket sender)
    {
      int channelId = (int)message["ChannelId"];
      bool startFullscreen = (message["StartFullscreen"] != null) && (bool)message["StartFullscreen"];

      if (!ServiceRegistration.IsRegistered<ITvHandler>())
      {
        Logger.Info("WifiRemote: playchannel - no tv handler");
        return false;
      }

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var channel = await tvHandler.ChannelAndGroupInfo.GetChannelAsync(channelId);
      if (!channel.Success)
      {
        Logger.Info("WifiRemote: playchannel - Channel with id '{0}' not found", channelId);
        return false;
      }

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      SlimTvClientModel model = workflowManager.GetModel(SlimTvClientModel.MODEL_ID) as SlimTvClientModel;
      if (model != null)
        await model.Tune(channel.Result);

      return true;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
