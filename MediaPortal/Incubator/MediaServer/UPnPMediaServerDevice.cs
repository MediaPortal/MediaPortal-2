#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Globalization;
using System.Xml;
using HttpServer;
using MediaPortal.Backend.Services.ClientCommunication;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MediaServer.Profiles;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.MediaServer
{
  public class UPnPMediaServerDevice : DvDevice
  {
    public const string MEDIASERVER_DEVICE_TYPE = "schemas-upnp-org:device:MediaServer";
    public const int MEDIASERVER_DEVICE_VERSION = 1;

    public const string CONTENT_DIRECTORY_SERVICE_TYPE = "schemas-upnp-org:service:ContentDirectory";
    public const int CONTENT_DIRECTORY_SERVICE_TYPE_VERSION = 1;
    public const string CONTENT_DIRECTORY_SERVICE_ID = "urn:upnp-org:serviceId:ContentDirectory";

    public const string CONNECTION_MANAGER_SERVICE_TYPE = "schemas-upnp-org:service:ConnectionManager";
    public const int CONNECTION_MANAGER_SERVICE_TYPE_VERSION = 1;
    public const string CONNECTION_MANAGER_SERVICE_ID = "urn:upnp-org:serviceId:ConnectionManager";

    public const string MEDIARECEIVER_REGISTRAR_SERVICE_TYPE = "microsoft.com:service:X_MS_MediaReceiverRegistrar";
    public const int MEDIARECEIVER_REGISTRAR_SERVICE_TYPE_VERSION = 1;
    public const string MEDIARECEIVER_REGISTRAR_SERVICE_ID = "urn:microsoft.com:serviceId:X_MS_MediaReceiverRegistrar";

    private static Dictionary<string, MediaServerUpnPDeviceInformation> _customDeviceCache = new Dictionary<string, MediaServerUpnPDeviceInformation>();

    public UPnPMediaServerDevice(string deviceUuid)
      : base(MEDIASERVER_DEVICE_TYPE, MEDIASERVER_DEVICE_VERSION, deviceUuid,
             new LocalizedUPnPDeviceInformation())
    {
      DescriptionGenerateHook += GenerateDescriptionFunc;
      DeviceInfoHook += DeviceInfoFunc;
      AddService(new UPnPContentDirectoryServiceImpl());
      AddService(new UPnPConnectionManagerServiceImpl());
      AddService(new UPnPMediaReceiverRegistrarServiceImpl());
    }

    private static void GenerateDescriptionFunc(IHttpRequest request, XmlWriter writer, DvDevice device, GenerationPosition pos, EndpointConfiguration config, CultureInfo culture)
    {
      if (request == null) return;

      EndPointSettings client = ProfileManager.DetectProfile(request.Headers);

      if (client == null || client.Profile == null)
      {
        return;
      }
      if (pos == GenerationPosition.DeviceStart)
      {
        writer.WriteElementString("dlna", "X_DLNACAP", "urn:schemas-dlna-org:device-1-0", "");
        writer.WriteElementString("dlna", "X_DLNADOC", "urn:schemas-dlna-org:device-1-0", "DMS-1.50");
        writer.WriteElementString("dlna", "X_DLNADOC", "urn:schemas-dlna-org:device-1-0", "M-DMS-1.50");

        if (string.IsNullOrEmpty(client.Profile.UpnpDevice.AdditionalElements) == false)
        {
          writer.WriteRaw(client.Profile.UpnpDevice.AdditionalElements);
        }
      }
      else if (pos == GenerationPosition.RootDeviceStart)
      {
        writer.WriteAttributeString("xmlns", "dlna", null, "urn:schemas-dlna-org:device-1-0");
        writer.WriteAttributeString("xmlns", "sec", null, "http://www.sec.co.kr/dlna");
      }
    }

    private static void DeviceInfoFunc(IHttpRequest request, ILocalizedDeviceInformation deviceInfo, ref ILocalizedDeviceInformation overriddenDeviceInfo)
    {
      if (request == null) return;
      string clientID = request.Headers["remote_addr"];
      EndPointSettings client = ProfileManager.DetectProfile(request.Headers);

      if (client != null && client.Profile != null)
      {
        if (_customDeviceCache.ContainsKey(client.Profile.ID) == false)
        {
          MediaServerUpnPDeviceInformation dev = null;
          if (client.Profile.UpnpDevice.DeviceInformation.FriendlyName != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.FriendlyName = client.Profile.UpnpDevice.DeviceInformation.FriendlyName;
          }
          if (client.Profile.UpnpDevice.DeviceInformation.Manufacturer != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.Manufacturer = client.Profile.UpnpDevice.DeviceInformation.Manufacturer;
          }
          if (client.Profile.UpnpDevice.DeviceInformation.ManufacturerURL != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.ManufacturerURL = client.Profile.UpnpDevice.DeviceInformation.ManufacturerURL;
          }
          if (client.Profile.UpnpDevice.DeviceInformation.ModelDescription != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.ModelDescription = client.Profile.UpnpDevice.DeviceInformation.ModelDescription;
          }
          if (client.Profile.UpnpDevice.DeviceInformation.ModelName != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.ModelName = client.Profile.UpnpDevice.DeviceInformation.ModelName;
          }
          if (client.Profile.UpnpDevice.DeviceInformation.ModelNumber != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.ModelNumber = client.Profile.UpnpDevice.DeviceInformation.ModelNumber;
          }
          if (client.Profile.UpnpDevice.DeviceInformation.ModelURL != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.ModelURL = client.Profile.UpnpDevice.DeviceInformation.ModelURL;
          }
          if (client.Profile.UpnpDevice.DeviceInformation.SerialNumber != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.SerialNumber = client.Profile.UpnpDevice.DeviceInformation.SerialNumber;
          }
          if (client.Profile.UpnpDevice.DeviceInformation.UPC != null)
          {
            if (dev == null) dev = new MediaServerUpnPDeviceInformation(deviceInfo);
            dev.UPC = client.Profile.UpnpDevice.DeviceInformation.UPC;
          }
          if (dev != null)
          {
            overriddenDeviceInfo = dev;
          }
          _customDeviceCache.Add(client.Profile.ID, dev);
        }
        else
        {
          overriddenDeviceInfo = _customDeviceCache[client.Profile.ID];
        }
      }
      else
      {
        Logger.Debug("MediaServerPlugin: No profile found for {0}", clientID);
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
