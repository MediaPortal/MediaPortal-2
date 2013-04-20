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
using System.Linq;
using System.Text;
using MediaPortal.Common.General;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using NativeWifi;
using MediaPortal.UiComponents.SkinBase.General;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  public class WifiConnectionModel : IDisposable
  {
    #region Consts

    public const string STR_WIFI_CONNECTION_MODEL_ID = "0A5A7384-72C5-412F-BCD4-6FBD64238858";
    public static Guid WIFI_CONNECTION_MODEL_ID = new Guid(STR_WIFI_CONNECTION_MODEL_ID);

    #endregion

    #region Protected fields

    protected AbstractProperty _isWifiAvailableProperty;
    protected ItemsList _networkList;
    protected AsynchronousMessageQueue _queue;

    #endregion

    public WifiConnectionModel()
    {
      _isWifiAvailableProperty = new WProperty(typeof(bool), WlanClient.Instance.Interfaces.Length > 0);
      _networkList = new ItemsList();

      _queue = new AsynchronousMessageQueue(this, new string[]
        {
            WifiConnectionMessaging.CHANNEL,
        });
      _queue.MessageReceived += OnMessageReceived;
      _queue.Start();

      FindNetworks();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WifiConnectionMessaging.CHANNEL)
      {
        WifiConnectionMessaging.MessageType messageType = (WifiConnectionMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case WifiConnectionMessaging.MessageType.ScanCompleted:
            FindNetworks();
            break;
        }
      }
    }

    #region Public members to be called from the GUI

    public AbstractProperty IsWifiAvailableProperty
    {
      get { return _isWifiAvailableProperty; }
    }

    public bool IsWifiAvailable
    {
      get { return (bool)_isWifiAvailableProperty.GetValue(); }
      set { _isWifiAvailableProperty.SetValue(value); }
    }

    public ItemsList Networks
    {
      get { return _networkList; }
    }

    #endregion

    /// <summary>
    /// Converts a 802.11 SSID to a string.
    /// </summary>
    internal static string GetStringForSSID(Wlan.Dot11Ssid ssid)
    {
      return Encoding.ASCII.GetString(ssid.SSID, 0, (int)ssid.SSIDLength);
    }

    void FindNetworks()
    {
      Dictionary<string, List<Tuple<Wlan.WlanAvailableNetwork, WlanClient.WlanInterface>>> hashedNetworks = new Dictionary<string, List<Tuple<Wlan.WlanAvailableNetwork, WlanClient.WlanInterface>>>();
      foreach (WlanClient.WlanInterface wlanIface in WlanClient.Instance.Interfaces)
      {
        try
        {
          Wlan.WlanAvailableNetwork[] networks = wlanIface.GetAvailableNetworkList(0);
          foreach (Wlan.WlanAvailableNetwork network in networks)
          {
            string ssid = GetStringForSSID(network.dot11Ssid);
            if (!string.IsNullOrEmpty(ssid))
            {
              List<Tuple<Wlan.WlanAvailableNetwork, WlanClient.WlanInterface>> networksForSSID = null;
              if (!hashedNetworks.TryGetValue(ssid, out networksForSSID)) hashedNetworks[ssid] = networksForSSID = new List<Tuple<Wlan.WlanAvailableNetwork, WlanClient.WlanInterface>>();
              networksForSSID.Add(new Tuple<Wlan.WlanAvailableNetwork, WlanClient.WlanInterface>(network, wlanIface));
            }
          }
        }
        catch
        {
          // the network interface might be disabled - wrapper throws an exception in that case when getting the available networks
        }
      }

      Networks.Clear();

      foreach (var ssidNetwork in hashedNetworks.OrderByDescending(hn => hn.Value.Max(n => n.Item1.wlanSignalQuality)))
      {
        var networkInfo = ssidNetwork.Value.OrderByDescending(n => n.Item1.wlanSignalQuality).First();
        var network = networkInfo.Item1;
        if (network.networkConnectable)
        {
          string readableSSID = GetStringForSSID(network.dot11Ssid);
          if (string.IsNullOrWhiteSpace(readableSSID)) readableSSID = "No SSID";
          ListItem item = new ListItem(Consts.KEY_NAME, readableSSID, false);
          item.Selected = (network.flags & Wlan.WlanAvailableNetworkFlags.Connected) == Wlan.WlanAvailableNetworkFlags.Connected;
          item.AdditionalProperties["SignalStrength"] = (float)network.wlanSignalQuality /100.0f;
          item.AdditionalProperties["Secure"] = network.securityEnabled;
          item.AdditionalProperties["HasProfile"] = (network.flags & Wlan.WlanAvailableNetworkFlags.HasProfile) == Wlan.WlanAvailableNetworkFlags.HasProfile;
          item.AdditionalProperties["Network"] = network;
          item.AdditionalProperties["Interface"] = networkInfo.Item2;
          Networks.Add(item);
        }
      }

      Networks.FireChange();
    }

    public void Dispose()
    {
      if (_queue != null)
        _queue.Shutdown();
      _queue = null;
      WlanClient.Instance.Dispose();
    }
  }
}
