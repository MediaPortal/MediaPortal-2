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
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemResolver;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.FrontendServer;
using MediaPortal.UI.Services.ServerCommunication;
using UPnP.Infrastructure;
using UPnP.Infrastructure.Dv;
using ILogger = MediaPortal.Common.Logging.ILogger;
using UPnPLogger = UPnP.Infrastructure.ILogger;

namespace MediaPortal.UI.Services.FrontendServer
{
  public class FrontendServer : IFrontendServer, IDisposable
  {
    protected AsynchronousMessageQueue _messageQueue;

    public const string MP2SERVER_DEVICEVERSION = "MediaPortal-2-Client/1.0";
    public const string MP2_HTTP_SERVER_NAME = "MediaPortal 2 (Client) Web Server";

    public class UPnPLoggerDelegate : UPnPLogger
    {
      public void Debug(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Debug(format, args);
      }

      public void Debug(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Debug(format, ex, args);
      }

      public void Info(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Info(format, args);
      }

      public void Info(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Info(format, ex, args);
      }

      public void Warn(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Warn(format, args);
      }

      public void Warn(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Warn(format, ex, args);
      }

      public void Error(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Error(format, args);
      }

      public void Error(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Error(format, ex, args);
      }

      public void Error(Exception ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Error(ex);
      }

      public void Critical(string format, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(format, args);
      }

      public void Critical(string format, Exception ex, params object[] args)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(format, ex, args);
      }

      public void Critical(Exception ex)
      {
        ILogger logger = ServiceRegistration.Get<ILogger>(false);
        if (logger != null)
          logger.Critical(ex);
      }
    }

    protected readonly UPnPFrontendServer _upnpServer;

    public FrontendServer()
    {
      ServerSettings serverSettings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      UPnPConfiguration.PRODUCT_VERSION = MP2SERVER_DEVICEVERSION;
      UPnPConfiguration.LOGGER = new UPnPLoggerDelegate();
      UPnPConfiguration.USE_IPV4 = serverSettings.UseIPv4;
      UPnPConfiguration.USE_IPV6 = serverSettings.UseIPv6;
      UPnPConfiguration.IP_ADDRESS_BINDINGS = serverSettings.IPAddressBindingsList;
      HttpResponse.HTTP_SERVER_NAME = MP2_HTTP_SERVER_NAME;

      ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
      _upnpServer = new UPnPFrontendServer(systemResolver.LocalSystemId);
      _messageQueue = new AsynchronousMessageQueue(this, new string[] { });
      _messageQueue.Start();
    }

    public void Dispose()
    {
      _upnpServer.Dispose();
      _messageQueue.Shutdown();
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.Resuming)
            {
              ServiceRegistration.Get<ILogger>().Info("FrontendServer: System resuming. Trigger OnNetworkAddressChanged for UPnPServer.");
              _upnpServer.OnNetworkAddressChanged(this, null);
            }
            break;
        }
      }
    }

    #region IFrontendServer implementation

    public UPnPServer UPnPFrontendServer
    {
      get { return _upnpServer; }
    }

    public void Startup()
    {
      SubscribeToMessages();
      _upnpServer.Start();
    }

    public void Shutdown()
    {
      _upnpServer.Stop();
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(SystemMessaging.CHANNEL);
      _messageQueue.PreviewMessage += OnMessageReceived;
    }

    public void UpdateUPnPConfiguration()
    {
      _upnpServer.UpdateConfiguration();
    }

    #endregion
  }
}