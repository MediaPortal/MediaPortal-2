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

using MediaPortal.UI.Control.InputManager;

namespace MediaPortal.Helpers.SkinHelper.General
{
  public class Consts
  {
    public const string KEY_NAME = "Name";
    public const string KEY_COLOR = "Color";

    public static readonly Key LOAD_SKIN_KEY = Key.F1;
    public static readonly Key LOAD_THEME_KEY = Key.F2;
    public static readonly Key RELOAD_SCREEN_KEY = Key.F3;
    public static readonly Key RELOAD_THEME_KEY = Key.F4;
    // ----- F5 is already used for media screen refresh -----
    public static readonly Key SAVE_SKIN_AND_THEME_KEY = Key.F12;

    public const string RES_LOAD_SKIN_TITLE = "[SkinHelper.LoadSkinTitle]";
    public const string RES_LOAD_THEME_TITLE = "[SkinHelper.LoadThemeTitle]";
  }
}