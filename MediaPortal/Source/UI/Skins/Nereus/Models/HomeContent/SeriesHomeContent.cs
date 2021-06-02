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
  public class SeriesHomeContent : AbstractHomeContent
  {
    public SeriesHomeContent()
    {
      _availableLists.Add(new LatestEpisodeList());
      _availableLists.Add(new ContinueSeriesList());
      _availableLists.Add(new FavoriteSeriesList());
      _availableLists.Add(new UnplayedSeriesList());
    }

    protected override void PopulateBackingList()
    {
      _backingList.Add(new MediaShortcutListWrapper(new List<ListItem>
      {
        new SeriesGenreShortcut(),
        new SeriesYearShortcut(),
        new SeriesAgeShortcut(),
        new SeriesActorShortcut(),
        new SeriesSearchShortcut()
      }));
    }

    protected override IContentListModel GetContentListModel()
    {
      return GetMediaListModel();
    }
  }

  public class LatestEpisodeList : MediaListItemsListWrapper
  {
    public LatestEpisodeList()
      : base("LatestEpisodes", "[Nereus.Home.LatestAdded]")
    { }
  }

  public class ContinueSeriesList : MediaListItemsListWrapper
  {
    public ContinueSeriesList()
      : base("ContinuePlaySeries", "[Nereus.Home.ContinuePlayed]")
    { }
  }

  public class FavoriteSeriesList : MediaListItemsListWrapper
  {
    public FavoriteSeriesList()
      : base("FavoriteSeries", "[Nereus.Home.Favorites]")
    { }
  }

  public class UnplayedSeriesList : MediaListItemsListWrapper
  {
    public UnplayedSeriesList()
      : base("UnplayedSeries", "[Nereus.Home.Unplayed]")
    { }
  }

  public class SeriesGenreShortcut : GenreShortcutItem
  {
    public SeriesGenreShortcut()
      : base(Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT, typeof(SeriesFilterByGenreScreenData))
    { }
  }

  public class SeriesYearShortcut : YearShortcutItem
  {
    public SeriesYearShortcut()
      : base(Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT, typeof(SeriesFilterByYearScreenData))
    { }
  }

  public class SeriesAgeShortcut : AgeShortcutItem
  {
    public SeriesAgeShortcut()
      : base(Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT, typeof(SeriesFilterByCertificationScreenData))
    { }
  }

  public class SeriesActorShortcut : ActorShortcutItem
  {
    public SeriesActorShortcut()
      : base(Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT, typeof(SeriesEpisodeFilterByActorScreenData))
    { }
  }

  public class SeriesSearchShortcut : SearchShortcutItem
  {
    public SeriesSearchShortcut()
      : base(Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT, typeof(SeriesSimpleSearchScreenData))
    { }
  }
}
