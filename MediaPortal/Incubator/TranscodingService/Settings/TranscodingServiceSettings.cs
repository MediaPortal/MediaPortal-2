#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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

using System.Collections.Generic;
using MediaPortal.Common.Settings;
using MediaPortal.Common;
using MediaPortal.Common.PathManager;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.Transcoding.Service.Settings
{
  public class TranscodingServiceSettings
  {
    private readonly string DEFAULT_CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TranscodeCache\");

    public TranscodingServiceSettings()
    {
      CacheEnabled = true;
      CacheMaximumSizeInGB = 0; //GB
      CacheMaximumAgeInDays = 30; //Days
      CachePath = DEFAULT_CACHE_PATH;
      AnalyzerMaximumThreads = 0; //Auto
      AnalyzerTimeout = 30000;
      AnalyzerStreamTimeout = 10000000;
      TranscoderMaximumThreads = 0; //Auto
      TranscoderTimeout = 5000;
      HLSSegmentTimeInSeconds = 10;
      SubtitleDefaultEncoding = "";
      SubtitleDefaultLanguage = "";
      NvidiaHWAccelerationAllowed = false;
      NvidiaHWMaximumStreams = 2; //For Gforce GPU
      IntelHWAccelerationAllowed = false;
      IntelHWMaximumStreams = 0;
      IntelHWSupportedCodecs = new List<VideoCodec>() { VideoCodec.Mpeg2, VideoCodec.H264, VideoCodec.H265 };
      NvidiaHWSupportedCodecs = new List<VideoCodec>() { VideoCodec.H264, VideoCodec.H265 };
    }

    [Setting(SettingScope.Global)]
    public bool CacheEnabled { get; private set; }
    [Setting(SettingScope.Global)]
    public string CachePath { get; private set; }
    [Setting(SettingScope.Global)]
    public long CacheMaximumSizeInGB { get; private set; }
    [Setting(SettingScope.Global)]
    public long CacheMaximumAgeInDays { get; private set; }
    [Setting(SettingScope.Global)]
    public int AnalyzerTimeout { get; private set; }
    [Setting(SettingScope.Global)]
    public long AnalyzerStreamTimeout { get; private set; }
    [Setting(SettingScope.Global)]
    public int AnalyzerMaximumThreads { get; private set; }
    [Setting(SettingScope.Global)]
    public int TranscoderMaximumThreads { get; private set; }
    [Setting(SettingScope.Global)]
    public int TranscoderTimeout { get; private set; }
    [Setting(SettingScope.Global)]
    public int HLSSegmentTimeInSeconds { get; private set; }
    [Setting(SettingScope.Global)]
    public string SubtitleDefaultEncoding { get; private set; }
    [Setting(SettingScope.Global)]
    public string SubtitleDefaultLanguage { get; private set; }
    [Setting(SettingScope.Global)]
    public bool NvidiaHWAccelerationAllowed { get; private set; }
    [Setting(SettingScope.Global)]
    public bool IntelHWAccelerationAllowed { get; private set; }
    [Setting(SettingScope.Global)]
    public int NvidiaHWMaximumStreams { get; private set; }
    [Setting(SettingScope.Global)]
    public int IntelHWMaximumStreams { get; private set; }
    [Setting(SettingScope.Global)]
    public List<VideoCodec> IntelHWSupportedCodecs { get; private set; }
    [Setting(SettingScope.Global)]
    public List<VideoCodec> NvidiaHWSupportedCodecs { get; private set; }
  }
}
