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

namespace MediaPortal.Plugins.SlimTv.Client.Settings
{
  class SlimTvClientSettings
  {
    /// <summary>
    /// Defines the number of rows to be visible in EPG.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = 7)]
    public int EpgNumberOfRows { get; set; }

    /// <summary>
    /// Defines the number of hours to be visible in EPG.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = 2d)]
    public double EpgVisibleHours { get; set; }

    /// <summary>
    /// Defines the zapping timeout in seconds.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = 2d)]
    public double ZapTimeout { get; set; }

    /// <summary>
    /// If set to <c>true</c>, TV gets started when entering TV home state.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = false)]
    public bool AutoStartTV { get; set; }

    /// <summary>
    /// If set to <c>true</c>, series info will be shown in program details.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = true)]
    public bool ShowSeriesInfo { get; set; }

    /// <summary>
    /// If set to <c>true</c>, zapping uses the actual index of channel inside current group.
    /// If <c>false</c>, the logical channel number of the channel will be used.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = true)]
    public bool ZapByChannelIndex { get; set; }

    /// <summary>
    /// If set to <c>true</c>, the inbuilt "All Channels" group will be hidden.
    /// </summary>
    [Setting(SettingScope.User, DefaultValue = false)]
    public bool HideAllChannelsGroup { get; set; }
  }
}
