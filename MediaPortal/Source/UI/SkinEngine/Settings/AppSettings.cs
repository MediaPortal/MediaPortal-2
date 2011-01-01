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

using MediaPortal.Core.Settings;

namespace MediaPortal.UI.SkinEngine.Settings
{
  public class AppSettings
  {
    protected int _fsScreenNum;
    protected bool _fullScreen;
    protected bool _cellPhoneInputStyle;

    [Setting(SettingScope.User, false)]
    public bool FullScreen
    {
      get { return _fullScreen; }
      set { _fullScreen = value; }
    }

    /// <summary>
    /// Gets or sets the number of the screen where the application is displayed in fullscreen.
    /// The value is an index in the array <see cref="System.Windows.Forms.Screen.AllScreens"/>.
    /// </summary>
    [Setting(SettingScope.User, -1)]
    public int FSScreenNum
    {
      get { return _fsScreenNum; }
      set { _fsScreenNum = value; }
    }

    [Setting(SettingScope.User, false)]
    public bool CellPhoneInputStyle
    {
      get { return _cellPhoneInputStyle; }
      set { _cellPhoneInputStyle = value; }
    }
  }
}
