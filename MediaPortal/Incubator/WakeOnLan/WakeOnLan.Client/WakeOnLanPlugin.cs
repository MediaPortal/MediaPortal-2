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
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.UI.ServerCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using WakeOnLan.Client.Helpers;
using WakeOnLan.Client.Settings;
using WakeOnLan.Interfaces;

namespace WakeOnLan.Client
{
  public class WakeOnLanPlugin : IPluginStateTracker
  {
    #region Protected Members

    protected AsynchronousMessageQueue _messageQueue;
    protected bool _suspended;

    #endregion

    #region IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      //Try to get the server address immediately in case we miss the connected message
      UpdateSavedServerAddress();
      SubscribeToMessages();
      DoWakeServer();
    }

    public void Continue()
    {

    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Shutdown()
    {

    }

    public void Stop()
    {
      UnsubscribeFromMessages();
    }

    #endregion

    #region Message Handling

    protected void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new[] { ServerConnectionMessaging.CHANNEL, SystemMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected virtual void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType type = (ServerConnectionMessaging.MessageType)message.MessageType;
        switch (type)
        {
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
            UpdateSavedServerAddress();
            break;
        }
      }
      else if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.Suspending)
            {
              _suspended = true;
            }
            else if (newState == SystemState.Resuming && _suspended)
            {
              _suspended = false;
              DoWakeServer();
            }
            break;
        }
      }
    }

    #endregion

    #region WOL Sending

    /// <summary>
    /// Asynchronously runs the server wake task
    /// </summary>
    protected void DoWakeServer()
    {
      Task.Run(WakeServerAsync);
    }

    /// <summary>
    /// Tries to determine whether the server is awake and if not tries to wake the server by sending
    /// a WOL packet to the address saved in settings.
    /// </summary>
    /// <returns></returns>
    protected async Task WakeServerAsync()
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      var settings = sm.Load<WakeOnLanSettings>();
      WakeOnLanAddress wolAddress = settings.ServerWakeOnLanAddress;
      if (wolAddress == null || !WakeOnLanHelper.IsValidHardwareAddress(wolAddress.HardwareAddress))
      {
        ServiceRegistration.Get<ILogger>().Debug("WakeOnLanClient: No address stored for the server yet");
        return;
      }

      ServiceRegistration.Get<ILogger>().Info("WakeOnLanHelper: Waking server at {0} using port {1}", wolAddress.IPAddress, settings.Port);
      try
      {
        await WakeOnLanHelper.WakeServer(wolAddress.IPAddress, wolAddress.HardwareAddress, settings.Port, settings.PingTimeout, settings.WakeTimeout);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("WakeOnLanClient: Error waking server", ex);
      }
    }

    #endregion

    #region Address Updating

    /// <summary>
    /// Tries to match an IP/hardware address from server settings to the current server connection and saves the hardware address to settings.
    /// </summary>
    protected void UpdateSavedServerAddress()
    {
      IPAddress serverAddress;
      if (!TryGetServerIPAddress(out serverAddress))
        return;

      if (IPAddress.IsLoopback(serverAddress))
      {
        ServiceRegistration.Get<ILogger>().Debug("WakeOnLanClient: Not configuring wake on lan for loopback address '{0}', server is located on the same machine", serverAddress);
        SaveWakeOnLanAddress(null);
        return;
      }

      WakeOnLanAddress wolAddress;
      if (TryMatchWakeOnLanAddress(serverAddress, out wolAddress))
      {
        ServiceRegistration.Get<ILogger>().Info("WakeOnLanClient: Updating server hardware address for IP address '{0}'", serverAddress);
        SaveWakeOnLanAddress(wolAddress);
        return;
      }
      ServiceRegistration.Get<ILogger>().Warn("WakeOnLanClient: Unable to determine hardware address for IP address '{0}'", serverAddress);
    }

    /// <summary>
    /// Tries to determine the server IP address from the current server connection.
    /// </summary>
    /// <param name="serverAddress">If successful, contains the IP address of the server.</param>
    /// <returns></returns>
    protected bool TryGetServerIPAddress(out IPAddress serverAddress)
    {
      serverAddress = null;
      try
      {
        var cp = ServiceRegistration.Get<IServerConnectionManager>().ControlPoint;
        if (cp == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("WakeOnLanClient: Could not get server IP address, UPnPControlPoint not found");
          return false;
        }

        if (cp.Connection == null)
          //Don't log here, the server isn't connected (yet), we'll retry getting the address when it connects.
          return false;

        ServerDescriptor serverDescriptor = ServerDescriptor.GetMPBackendServerDescriptor(cp.Connection.RootDescriptor);
        if (serverDescriptor == null)
        {
          ServiceRegistration.Get<ILogger>().Warn("WakeOnLanClient: Could not get server IP address, unable to verify UPnP root descriptor");
          return false;
        }

        SystemName preferredLink = serverDescriptor.GetPreferredLink();
        if (IPAddress.TryParse(preferredLink.Address, out serverAddress))
        {
          ServiceRegistration.Get<ILogger>().Debug("WakeOnLanClient: Got server IP address '{0}' for '{1}'", serverAddress, preferredLink.HostName);
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("WakeOnLanClient: Error getting server IP address", ex);
      }
      return false;
    }

    /// <summary>
    /// Tries to match a hardware address from settings to the provided <paramref name="serverAddress"/>.
    /// </summary>
    /// <param name="serverAddress">The IP address of the server.</param>
    /// <param name="wolAddress">If successful, the matching WakeOnLanAddress setting for the <paramref name="serverAddress"/>.</param>
    /// <returns></returns>
    protected bool TryMatchWakeOnLanAddress(IPAddress serverAddress, out WakeOnLanAddress wolAddress)
    {
      wolAddress = null;
      var ssm = ServiceRegistration.Get<IServerSettingsClient>();
      var settings = ssm.Load<WakeOnLanServerSettings>();
      List<WakeOnLanAddress> addresses = settings.WakeOnLanAddresses;
      if (addresses == null || addresses.Count == 0)
      {
        ServiceRegistration.Get<ILogger>().Warn("WakeOnLanClient: Unable to determine server hardware address, no addresses found in settings");
        return false;
      }
      wolAddress = addresses.FirstOrDefault(a => IsValidAddress(a, serverAddress));
      return wolAddress != null;
    }

    /// <summary>
    /// Determines whether the provided <paramref name="wolAddress"/> matches the provided <paramref name="serverAddress"/>
    /// and contains a valid hardware address.
    /// </summary>
    /// <param name="wolAddress">IP/Hardware address combination</param>
    /// <param name="serverAddress">Known IP address of the server</param>
    /// <returns></returns>
    protected bool IsValidAddress(WakeOnLanAddress wolAddress, IPAddress serverAddress)
    {
      IPAddress ipAddress;
      return IPAddress.TryParse(wolAddress.IPAddress, out ipAddress) && ipAddress.Equals(serverAddress) && WakeOnLanHelper.IsValidHardwareAddress(wolAddress.HardwareAddress);
    }

    /// <summary>
    /// Saves the provided <paramref name="wolAddress"/> to client settings.
    /// </summary>
    /// <param name="wolAddress">The WakeOnLanAddress for the server.</param>
    protected void SaveWakeOnLanAddress(WakeOnLanAddress wolAddress)
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      var settings = sm.Load<WakeOnLanSettings>();
      settings.ServerWakeOnLanAddress = wolAddress;
      sm.Save(settings);
    }

    #endregion
  }
}