using System.Collections.Generic;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnP.Infrastructure.Dv.SSDP
{
  /// <summary>
  /// Simplifies sending of UPnP notification messages for all objects in a device tree, starting
  /// with a root device.
  /// </summary>
  /// <remarks>
  /// The structure and sequence of the messages which are sent here is described in the document
  /// "UPnP-arch-DeviceArchitecture-v1.1" (DevArch) of the UPnP forum.
  /// As all messages of method "NOTIFY" are sent in the same message sequence with the same NT/USN parameter
  /// constellations, we use this class to produce the seqnence of messages.
  /// </remarks>
  internal class DeviceTreeNotificationProducer
  {
    protected static void SendMessagesEmbeddedDevice(DvDevice rootDevice, DvDevice device, ISSDPDiscoveryMessageSender messageSender)
    {
      string deviceUDN = device.UDN;
      messageSender.SendMessage(deviceUDN, deviceUDN, rootDevice);
      messageSender.SendMessage(device.DeviceTypeVersion_URN, deviceUDN + "::" + device.DeviceTypeVersion_URN, rootDevice);
    }

    protected static void SendMessagesEmbeddedDevicesRecursive(DvDevice rootDevice, DvDevice device, ISSDPDiscoveryMessageSender messageSender)
    {
      SendMessagesEmbeddedDevice(rootDevice, device, messageSender);
      SendMessagesServices(rootDevice, device, messageSender);
      foreach (DvDevice embeddedDevice in device.EmbeddedDevices)
        SendMessagesEmbeddedDevicesRecursive(rootDevice, embeddedDevice, messageSender);
    }

    protected static void SendMessagesService(DvDevice rootDevice, DvDevice device, string serviceTypeVersion_URN,
        ISSDPDiscoveryMessageSender messageSender)
    {
      string deviceUUID = device.UDN;
      messageSender.SendMessage(serviceTypeVersion_URN, deviceUUID + "::" + serviceTypeVersion_URN, rootDevice);
    }

    protected static void SendMessagesServices(DvDevice rootDevice, DvDevice device, ISSDPDiscoveryMessageSender messageSender)
    {
      ICollection<string> services = device.GetServiceTypeVersion_URNs();
      foreach (string serviceTypeVersion_URN in services)
          // One notification for each service
        SendMessagesService(rootDevice, device, serviceTypeVersion_URN, messageSender);
    }

    protected static void SendMessagesRootDevice(DvDevice rootDevice, ISSDPDiscoveryMessageSender messageSender)
    {
      string deviceUDN = rootDevice.UDN;
      messageSender.SendMessage("upnp:rootdevice", deviceUDN + "::upnp:rootdevice", rootDevice);
      messageSender.SendMessage(deviceUDN, deviceUDN, rootDevice);
      messageSender.SendMessage(rootDevice.DeviceTypeVersion_URN, deviceUDN + "::" + rootDevice.DeviceTypeVersion_URN, rootDevice);
      SendMessagesServices(rootDevice, rootDevice, messageSender);
      foreach (DvDevice embeddedDevice in rootDevice.EmbeddedDevices)
        SendMessagesEmbeddedDevicesRecursive(rootDevice, embeddedDevice, messageSender);
    }

    /// <summary>
    /// Walks through the tree of all root devices and services of the specified UPnP <paramref name="server"/>
    /// and calls the <see cref="ISSDPDiscoveryMessageSender.SendMessage"/>
    /// method of the <paramref name="messageSender"/> for each device and service. As specified in (DevArch),
    /// the message sender method will be called three times for the root device, twice for each device and once for
    /// each service with the NT/USN parameter constellation described in (DevArch), section 1.2.2.
    /// </summary>
    /// <param name="server">UPnP server of the root UPnP device tree to send discovery messages for.</param>
    /// <param name="messageSender">Message producer and sender class which creates the actual messages to be sent
    /// for each of the given NT/USN parameter combinations.</param>
    public static void SendMessagesServer(UPnPServer server, ISSDPDiscoveryMessageSender messageSender)
    {
      foreach (DvDevice rootDevice in server.RootDevices)
        SendMessagesRootDevice(rootDevice, messageSender);
    }
  }
}