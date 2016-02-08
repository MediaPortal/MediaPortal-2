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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Runtime;
using System.IO;
using MediaPortal.Common.PathManager;
using System.Xml;
using System;
using System.Threading;
using MediaPortal.Plugins.Transcoding.Service;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MediaPortal.Common.Threading;
using MediaPortal.Plugins.Transcoding.Service.Settings;
using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public class TranscodingServicePlugin : IPluginStateTracker
  {
    public static TranscodingServiceSettings Settings = new TranscodingServiceSettings();
    private readonly TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(5);
    
    private IntervalWork _tidyUpCacheWork;

    public TranscodingServicePlugin()
    {
      MediaConverter.StopAllTranscodes();
      MediaConverter.CleanUpTranscodeCache();

      _tidyUpCacheWork = new IntervalWork(TidyUpCache, CACHE_CLEANUP_INTERVAL);
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.AddIntervalWork(_tidyUpCacheWork, false);
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      LoadTranscodeSettings();
    }

    private void TidyUpCache()
    {
      MediaConverter.CleanUpTranscodeCache();
    }

    private void LoadTranscodeSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      Settings = settingsManager.Load<TranscodingServiceSettings>();
      if (Directory.Exists(Settings.CachePath) == false)
      {
        Directory.CreateDirectory(Settings.CachePath);
      }
      MediaConverter.LoadSettings();
    }

    private void SaveTranscodeSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      settingsManager.Save(Settings);
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
      LoadTranscodeSettings();
    }

    public void Shutdown()
    {
      SaveTranscodeSettings();
      MediaConverter.StopAllTranscodes();
      MediaConverter.CleanUpTranscodeCache();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
