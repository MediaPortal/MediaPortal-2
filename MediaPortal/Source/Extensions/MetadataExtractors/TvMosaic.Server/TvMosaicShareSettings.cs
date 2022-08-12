#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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

namespace TvMosaic.Server
{
  public class TvMosaicShareSettings
  {
    /// <summary>
    /// Gets or sets whether to watch the TvMosaic recorded tv share for changes.
    /// </summary>
    [Setting(SettingScope.Global, true)]
    public bool EnableRecordedTvShareWatcher { get; set; }

    /// <summary>
    /// Gets or sets the initial number of seconds to wait before checking the TvMosaic recorded tv share for changes.
    /// </summary>
    [Setting(SettingScope.Global, 30)]
    public int InitialCheckDelaySeconds { get; set; }

    /// <summary>
    /// Gets or sets the number of seconds between checking the TvMosaic recorded tv share for changes.
    /// </summary>
    [Setting(SettingScope.Global, 60)]
    public int CheckIntervalSeconds { get; set; }
  }
}
