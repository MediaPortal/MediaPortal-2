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
using SkinSettings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.WMCSkin.Settings
{
  public class WMCSkinSettings
  {
    public const string SKIN_NAME = "WMCSkin";

    [Setting(SettingScope.User, true)]
    public bool EnableFanart { get; set; }

    [Setting(SettingScope.User, 1.0)]
    public double FanartOverlayOpacity { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableListWatchedFlags { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableGridWatchedFlags { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableCoverWatchedFlags { get; set; }

    [Setting(SettingScope.User, false)]
    public bool EnableMovieGridBanners { get; set; }

    [Setting(SettingScope.User, false)]
    public bool EnableSeriesGridBanners { get; set; }

    [Setting(SettingScope.User, false)]
    public bool EnableSeasonGridBanners { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableAnimatedBackground { get; set; }

    [Setting(SettingScope.User, false)]
    public bool EnableMediaItemDetailsView { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableAgeCertificationLogos { get; set; }
  }
}
