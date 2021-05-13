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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Settings
{
  public class SlimTvServerSettings
  {
    /// <summary>
    /// Gets or sets the start hour on current day for the schedule check.
    /// </summary>
    [Setting(SettingScope.Global, 7.0d)]
    public double ScheduleCheckStartTime { get; set; }

    /// <summary>
    /// Gets or sets the scheme to use for checking for existing episodes for a series.
    /// </summary>
    [Setting(SettingScope.Global, 1)]
    public int EpisodeManagementScheme { get; set; }

    /// <summary>
    /// Gets or sets the whether to check for program movement.
    /// </summary>
    [Setting(SettingScope.Global, false)]
    public bool DetectMovedPrograms { get; set; }

    /// <summary>
    /// Gets or sets the number of minutes to check a schedule before recording for program movement detection.
    /// </summary>
    [Setting(SettingScope.Global, 30.0d)]
    public double MovedProgramsDetectionOffset { get; set; }

    /// <summary>
    /// Gets or sets the maximum movement detection time in minutes against the start time of the schedule.
    /// </summary>
    [Setting(SettingScope.Global, 30.0d)]
    public double MovedProgramsDetectionWindow { get; set; }
  }
}
