using MediaPortal.Backend.BackendServer;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.UPnP;
using MediaPortal.Plugins.ServerStateService.UPnP;
using System.Linq;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.ServerStateService
{
  public class ServerStateServicePlugin : IPluginStateTracker
  {
    public void Activated(PluginRuntime pluginRuntime)
    {
      var stateService = new ServerStateServiceImpl();
      ServiceRegistration.Set<IServerStateService>(stateService);

      DvDevice device = ServiceRegistration.Get<IBackendServer>().UPnPBackendServer
        .FindDevicesByDeviceTypeAndVersion(UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE, UPnPTypesAndIds.BACKEND_SERVER_DEVICE_TYPE_VERSION, true).FirstOrDefault();
      if (device != null)
      {
        Logger.Debug("ServerStateService: Registering ServerStateService service.");
        device.AddService(stateService);
        Logger.Debug("ServerStateService: Adding ServerStateService service to MP2 backend root device");
      }
      else
      {
        Logger.Error("ServerStateService: MP2 backend root device not found!");
      }
    }    

    public void Continue()
    {

    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Shutdown()
    {

    }

    public void Stop()
    {

    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}