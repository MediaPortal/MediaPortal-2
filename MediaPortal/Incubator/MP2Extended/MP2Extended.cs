using System.Linq.Expressions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Common.PathManager;
using System.IO;
using System.Xml;
using System;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.Settings;

namespace MediaPortal.Plugins.MP2Extended
{
  public class MP2Extended : IPluginStateTracker
  {
    public static MP2ExtendedSettings Settings = new MP2ExtendedSettings();
    public static MP2ExtendedUsers Users = new MP2ExtendedUsers();
    public static OnlineVideosManager OnlineVideosManager;

    private void StartUp()
    {
      Logger.Debug("MP2Extended: Registering HTTP resource access module");
      ServiceRegistration.Get<IResourceServer>().AddHttpModule(new MainRequestHandler());
      if (Settings.OnlineVideosEnabled)
        OnlineVideosManager = new OnlineVideosManager(); // must be loaded after the settings are loaded
    }

    private void LoadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      Settings = settingsManager.Load<MP2ExtendedSettings>();
      Users = settingsManager.Load<MP2ExtendedUsers>();

      ProfileManager.Profiles.Clear();
      ProfileManager.LoadProfiles(false);
      ProfileManager.LoadProfiles(true);
    }

    private void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      settingsManager.Save(Settings);
      settingsManager.Save(Users);
    }

    #region IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      LoadSettings();
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
      LoadSettings();
    }

    public void Shutdown()
    {
      SaveSettings();
      MainRequestHandler.Shutdown();
    }

    #endregion IPluginStateTracker

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
