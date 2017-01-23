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

using System.Drawing;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using MediaPortal.UI.Presentation.Screens;
using SharpDX.Direct3D9;

namespace MediaPortal.UI.SkinEngine.Settings
{
  public class AppSettings
  {
    #region Consts

    protected const ScreenMode DEFAULT_SCREEN_MODE = ScreenMode.FullScreen;
    protected const bool DEFAULT_SKIN_SOUNDS = true;
    protected const bool DEFAULT_CELL_PHONE_INPUT_STYLE = false;
    protected const bool DEFAULT_SCREEN_SAVER_ENABLED = true;
    protected const double DEFAULT_SCREEN_SAVER_TIMEOUT_MIN = 5;
    protected const MultisampleType DEFAULT_MULTISAMPLING_TYPE = MultisampleType.None;
    protected const SuspendLevel DEFAULT_SUSPEND_LEVEL = SuspendLevel.None;

    #endregion

    protected bool _skinSounds = DEFAULT_SKIN_SOUNDS;
    protected bool _cellPhoneInputStyle = DEFAULT_CELL_PHONE_INPUT_STYLE;
    protected bool _screenSaverEnabled = DEFAULT_SCREEN_SAVER_ENABLED;
    protected double _screenSaverTimoutMin = DEFAULT_SCREEN_SAVER_TIMEOUT_MIN;
    protected MultisampleType _multisampleType = DEFAULT_MULTISAMPLING_TYPE;
    protected SuspendLevel _suspendLevel = DEFAULT_SUSPEND_LEVEL;

    [Setting(SettingScope.User, 0)]
    public MultisampleType MultisampleType
    {
      get { return _multisampleType; }
      set { _multisampleType = value; }
    }

    [Setting(SettingScope.User, DEFAULT_SCREEN_MODE)]
    public ScreenMode ScreenMode { get; set; }

    [Setting(SettingScope.User, null)]
    public Point? WindowPosition {  get; set; }

    [Setting(SettingScope.User, null)]
    public Size? WindowSize { get; set; }

    [Setting(SettingScope.User, DEFAULT_SKIN_SOUNDS)]
    public bool SkinSounds
    {
      get { return _skinSounds; }
      set { _skinSounds = value; }
    }

    [Setting(SettingScope.User, DEFAULT_CELL_PHONE_INPUT_STYLE)]
    public bool CellPhoneInputStyle
    {
      get { return _cellPhoneInputStyle; }
      set { _cellPhoneInputStyle = value; }
    }

    [Setting(SettingScope.User, DEFAULT_SCREEN_SAVER_ENABLED)]
    public bool ScreenSaverEnabled
    {
      get { return _screenSaverEnabled; }
      set { _screenSaverEnabled = value; }
    }

    [Setting(SettingScope.User, DEFAULT_SCREEN_SAVER_TIMEOUT_MIN)]
    public double ScreenSaverTimeoutMin
    {
      get { return _screenSaverTimoutMin; }
      set { _screenSaverTimoutMin = value; }
    }

    /// <summary>
    /// Gets or sets a value which avoids Windows automatic energy saver.
    /// </summary>
    [Setting(SettingScope.User, DEFAULT_SUSPEND_LEVEL)]
    public SuspendLevel SuspendLevel
    {
      get { return _suspendLevel; }
      set { _suspendLevel = value; }
    }
  }
}
