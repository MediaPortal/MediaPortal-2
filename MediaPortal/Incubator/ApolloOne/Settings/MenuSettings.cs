#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.ApolloOne.Models;

namespace MediaPortal.UiComponents.ApolloOne.Settings
{
  public class MenuSettings
  {
    public const string OTHERS_MENU_NAME = "[Menu.Others]";
    
    public const int DEFAULT_ROWSPAN_SMALL = 2;
    public const int DEFAULT_COLSPAN_SMALL = 2;

    public const int DEFAULT_ROWSPAN_NORMAL = 3;
    public const int DEFAULT_COLSPAN_NORMAL = 5;

    public const int DEFAULT_ROWSPAN_LARGE = 6;
    public const int DEFAULT_COLSPAN_LARGE = 6;
    
    public const int DEFAULT_NUM_ROWS = 6;
    public const int DEFAULT_NUM_COLS = 16;

    public MenuSettings()
    {
      MainMenuGroupNames = new List<string>();
      MenuItems = new Dictionary<string, Dictionary<Guid, HomeMenuModel.GridPosition>>();
    }

    [Setting(SettingScope.Global, 0)]
    public int DefaultIndex { get; set; }

    [Setting(SettingScope.Global)]
    public IList<string> MainMenuGroupNames { get; set; }

    [Setting(SettingScope.Global)]
    public Dictionary<string, Dictionary<Guid, HomeMenuModel.GridPosition>> MenuItems { get; set; }
  }
}
