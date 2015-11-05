using System.Linq.Expressions;
using MediaPortal.Common;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;

namespace MediaPortal.Plugins.MP2Extended
{
  public class MP2Extended : IPluginStateTracker
  {
    public const bool TRANSCODING_ALLOWED = true;
    public const bool HARDCODED_SUBS_ALLOWED = true;
    
    private void StartUp()
    {
      ProfileManager.LoadProfiles();
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