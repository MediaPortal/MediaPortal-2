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

using System;
using System.Collections.Generic;
using MediaPortal.Common.Settings;
using MediaPortal.UiComponents.BlueVision.Models;
using MediaPortal.Utilities.Xml;

namespace MediaPortal.UiComponents.BlueVision.Settings
{
  public class MenuSettings
  {
    public const string MENU_NAME_AUDIO = "[Menu.Audio]";
    public const string MENU_ID_AUDIO = "1190A648-90F3-413D-88AC-DA89684E9766";
    public const string MENU_NAME_IMAGE = "[Menu.Image]";
    public const string MENU_ID_IMAGE = "E1748A80-347C-4E1A-8A26-DABEC246A540";
    public const string MENU_NAME_MEDIAHUB = "[Menu.MediaHub]";
    public const string MENU_ID_MEDIAHUB = "D887C930-3CFB-4FC5-A559-FFF6C8E30B25";
    public const string MENU_NAME_TV = "[Menu.TV]";
    public const string MENU_ID_TV = "E32BBACF-56C6-406E-9576-EA68271F053C";
    public const string MENU_NAME_WEATHER = "[Menu.Weather]";
    public const string MENU_ID_WEATHER = "F0E6117B-CF56-41E0-8171-CAA3F3544CC1";
    public const string MENU_NAME_NEWS = "[Menu.News]";
    public const string MENU_ID_NEWS = "D675D9BA-A27B-44B9-BC6B-8CF2DC7D220F";
    public const string MENU_NAME_SETTINGS = "[Menu.Settings]";
    public const string MENU_ID_SETTINGS = "9AD4076C-8F1F-4353-AF60-4F6C4238DCB7";
    public const string MENU_NAME_OTHERS = "[Menu.Others]";
    public const string MENU_ID_OTHERS = "D805F21B-0A1F-4323-BEB5-B108778F89AA";
    public const string MENU_NAME_HOME = "[Menu.Home]";
    public const string MENU_ID_HOME = "DF153D5E-0CD9-416c-B18B-E09AD5A864D8";
    public const string MENU_NAME_PLAYING = "[Players.CurrentMediaInfo]";
    public const string MENU_ID_PLAYING = "05D4F591-CB92-4204-955A-5FF665BD8727";

    public static Guid WF_ACTION_CP = new Guid("D83604C0-0936-4416-9DE8-7B6D7C50023C"); // Currently Playing action
    public static Guid WF_ACTION_FS = new Guid("9C3E6701-6856-49ec-A4CD-0CEB15F385F6"); // Fullscreen Content action

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
      MainMenuGroupNames = new List<GroupItemSetting>();
      MainMenuShortCuts = new List<GroupItemSetting>();
      MenuItems = new SerializableDictionary<string, SerializableDictionary<Guid, HomeMenuModel.GridPosition>>();
    }

    [Setting(SettingScope.Global, "")]
    public string DefaultMenuGroupId { get; set; }

    [Setting(SettingScope.Global)]
    public List<GroupItemSetting> MainMenuGroupNames { get; set; }

    [Setting(SettingScope.Global)]
    public List<GroupItemSetting> MainMenuShortCuts { get; set; }

    [Setting(SettingScope.Global)]
    public SerializableDictionary<string, SerializableDictionary<Guid, HomeMenuModel.GridPosition>> MenuItems { get; set; }

    [Setting(SettingScope.Global)]
    public bool DisableHomeTab { get; set; }

    [Setting(SettingScope.Global, true)]
    public bool DisableAutoSelection { get; set; }

    [Setting(SettingScope.Global)]
    public bool UseAlternativeSplashscreen { get; set; }
  }

  public class GroupItemSetting
  {
    public string Name { get; set; }
    public Guid Id { get; set; }
    public Guid? ActionId { get; set; }
  }
}
