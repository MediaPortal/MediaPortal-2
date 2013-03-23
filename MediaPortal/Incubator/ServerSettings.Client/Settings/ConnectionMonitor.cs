#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Common.Messaging;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.Plugins.ServerSettings.Settings
{
  /// <summary>
  /// Helper class to enable/disable configuration items based on the availability of server connection. Items need to register
  /// themself using the <see cref="RegisterConfiguration"/> method, and unregister using <see cref="UnregisterConfiguration"/>
  /// (usually done in <see cref="IDisposable.Dispose"/> of item).
  /// </summary>
  public class ConnectionMonitor: IDisposable
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
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            Enable(false);
            break;
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
            Visible(true);
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
            Visible(false);
            break;
        }
      }
    }

    private void Visible(bool newState)
    {
      foreach (var configItem in _configItems)
        configItem.Visible = newState;
    }

    private void Enable(bool newState)
    {
      foreach (var configItem in _configItems)
        configItem.Enabled = newState;
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
      // Set initial "Enabled" value, depending on current connection state
      configItem.Enabled = cd != null;
      _configItems.Add(configItem);
    }

    public void UnregisterConfiguration(ConfigBase configItem)
    {
      _configItems.Remove(configItem);
    }

    #endregion

  }
}
