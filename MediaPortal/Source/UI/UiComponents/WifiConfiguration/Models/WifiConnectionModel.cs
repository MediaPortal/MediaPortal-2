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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.SkinBase.General;
using NativeWifi;

namespace MediaPortal.UiComponents.WifiConfiguration.Models
{
  public class WifiConnectionModel : IWorkflowModel, IDisposable
  {
    #region Consts

    public const string STR_WIFI_CONNECTION_MODEL_ID = "0A5A7384-72C5-412F-BCD4-6FBD64238858";
    public static Guid WIFI_CONNECTION_MODEL_ID = new Guid(STR_WIFI_CONNECTION_MODEL_ID);
    const int CONNECTION_TIMEOUT_SECONDS = 30;

    #endregion

    #region Protected fields

    protected AbstractProperty _isWifiAvailableProperty;
    protected ItemsList _networkList;
    protected AsynchronousMessageQueue _queue;
    protected ListItem _currentItem;

    #endregion

    #region Constructor

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
    }

    #endregion

    #region Public members to be called from the GUI

    public AbstractProperty IsWifiAvailableProperty
    {
      get { return _isWifiAvailableProperty; }
    }

    public bool IsWifiAvailable
    {
      get { return (bool) _isWifiAvailableProperty.GetValue(); }
      set { _isWifiAvailableProperty.SetValue(value); }
    }

    public ItemsList Networks
    {
      get { return _networkList; }
    }

    public void SetCurrentItem(ListItem currentItem)
    {
      _currentItem = currentItem;
    }

    public ItemsList GetOptionsList
    {
      get
      {
        ItemsList options = new ItemsList();
        if (_currentItem != null)
        {
          if (_currentItem.Selected)
          {
            var item = new ListItem(Consts.KEY_NAME, "[Network.Disconnect]");
            item.AdditionalProperties.Add(Consts.KEY_PATH, "Disconnect");
            options.Add(item);
          }
          else
          {
            var item = new ListItem(Consts.KEY_NAME, "[Network.Connect]");
            item.AdditionalProperties.Add(Consts.KEY_PATH, "Connect");
            options.Add(item);
          }
          if ((bool) _currentItem.AdditionalProperties["HasProfile"])
          {
            var item = new ListItem(Consts.KEY_NAME, "[Network.DeleteProfile]");
            item.AdditionalProperties.Add(Consts.KEY_PATH, "DeleteProfile");
            options.Add(item);
          }
        }
        return options;
      }
    }

    public void ExecuteAction(ListItem option)
    {
      ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
      var nic = _currentItem.AdditionalProperties["Interface"] as WlanClient.WlanInterface;
      if (nic == null)
        return;
      var network = ((Wlan.WlanAvailableNetwork) _currentItem.AdditionalProperties["Network"]);
      switch (option.AdditionalProperties[Consts.KEY_PATH] as string)
      {
        case "Disconnect":
          nic.Disconnect();
          FindNetworks();
          break;
        case "Connect":
          ConnectNetwork(_currentItem.Labels[Consts.KEY_NAME].Evaluate(), nic, network);
          break;
        case "DeleteProfile":
          nic.DeleteProfile(network.profileName);
          FindNetworks();
          break;
      }
    }

    public void ConnectWithPassword(string key)
    {
      if (!string.IsNullOrWhiteSpace(key))
      {
        ConnectNewSecureNetwork(
            _currentItem.Labels[Consts.KEY_NAME].Evaluate(),
            key,
            (WlanClient.WlanInterface) _currentItem.AdditionalProperties["Interface"],
            (Wlan.WlanAvailableNetwork) _currentItem.AdditionalProperties["Network"]);
      }
    }

    #endregion

    #region IDisposable implementation

