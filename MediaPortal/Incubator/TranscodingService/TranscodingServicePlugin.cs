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

using System;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Threading;
using MediaPortal.Plugins.Transcoding.Service.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.Transcoding.Service
{
  public class TranscodingServicePlugin : IPluginStateTracker
  {
    public static TranscodingServiceSettings Settings = new TranscodingServiceSettings();
    private readonly TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(5);
    
    private IntervalWork _tidyUpCacheWork;

    public TranscodingServicePlugin()
    {
      _tidyUpCacheWork = new IntervalWork(TidyUpCache, CACHE_CLEANUP_INTERVAL);
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.AddIntervalWork(_tidyUpCacheWork, false);
    }

    public void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      LoadTranscodeSettings();

      var converter = new MediaConverter();
      converter.CleanUpTranscodeCache();
      ServiceRegistration.Set<IMediaConverter>(converter);
      Logger.Debug("TranscodingService: Registered FFMpeg MediaConverter.");

      var analyzer = new MediaAnalyzer();
      ServiceRegistration.Set<IMediaAnalyzer>(analyzer);
      Logger.Debug("TranscodingService: Registered FFMpeg MediaAnalyzer.");
    }

    private void TidyUpCache()
    {
      IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>(false);
      if (converter != null && converter is MediaConverter)
        ((MediaConverter)converter).CleanUpTranscodeCache();
    }

    private void LoadTranscodeSettings()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      Settings = settingsManager.Load<TranscodingServiceSettings>();
      if (Directory.Exists(Settings.CachePath) == false)
      {
        Directory.CreateDirectory(Settings.CachePath);
      }
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
    }

    public void Shutdown()
    {
      IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>(false);
      if (converter != null && converter is MediaConverter)
      {
        ((MediaConverter)converter).StopAllTranscodes();
        ((MediaConverter)converter).CleanUpTranscodeCache();
      }

      SaveTranscodeSettings();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
