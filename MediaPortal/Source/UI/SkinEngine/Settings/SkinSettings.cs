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

using MediaPortal.Common.Settings;

namespace MediaPortal.UI.SkinEngine.Settings
{
  public class SkinSettings
  {
    protected string _skin;
    protected string _theme;

    [Setting(SettingScope.User, "BlueVision")]
    public string Skin
    {
      get { return _skin; }
      set { _skin = value; }
    }
    [Setting(SettingScope.User, "default")]
    public string Theme
    {
      get { return _theme; }
      set { _theme = value; }
    }
  }
}
