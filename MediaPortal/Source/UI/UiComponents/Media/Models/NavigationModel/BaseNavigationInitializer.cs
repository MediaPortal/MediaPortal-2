#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Settings;
using MediaPortal.UiComponents.Media.Views;

namespace MediaPortal.UiComponents.Media.Models.NavigationModel
{
  /// <summary>
  /// Base class for <see cref="IMediaNavigationInitializer"/>. Derived classes should only fill the protected fields and use the logic from this base class.
  /// </summary>
  public abstract class BaseNavigationInitializer : IMediaNavigationInitializer
  {
    #region Protected fields

    protected string _viewName;
    protected string _mediaNavigationMode;
    protected Guid _mediaNavigationRootState;
    protected Guid[] _necessaryMias;
    protected AbstractScreenData _defaultScreen;
    protected ICollection<AbstractScreenData> _availableScreens;
    protected Sorting.Sorting _defaultSorting;
    protected ICollection<Sorting.Sorting> _availableSortings;

    #endregion

    public string MediaNavigationMode
    {
      get { return _mediaNavigationMode; }
    }

    public virtual Guid MediaNavigationRootState
    {
      get { return _mediaNavigationRootState; }
    }

    public virtual void InitMediaNavigation(out string mediaNavigationMode, out NavigationData navigationData)
    {
      IEnumerable<Guid> skinDependentOptionalMIATypeIDs = MediaNavigationModel.GetMediaSkinOptionalMIATypes(MediaNavigationMode);
      ViewSpecification rootViewSpecification = new MediaLibraryQueryViewSpecification(_viewName, null, _necessaryMias, skinDependentOptionalMIATypeIDs, true)
      {
        MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
      };
      string nextScreenName;
      AbstractScreenData nextScreen = null;

      // Try to load the prefered next screen from settings.
      if (NavigationData.LoadScreenHierarchy(_viewName, out nextScreenName))
        nextScreen = _availableScreens.FirstOrDefault(s => s.GetType().ToString() == nextScreenName);

      if (nextScreen == null)
        nextScreen = _defaultScreen;

      ScreenConfig nextScreenConfig;
      NavigationData.LoadLayoutSettings(nextScreen.GetType().ToString(), out nextScreenConfig);

      Sorting.Sorting nextSortingMode = _availableSortings.FirstOrDefault(sorting => sorting.GetType().ToString() == nextScreenConfig.Sorting) ?? _defaultSorting;

      navigationData = new NavigationData(null, _viewName, MediaNavigationRootState,
        MediaNavigationRootState, rootViewSpecification, nextScreen, _availableScreens, nextSortingMode)
      {
        AvailableSortings = _availableSortings,
        LayoutType = nextScreenConfig.LayoutType,
        LayoutSize = nextScreenConfig.LayoutSize
      };
      mediaNavigationMode = MediaNavigationMode;
    }
  }
}
