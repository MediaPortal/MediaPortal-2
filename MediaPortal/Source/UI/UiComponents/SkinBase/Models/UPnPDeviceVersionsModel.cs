#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.SkinBase.General;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.Description;
using MediaPortal.UI.SkinEngine.Controls.Visuals;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Model which attends the workflow state "Show versions".
  /// </summary>
  public class UPnPDeviceVersionsModel : IDisposable
  {
    #region Consts

    public const string STR_UPNP_DEVICE_INFO_MODEL_ID = "5D2EE1D6-BE95-4E21-B34C-C1F204B565D7";
    public static Guid UPNP_DEVICE_INFO_MODEL_ID = new Guid(STR_UPNP_DEVICE_INFO_MODEL_ID);
    private static Regex RE_NAME_FILTER = new Regex("^(MediaPortal)|(MP2)", RegexOptions.IgnoreCase);

    #endregion

    #region Protected fields

    protected UPnPNetworkTracker _networkTracker;

    public ItemsList Devices = new ItemsList();

    #endregion

    public UPnPDeviceVersionsModel()
    {
      CPData cpData = new CPData();
      _networkTracker = new UPnPNetworkTracker(cpData);
      _networkTracker.RootDeviceAdded += networkTracker_RootDeviceAdded;
      _networkTracker.RootDeviceRemoved += networkTracker_RootDeviceRemoved;
      _networkTracker.Start();
    }

    public virtual void Dispose()
    {
      _networkTracker.Dispose();
    }

    public void Refresh()
    {
      _networkTracker.SharedControlPointData.SSDPController.SearchAll(null);
    }

    private void networkTracker_RootDeviceAdded(RootDescriptor rootdescriptor)
    {
      UpdateDevices();
    }

    private void UpdateDevices()
    {
      IDictionary<string, RootDescriptor> knownDevices = _networkTracker.KnownRootDevices;
      if (knownDevices == null)
      {
        return;
      }

      Devices.Clear();
      var tmpList = new List<ListItem>();
      foreach (RootDescriptor rootDescriptor in knownDevices.Values)
      {
        if (rootDescriptor.State != RootDescriptorState.Ready)
          continue;
        DeviceDescriptor deviceDescriptor = DeviceDescriptor.CreateRootDeviceDescriptor(rootDescriptor);

        var serverDescriptor = ServerDescriptor.GetMPBackendServerDescriptor(rootDescriptor);
        var clientDescriptor = ClientDescriptor.GetMPFrontendServerDescriptor(rootDescriptor);

        SystemName systemName = GetSystemName(rootDescriptor);

        if (serverDescriptor == null && clientDescriptor == null && !RE_NAME_FILTER.IsMatch(deviceDescriptor.FriendlyName))
          continue;

        string deviceUUID = serverDescriptor?.MPBackendServerUUID ?? clientDescriptor?.MPFrontendServerUUID;

        ListItem item = new ListItem();
        if (systemName != null)
        {
          item.SetLabel("HostNameWithAddress", $"{systemName.HostName} ({systemName.Address})");
          item.SetLabel("HostName", systemName.HostName);
          item.SetLabel("IPAddress", systemName.Address);
        }
        item.SetLabel("FriendlyName", deviceDescriptor.FriendlyName);
        item.SetLabel("SoftwareVersion", deviceDescriptor.SoftwareVersion);
        item.SetLabel("Icon",
          serverDescriptor != null ?
            "MP2Server.png" :
            (clientDescriptor != null ? "MP2Client.png" : ""));
        tmpList.Add(item);
      }

      foreach (var listItem in tmpList.OrderBy(l => l["FriendlyName"]))
      {
        Devices.Add(listItem);
      }
      Devices.FireChange();
    }

    private SystemName GetSystemName(RootDescriptor rootDescriptor)
    {
      DeviceDescriptor deviceDescriptor = DeviceDescriptor.CreateRootDeviceDescriptor(rootDescriptor);
      if (deviceDescriptor == null)
        return null;

      try
      {
        return new SystemName(new Uri(deviceDescriptor.RootDescriptor.SSDPRootEntry.PreferredLink.DescriptionLocation).Host);
      }
      catch
      {
        return null;
      }
    }

    private void networkTracker_RootDeviceRemoved(RootDescriptor rootdescriptor)
    {
      UpdateDevices();
    }

    public Guid ModelId
    {
      get { return UPNP_DEVICE_INFO_MODEL_ID; }
    }

  }
}
