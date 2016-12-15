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
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.WifiConfiguration.Models;
using NativeWifi;

namespace MediaPortal.UiComponents.WifiConfiguration.Actions
{
  public class FindWirelessNetworksAction : IWorkflowContributor
  {
    #region Consts

    public const string FIND_WIFI_NETWORKS_CONTRIBUTOR_MODEL_ID_STR = "3D5CA839-F47E-43AE-B796-9906D57FAFB0";
    public static readonly Guid FIND_WIFI_NETWORKS_CONTRIBUTOR_MODEL_ID = new Guid(FIND_WIFI_NETWORKS_CONTRIBUTOR_MODEL_ID_STR);

    #endregion

    public FindWirelessNetworksAction()
    {
      _titleRes = LocalizationHelper.CreateResourceString("[Network.SearchForWirelessNetworks]");
    }

    protected IResourceString _titleRes = null;
    protected Dictionary<Guid, WlanClient.WlanInterface> _scanningNICs = new Dictionary<Guid, WlanClient.WlanInterface>();

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public IResourceString DisplayTitle
    {
      get { return _titleRes; }
    }

    public void Initialize()
    {
      
    }

    public void Uninitialize()
    {
      
    }

    public bool IsActionVisible(NavigationContext context)
    {
      return true;
    }

    public bool IsActionEnabled(NavigationContext context)
    {
      return _scanningNICs.Count == 0;
    }

    public void Execute()
    {
      if (_scanningNICs.Count == 0)
      {
        foreach (var nic in WlanClient.Instance.Interfaces)
        {
          nic.WlanNotification += OnWlanNotification;
          _scanningNICs.Add(nic.InterfaceGuid, nic);
          nic.Scan();
        }
        if (StateChanged != null)
          StateChanged();
      }
    }

    void OnWlanNotification(Wlan.WlanNotificationData notifyData)
    {
      if (notifyData.notificationSource == Wlan.WlanNotificationSource.ACM)
      {
        if (_scanningNICs.Count > 0 && (notifyData.notificationCode == (int)Wlan.WlanNotificationCodeAcm.ScanComplete || notifyData.notificationCode == (int)Wlan.WlanNotificationCodeAcm.ScanFail))
        {
          _scanningNICs[notifyData.interfaceGuid].WlanNotification -= OnWlanNotification;
          _scanningNICs.Remove(notifyData.interfaceGuid);
          if (_scanningNICs.Count <= 0)
          {
            if (StateChanged != null)
              StateChanged();

            WifiConnectionMessaging.SendWifiConnectionMessage(WifiConnectionMessaging.MessageType.ScanCompleted);
          }
        }
      }
    }

    #endregion
  }
}
