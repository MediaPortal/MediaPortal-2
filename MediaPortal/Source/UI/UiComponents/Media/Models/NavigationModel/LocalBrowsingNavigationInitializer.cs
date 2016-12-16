#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Linq;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.UiComponents.Media.Views;

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  class LocalBrowsingNavigationInitializer : BaseNavigationInitializer
  {
    public LocalBrowsingNavigationInitializer()
    {
      _mediaNavigationMode = Models.MediaNavigationMode.BrowseLocalMedia;
      _mediaNavigationRootState = Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT;
      _viewName = Consts.RES_LOCAL_MEDIA_ROOT_VIEW_NAME;
      _necessaryMias = Consts.NECESSARY_BROWSING_MIAS;
    }

    protected override void Prepare()
    {
      base.Prepare();

      _defaultScreen = new LocalMediaNavigationScreenData(_genericPlayableItemCreatorDelegate);

      // Dynamic screens remain null - browse media states don't provide dynamic filters
      _availableScreens = null;

      _defaultSorting = new BrowseDefaultSorting();
      _availableSortings = new List<Sorting.Sorting>
        {
          _defaultSorting,
          new SortByTitle(),
          new SortByDate(),
          new SortByAddedDate(),
          // We could offer sortings here which are specific for one media item type but which will cope with all three item types (and sort items of the three types in a defined order)
        };

      _defaultGrouping = null;
      _availableGroupings = new List<Sorting.Sorting>
        {
          //_defaultGrouping,
          new SortByTitle(),
          new SortByDate(),
          new SortByAddedDate(),
        };

      var optionalMias = Consts.OPTIONAL_LOCAL_BROWSING_MIAS
        .Union(MediaNavigationModel.GetMediaSkinOptionalMIATypes(MediaNavigationMode));

      _customRootViewSpecification = new AddedRemovableMediaViewSpecificationFacade(
        new LocalMediaRootProxyViewSpecification(_viewName, _necessaryMias, optionalMias));
    }
  }
}
