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

using System;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Threading;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.Transcoding.Interfaces;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.FFMpeg;
using MediaPortal.Plugins.Transcoding.Interfaces.Profiles;
using MediaPortal.Plugins.Transcoding.Interfaces.Settings;

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

    public async void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      LoadTranscodeSettings();

      var profileManager = new TranscodeProfileManager();
      profileManager.SubtitleFont = Settings.SubtitleFont;
      profileManager.SubtitleFontSize = Settings.SubtitleFontSize;
      profileManager.SubtitleColor = Settings.SubtitleColor;
      profileManager.SubtitleBox = Settings.SubtitleBox;
      profileManager.ForceSubtitles = Settings.ForceSubtitles;
      ServiceRegistration.Set<ITranscodeProfileManager>(profileManager);
      Logger.Debug("TranscodingService: Registered TranscodeProfileManager.");

      if (Settings.Transcoder == Transcoder.FFMpeg)
      {
        var converter = new FFMpegMediaConverter();
        await converter.CleanUpTranscodeCacheAsync();
        ServiceRegistration.Set<IMediaConverter>(converter);
        Logger.Debug("TranscodingService: Registered FFMpeg MediaConverter.");

        var analyzer = new FFMpegMediaAnalyzer();
        ServiceRegistration.Set<IMediaAnalyzer>(analyzer);
        Logger.Debug("TranscodingService: Registered FFMpeg MediaAnalyzer.");
      }
    }

    private async void TidyUpCache()
    {
      IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>(false);
      if(converter != null)
        await converter.CleanUpTranscodeCacheAsync();
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

    public async void Shutdown()
    {
      IMediaConverter converter = ServiceRegistration.Get<IMediaConverter>(false);
      if (converter != null)
      {
        await converter.StopAllTranscodesAsync();
        await converter.CleanUpTranscodeCacheAsync();
      }
      SaveTranscodeSettings();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
