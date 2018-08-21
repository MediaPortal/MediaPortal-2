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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MediaPortal.Plugins.MP2Extended.Settings;
using System;
using System.IO;
using System.Reflection;

namespace MediaPortal.Plugins.MP2Extended
{
   public class MP2Extended : IPluginStateTracker
  {
    public static MP2ExtendedSettings Settings = new MP2ExtendedSettings();
    public static OnlineVideosManager OnlineVideosManager;

    private void StartUp()
    {
      Logger.Debug("MP2Extended: Registering HTTP resource access module");

      //ServiceRegistration.Get<IResourceServer>().AddHttpModule(new MainRequestHandler());
      if (Settings.OnlineVideosEnabled)
        OnlineVideosManager = new OnlineVideosManager(); // must be loaded after the settings are loaded
    }

    private void LoadSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      Settings = settingsManager.Load<MP2ExtendedSettings>();

      ProfileManager.Profiles.Clear();
      ProfileManager.LoadProfiles(false);
      ProfileManager.LoadProfiles(true);
    }

    private void SaveSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      settingsManager.Save(Settings);
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
      BaseSendData.SendDataCancellation.Cancel();
    }

    #endregion IPluginStateTracker


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
