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

using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class EmulatorsHomeContent : AbstractHomeContent
  {
    public EmulatorsHomeContent()
    {
      _availableLists.Add(new LastPlayedGameList());
      _availableLists.Add(new FavoritedGameList());
      _availableLists.Add(new LatestGameList());
      _availableLists.Add(new UnplayedGameList());
    }

    protected override IContentListModel GetContentListModel()
    {
      return GetMediaListModel();
    }
  }

  public class LastPlayedGameList : MediaListItemsListWrapper
  {
    public LastPlayedGameList()
      : base("LastPlayedGames", "[Nereus.Home.LatestPlayed]")
    { }
  }

  public class FavoritedGameList : MediaListItemsListWrapper
  {
    public FavoritedGameList()
      : base("FavoriteGames", "[Nereus.Home.Favorites]")
    { }
  }

  public class LatestGameList : MediaListItemsListWrapper
  {
    public LatestGameList()
      : base("LatestGames", "[Nereus.Home.LatestAdded]")
    { }
  }

  public class UnplayedGameList : MediaListItemsListWrapper
  {
    public UnplayedGameList()
      : base("UnplayedGames", "[Nereus.Home.Unplayed]")
    { }
  }
}
