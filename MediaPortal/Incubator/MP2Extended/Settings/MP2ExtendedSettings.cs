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
  }
}
