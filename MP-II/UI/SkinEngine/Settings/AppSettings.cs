#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using MediaPortal.Core.Settings;

namespace MediaPortal.SkinEngine.Settings
{
  public class AppSettings
  {
    private bool _fullScreen;
    private bool _refreshRateControl;
    private string _FPS24;
    private string _FPS25;
    private string _FPS30;
    private string _FPSDefault;

    [Setting(SettingScope.User, false)]
    public bool FullScreen
    {
      get { return _fullScreen; }
      set { _fullScreen = value; }
    }
    [Setting(SettingScope.User, false)]
    public bool RefreshRateControl
    {
      get { return _refreshRateControl; }
      set { _refreshRateControl = value; }
    }
    [Setting(SettingScope.Global, "")]
    public string FPS24
    {
      get { return _FPS24; }
      set { _FPS24 = value; }
    }
    [Setting(SettingScope.Global, "")]
    public string FPS25
    {
      get { return _FPS25; }
      set { _FPS25 = value; }
    }
    [Setting(SettingScope.Global, "")]
    public string FPS30
    {
      get { return _FPS30; }
      set { _FPS30 = value; }
    }
    [Setting(SettingScope.Global, "")]
    public string FPSDefault
    {
      get { return _FPSDefault; }
      set { _FPSDefault = value; }
    }
  }
}
