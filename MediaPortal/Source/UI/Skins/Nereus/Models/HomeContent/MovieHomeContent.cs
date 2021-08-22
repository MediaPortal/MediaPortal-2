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
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using System.Collections.Generic;
using MediaPortal.UI.Presentation.Models;

namespace MediaPortal.UiComponents.Nereus.Models.HomeContent
{
  public class MovieHomeContent : AbstractHomeContent
  {
    public MovieHomeContent()
    {
      _availableLists.Add(new LatestMovieList());
      _availableLists.Add(new ContinueMovieList());
      _availableLists.Add(new FavoriteMovieList());
      _availableLists.Add(new UnplayedMovieList());
    }

    protected override void PopulateBackingList()
    {
      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new MovieGenreShortcut(),
        new MovieYearShortcut(),
        new MovieAgeShortcut(),
        new MovieActorShortcut(),
        new MovieSearchShortcut()
      }));
    }

    protected override IContentListModel GetContentListModel()
    {
      return GetMediaListModel();
    }
  }

  public class LatestMovieList : MediaListItemsListWrapper
  {
    public LatestMovieList()
      : base("LatestMovies", "[Nereus.Home.LatestAdded]")
    { }
  }

  public class ContinueMovieList : MediaListItemsListWrapper
  {
    public ContinueMovieList()
      : base("ContinuePlayMovies", "[Nereus.Home.ContinuePlayed]")
    { }
  }

  public class FavoriteMovieList : MediaListItemsListWrapper
  {
    public FavoriteMovieList()
      : base("FavoriteMovies", "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedMovieList : MediaListItemsListWrapper
  {
    public UnplayedMovieList()
      : base("UnplayedMovies", "[Nereus.Home.Unplayed]")
    { }
  }

  public class MovieGenreShortcut : GenreShortcutItem
  {
    public MovieGenreShortcut()
      : base(Consts.WF_STATE_ID_MOVIES_NAVIGATION_ROOT, typeof(MovieFilterByGenreScreenData))
    { }
  }

  public class MovieYearShortcut : YearShortcutItem
  {
    public MovieYearShortcut()
      : base(Consts.WF_STATE_ID_MOVIES_NAVIGATION_ROOT, typeof(VideosFilterByYearScreenData))
    { }
  }

  public class MovieAgeShortcut : AgeShortcutItem
  {
    public MovieAgeShortcut()
      : base(Consts.WF_STATE_ID_MOVIES_NAVIGATION_ROOT, typeof(MovieFilterByCertificationScreenData))
    { }

  }

  public class MovieActorShortcut : ActorShortcutItem
  {
    public MovieActorShortcut()
      : base(Consts.WF_STATE_ID_MOVIES_NAVIGATION_ROOT, typeof(MovieFilterByActorScreenData))
    { }
  }

  public class MovieSearchShortcut : SearchShortcutItem
  {
    public MovieSearchShortcut()
      : base(Consts.WF_STATE_ID_MOVIES_NAVIGATION_ROOT, typeof(VideosSimpleSearchScreenData))
    { }
  }
}
