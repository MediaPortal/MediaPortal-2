using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Xml.XPath;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using UPnP.Infrastructure.CP;

namespace MediaPortal.Plugins.MediaServer.Profiles
{
  public class UPnPDeviceTracker
  {
    #region variables

    private readonly CPData _upnpControlPointData;
    private readonly UPnPNetworkTracker _upnpAgent;
    private readonly UPnPControlPoint _upnpControlPoint;
    public ConcurrentDictionary<string, TrackedDevice> KnownUpnpRootDevices = new ConcurrentDictionary<string, TrackedDevice>();

    #endregion variables

    public UPnPDeviceTracker()
    {
      _upnpControlPointData = new CPData();
      _upnpAgent = new UPnPNetworkTracker(_upnpControlPointData);
      _upnpAgent.RootDeviceAdded += OnUpnpRootDeviceAdded;
      _upnpAgent.RootDeviceRemoved += OnUpnpRootDeviceRemoved;
      _upnpControlPoint = new UPnPControlPoint(_upnpAgent);
    }

    public void Start()
    {
      _upnpControlPoint.Start();
      _upnpAgent.Start();
      Search();

      Logger.Info("Media Server - UPnPDeviceTracker: Started");
    }

    public void Search()
    {
      _upnpAgent.SharedControlPointData.SSDPController.SearchAll(null);
    }

    private void OnUpnpRootDeviceAdded(RootDescriptor rootDescriptor)
    {
      if (rootDescriptor == null || rootDescriptor.SSDPRootEntry == null || rootDescriptor.SSDPRootEntry.RootDeviceUUID == null || rootDescriptor.State != RootDescriptorState.Ready)
      {
        return;
      }

      string remoteHost = new Uri(rootDescriptor.SSDPRootEntry.PreferredLink.DescriptionLocation).Host;
      string uuid = rootDescriptor.SSDPRootEntry.RootDeviceUUID;

      if (!KnownUpnpRootDevices.ContainsKey(uuid))
      {
        Logger.Info("Media Server - Adding Rootdevice: {0}", uuid);

        TrackedDevice trackedDevice = new TrackedDevice
        {
          RootDescriptor = rootDescriptor,
          RemoteHost = IPAddress.Parse(remoteHost)
        };

        XPathNavigator navigator = rootDescriptor.DeviceDescription.CreateNavigator();
        XmlTextReader reader = new XmlTextReader(new StringReader(navigator.InnerXml));
        while (reader.Read())
        {
          if (reader.NodeType != XmlNodeType.Element && reader.NodeType != XmlNodeType.EndElement)
          {
            continue;
          }
          string nodeName = reader.Name;
          
          if (nodeName == "device" && reader.NodeType == XmlNodeType.Element)
          {
            while (reader.Read()) // Read the attributes.
            {
              if (reader.NodeType != XmlNodeType.Element)
                continue;

              switch (reader.Name)
              {
                case "deviceType":
                  trackedDevice.DeviceType = reader.ReadElementContentAsString();
                  break;
                case "friendlyName":
                  trackedDevice.FriendlyName = reader.ReadElementContentAsString();
                  break;
                case "manufacturer":
                  trackedDevice.Manufacturer = reader.ReadElementContentAsString();
                  break;
                case "manufacturerURL":
                  trackedDevice.ManufacturerUrl = reader.ReadElementContentAsString();
                  break;
                case "modelDescription":
                  trackedDevice.ModelDescription = reader.ReadElementContentAsString();
                  break;
                case "modelName":
                  trackedDevice.ModelName = reader.ReadElementContentAsString();
                  break;
                case "modelNumber":
                  trackedDevice.ModelNumber = reader.ReadElementContentAsString();
                  break;
                case "modelURL":
                  trackedDevice.ModelUrl = reader.ReadElementContentAsString();
                  break;
                case "ProductNumber":
                  trackedDevice.ProductNumber = reader.ReadElementContentAsString();
                  break;
                case "Server":
                  trackedDevice.Server = reader.ReadElementContentAsString();
                  break;
                case "UDN":
                  trackedDevice.Udn = reader.ReadElementContentAsString();
                  break;
              }
            }
          }
        }

        Logger.Info("Media Server - Tracked Device: {0}", trackedDevice.ToString());

        if (!KnownUpnpRootDevices.TryAdd(uuid, trackedDevice))
        {
          Logger.Error("Media Server: Failed to add Rootdevice {0}", rootDescriptor.SSDPRootEntry.RootDeviceUUID);
        }
      }
    }

    private void OnUpnpRootDeviceRemoved(RootDescriptor rootDescriptor)
    {
      if (rootDescriptor == null || rootDescriptor.SSDPRootEntry == null || rootDescriptor.SSDPRootEntry.RootDeviceUUID == null)
      {
        return;
      }

      string remoteHost = new Uri(rootDescriptor.SSDPRootEntry.PreferredLink.DescriptionLocation).Host;
      string uuid = rootDescriptor.SSDPRootEntry.RootDeviceUUID;

      if (KnownUpnpRootDevices.ContainsKey(uuid))
      {
        Logger.Info("Media Server - Remove Rootdevice: {0}", rootDescriptor.SSDPRootEntry.RootDeviceUUID);
        TrackedDevice value;
        if (!KnownUpnpRootDevices.TryRemove(uuid, out value))
        {
          Logger.Error("Media Server: Failed to remove Rootdevice");
        }
      }
    }

    public List<TrackedDevice> GeTrackedDevicesByIp(IPAddress ip)
    {
      // remove the scope ID
      // only for IPv6
      if (ip.AddressFamily != AddressFamily.InterNetwork)
        ip.ScopeId = 0;
#if DEBUG
      foreach (var device in KnownUpnpRootDevices)
      {
        Logger.Debug("IP: {0} - {1}", ip, device.Value.RemoteHost);
      }
#endif

      return (from device in KnownUpnpRootDevices where Equals(device.Value.RemoteHost, ip) select device.Value).ToList();
    }

    private static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }

  public class TrackedDevice
  {
    public RootDescriptor RootDescriptor;
    public string DeviceType;
    public string FriendlyName;
    public string Manufacturer;
    public string ManufacturerUrl;
    public string ModelDescription;
    public string ModelName;
    public string ModelNumber;
    public string ModelUrl;
    public string ProductNumber;
    public string Server;
    public string Udn;
    private IPAddress _remoteHost;

    public IPAddress RemoteHost
    {
      get
      {
        return _remoteHost;
      }
      set
      {
        _remoteHost = value;
        // only for IPv6
        if (_remoteHost.AddressFamily != AddressFamily.InterNetwork)
          _remoteHost.ScopeId = 0;
      } 
    }

    public override string ToString()
    {
      return string.Format("DeviceType: {0}, " +
                    "FriendlyName: {1}, " +
                    "Manufacturer: {2}, " +
                    "ManufacturerUrl: {3}, " +
                    "ModelDescription: {4}, " +
                    "ModelName: {5}, " +
                    "ModelNumber: {6}, " +
                    "ModelUrl: {7}, " +
                    "ProductNumber: {8}, " +
                    "Server: {9}, " +
                    "Udn: {10}, " +
                    "RemoteHost: {11}",
                    DeviceType, FriendlyName, Manufacturer, ManufacturerUrl, ModelDescription, ModelName, ModelNumber,
                    ModelUrl, ProductNumber, Server, Udn, RemoteHost);
    }
  }
}
