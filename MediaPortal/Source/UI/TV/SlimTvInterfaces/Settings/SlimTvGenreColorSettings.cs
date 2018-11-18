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

using MediaPortal.Common.Settings;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Settings
{
  public class SlimTvGenreColorSettings
  {
    [Setting(SettingScope.Global, DefaultValue = "#7532a8")]
    public string MovieGenreColor { get; set; }

    [Setting(SettingScope.Global, DefaultValue = "#ed7d31")]
    public string SeriesGenreColor { get; set; }

    [Setting(SettingScope.Global, DefaultValue = "#4f7a32")]
    public string DocumentaryGenreColor { get; set; }

    [Setting(SettingScope.Global, DefaultValue = "#c89800")]
    public string MusicGenreColor { get; set; }

    [Setting(SettingScope.Global, DefaultValue = "#4e93d2")]
    public string KidsGenreColor { get; set; }

    [Setting(SettingScope.Global, DefaultValue = "#c03636")]
    public string NewsGenreColor { get; set; }

    [Setting(SettingScope.Global, DefaultValue = "#00817e")]
    public string SportGenreColor { get; set; }

    [Setting(SettingScope.Global)]
    public string SpecialGenreColor { get; set; }
  }
}