    public void Dispose()
    {
      if (_queue != null)
        _queue.Shutdown();
      _queue = null;
      WlanClient.Instance.Dispose();
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return WIFI_CONNECTION_MODEL_ID; }
    }

    public bool CanEnterState(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
      FindNetworks();
    }

    public void ExitModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
    }

    public void ChangeModelContext(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext, bool push)
    {
    }

    public void Deactivate(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
    }

    public void Reactivate(UI.Presentation.Workflow.NavigationContext oldContext, UI.Presentation.Workflow.NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(UI.Presentation.Workflow.NavigationContext context, IDictionary<Guid, UI.Presentation.Workflow.WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(UI.Presentation.Workflow.NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    #region Private members

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WifiConnectionMessaging.CHANNEL)
      {
        WifiConnectionMessaging.MessageType messageType = (WifiConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case WifiConnectionMessaging.MessageType.ScanCompleted:
            FindNetworks();
            break;
        }
      }
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
            string ssid = Helper.GetStringForSSID(network.dot11Ssid);
            if (!string.IsNullOrEmpty(ssid))
            {
              List<Tuple<Wlan.WlanAvailableNetwork, WlanClient.WlanInterface>> networksForSSID;
              if (!hashedNetworks.TryGetValue(ssid, out networksForSSID)) 
                hashedNetworks[ssid] = networksForSSID = new List<Tuple<Wlan.WlanAvailableNetwork, WlanClient.WlanInterface>>();
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
          string readableSSID = Helper.GetStringForSSID(network.dot11Ssid);
          if (string.IsNullOrWhiteSpace(readableSSID)) readableSSID = "No SSID";
          ListItem item = new ListItem(Consts.KEY_NAME, readableSSID, false);
          item.Selected = (network.flags & Wlan.WlanAvailableNetworkFlags.Connected) == Wlan.WlanAvailableNetworkFlags.Connected;
          item.AdditionalProperties["SignalStrength"] = network.wlanSignalQuality / 100.0f;
          item.AdditionalProperties["Secure"] = network.securityEnabled;
          item.AdditionalProperties["HasProfile"] = (network.flags & Wlan.WlanAvailableNetworkFlags.HasProfile) == Wlan.WlanAvailableNetworkFlags.HasProfile;
          item.AdditionalProperties["Network"] = network;
          item.AdditionalProperties["Interface"] = networkInfo.Item2;
          Networks.Add(item);
        }
      }

      Networks.FireChange();
    }

    void ConnectNetwork(string ssid, WlanClient.WlanInterface nic, Wlan.WlanAvailableNetwork network)
    {
      var log = ServiceRegistration.Get<ILogger>();
      if (!string.IsNullOrEmpty(network.profileName))
      {
        try
        {
          log.Info("WifiConfiguration: Using existing Profile to connect to WLAN '{0}'", ssid);
          bool success = nic.ConnectSynchronously(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, network.profileName, CONNECTION_TIMEOUT_SECONDS);
          if (success)
            FindNetworks();
          else
            log.Warn("Connection to WLAN '{0}' failed.", ssid);
        }
        catch (Exception ex)
        {
          log.Warn("Connection to WLAN '{0}' failed: {1}", ex, ssid, ex.Message);
        }
      }
      else
      {
        if (network.securityEnabled)
        {
          ServiceRegistration.Get<IScreenManager>().ShowDialog("dialogWifiPassword");
        }
        else
        {
          log.Info("WifiConfiguration: Connecting to unsecured WLAN '{0}'", ssid);
          nic.Connect(Wlan.WlanConnectionMode.Auto, Wlan.Dot11BssType.Any, network.dot11Ssid, 0);
        }
      }
    }

    void ConnectNewSecureNetwork(string ssid, string key, WlanClient.WlanInterface nic, Wlan.WlanAvailableNetwork network)
    {
      var log = ServiceRegistration.Get<ILogger>();
      log.Info("WifiConfiguration: Building new Profile to connect to WLAN '{0}'", ssid);
      string profileXml = Helper.GetProfileXml(ssid, key, network.dot11DefaultAuthAlgorithm, network.dot11DefaultCipherAlgorithm);
      if (profileXml != null)
      {
        string error = null;
        try
        {
          Wlan.WlanReasonCode reasonCode = nic.SetProfile(Wlan.WlanProfileFlags.User, profileXml, true);
          if (reasonCode != Wlan.WlanReasonCode.Success) error = reasonCode.ToString();
        }
        catch (Exception ex)
        {
          error = ex.Message;
        }
        if (error == null)
        {
          nic.ConnectSynchronously(Wlan.WlanConnectionMode.Profile, Wlan.Dot11BssType.Any, ssid, CONNECTION_TIMEOUT_SECONDS);
        }
        else
        {
          log.Warn("WifiConfiguration: Setting Profile for WLAN '{0}' failed: '{1}'", ssid, error);
          ServiceRegistration.Get<IDialogManager>().ShowDialog("[Dialogs.ErrorHeaderText]", error, DialogType.OkDialog, false, DialogButtonType.Ok);
        }
      }
      else
      {
        // don't know how to build profile
        log.Warn("WifiConfiguration: No known Mapping to create Profile '{0}' for AuthAlg: '{1}' and CipherAlg: '{2}'", ssid, network.dot11DefaultAuthAlgorithm, network.dot11DefaultCipherAlgorithm);
        ServiceRegistration.Get<IDialogManager>().ShowDialog("[Dialogs.ErrorHeaderText]", "Unable to build profile. Connect in Windows.", DialogType.OkDialog, false, DialogButtonType.Ok);
      }
    }

    #endregion
  }
}
