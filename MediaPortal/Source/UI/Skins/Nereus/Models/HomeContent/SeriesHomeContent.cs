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

using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UiComponents.Media.Models;
using System.Collections.Generic;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class SeriesHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      MediaListModel mlm = GetMediaListModel();

      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new SeriesGenreShortcut(),
        new SeriesYearShortcut(),
        new SeriesAgeShortcut(),
        new SeriesActorShortcut(),
        new SeriesSearchShortcut()
      }));

      _backingList.Add(new LatestEpisodeList(mlm.Lists["LatestEpisodes"].AllItems));
      _backingList.Add(new ContinueSeriesList(mlm.Lists["ContinuePlaySeries"].AllItems));
      _backingList.Add(new FavoriteSeriesList(mlm.Lists["FavoriteSeries"].AllItems));
      _backingList.Add(new UnplayedSeriesList(mlm.Lists["UnplayedSeries"].AllItems));
    }
  }

  public class LatestEpisodeList : ItemsListWrapper
  {
    public LatestEpisodeList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestAdded]")
    { }
  }

  public class ContinueSeriesList : ItemsListWrapper
  {
    public ContinueSeriesList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.ContinuePlayed]")
    { }
  }

  public class FavoriteSeriesList : ItemsListWrapper
  {
    public FavoriteSeriesList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedSeriesList : ItemsListWrapper
  {
    public UnplayedSeriesList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Unplayed]")
    { }
  }

  public class SeriesGenreShortcut : GenreShortcutItem
  { }

  public class SeriesYearShortcut : YearShortcutItem
  { }

  public class SeriesAgeShortcut : AgeShortcutItem
  { }

  public class SeriesActorShortcut : ActorShortcutItem
  { }

  public class SeriesSearchShortcut : SearchShortcutItem
  { }
}
