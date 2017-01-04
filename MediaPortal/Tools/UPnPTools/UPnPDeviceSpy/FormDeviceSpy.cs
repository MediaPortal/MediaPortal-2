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

using System.Collections.Generic;
using System.Windows.Forms;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.Description;

namespace UPnPDeviceSpy
{
  public partial class FormDeviceSpy : Form
  {
    protected UPnPNetworkTracker _networkTracker;

    public FormDeviceSpy()
    {
      InitializeComponent();
      CPData cpData = new CPData();
      _networkTracker = new UPnPNetworkTracker(cpData);
    }

    protected void UpdateTreeView()
    {
      UpdateTreeView(false);
    }

    protected void UpdateTreeView(bool showPending)
    {
      TreeNodeCollection nodes = tvDeviceTree.Nodes;
      nodes.Clear();
      TreeNode devicesNode = nodes.Add("DevicesKey", "Devices");
      IDictionary<string, RootDescriptor> knownDevices = _networkTracker.KnownRootDevices;
      try
      {
        if (knownDevices == null)
        {
          if (showPending)
            devicesNode.Nodes.Add(null, " - pending -");
          return;
        }
        foreach (RootDescriptor rootDescriptor in knownDevices.Values)
        {
          if (rootDescriptor.State != RootDescriptorState.Ready)
            continue;
          string deviceUUID = rootDescriptor.SSDPRootEntry.RootDeviceUUID;
          DeviceDescriptor deviceDescriptor = DeviceDescriptor.CreateRootDeviceDescriptor(rootDescriptor);
          TreeNode deviceNode = devicesNode.Nodes.Add(deviceUUID, BuildDeviceName(deviceDescriptor));
          deviceNode.Tag = new UPnPDeviceDescriptor(deviceDescriptor);
        }
      }
      finally
      {
        devicesNode.Expand();
      }
    }

    protected void ClearDetails()
    {
      tbDetails.Clear();
    }

    protected void ShowDetails(UPnPNodeDescriptor descriptor)
    {
      tbDetails.Clear();
      UPnPDeviceDescriptor deviceDescriptor = descriptor as UPnPDeviceDescriptor;
      if (deviceDescriptor != null)
        tbDetails.Text = deviceDescriptor.Descriptor.RootDescriptor.DeviceDescription.CreateNavigator().OuterXml;
    }

    protected string BuildDeviceName(DeviceDescriptor descriptor)
    {
      return descriptor.FriendlyName + " (" + descriptor.RootDescriptor.SSDPRootEntry.PreferredLink.DescriptionLocation + ")";
    }

    private void exitToolStripMenuItem_Click(object sender, System.EventArgs e)
    {
      Close();
    }

    private void FormDeviceSpy_Shown(object sender, System.EventArgs e)
    {
      UpdateTreeView(true);
      _networkTracker.RootDeviceAdded += networkTracker_RootDeviceAdded;
      _networkTracker.RootDeviceRemoved += networkTracker_RootDeviceRemoved;
      _networkTracker.Start();
    }

    protected delegate void Dlgt();

    protected void ExecuteInUIThread(Dlgt dlgt)
    {
      Invoke(dlgt);
    }

    private void networkTracker_RootDeviceAdded(RootDescriptor rootdescriptor)
    {
      ExecuteInUIThread(UpdateTreeView);
    }

    private void networkTracker_RootDeviceRemoved(RootDescriptor rootdescriptor)
    {
      ExecuteInUIThread(UpdateTreeView);
    }

    private void FormDeviceSpy_FormClosing(object sender, FormClosingEventArgs e)
    {
      _networkTracker.Close();
    }

    private void bRefresh_Click(object sender, System.EventArgs e)
    {
      UpdateTreeView();
    }

    private void tvDeviceTree_AfterSelect(object sender, TreeViewEventArgs e)
    {
      TreeNode selectedNode = tvDeviceTree.SelectedNode;
      if (selectedNode == null)
        ClearDetails();
      else
        ShowDetails(selectedNode.Tag as UPnPNodeDescriptor);
    }

    private void bUPnPSearch_Click(object sender, System.EventArgs e)
    {
      _networkTracker.SharedControlPointData.SSDPController.SearchAll(null);
    }
  }
}
