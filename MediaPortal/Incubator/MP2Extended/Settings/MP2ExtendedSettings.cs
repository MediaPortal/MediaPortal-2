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
using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.MP2Extended.Settings
{
  public class MP2ExtendedSettings
  {
    public MP2ExtendedSettings()
    {
      TranscodingAllowed = true;
      HardcodedSubtitlesAllowed = true;
      PreRecordInterval = 5;
      PostRecordInterval = 5;
      UseAuth = false;
      SkinName = "";

      // OnlineVideos
      OnlineVideosEnabled = true;
      OnlineVideosUseAgeConfirmation = false;
      OnlineVideosCacheTimeout = 30;
      OnlineVideosUtilTimeout = 15;
      OnlineVideosDownloadFolder = "";
    }

    [Setting(SettingScope.Global)]
    public bool TranscodingAllowed { get; set; }
    [Setting(SettingScope.Global)]
    public bool HardcodedSubtitlesAllowed { get; set; }
    [Setting(SettingScope.Global)]
    public int PreRecordInterval { get; set; }
    [Setting(SettingScope.Global)]
    public int PostRecordInterval { get; set; }
    [Setting(SettingScope.Global)]
    public bool UseAuth { get; set; }
    [Setting(SettingScope.Global)]
    public string SkinName { get; set; }

    // OnlineVideos
    [Setting(SettingScope.Global)]
    public bool OnlineVideosEnabled { get; set; }
    [Setting(SettingScope.Global)]
    public bool OnlineVideosUseAgeConfirmation { get; set; }
    [Setting(SettingScope.Global)]
    public int OnlineVideosCacheTimeout { get; set; }
    [Setting(SettingScope.Global)]
    public int OnlineVideosUtilTimeout { get; set; }
    [Setting(SettingScope.Global)]
    public string OnlineVideosDownloadFolder { get; set; }
    [Setting(SettingScope.Global)]
    public DateTime OnlineVideosLastAutomaticUpdate { get; set; }
  }
}
