using System.Linq;
using MediaPortal.Backend.BackendServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.UPnP;
using MediaPortal.Extensions.UserServices.FanArtService.UPnP;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Extensions.UserServices.FanArtService
{
  public class FanArtServicePlugin: IPluginStateTracker
  {
    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      DvDevice device = ServiceRegistration.Get<IBackendServer>().UPnPBackendServer.FindDevicesByDeviceTypeAndVersion(UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION, true).FirstOrDefault();
      if (device != null)
      {
        device.AddService(new FanArtServiceImpl());
        Logger.Debug("MediaServerPlugin: Adding FanArt service to MP2 backend root device");
      }
      else
      {
        Logger.Error("MediaServerPlugin: MP2 backend root device not found!");
      }
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
    }

    public void Continue()
    {
    }

    public void Shutdown()
    {
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
