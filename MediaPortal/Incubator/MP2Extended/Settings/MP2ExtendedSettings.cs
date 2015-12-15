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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.MP2Extended.Settings
{
  public class MP2ExtendedSettings
  {
    public MP2ExtendedSettings()
    {
      TranscodingAllowed = true;
      HardcodedSubtitlesAllowed = true;
      PreferredAudioLanguages = "EN";
      PreRecordInterval = 5;
      PostRecordInterval = 5;
      UseAuth = false;

      // OnlineVideos
      OnlineVideosEnabled = true;
      OnlineVideosUseAgeConfirmation = false;
      OnlineVideosCacheTimeout = 30;
      OnlineVideosUtilTimeout = 15;
      OnlineVideosDownloadFolder = "";
    }

    [Setting(SettingScope.Global)]
    public bool TranscodingAllowed { get; private set; }
    [Setting(SettingScope.Global)]
    public bool HardcodedSubtitlesAllowed { get; private set; }
    [Setting(SettingScope.Global)]
    public string PreferredAudioLanguages { get; private set; }
    [Setting(SettingScope.Global)]
    public int PreRecordInterval { get; private set; }
    [Setting(SettingScope.Global)]
    public int PostRecordInterval { get; private set; }
    [Setting(SettingScope.Global)]
    public bool UseAuth { get; private set; }

    // OnlineVideos
    [Setting(SettingScope.Global)]
    public bool OnlineVideosEnabled { get; private set; }
    [Setting(SettingScope.Global)]
    public bool OnlineVideosUseAgeConfirmation { get; private set; }
    [Setting(SettingScope.Global)]
    public int OnlineVideosCacheTimeout { get; private set; }
    [Setting(SettingScope.Global)]
    public int OnlineVideosUtilTimeout { get; private set; }
    [Setting(SettingScope.Global)]
    public string OnlineVideosDownloadFolder { get; private set; }
    [Setting(SettingScope.Global)]
    public DateTime OnlineVideosLastAutomaticUpdate { get; set; }
  }
}
