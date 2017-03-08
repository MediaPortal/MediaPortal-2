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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.UiComponents.Media.Extensions;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.Navigation;
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
    protected Guid[] _optionalMias;
    protected AbstractScreenData _defaultScreen;
    protected ICollection<AbstractScreenData> _availableScreens;
    protected Sorting.Sorting _defaultSorting;
    protected ICollection<Sorting.Sorting> _availableSortings;
    protected Sorting.Sorting _defaultGrouping;
    protected ICollection<Sorting.Sorting> _availableGroupings;
    protected AbstractItemsScreenData.PlayableItemCreatorDelegate _genericPlayableItemCreatorDelegate;
    protected ViewSpecification _customRootViewSpecification;
    protected IEnumerable<string> _restrictedMediaCategories = null;
    protected IFilter _filter = null; // Can be set by derived classes to apply an inital filter
    protected List<IFilter> _filters = new List<IFilter>();
    protected FixedItemStateTracker _tracker;

    #endregion

    protected BaseNavigationInitializer()
    {
      // Create a generic delegate that knows all kind of our inbuilt media item types.
      _genericPlayableItemCreatorDelegate = mi =>
      {
        if (mi.Aspects.ContainsKey(EpisodeAspect.ASPECT_ID))
          return new EpisodeItem(mi) { Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi)) };
        if (mi.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
          return new MovieItem(mi) { Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi)) };
        if (mi.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
          return new AudioItem(mi) { Command = new MethodDelegateCommand(() => MediaNavigationModel.AddCurrentViewToPlaylist(mi)) };
        if (mi.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
          return new VideoItem(mi) { Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi)) };
        if (mi.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
          return new ImageItem(mi) { Command = new MethodDelegateCommand(() => MediaNavigationModel.AddCurrentViewToPlaylist(mi)) };
        return null;
      };
    }

    public string MediaNavigationMode
    {
      get { return _mediaNavigationMode; }
    }

    public virtual Guid MediaNavigationRootState
    {
      get { return _mediaNavigationRootState; }
    }

    /// <summary>
    /// Prepares custom views or initializes specific data, which are not available at construction time (i.e. <see cref="MediaNavigationModel.GetMediaSkinOptionalMIATypes(string)"/>).
    /// </summary>
    protected virtual void Prepare()
    {
      // Read filters from plugin.xml and apply the matching ones
      BuildFilters();

      _customRootViewSpecification = null;
    }

    public virtual void InitMediaNavigation(out string mediaNavigationMode, out NavigationData navigationData)
    {
      Prepare();

      string nextScreenName;
      AbstractScreenData nextScreen = null;

      // Try to load the prefered next screen from settings.
      if (NavigationData.LoadScreenHierarchy(_viewName, out nextScreenName))
      {
        // Support for browsing mode.
        if (nextScreenName == Consts.USE_BROWSE_MODE)
          SetBrowseMode();

        if (_availableScreens != null)
          nextScreen = _availableScreens.FirstOrDefault(s => s.GetType().ToString() == nextScreenName);
      }

      IEnumerable<Guid> optionalMIATypeIDs = MediaNavigationModel.GetMediaSkinOptionalMIATypes(MediaNavigationMode);
      if(_optionalMias != null)
      {
        optionalMIATypeIDs = optionalMIATypeIDs.Union(_optionalMias);
        optionalMIATypeIDs = optionalMIATypeIDs.Except(_necessaryMias);
      }
      // Prefer custom view specification.
      ViewSpecification rootViewSpecification = _customRootViewSpecification ??
        new MediaLibraryQueryViewSpecification(_viewName, _filter, _necessaryMias, optionalMIATypeIDs, true, _necessaryMias)
        {
          MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
        };

      if (nextScreen == null)
        nextScreen = _defaultScreen;

      ScreenConfig nextScreenConfig;
      NavigationData.LoadLayoutSettings(nextScreen.GetType().ToString(), out nextScreenConfig);

      Sorting.Sorting nextSortingMode = _availableSortings.FirstOrDefault(sorting => sorting.GetType().ToString() == nextScreenConfig.Sorting) ?? _defaultSorting;
      Sorting.Sorting nextGroupingMode = _availableGroupings == null || String.IsNullOrEmpty(nextScreenConfig.Grouping) ? null : _availableGroupings.FirstOrDefault(grouping => grouping.GetType().ToString() == nextScreenConfig.Grouping) ?? _defaultGrouping;

      navigationData = new NavigationData(null, _viewName, MediaNavigationRootState,
        MediaNavigationRootState, rootViewSpecification, nextScreen, _availableScreens, nextSortingMode, nextGroupingMode)
      {
        AvailableSortings = _availableSortings,
        AvailableGroupings = _availableGroupings,
        LayoutType = nextScreenConfig.LayoutType,
        LayoutSize = nextScreenConfig.LayoutSize
      };
      mediaNavigationMode = MediaNavigationMode;
    }

    /// <summary>
    /// Switches to browsing by MediaLibray shares, limited to restricted MediaCategories.
    /// </summary>
    protected void SetBrowseMode()
    {
      _availableScreens = null;
      _defaultScreen = new BrowseMediaNavigationScreenData(_genericPlayableItemCreatorDelegate);
      _customRootViewSpecification = new BrowseMediaRootProxyViewSpecification(_viewName, _necessaryMias, null, _restrictedMediaCategories);
    }

    /// <summary>
    /// Reads filter settings for <see cref="BaseNavigationInitializer"/> derived classes from plugin.xml.
    /// </summary>
    protected void BuildFilters()
    {
      if (_tracker != null)
        return;

      _tracker = new FixedItemStateTracker("BaseNavigationInitializer - Media navigation filter registration");

      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(MediaNavigationFilterBuilder.MEDIA_FILTERS_PATH))
      {
        try
        {
          MediaNavigationFilter navigationFilter = pluginManager.RequestPluginItem<MediaNavigationFilter>(
              MediaNavigationFilterBuilder.MEDIA_FILTERS_PATH, itemMetadata.Id, _tracker);
          if (navigationFilter == null)
            ServiceRegistration.Get<ILogger>().Warn("Could not instantiate Media navigation filter with id '{0}'", itemMetadata.Id);
          else
          {
            string extensionClass = navigationFilter.ClassName;
            if (extensionClass == null)
              throw new PluginInvalidStateException("Could not find class type for Media navigation filter  {0}", navigationFilter.ClassName);

            if (extensionClass != GetType().Name)
              continue;

            _filters.Add(navigationFilter.Filter);
          }
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add Media navigation filter with id '{0}'", e, itemMetadata.Id);
        }
      }

      if (_filters.Count == 0)
      {
        _filter = null;
        return;
      }

      _filter = _filters.Count == 1 ? 
        // Single filter
        _filters[0] : 
        // Or a "AND" combined filter
        new BooleanCombinationFilter(BooleanOperator.And, _filters);
    }
  }
}
