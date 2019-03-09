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

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  class ImagesNavigationInitializer : BaseNavigationInitializer
  {
    internal static IEnumerable<string> RESTRICTED_MEDIA_CATEGORIES = new List<string> { Models.MediaNavigationMode.Images }; // "Images"

    public ImagesNavigationInitializer()
    {
      _mediaNavigationMode = Models.MediaNavigationMode.Images;
      _mediaNavigationRootState = Consts.WF_STATE_ID_IMAGES_NAVIGATION_ROOT;
      _viewName = Consts.RES_IMAGES_VIEW_NAME;
      _necessaryMias = Consts.NECESSARY_IMAGE_MIAS;
      _optionalMias = Consts.OPTIONAL_IMAGE_MIAS;
      _restrictedMediaCategories = RESTRICTED_MEDIA_CATEGORIES;
    }

    protected override async Task PrepareAsync()
    {
      await base.PrepareAsync();

      _defaultScreen = new ImagesFilterByYearScreenData();
      _availableScreens = new List<AbstractScreenData>
        {
          new ImagesShowItemsScreenData(_genericPlayableItemCreatorDelegate),
          _defaultScreen,
          new ImagesFilterByCountryScreenData(),
          new ImagesFilterByStateScreenData(),
          new ImagesFilterByCityScreenData(),
          new ImagesFilterBySizeScreenData(),
          new ImagesFilterBySystemScreenData(),
          new ImagesSimpleSearchScreenData(_genericPlayableItemCreatorDelegate),
        };

      _defaultSorting = new SortByYear();
      _availableSortings = new List<Sorting.Sorting>
        {
          _defaultSorting,
          new SortByTitle(),
          new ImageSortBySize(),
          new SortByAddedDate(),
          new SortBySystem(),
        };

      _defaultGrouping = null;
      _availableGroupings = new List<Sorting.Sorting>
        {
          //_defaultGrouping,
          new SortByYear(),
          new SortByTitle(),
          new ImageSortBySize(),
          new SortByAddedDate(),
          new SortBySystem(),
        };
    }
  }
}
