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

using System;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Configuration;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.Settings;
using MediaPortal.Plugins.ServerSettings.UPnP;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.Plugins.ServerSettings.Settings
{
  /// <summary>
  /// Helper class to enable/disable configuration items based on the availability of server connection. Items need to register
  /// themself using the <see cref="RegisterConfiguration"/> method, and unregister using <see cref="UnregisterConfiguration"/>
  /// (usually done in <see cref="IDisposable.Dispose"/> of item).
  /// </summary>
  public class ConnectionMonitor : IDisposable
  {
    public static ConnectionMonitor Instance = new ConnectionMonitor();

    protected readonly IList<ConfigBase> _configItems = new List<ConfigBase>();

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType type = (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (type)
        {
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
            Enable(true);
            RefreshNetworkNeighborhoodResourceProviderSettings();
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            Enable(false);
            break;
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
            RegisterService();
            Visible(true);
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
            Visible(false);
            break;
        }
      }
    }

    private void RegisterService()
    {
      // After the home server got attached we need to register our service proxy.
      ServiceRegistration.Get<ServerSettingsProxyRegistration>().RegisterService();
    }

    private void Visible(bool newState)
    {
      foreach (var configItem in _configItems)
      {
        ServiceRegistration.Get<ILogger>().Debug("ConnectionMonitor: Setting Configuration '{0}' visibility to {1}", configItem.Text, newState);
        configItem.Visible = newState;
      }
    }

    private void Enable(bool newState)
    {
      foreach (var configItem in _configItems)
      {
        ServiceRegistration.Get<ILogger>().Debug("ConnectionMonitor: Setting Configuration '{0}' enabled to {1}", configItem.Text, newState);
        configItem.Enabled = newState;
      }
    }

    /// <summary>
    /// Refreshes the <see cref="NetworkNeighborhoodResourceProviderSettings"/> and stores them locally
    /// </summary>
    /// <remarks>
    /// This is a workaround that makes sure that we have changes made to the <see cref="NetworkNeighborhoodResourceProviderSettings"/>
    /// by another MP2-Client locally available at least after a restart of this MP2-Client or the MP2-Server.
    /// ToDo: Remove this once we have SystemSettings that are automatically updated.
    /// </remarks>
    private void RefreshNetworkNeighborhoodResourceProviderSettings()
    {
      var settings = ServiceRegistration.Get<IServerSettingsClient>().Load<NetworkNeighborhoodResourceProviderSettings>();
      ServiceRegistration.Get<ISettingsManager>().Save(settings);
      ServiceRegistration.Get<ILogger>().Debug("ConnectionMonitor: Server connected, NetworkNeighborhoodResourceProviderSettings refreshed.");
    }

    #region IViewChangeNotificator implementation

    public void Dispose()
    {
      if (_messageQueue != null)
      {
        _messageQueue.Terminate();
        _messageQueue = null;
      }
    }

    public void Install()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            ServerConnectionMessaging.CHANNEL
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    public void RegisterConfiguration(ConfigBase configItem)
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      // Set initial "Enabled" value, depending on current connection state.
      configItem.Enabled = cd != null;
      // Set initial "Visible" value, depending on attachment state.
      configItem.Visible = scm.HomeServerSystemId != null;
      _configItems.Add(configItem);
    }

    public void UnregisterConfiguration(ConfigBase configItem)
    {
      _configItems.Remove(configItem);
    }

    #endregion

  }
}
