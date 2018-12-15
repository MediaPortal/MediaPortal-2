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

using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.UiComponents.Media.Helpers;

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  class MoviesNavigationInitializer : BaseNavigationInitializer
  {
    internal static IEnumerable<string> RESTRICTED_MEDIA_CATEGORIES = new List<string> { Models.MediaNavigationMode.Movies }; // "Movies"

    public MoviesNavigationInitializer()
    {
      _mediaNavigationMode = Models.MediaNavigationMode.Movies;
      _mediaNavigationRootState = Consts.WF_STATE_ID_MOVIES_NAVIGATION_ROOT;
      _viewName = Consts.RES_MOVIES_VIEW_NAME;
      _necessaryMias = Consts.NECESSARY_MOVIES_MIAS;
      _optionalMias = Consts.OPTIONAL_MOVIES_MIAS;
      _restrictedMediaCategories = RESTRICTED_MEDIA_CATEGORIES;
      _rootRole = MovieAspect.ROLE_MOVIE;
    }

    protected override async Task PrepareAsync()
    {
      await base.PrepareAsync();

      //Update filter by adding the user filter to the already loaded filters
      IFilter userFilter = await CertificationHelper.GetUserCertificateFilter(_necessaryMias);
      if (userFilter != null)
      {
        _filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, userFilter,
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, _filters));
      }
      else
      {
         _filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, _filters);
      }

      _defaultScreen = new MoviesShowItemsScreenData(_genericPlayableItemCreatorDelegate);
      _availableScreens = new List<AbstractScreenData>
        {
          _defaultScreen,
          new MovieFilterByGenreScreenData(),
          new MovieFilterByCollectionScreenData(),
          new VideosFilterByPlayCountScreenData(),
          new MovieFilterByCertificationScreenData(),
          new MovieFilterByActorScreenData(),
          new MovieFilterByCharacterScreenData(),
          new MovieFilterByDirectorScreenData(),
          new MovieFilterByWriterScreenData(),
          new MovieFilterByCompanyScreenData(),
          new VideosFilterByYearScreenData(),
          new VideosFilterBySystemScreenData(),
          new VideosSimpleSearchScreenData(_genericPlayableItemCreatorDelegate),
        };

      _defaultSorting = new SortByTitle();
      _availableSortings = new List<Sorting.Sorting>
        {
          _defaultSorting,
          new SortBySortTitle(),
          new SortByName(),
          new SortByYear(),
          new VideoSortByFirstGenre(),
          new MovieSortByCertification(),
          new VideoSortByDuration(),
          new VideoSortByFirstActor(),
          new VideoSortByFirstDirector(),
          new VideoSortByFirstWriter(),
          new VideoSortBySize(),
          new VideoSortByAspectRatio(),
          new SortByAddedDate(),
          new SortBySystem(),
        };

      _defaultGrouping = null;
      _availableGroupings = new List<Sorting.Sorting>
        {
          //_defaultGrouping,
          new SortByTitle(),
          new SortBySortTitle(),
          new SortByName(),
          new SortByYear(),
          new VideoSortByFirstGenre(),
          new MovieSortByCertification(),
          new VideoSortByDuration(),
          new VideoSortByFirstActor(),
          new VideoSortByFirstDirector(),
          new VideoSortByFirstWriter(),
          new VideoSortBySize(),
          new VideoSortByAspectRatio(),
          new SortByAddedDate(),
          new SortBySystem(),
        };
    }
  }
}
