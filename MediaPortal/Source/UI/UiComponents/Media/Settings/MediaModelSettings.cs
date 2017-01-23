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

namespace MediaPortal.UiComponents.Media.Settings
{
  public class MediaModelSettings
  {
    protected const bool DEFAULT_CLOSE_PLAYER_WHEN_FINISHED = true;
    protected const double DEFAULT_INSTANT_SKIP_PERCENT = 20;
    protected const double DEFAULT_SKIPSTEP_TIMEOUT = 1.5f;
    protected const string DEFAULT_SKIPSTEP_LIST = "15,30,60,180,300,600,900,1800,3600,7200"; // list of seconds

    protected bool _closePlayerWhenFinished = DEFAULT_CLOSE_PLAYER_WHEN_FINISHED;

    [Setting(SettingScope.Global, DEFAULT_CLOSE_PLAYER_WHEN_FINISHED)]
    public bool ClosePlayerWhenFinished
    {
      get { return _closePlayerWhenFinished; }
      set { _closePlayerWhenFinished = value; }
    }

    /// <summary>
    /// Percent of total media duration to instant skip playback back-/forward.
    /// </summary>
    [Setting(SettingScope.Global, DEFAULT_INSTANT_SKIP_PERCENT)]
    public double InstantSkipPercent { get; set; }

    /// <summary>
    /// Timeout in seconds before skip step is executed.
    /// </summary>
    [Setting(SettingScope.Global, DEFAULT_SKIPSTEP_TIMEOUT)]
    public double SkipStepTimeout { get; set; }

    /// <summary>
    /// List of seconds to be allowed for skip steps.
    /// </summary>
    [Setting(SettingScope.Global, DEFAULT_SKIPSTEP_LIST)]
    public string SkipStepList { get; set; }
  }
}
