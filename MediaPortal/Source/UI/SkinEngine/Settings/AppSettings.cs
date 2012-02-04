#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

namespace MediaPortal.UI.SkinEngine.Settings
{
  public class AppSettings
  {
    #region Consts

    protected const bool DEFAULT_FULL_SCREEN = true;
    protected const int DEFAULT_FS_SCREEN_NUM = -1;
    protected const bool DEFAULT_CELL_PHONE_INPUT_STYLE = false;
    protected const bool DEFAULT_SCREEN_SAVER_ENABLED = true;
    protected const double DEFAULT_SCREEN_SAVER_TIMEOUT_MIN = 5;

    #endregion

    protected int _fsScreenNum = DEFAULT_FS_SCREEN_NUM;
    protected bool _fullScreen = DEFAULT_FULL_SCREEN;
    protected bool _cellPhoneInputStyle = DEFAULT_CELL_PHONE_INPUT_STYLE;
    protected bool _screenSaverEnabled = DEFAULT_SCREEN_SAVER_ENABLED;
    protected double _screenSaverTimoutMin = DEFAULT_SCREEN_SAVER_TIMEOUT_MIN;
    protected int _multiSampleTypeIndex = 0;

    [Setting(SettingScope.User, 0)]
    public int MultiSampleType
    {
      get { return _multiSampleTypeIndex; }
      set { _multiSampleTypeIndex = value; }
    }

    [Setting(SettingScope.User, DEFAULT_FULL_SCREEN)]
    public bool FullScreen
    {
      get { return _fullScreen; }
      set { _fullScreen = value; }
    }

    /// <summary>
    /// Gets or sets the number of the screen where the application is displayed in fullscreen.
    /// The value is an index in the array <see cref="System.Windows.Forms.Screen.AllScreens"/>.
    /// </summary>
    [Setting(SettingScope.User, DEFAULT_FS_SCREEN_NUM)]
    public int FSScreenNum
    {
      get { return _fsScreenNum; }
      set { _fsScreenNum = value; }
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
  }
}
