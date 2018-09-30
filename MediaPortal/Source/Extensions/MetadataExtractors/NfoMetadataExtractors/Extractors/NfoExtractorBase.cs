#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.PathManager;
using MediaPortal.Common.Services.Logging;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Settings;
using System;
using System.Net;
using System.Net.Http;

namespace MediaPortal.Extensions.MetadataExtractors.NfoMetadataExtractors.Extractors
{
  public abstract class NfoExtractorBase<TSettings> : IDisposable where TSettings : NfoMetadataExtractorSettingsBase
  {
    #region Private fields

    protected readonly string _name;

    /// <summary>
    /// Settings of the <see cref="NfoExtractorBase<>"/>
    /// </summary>
    protected TSettings _settings;

    /// <summary>
    /// Debug logger
    /// </summary>
    /// <remarks>
    /// NoLogger if _settings.EnableDebugLogging == <c>false</c>"/>
    /// FileLogger if _settings.EnableDebugLogging == <c>true</c>"/>
    /// </remarks>
    protected readonly ILogger _debugLogger;

    /// <summary>
    /// Unique number of the last MediaItem for which this MetadataExtractor was called
    /// </summary>
    protected long _lastMediaItemNumber = 1;

    /// <summary>
    /// <see cref="HttpClient"/> used to download from http URLs contained in nfo-files
    /// </summary>
    protected HttpClient _httpClient;

    protected SettingsChangeWatcher<TSettings> _settingWatcher;

    #endregion

    #region Ctor

    /// <summary>
    /// Instantiates a new <see cref="NfoMovieMetadataExtractor"/> object
    /// </summary>
    public NfoExtractorBase()
    {
      _name = GetType().Name;

      _settingWatcher = new SettingsChangeWatcher<TSettings>();
      _settingWatcher.SettingsChanged += SettingsChanged;
      _settings = _settingWatcher.Settings;

      if (_settings.EnableDebugLogging)
      {
        _debugLogger = FileLogger.CreateFileLogger(ServiceRegistration.Get<IPathManager>().GetPath(@"<LOG>\" + _name + "Debug.log"), LogLevel.Debug, false, true);
        LogSettings();
      }
      else
        _debugLogger = new NoLogger();

      var handler = new HttpClientHandler();
      if (handler.SupportsAutomaticDecompression)
        // This enables the automatic decompression of the content. It does not automatically send an "Accept-Encoding" header!
        // We therefore have to add the Accept-Encoding header(s) manually below.
        // Additionally, due to the automatic decompression, HttpResponseMessage.Content.Headers DOES NOT contain
        // a "Content-Encoding" header anymore when we try to access it. It is automatically removed when decompressing.
        handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
      else
        _debugLogger.Warn("HttpClient does not support compression");
      _httpClient = new HttpClient(handler);
      _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
      _httpClient.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
    }

    #endregion

    #region Virtual methods

    protected virtual void LogSettings()
    {

    }

    protected virtual void LoadSettings()
    {
    }

    protected virtual void SettingsChanged(object sender, EventArgs e)
    {
      LoadSettings();
    }

    #endregion

    #region IDisposable implementation

    public virtual void Dispose()
    {
      if (_httpClient == null)
        return;
      _httpClient.Dispose();
      _httpClient = null;
    }

    #endregion
  }
}
