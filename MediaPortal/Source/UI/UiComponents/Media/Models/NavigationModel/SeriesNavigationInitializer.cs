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

using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.FilterTrees;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Helpers;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  internal class SeriesNavigationInitializer : BaseNavigationInitializer
  {
    internal static IEnumerable<string> RESTRICTED_MEDIA_CATEGORIES = new List<string> { Models.MediaNavigationMode.Series }; // "Series"

    public SeriesNavigationInitializer()
    {
      _mediaNavigationMode = Models.MediaNavigationMode.Series;
      _mediaNavigationRootState = Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT;
      _viewName = Consts.RES_SERIES_VIEW_NAME;
      _necessaryMias = Consts.NECESSARY_EPISODE_MIAS;
      _optionalMias = Consts.OPTIONAL_EPISODE_MIAS;
      _restrictedMediaCategories = RESTRICTED_MEDIA_CATEGORIES;
      _rootRole = EpisodeAspect.ROLE_EPISODE;
    }

    public static void NavigateToSeries(Guid seriesId)
    {
      MediaNavigationConfig config = new MediaNavigationConfig
      {
        RootScreenType = typeof(SeriesFilterByNameScreenData),
        DefaultScreenType = typeof(SeriesFilterBySeasonScreenData),
        FilterPath = new FilterTreePath(SeriesAspect.ROLE_SERIES),
        LinkedId = seriesId
      };
      MediaNavigationModel.NavigateToRootState(Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT, config);
    }

    public static void NavigateToSeason(Guid seasonId)
    {
      MediaNavigationConfig config = new MediaNavigationConfig
      {
        RootScreenType = typeof(SeriesFilterBySeasonScreenData),
        DefaultScreenType = typeof(SeriesShowItemsScreenData),
        FilterPath = new FilterTreePath(SeasonAspect.ROLE_SEASON),
        LinkedId = seasonId
      };
      MediaNavigationModel.NavigateToRootState(Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT, config);
    }

    protected virtual async Task PrepareFilterTree()
    {
      if (!_rootRole.HasValue)
        return;

      _customFilterTree = new RelationshipFilterTree(_rootRole.Value);

      //Update filter by adding the user filter to the already loaded filters
      IFilter userFilter = await CertificationHelper.GetUserCertificateFilter(_necessaryMias);
      if (userFilter != null)
        _customFilterTree.AddFilter(userFilter);

      userFilter = await CertificationHelper.GetUserCertificateFilter(new[] { SeriesAspect.ASPECT_ID });
      if (userFilter != null)
        _customFilterTree.AddFilter(userFilter, new FilterTreePath(SeriesAspect.ROLE_SERIES));
    }

    protected override async Task PrepareAsync()
    {
      await base.PrepareAsync();
      await PrepareFilterTree();

      _defaultScreen = new SeriesFilterByNameScreenData();
      _availableScreens = new List<AbstractScreenData>
      {
        new SeriesShowItemsScreenData(_genericPlayableItemCreatorDelegate),
        // C# doesn't like it to have an assignment inside a collection initializer
        _defaultScreen,
        new SeriesFilterBySeasonScreenData(),
        new VideosFilterByLanguageScreenData(),
        new VideosFilterByPlayCountScreenData(),
        new SeriesFilterByGenreScreenData(),
        new SeriesFilterByCertificationScreenData(),
        new SeriesEpisodeFilterByActorScreenData(),
        new SeriesEpisodeFilterByCharacterScreenData(),
        new SeriesFilterByCompanyScreenData(),
        new SeriesFilterByTvNetworkScreenData(),
        new SeriesSimpleSearchScreenData(_genericPlayableItemCreatorDelegate),
      };
      _defaultSorting = new SeriesSortByEpisode();
      _availableSortings = new List<Sorting.Sorting>
      {
        _defaultSorting,
        new SeriesSortByDVDEpisode(),
        new VideoSortByFirstGenre(),
        new SeriesSortByCertification(),
        new SeriesSortByFirstActor(),
        new SeriesSortByFirstCharacter(),
        new VideoSortByFirstActor(),
        new VideoSortByFirstCharacter(),
        new VideoSortByFirstDirector(),
        new VideoSortByFirstWriter(),
        new SeriesSortByFirstTvNetwork(),
        new SeriesSortByFirstProductionStudio(),
        new SeriesSortBySeasonTitle(),
        new SeriesSortByEpisodeTitle(),
        new SortByTitle(),
        new SortBySortTitle(),
        new SortByName(),
        new SortByFirstAiredDate(),
        new SortByAddedDate(),
        new SortBySystem(),
      };
      _defaultGrouping = null;
      _availableGroupings = new List<Sorting.Sorting>
      {
        //_defaultGrouping,
        new SeriesSortByEpisode(),
        new SeriesSortByDVDEpisode(),
        new VideoSortByFirstGenre(),
        new SeriesSortByCertification(),
        new SeriesSortByFirstActor(),
        new SeriesSortByFirstCharacter(),
        new VideoSortByFirstActor(),
        new VideoSortByFirstCharacter(),
        new VideoSortByFirstDirector(),
        new VideoSortByFirstWriter(),
        new SeriesSortByFirstTvNetwork(),
        new SeriesSortByFirstProductionStudio(),
        new SeriesSortBySeasonTitle(),
        new SeriesSortByEpisodeTitle(),
        new SortByTitle(),
        new SortBySortTitle(),
        new SortByName(),
        new SortByFirstAiredDate(),
        new SortByAddedDate(),
        new SortBySystem(),
      };
    }
  }
}
