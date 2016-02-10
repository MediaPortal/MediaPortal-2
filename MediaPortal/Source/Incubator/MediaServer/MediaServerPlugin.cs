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

using MediaPortal.Backend.BackendServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MediaServer.Profiles;
using MediaPortal.Plugins.MediaServer.ResourceAccess;
using MediaPortal.Plugins.MediaServer.Settings;

namespace MediaPortal.Plugins.MediaServer
{
  public class MediaServerPlugin : IPluginStateTracker
  {
    private readonly UPnPMediaServerDevice _device;
    /// <summary>
    /// Tracks all UPnP Rootdevices available in the Network
    /// </summary>
    public static UPnPDeviceTracker Tracker;
    public static MediaServerSettings Settings = new MediaServerSettings();
    public const string DEVICE_UUID = "45F2C54D-8C0A-4736-AA04-E6F91CD45457";

    public MediaServerPlugin()
    {
      _device = new UPnPMediaServerDevice(DEVICE_UUID.ToLower());
      Tracker = new UPnPDeviceTracker();
      Tracker.Start();
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      Logger.Debug("MediaServerPlugin: Adding UPNP device as a root device");
      ServiceRegistration.Get<IBackendServer>().UPnPBackendServer.AddRootDevice(_device);

      Logger.Debug("MediaServerPlugin: Registering DLNA HTTP resource access module");
      ServiceRegistration.Get<IResourceServer>().AddHttpModule(new DlnaResourceAccessModule());

      LoadSettings();
    }

    private void LoadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      Settings = settingsManager.Load<MediaServerSettings>();
    }

    private void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      settingsManager.Save(Settings);

      ProfileManager.SaveProfileLinks();
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
      SaveSettings();
      DlnaResourceAccessModule.Shutdown();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
