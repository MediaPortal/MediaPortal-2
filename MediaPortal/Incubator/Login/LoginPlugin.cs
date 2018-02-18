using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.UiComponents.Login.Settings;
using System.Linq;


namespace MediaPortal.UiComponents.Login
{
  public class LoginPlugin : IPluginStateTracker
  {
    public void Activated(PluginRuntime pluginRuntime)
    {
      //var meta = pluginRuntime.Metadata;
      //Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      UserSettingStorage.Refresh();
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
