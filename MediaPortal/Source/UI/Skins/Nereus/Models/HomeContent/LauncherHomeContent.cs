﻿#region Copyright (C) 2007-2018 Team MediaPortal

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

using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class LauncherHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      var alm = GetAppListModel();

      _backingList.Add(new LatestLaunchedAppList(alm.Lists["LastLaunchApps"].AllItems));
      _backingList.Add(new FavoriteAppList(alm.Lists["FavoriteApps"].AllItems));
    }
  }

  public class LatestLaunchedAppList : ItemsListWrapper
  {
    public LatestLaunchedAppList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestLaunched]")
    { }
  }

  public class FavoriteAppList : ItemsListWrapper
  {
    public FavoriteAppList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }
}
