#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Utilities.Network;
using System;
using System.Net;
using System.Threading.Tasks;
using WakeOnLan.Client.Settings;
using WakeOnLan.Common;

namespace WakeOnLan.Client
{
  public class WakeOnLanPlugin : IPluginStateTracker
  {
    #region Protected Members

    protected AsynchronousMessageQueue _messageQueue;
    protected bool _suspended;
    protected readonly object _addressUpdateSync = new object();
    protected readonly object _wolSendSync = new object();
    protected bool _isUpdatingAddress;
    protected bool _isSendingWol;

    #endregion

    #region IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      SubscribeToMessages();
      Task.Run(InitAsync);
    }

    protected async Task InitAsync()
    {
      //Try to get the server address immediately in case we miss the connected message
      UpdateServerAddress();
      await WakeServerAsync();
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
            UpdateServerAddress();
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
      if (!settings.EnableWakeOnLan)
        return;

      lock (_wolSendSync)
      {
        if (_isSendingWol)
          //wol request already in progress
          return;
        _isSendingWol = true;
      }

      try
      {
        WakeOnLanAddress wolAddress = settings.ServerWakeOnLanAddress;
        if (wolAddress == null || !WakeOnLanHelper.IsValidHardwareAddress(wolAddress.HardwareAddress))
        {
          ServiceRegistration.Get<ILogger>().Debug("WakeOnLanClient: No address stored for the server yet");
          return;
        }

        //Wait for the network connection to become available, can be delayed if we have just woken from sleep
        if (!await WaitForNetworkConnection(settings.NetworkConnectedTimeout))
        {
          ServiceRegistration.Get<ILogger>().Warn("WakeOnLanClient: No network connection found within timeout {0}ms", settings.NetworkConnectedTimeout);
          return;
        }
                
        ServiceRegistration.Get<ILogger>().Info("WakeOnLanHelper: Waking server at {0} using port {1}", wolAddress.IPAddress, settings.Port);
        await WakeOnLanHelper.WakeServer(NetworkUtils.GetAllLocalIPv4Networks(),
          wolAddress.IPAddress, wolAddress.HardwareAddress, settings.Port, settings.PingTimeout, settings.WakeTimeout);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("WakeOnLanClient: Error waking server", ex);
      }
      finally
      {
        lock (_wolSendSync)
          _isSendingWol = false;
      }
    }

    protected async Task<bool> WaitForNetworkConnection(int timeout)
    {
      if (NetworkUtils.IsNetworkAvailable(null, false))
        return true;

      DateTime end = DateTime.Now.AddMilliseconds(timeout);
      while (DateTime.Now < end)
      {
        await Task.Delay(2000);
        if (NetworkUtils.IsNetworkAvailable(null, false))
          return true;
      }
      return false;
    }

    #endregion

    #region Address Updating

    /// <summary>
    /// Tries to resolve the hardware address of the server.
    /// </summary>
    protected void UpdateServerAddress()
    {
      lock (_addressUpdateSync)
      {
        if (_isUpdatingAddress)
          //Address updating already in progress
          return;
        _isUpdatingAddress = true;
      }

      try
      {
        IPAddress localAddress;
        IPAddress serverAddress;
        if (!TryGetServerIPAddress(out localAddress, out serverAddress))
          return;

        if (IPAddress.IsLoopback(serverAddress))
        {
          ServiceRegistration.Get<ILogger>().Debug("WakeOnLanClient: Not configuring wake on lan for loopback address '{0}', server is located on the same machine", serverAddress);
          SaveWakeOnLanAddress(null);
          return;
        }

        byte[] macAddress;
        if (NetworkInformationHelper.TryGetRemoteHardwareAddress(localAddress, serverAddress, out macAddress))
        {
          ServiceRegistration.Get<ILogger>().Info("WakeOnLanClient: Updating server hardware address '{0}' for IP address '{1}'", BitConverter.ToString(macAddress), serverAddress);
          SaveWakeOnLanAddress(new WakeOnLanAddress() { IPAddress = serverAddress.ToString(), HardwareAddress = macAddress });
          return;
        }
        ServiceRegistration.Get<ILogger>().Warn("WakeOnLanClient: Unable to determine hardware address for IP address '{0}'", serverAddress);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("WakeOnLanClient: Error determining server hardware address", ex);
      }
      finally
      {
        lock (_addressUpdateSync)
          _isUpdatingAddress = false;
      }
    }

    /// <summary>
    /// Tries to determine the server IP address from the current server connection.
    /// </summary>
    /// <param name="serverAddress">If successful, contains the IP address of the server.</param>
    /// <returns></returns>
    protected bool TryGetServerIPAddress(out IPAddress localAddress, out IPAddress serverAddress)
    {
      localAddress = null;
      serverAddress = null;

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
        localAddress = cp.Connection.RootDescriptor.SSDPRootEntry.PreferredLink.Endpoint.EndPointIPAddress;
        ServiceRegistration.Get<ILogger>().Debug("WakeOnLanClient: Got server IP address '{0}' for '{1}', local address '{2}'", serverAddress, preferredLink.HostName, localAddress);
        return true;
      }
      return false;
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