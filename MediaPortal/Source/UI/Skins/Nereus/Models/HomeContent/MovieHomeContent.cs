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
  public class MovieHomeContent : AbstractHomeContent
  {
    protected override void PopulateBackingList()
    {
      MediaListModel mlm = GetMediaListModel();

      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new MovieGenreShortcut(),
        new MovieYearShortcut(),
        new MovieAgeShortcut(),
        new MovieActorShortcut(),
        new MovieSearchShortcut()
      }));

      _backingList.Add(new LatestMovieList(mlm.Lists["LatestMovies"].AllItems));
      _backingList.Add(new ContinueMovieList(mlm.Lists["ContinuePlayMovies"].AllItems));
      _backingList.Add(new FavoriteMovieList(mlm.Lists["FavoriteMovies"].AllItems));
      _backingList.Add(new UnplayedMovieList(mlm.Lists["UnplayedMovies"].AllItems));
    }
  }

  public class LatestMovieList : ItemsListWrapper
  {
    public LatestMovieList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.LatestAdded]")
    { }
  }

  public class ContinueMovieList : ItemsListWrapper
  {
    public ContinueMovieList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.ContinuePlayed]")
    { }
  }

  public class FavoriteMovieList : ItemsListWrapper
  {
    public FavoriteMovieList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedMovieList : ItemsListWrapper
  {
    public UnplayedMovieList(ItemsList mediaList)
      : base(mediaList, "[Nereus.Home.Unplayed]")
    { }
  }

  public class MovieGenreShortcut : GenreShortcutItem
  { }

  public class MovieYearShortcut : YearShortcutItem
  { }

  public class MovieAgeShortcut : AgeShortcutItem
  { }

  public class MovieActorShortcut : ActorShortcutItem
  { }

  public class MovieSearchShortcut : SearchShortcutItem
  { }
}
