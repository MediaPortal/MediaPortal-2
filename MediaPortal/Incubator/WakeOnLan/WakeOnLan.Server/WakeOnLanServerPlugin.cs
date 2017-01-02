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
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using UPnP.Infrastructure;
using UPnP.Infrastructure.Utils;
using WakeOnLan.Interfaces;

namespace WakeOnLan.Server
{
  public class WakeOnLanServerPlugin : IPluginStateTracker
  {
    #region Protected Members

    protected AsynchronousMessageQueue _messageQueue;

    #endregion

    #region IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      SubscribeToMessages();
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
      _messageQueue = new AsynchronousMessageQueue(this, new[] { SystemMessaging.CHANNEL });
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
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.Running)
              UpdateHardwareAddresses();
            break;
        }
      }
    }

    #endregion

    #region Settings

    protected void UpdateHardwareAddresses()
    {
      var ips = NetworkHelper.GetUPnPEnabledIPAddresses(UPnPConfiguration.IP_ADDRESS_BINDINGS);
      var nis = NetworkInterface.GetAllNetworkInterfaces();
      List<WakeOnLanAddress> addresses = new List<WakeOnLanAddress>();
      foreach (var ni in nis)
      {
        byte[] hwAddress = ni.GetPhysicalAddress().GetAddressBytes();
        foreach (var ua in ni.GetIPProperties().UnicastAddresses.Where(ua => ips.Contains(ua.Address)))
          addresses.Add(new WakeOnLanAddress() { IPAddress = ua.Address.ToString(), HardwareAddress = hwAddress });
      }
      SaveHardwareAddresses(addresses);
    }

    protected void SaveHardwareAddresses(List<WakeOnLanAddress> addresses)
    {
      var sm = ServiceRegistration.Get<ISettingsManager>();
      var settings = sm.Load<WakeOnLanServerSettings>();
      settings.WakeOnLanAddresses = addresses;
      sm.Save(settings);
    }

    #endregion
  }
}