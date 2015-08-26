using UPnP.Infrastructure.Dv.DeviceTree;
using UPnP_Renderer;

namespace MediaPortal.Extensions.UPnPRenderer
{
  public class upnpDevice : DvDevice
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

    public upnpDevice(string deviceUuid)
      : base(MEDIASERVER_DEVICE_TYPE, MEDIASERVER_DEVICE_VERSION, deviceUuid, new MediaServerUpnPDeviceInformation())
    {
      AddService(new UPnPConnectionManagerServiceImpl());
      AddService(UPnPRenderingControlServiceImpl);
      AddService(UPnPAVTransportServiceImpl);
    }
  }
}