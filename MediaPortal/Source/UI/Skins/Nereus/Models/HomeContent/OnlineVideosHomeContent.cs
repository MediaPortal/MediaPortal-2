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

using MediaPortal.UI.Presentation.DataObjects;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class OnlineVideosHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      var onlineVideosModel = GetOnlineVideosListModel();

      _backingList.Add(new TopLanguagesOnlineVideoSitesList(onlineVideosModel.Lists["TopLanguagesOnlineVideoSites"].AllItems));
      _backingList.Add(new LastOnlineVideoSiteList(onlineVideosModel.Lists["LastOnlineVideoSites"].AllItems));
      _backingList.Add(new FavoriteOnlineVideoSiteList(onlineVideosModel.Lists["FavoriteOnlineVideoSites"].AllItems));
      
    }

    protected override void ForceUpdateBackingList()
    {
      var onlineVideosModel = GetOnlineVideosListModel();

      onlineVideosModel.ForceUpdate("TopLanguagesOnlineVideoSites");
      onlineVideosModel.ForceUpdate("LastOnlineVideoSites");
      onlineVideosModel.ForceUpdate("FavoriteOnlineVideoSites");
    }
  }

  public class LastOnlineVideoSiteList : ItemsListWrapper
  {
    public LastOnlineVideoSiteList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestLaunched]")
    { }
  }

  public class FavoriteOnlineVideoSiteList : ItemsListWrapper
  {
    public FavoriteOnlineVideoSiteList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }

  public class TopLanguagesOnlineVideoSitesList : ItemsListWrapper
  {
    public TopLanguagesOnlineVideoSitesList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.TopLanguages]")
    { }
  }
}
