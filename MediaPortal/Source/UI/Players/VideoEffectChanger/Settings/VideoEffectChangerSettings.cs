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

using MediaPortal.Common.Settings;

namespace MediaPortal.UiComponents.VideoEffectChanger.Settings
{
  public class VideoEffectChangerSettings
  {
    [Setting(SettingScope.User, true)]
    public bool IsEnabled { get; set; }

    [Setting(SettingScope.User, 576 /* PAL SD */)]
    public int ResolutionLimit { get; set; }

    [Setting(SettingScope.User, "effects\\sharpen_complex")]
    public string LowerResolutionEffect { get; set; }

    [Setting(SettingScope.User, null)]
    public string HigherResolutionEffect { get; set; }
  }
}
