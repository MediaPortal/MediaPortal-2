#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.Services.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Service.Transcoders.FFMpeg;
using MediaPortal.Extensions.TranscodingService.Interfaces.Profiles;
using MediaPortal.Extensions.TranscodingService.Interfaces.Settings;

namespace MediaPortal.Extensions.TranscodingService.Service
{
  public class TranscodingServicePlugin : IPluginStateTracker
  {
    public static TranscodingServiceSettings Settings = new TranscodingServiceSettings();
    private readonly TimeSpan CACHE_CLEANUP_INTERVAL = TimeSpan.FromMinutes(5);

    private SettingsChangeWatcher<TranscodingServiceSettings> _settings;
    private IntervalWork _tidyUpCacheWork;
    private AnalysisLibraryManager _analysisLibraryManager;
    private TranscodeProfileManager _profileManager;
    private Transcoder _currenTranscoder = Transcoder.None;
    private IMediaConverter _converter;
    private IMediaAnalyzer _analyzer;

    public TranscodingServicePlugin()
    {
      _profileManager = new TranscodeProfileManager();
      ServiceRegistration.Set<ITranscodeProfileManager>(_profileManager);
      Logger.Debug("TranscodingService: Registered TranscodeProfileManager.");

      _analysisLibraryManager = new AnalysisLibraryManager();

      _settings = new SettingsChangeWatcher<TranscodingServiceSettings>();
      _settings.SettingsChanged = OnSettingsChanged;
      _settings.Refresh();

      _tidyUpCacheWork = new IntervalWork(TidyUpCache, CACHE_CLEANUP_INTERVAL);
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.AddIntervalWork(_tidyUpCacheWork, false);
    }

    private void OnSettingsChanged(object sender, EventArgs e)
    {
      Settings = _settings.Settings;

      _profileManager.SubtitleFont = Settings.SubtitleFont;
      _profileManager.SubtitleFontSize = Settings.SubtitleFontSize;
      _profileManager.SubtitleColor = Settings.SubtitleColor;
      _profileManager.SubtitleBox = Settings.SubtitleBox;
      _profileManager.ForceSubtitles = Settings.ForceSubtitles;

      _analysisLibraryManager.UpdateAnalysisCleanupIntervalWork(_settings.Settings);

      if (_currenTranscoder != _settings.Settings.Transcoder && _settings.Settings.Transcoder == Transcoder.FFMpeg)
      {
        _currenTranscoder = _settings.Settings.Transcoder;

        _converter = new FFMpegMediaConverter();
        ServiceRegistration.Set<IMediaConverter>(_converter);
        Logger.Debug("TranscodingService: Registered FFMpeg MediaConverter.");

        _analyzer = new FFMpegMediaAnalyzer();
        ServiceRegistration.Set<IMediaAnalyzer>(_analyzer);
        Logger.Debug("TranscodingService: Registered FFMpeg MediaAnalyzer.");
      }
    }

    public async void Activated(PluginRuntime pluginRuntime)
    {
      var meta = pluginRuntime.Metadata;
      Logger.Info(string.Format("{0} v{1} [{2}] by {3}", meta.Name, meta.PluginVersion, meta.Description, meta.Author));

      if (Directory.Exists(Settings.CachePath) == false)
        Directory.CreateDirectory(Settings.CachePath);

      await ServiceRegistration.Get<IMediaConverter>().CleanUpTranscodeCacheAsync();
    }

    private async void TidyUpCache()
    {
      if(_converter != null)
        await _converter.CleanUpTranscodeCacheAsync();
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
      if (_settings != null)
      {
        _settings.Dispose();
        _settings = null;
      }
      if (_converter != null)
      {
        await _converter.StopAllTranscodesAsync();
        await _converter.CleanUpTranscodeCacheAsync();
        _converter = null;
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
