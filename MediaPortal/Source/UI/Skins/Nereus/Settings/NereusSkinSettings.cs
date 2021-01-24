#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System;

namespace MediaPortal.UiComponents.Nereus.Settings
{
  public enum GridViewType
  {
    Poster,
    Banner,
    Thumbnail
  }

  public class NereusSkinSettings
  {
    public const string SKIN_NAME = "Nereus";

    [Setting(SettingScope.User, true)]
    public bool EnableFanart { get; set; }

    [Setting(SettingScope.User, 1.0)]
    public double FanartOverlayOpacity { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableGridDetails { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableCoverDetails { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableListWatchedFlags { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableGridWatchedFlags { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableCoverWatchedFlags { get; set; }

    [Setting(SettingScope.User, GridViewType.Poster)]
    public GridViewType MovieGridViewType { get; set; }

    [Setting(SettingScope.User, GridViewType.Poster)]
    public GridViewType SeriesGridViewType { get; set; }

    [Setting(SettingScope.User, GridViewType.Poster)]
    public GridViewType SeasonGridViewType { get; set; }

    [Setting(SettingScope.User, false)]
    public bool EnableMediaItemDetailsView { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableAgeCertificationLogos { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableAutoScrolling { get; set; }

    [Setting(SettingScope.User, 20)]
    public double AutoScrollSpeed { get; set; }

    [Setting(SettingScope.User, 2)]
    public double AutoScrollDelay { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableLoopScrolling { get; set; }

    [Setting(SettingScope.User, true)]
    public bool EnableHelpTexts { get; set; }

    static readonly Guid[] DEFAULT_HOME_MENU_ACTION_IDS = new []
    {
      // Audio
      new Guid("30715d73-4205-417f-80aa-e82f0834171f"),
      // Movies
      new Guid("80d2e2cc-baaa-4750-807b-f37714153751"),
      // Series
      new Guid("30f57cba-459c-4202-a587-09fff5098251"),
      // Videos
      new Guid("a4df2df6-8d66-479a-9930-d7106525eb07"),
      // OnlineVideos
      new Guid("c33E39cc-910e-41c8-bffd-9eccd340b569"),
      // Images
      new Guid("55556593-9fe9-436c-a3b6-a971e10c9d44"),
      // TV
      new Guid("b4a9199f-6dd4-4bda-a077-de9c081f7703"),
      // News
      new Guid("bb49a591-7705-408f-8177-45d633fdfad0"),
      // Weather
      new Guid("e34fdb62-1f3e-4aa9-8a61-d143e0af77b5"),
      // 
      new Guid("a24958e2-538a-455e-a1db-a7bb241aF7ec"),
      // WebRadio
      new Guid("2ded75c0-5eae-4e69-9913-6b50a9ab2956"),
      // App Launcher
      new Guid("873EB147-C998-4632-8F86-D5E24062BE2E")
    };

    Guid[] _homeMenuActionIds = null;

    [Setting(SettingScope.User, null)]
    public Guid[] HomeMenuActionIds
    {
      get
      {
        var actionIds = _homeMenuActionIds;
        if (actionIds == null || actionIds.Length == 0)
          _homeMenuActionIds = actionIds = DEFAULT_HOME_MENU_ACTION_IDS;
        return actionIds;
      }
      set { _homeMenuActionIds = value; }
    }
  }
}
