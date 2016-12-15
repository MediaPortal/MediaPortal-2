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

namespace MediaPortal.UI.Settings
{
  public class StartupSettings
  {
    #region Consts

    protected const int STARTUP_SCREEN_NUM = -1;

    #endregion

    protected int _startupScreenNum = STARTUP_SCREEN_NUM;

    /// <summary>
    /// Gets or sets the number of the screen where the application is displayed in fullscreen.
    /// The value is an index in the array <see cref="System.Windows.Forms.Screen.AllScreens"/>.
    /// </summary>
    [Setting(SettingScope.User, STARTUP_SCREEN_NUM)]
    public int StartupScreenNum
    {
      get { return _startupScreenNum; }
      set { _startupScreenNum = value; }
    }

    /// <summary>
    /// Gets or sets an alternative path to an image which will be used as splash screen. If not set, the default image is used.
    /// The path needs to be relative to entry assembly.
    /// </summary>
    [Setting(SettingScope.User, null)]
    public string AlternativeSplashScreen { get; set; }
  }
}
