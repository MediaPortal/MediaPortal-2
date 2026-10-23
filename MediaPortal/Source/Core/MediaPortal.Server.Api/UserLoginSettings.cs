#region Copyright (C) 2007-2026 Team MediaPortal

/*
    Copyright (C) 2007-2026 Team MediaPortal
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

namespace MediaPortal.Server.Api
{
  /// <summary>
  /// The household's login policy, kept where the reference kept it: as server settings, so every television
  /// in the house is let in by the same rules and a second client does not get to decide for the first one.
  /// </summary>
  /// <remarks>
  /// <para>
  /// These are the reference's five <c>UserSettings</c> fields, under the same names and with the same
  /// defaults, in the namespace a headless server can actually carry - the reference's own class sits in
  /// <c>MediaPortal.UI</c>, which the server process does not load. They are kept through the same
  /// <c>ISettingsManager</c> every other server setting goes through, so they live and die with the
  /// server's own configuration like the reference promised for Global settings.
  /// </para>
  /// </remarks>
  public class UserLoginSettings
  {
    /// <summary>
    /// Whether the household has to say who is watching at all. Off means nobody is asked.
    /// <para>
    /// The reference shipped this off; this server ships it on, because the front end's wake is built around
    /// asking and a household that has just given its people names and PINs should be asked from the first
    /// start rather than silently not - the way a door nobody hung a lock on does not "lock itself" just
    /// because somebody wrote down a code. A house that wants the open door says so here, once.
    /// </para>
    /// </summary>
    [Setting(SettingScope.Global, DefaultValue = true)]
    public bool EnableUserLogin { get; set; } = true;

    /// <summary>
    /// Whether the screen that asks is shown when somebody is expected. Turning the screen off while keeping
    /// <see cref="EnableUserLogin"/> on keeps the restrictions of the signed-in person without an interview -
    /// which is what <see cref="AutoLoginUser"/> is usually for. On with its twin, for the reason above.
    /// </summary>
    [Setting(SettingScope.Global, DefaultValue = true)]
    public bool EnableUserLoginScreen { get; set; } = true;

    /// <summary>
    /// The person who is signed in without being asked, or <see cref="Guid.Empty"/> for nobody. A name the
    /// server no longer has is treated as nobody rather than as a door.
    /// </summary>
    [Setting(SettingScope.Global)]
    public Guid AutoLoginUser { get; set; }

    /// <summary>Whether an untouched television gives its user back to the household.</summary>
    [Setting(SettingScope.Global, DefaultValue = false)]
    public bool AutoLogoutEnabled { get; set; }

    /// <summary>How many minutes of nothing it takes. The reference's own default: half an hour.</summary>
    [Setting(SettingScope.Global, DefaultValue = 30)]
    public int AutoLogoutIdleTimeoutInMin { get; set; } = 30;
  }
}
