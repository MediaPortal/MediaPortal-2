using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess;

namespace MediaPortal.Plugins.MP2Extended
{
  public class MP2Extended : IPluginStateTracker
  {

    private void StartUp()
    {
      ServiceRegistration.Get<IResourceServer>().AddHttpModule(new MainRequestHandler());
    }
    
    #region IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      StartUp();
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

    #endregion IPluginStateTracker
  }
}