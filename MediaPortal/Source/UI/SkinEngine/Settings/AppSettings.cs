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

using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using SharpDX.Direct2D1;

namespace MediaPortal.UI.SkinEngine.Settings
{
  public class AppSettings
  {
    #region Consts

    protected const bool DEFAULT_FULL_SCREEN = true;
    protected const bool DEFAULT_CELL_PHONE_INPUT_STYLE = false;
    protected const bool DEFAULT_SCREEN_SAVER_ENABLED = true;
    protected const double DEFAULT_SCREEN_SAVER_TIMEOUT_MIN = 5;
    protected const SuspendLevel DEFAULT_SUSPEND_LEVEL = SuspendLevel.None;

    protected const bool DEFAULT_ANTIALIASING = true;
    protected const InterpolationMode DEFAULT_IMAGE_INTERPOLATION = InterpolationMode.Linear;
    protected const InterpolationMode DEFAULT_VIDEO_INTERPOLATION = InterpolationMode.Cubic;

    #endregion

    [Setting(SettingScope.User, DEFAULT_ANTIALIASING)]
    public bool UseAntialiasing { get; set; }

    [Setting(SettingScope.User, DEFAULT_IMAGE_INTERPOLATION)]
    public InterpolationMode ImageInterpolationMode { get; set; }

    [Setting(SettingScope.User, DEFAULT_VIDEO_INTERPOLATION)]
    public InterpolationMode VideoInterpolationMode { get; set; }

    [Setting(SettingScope.User, DEFAULT_FULL_SCREEN)]
    public bool FullScreen { get; set; }

    [Setting(SettingScope.User, DEFAULT_CELL_PHONE_INPUT_STYLE)]
    public bool CellPhoneInputStyle { get; set; }

    [Setting(SettingScope.User, DEFAULT_SCREEN_SAVER_ENABLED)]
    public bool ScreenSaverEnabled { get; set; }

    [Setting(SettingScope.User, DEFAULT_SCREEN_SAVER_TIMEOUT_MIN)]
    public double ScreenSaverTimeoutMin { get; set; }

    /// <summary>
    /// Gets or sets a value which avoids Windows automatic energy saver.
    /// </summary>
    [Setting(SettingScope.User, DEFAULT_SUSPEND_LEVEL)]
    public SuspendLevel SuspendLevel { get; set; }
  }
}
