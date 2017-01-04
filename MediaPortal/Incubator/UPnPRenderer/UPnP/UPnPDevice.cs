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

using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.UPnPRenderer.UPnP
{
  public class UPnPDevice : DvDevice
  {
    public const string MEDIASERVER_DEVICE_TYPE = "schemas-upnp-org:device:MediaRenderer";
    public const int MEDIASERVER_DEVICE_VERSION = 1;
    public const string CONTENT_DIRECTORY_SERVICE_TYPE = "schemas-upnp-org:service:ContentDirectory";
    public const int CONTENT_DIRECTORY_SERVICE_TYPE_VERSION = 1;
    public const string CONTENT_DIRECTORY_SERVICE_ID = "urn:upnp-org:serviceId:ContentDirectory";

    public const string CONNECTION_MANAGER_SERVICE_TYPE = "schemas-upnp-org:service:ConnectionManager";
    public const int CONNECTION_MANAGER_SERVICE_TYPE_VERSION = 1;
    public const string CONNECTION_MANAGER_SERVICE_ID = "urn:upnp-org:serviceId:ConnectionManager";

    public const string AV_TRANSPORT_SERVICE_TYPE = "schemas-upnp-org:service:AVTransport";
    public const int AV_TRANSPORT_SERVICE_TYPE_VERSION = 1;
    public const string AV_TRANSPORT_SERVICE_ID = "urn:schemas-upnp-org:service:AVTransport";

    public const string RENDERING_CONTROL_SERVICE_TYPE = "schemas-upnp-org:service:RenderingControl";
    public const int RENDERING_CONTROL_SERVICE_TYPE_VERSION = 1;
    public const string RENDERING_CONTROL_SERVICE_ID = "urn:schemas-upnp-org:service:RenderingControl";

    public UPnPRenderingControlServiceImpl UPnPRenderingControlServiceImpl = new UPnPRenderingControlServiceImpl();
    public UPnPAVTransportServiceImpl UPnPAVTransportServiceImpl = new UPnPAVTransportServiceImpl();

    public UPnPDevice(string deviceUuid)
      : base(MEDIASERVER_DEVICE_TYPE, MEDIASERVER_DEVICE_VERSION, deviceUuid, new MediaServerUpnPDeviceInformation())
    {
      AddService(new UPnPConnectionManagerServiceImpl());
      AddService(UPnPRenderingControlServiceImpl);
      AddService(UPnPAVTransportServiceImpl);
    }
  }
}
