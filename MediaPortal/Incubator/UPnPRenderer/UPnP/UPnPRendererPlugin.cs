#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Presentation.Players;

namespace MediaPortal.UPnPRenderer.UPnP
{
  internal class UPnPRendererPlugin : IPluginStateTracker, IMessageReceiver
  {
    //private readonly upnpDevice _device;
    public const string DEVICE_UUID = "D6185023-12EC-4E27-A4AA-C8D9E993974D";

    //static HttpServer.HttpServer _httpServer = new HttpServer.HttpServer();
    public static UPnPLightServer UPnPServer = null;
    private AsynchronousMessageQueue _messageQueue;
    private readonly Player _player;

    public UPnPRendererPlugin()
    {
      UPnPServer = new UPnPLightServer(DEVICE_UUID);
      UPnPServer.Start();
      _player = new Player();
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));
      ServiceRegistration.Get<IMessageBroker>().RegisterMessageReceiver(SystemMessaging.CHANNEL, this);
      SubscribeToMessages();
      //Logger.Debug("UPnPRenderPlugin: Adding UPNP device as a root device");
      //ServiceRegistration.Get<IFrontendServer>().UPnPFrontendServer.AddRootDevice(_device);
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      UnsubscribeFromMessages();
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }

    public void Receive(SystemMessage message)
    {
      if (message.MessageType as SystemMessaging.MessageType? == SystemMessaging.MessageType.SystemStateChanged)
      {
        SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
        if (newState == SystemState.Running)
        {
          RegisterWithServices();
        }
      }
    }

    protected void RegisterWithServices()
    {
      // All non-default media item aspects must be registered
    }

    #region Messages

    void SubscribeToMessages()
    {
      //TraceLogger.WriteLine("subscribe to player messages");
      if (_messageQueue != null)
        return;
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
                                                  {
                                                    PlayerManagerMessaging.CHANNEL,
                                                  });
      _messageQueue.MessageReceived += _player.OnMessageReceived;
      _messageQueue.Start();
      //TraceLogger.WriteLine("subscribe to player messages end of function");
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    #endregion Messages
  }
}
