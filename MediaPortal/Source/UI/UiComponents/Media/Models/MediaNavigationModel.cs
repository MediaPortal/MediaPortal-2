#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.PluginItemBuilders;
using MediaPortal.Common.PluginManager;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.SkinResources;
using MediaPortal.UiComponents.Media.Models.Sorting;
using MediaPortal.UiComponents.Media.Views;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.Media.Models.ScreenData;
using MediaPortal.UiComponents.Media.Models.Navigation;

namespace MediaPortal.UiComponents.Media.Models
{
  /// <summary>
  /// Model which holds the GUI state for the current navigation in the media views.
  /// </summary>
  /// <remarks>
  /// The media model attends all media navigation workflow states. It can handle the local media navigation as well as
  /// media library based audio, videos and images navigation. For each of those modes, there exists an own workflow
  /// state which represents the according root view. So when navigating to the root view state for the audio navigation,
  /// this model will show the audio navigation screen with a view representing the complete audio contents of the media
  /// library, for example. For the navigation, it delegates to <see cref="AbstractScreenData"/> and its sub classes.
  /// </remarks>
  public class MediaNavigationModel : IWorkflowModel
  {
    #region Consts

    // Global ID definitions and references
    public const string MEDIA_MODEL_ID_STR = "4CDD601F-E280-43b9-AD0A-6D7B2403C856";

    // ID variables
    public static readonly Guid MEDIA_MODEL_ID = new Guid(MEDIA_MODEL_ID_STR);

    protected static readonly IList<Guid> EMPTY_GUID_LIST = new List<Guid>();

    #endregion

    #region Protected fields

    // Screen data is stored in current navigation context
    protected NavigationContext _currentNavigationContext = null;
    protected static readonly IPluginItemStateTracker _mediaSkinMIATypeRegistrationStateTracker = new DefaultItemStateTracker("MediaModelNavigation");

    #endregion

    #region Public members

    /// <summary>
    /// Gets the current media navigation mode.
    /// </summary>
    /// <remarks>
    /// The media navigation mode determines the media library part which is navigated: Audio, Videos or Images. Other
    /// navigation modes are BrowseLocalMedia, which is completely decoupled from the media library, and and BrowseMediaLibrary,
    /// which lets the user browse through the media library contents.
    /// </remarks>
    public MediaNavigationMode Mode
    {
      get
      {
        if (_currentNavigationContext == null)
          return MediaNavigationMode.BrowseLocalMedia;
        return (_currentNavigationContext.GetContextVariable(Consts.KEY_NAVIGATION_MODE, true) as MediaNavigationMode?) ?? MediaNavigationMode.BrowseLocalMedia;
      }
      internal set
      {
        if (_currentNavigationContext == null)
          return;
        _currentNavigationContext.SetContextVariable(Consts.KEY_NAVIGATION_MODE, value);
      }
    }

    /// <summary>
    /// Gets the navigation data which is set in the current workflow navigation context.
    /// </summary>
    public NavigationData NavigationData
    {
      get { return GetNavigationData(_currentNavigationContext, true); }
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item of the media contents view.
    /// Depending on the item type, we will navigate to the choosen view, play the choosen item or filter by the item.
    /// </summary>
    /// <param name="item">The choosen item. Should contain a <see cref="ListItem.Command"/>.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      if (item.Command != null)
        item.Command.Execute();
    }

    #endregion

    #region Static members which also can be used from other models

    public static MediaNavigationModel GetCurrentInstance()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      return (MediaNavigationModel) workflowManager.GetModel(MEDIA_MODEL_ID);
    }

    /// <summary>
    /// Gets the information if there is a navigation data available.
    /// </summary>
    public static bool IsNavigationDataEnabled
    {
      get
      {
        MediaNavigationModel model = GetCurrentInstance();
        NavigationData navigationData = model.NavigationData;
        return navigationData != null && navigationData.IsEnabled;
      }
    }

    /// <summary>
    /// Adds the current view to the playlist of the current player.
    /// </summary>
    /// <remarks>
    /// This action can be called from outside when there is an enabled navigation data present (<see cref="IsNavigationDataEnabled"/>.
    /// </remarks>
    public static void AddCurrentViewToPlaylist()
    {
      MediaNavigationModel model = GetCurrentInstance();
      NavigationData navigationData = model.NavigationData;
      if (navigationData == null || !navigationData.IsEnabled)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaNavigationModel.AddCurrentViewToPlaylist: No enabled navigation data present");
        return;
      }
      if (navigationData.CurrentScreenData.IsItemsEmpty)
      {
        ServiceRegistration.Get<IDialogManager>().ShowDialog(Consts.RES_NO_ITEMS_TO_ADD_HEADER, Consts.RES_NO_ITEMS_TO_ADD_TEXT, DialogType.OkDialog, false, DialogButtonType.Ok);
        return;
      }
      model.AddCurrentViewToPlaylistInternal();
    }

    #endregion

    #region Protected members

    protected internal static NavigationData GetNavigationData(NavigationContext navigationContext, bool inheritFromPredecessor)
    {
      return navigationContext.GetContextVariable(Consts.KEY_NAVIGATION_DATA, inheritFromPredecessor) as NavigationData;
    }

    protected static void SetNavigationData(NavigationData navigationData, NavigationContext navigationContext)
    {
      navigationContext.SetContextVariable(Consts.KEY_NAVIGATION_DATA, navigationData);
    }

    protected IEnumerable<MediaItem> GetMediaItemsFromCurrentView()
    {
      NavigationData navigationData = NavigationData;
      if (navigationData == null)
        yield break;
      foreach (MediaItem mediaItem in navigationData.CurrentScreenData.GetAllMediaItems())
        yield return mediaItem;
    }

    protected void AddCurrentViewToPlaylistInternal()
    {
      NavigationData navigationData = NavigationData;
      if (navigationData == null || !navigationData.IsEnabled)
      {
        ServiceRegistration.Get<ILogger>().Error("MediaNavigationModel: Cannot add current view to playlist - There is no enabled navigation data available");
      }
      MediaNavigationMode mode = Mode;
      switch (mode)
      {
        case MediaNavigationMode.Audio:
          PlayItemsModel.CheckQueryPlayAction(GetMediaItemsFromCurrentView, AVType.Audio);
          break;
        case MediaNavigationMode.Series:
        case MediaNavigationMode.Videos:
        case MediaNavigationMode.Images:
          PlayItemsModel.CheckQueryPlayAction(GetMediaItemsFromCurrentView, AVType.Video);
          break;
        case MediaNavigationMode.BrowseLocalMedia:
        case MediaNavigationMode.BrowseMediaLibrary:
          PlayItemsModel.CheckQueryPlayAction(GetMediaItemsFromCurrentView);
          break;
      }
    }

    private static void GetSkinAndThemeName(ISkinResourceBundle resourceBundle, out string skinName, out string themeName)
    {
      ISkin skin = resourceBundle as ISkin;
      if (skin != null)
      {
        skinName = skin.Name;
        themeName = null;
        return;
      }
      ITheme theme = resourceBundle as ITheme;
      if (theme == null)
      {
        skinName = null;
        themeName = null;
        return;
      }
      themeName = theme.Name;
      skin = theme.ParentSkin;
      skinName = skin == null ? string.Empty : skin.Name;
    }

    // Currently, we don't track skin changes while we're in the media navigation. Normally, that should not be necessary because to switch the skin,
    // the user has to navigate out of media navigation. If we wanted to track skin changes and then update all our navigation data,
    // we would need to register a plugin item registration change listener, which would need to trigger an update of all active media state data.
    protected IEnumerable<Guid> GetMediaSkinOptionalMIATypes(MediaNavigationMode navigationMode)
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      return GetMediaSkinOptionalMIATypes(navigationMode, screenManager.CurrentSkinResourceBundle);
    }

    protected IEnumerable<Guid> GetMediaSkinOptionalMIATypes(MediaNavigationMode navigationMode, ISkinResourceBundle bundle)
    {
      if (bundle == null)
        return EMPTY_GUID_LIST;
      string skinName;
      string themeName;
      GetSkinAndThemeName(bundle, out skinName, out themeName);
      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      string registrationLocation = Consts.MEDIA_SKIN_SETTINGS_REGISTRATION_PATH + "/" + skinName + "/";
      if (!string.IsNullOrEmpty(themeName))
        registrationLocation += themeName + "/";
      registrationLocation += navigationMode + "/" + Consts.MEDIA_SKIN_SETTINGS_REGISTRATION_OPTIONAL_TYPES_PATH;
      IEnumerable<Guid> result = pluginManager.RequestAllPluginItems<MIATypeRegistration>(
          registrationLocation, _mediaSkinMIATypeRegistrationStateTracker).Select(registration => registration.MediaItemAspectTypeId);
      pluginManager.RevokeAllPluginItems(registrationLocation, _mediaSkinMIATypeRegistrationStateTracker);
      return result.Union(GetMediaSkinOptionalMIATypes(navigationMode, bundle.InheritedSkinResources));
    }

    protected void PrepareRootState()
    {
      // Initialize root media navigation state. We will set up all sub processes for each media model "part", i.e.
      // audio, videos, images, browse local media and browse media library.
      Guid currentStateId = _currentNavigationContext.WorkflowState.StateId;
      // The initial state ID determines the media model "part" to initialize: Browse local media, browse media library, audio, videos or images.
      // The media model part determines the media navigation mode and the view contents to be set.
      NavigationData navigationData;
      if (currentStateId == Consts.WF_STATE_ID_AUDIO_NAVIGATION_ROOT)
      {
        Mode = MediaNavigationMode.Audio;
        IEnumerable<Guid> skinDependentOptionalMIATypeIDs = GetMediaSkinOptionalMIATypes(Mode);
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new AudioItem(mi)
          {
              Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryQueryViewSpecification(Consts.RES_AUDIO_VIEW_NAME,
            null, Consts.NECESSARY_AUDIO_MIAS, skinDependentOptionalMIATypeIDs, true)
          {
              MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
          };
        AbstractScreenData filterByAlbum = new AudioFilterByAlbumScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new AudioShowItemsScreenData(picd),
              new AudioFilterByArtistScreenData(),
              filterByAlbum, // C# doesn't like it to have an assignment inside a collection initializer
              new AudioFilterByGenreScreenData(),
              new AudioFilterByDecadeScreenData(),
              new AudioFilterBySystemScreenData(),
              new AudioSimpleSearchScreenData(picd),
            };
        Sorting.Sorting sortByAlbumTrack = new AudioSortByAlbumTrack();
        ICollection<Sorting.Sorting> availableSortings = new List<Sorting.Sorting>
          {
              sortByAlbumTrack,
              new SortByTitle(),
              new AudioSortByFirstGenre(),
              new AudioSortByFirstArtist(),
              new AudioSortByAlbum(),
              new AudioSortByTrack(),
              new SortByYear(),
              new SortBySystem(),
          };
        navigationData = new NavigationData(null, Consts.RES_AUDIO_VIEW_NAME, currentStateId,
            currentStateId, rootViewSpecification, filterByAlbum, availableScreens, sortByAlbumTrack)
          {
              AvailableSortings = availableSortings
          };
      }
      else if (currentStateId == Consts.WF_STATE_ID_VIDEOS_NAVIGATION_ROOT)
      {
        Mode = MediaNavigationMode.Videos;
        IEnumerable<Guid> skinDependentOptionalMIATypeIDs = GetMediaSkinOptionalMIATypes(Mode);
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new VideoItem(mi)
          {
              Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryQueryViewSpecification(Consts.RES_VIDEOS_VIEW_NAME,
            null, Consts.NECESSARY_VIDEO_MIAS, skinDependentOptionalMIATypeIDs, true)
          {
              MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
          };
        AbstractScreenData filterByGenre = new VideosFilterByGenreScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new VideosShowItemsScreenData(picd),
              new VideosFilterByActorScreenData(),
              filterByGenre, // C# doesn't like it to have an assignment inside a collection initializer
              new VideosFilterByYearScreenData(),
              new VideosFilterBySystemScreenData(),
              new VideosSimpleSearchScreenData(picd),
          };
        Sorting.Sorting sortByTitle = new SortByTitle();
        ICollection<Sorting.Sorting> availableSortings = new List<Sorting.Sorting>
          {
              sortByTitle,
              new SortByYear(),
              new VideoSortByFirstGenre(),
              new VideoSortByDuration(),
              new VideoSortByDirector(),
              new VideoSortByFirstActor(),
              new VideoSortBySize(),
              new VideoSortByAspectRatio(),
              new SortBySystem(),
          };
        navigationData = new NavigationData(null, Consts.RES_VIDEOS_VIEW_NAME, currentStateId,
            currentStateId, rootViewSpecification, filterByGenre, availableScreens, sortByTitle)
          {
              AvailableSortings = availableSortings
          };
      } 
      else if (currentStateId == Consts.WF_STATE_ID_SERIES_NAVIGATION_ROOT)
      {
        Mode = MediaNavigationMode.Videos;
        IEnumerable<Guid> skinDependentOptionalMIATypeIDs = GetMediaSkinOptionalMIATypes(Mode);
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new SeriesItem(mi)
          {
              Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryQueryViewSpecification(Consts.RES_SERIES_VIEW_NAME,
            null, Consts.NECESSARY_SERIES_MIAS, skinDependentOptionalMIATypeIDs, true)
          {
              MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
          };
        AbstractScreenData filterBySeries = new SeriesFilterByNameScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new SeriesShowItemsScreenData(picd),
              filterBySeries, // C# doesn't like it to have an assignment inside a collection initializer
              new SeriesFilterBySeasonScreenData(),
              new VideosFilterByGenreScreenData(),
              new VideosSimpleSearchScreenData(picd),
          };
        Sorting.Sorting sortByTitle = new SortByTitle();
        ICollection<Sorting.Sorting> availableSortings = new List<Sorting.Sorting>
          {
              sortByTitle,
              new SortByYear(),
              new VideoSortByFirstGenre(),
              new VideoSortByDuration(),
              new VideoSortByDirector(),
              new VideoSortByFirstActor(),
              new VideoSortBySize(),
              new VideoSortByAspectRatio(),
              new SortBySystem(),
          };
        navigationData = new NavigationData(null, Consts.RES_SERIES_VIEW_NAME, currentStateId,
            currentStateId, rootViewSpecification, filterBySeries, availableScreens, sortByTitle)
          {
              AvailableSortings = availableSortings
          };
      }
      else if (currentStateId == Consts.WF_STATE_ID_IMAGES_NAVIGATION_ROOT)
      {
        Mode = MediaNavigationMode.Images;
        IEnumerable<Guid> skinDependentOptionalMIATypeIDs = GetMediaSkinOptionalMIATypes(Mode);
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new ImageItem(mi)
          {
              Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryQueryViewSpecification(Consts.RES_IMAGES_VIEW_NAME,
            null, Consts.NECESSARY_IMAGE_MIAS, skinDependentOptionalMIATypeIDs, true)
          {
              MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
          };
        AbstractScreenData filterByYear = new ImagesFilterByYearScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new ImagesShowItemsScreenData(picd),
              filterByYear, // C# doesn't like it to have an assignment inside a collection initializer
              new ImagesFilterBySizeScreenData(),
              new ImagesFilterBySystemScreenData(),
              new ImagesSimpleSearchScreenData(picd),
          };
        Sorting.Sorting sortByYear = new SortByYear();
        ICollection<Sorting.Sorting> availableSortings = new List<Sorting.Sorting>
          {
              new SortByYear(),
              new SortByTitle(),
              new ImageSortBySize(),
              new SortBySystem(),
          };
        navigationData = new NavigationData(null, Consts.RES_IMAGES_VIEW_NAME, currentStateId,
            currentStateId, rootViewSpecification, filterByYear, availableScreens, sortByYear)
          {
              AvailableSortings = availableSortings
          };
      }
      else
      {
        // If we were called with a supported root state, we should be either in state WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT
        // or WF_STATE_ID_MEDIA_BROWSE_NAVIGATION_ROOT here
        if (currentStateId != Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT &&
            currentStateId != Consts.WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT)
        {
          // Error case: We cannot handle the given state
          ServiceRegistration.Get<ILogger>().Warn("MediaNavigationModel: Unknown root workflow state with ID '{0}', initializing local media navigation", currentStateId);
          // We simply use the local media mode as fallback for this case, so we go on
          currentStateId = Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT;
        }
        Mode = currentStateId == Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT ? MediaNavigationMode.BrowseLocalMedia :
            MediaNavigationMode.BrowseMediaLibrary;
        IEnumerable<Guid> skinDependentOptionalMIATypeIDs = GetMediaSkinOptionalMIATypes(Mode);
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi =>
          {
            if (mi.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
              return new AudioItem(mi)
                {
                    Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
                };
            if (mi.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
              return new VideoItem(mi)
                {
                    Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
                };
            if (mi.Aspects.ContainsKey(ImageAspect.ASPECT_ID))
              return new ImageItem(mi)
                {
                    Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
                };
            return null;
          };
        IEnumerable<Guid> necessaryMIATypeIDs = new Guid[]
            {
                ProviderResourceAspect.ASPECT_ID,
                MediaAspect.ASPECT_ID,
            };
        IEnumerable<Guid> optionalMIATypeIDs = new Guid[]
            {
                AudioAspect.ASPECT_ID,
                VideoAspect.ASPECT_ID,
                ImageAspect.ASPECT_ID,
            }.Union(skinDependentOptionalMIATypeIDs);
        string viewName = currentStateId == Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT ?
            Consts.RES_LOCAL_MEDIA_ROOT_VIEW_NAME : Consts.RES_BROWSE_MEDIA_ROOT_VIEW_NAME;
        ViewSpecification rootViewSpecification = currentStateId == Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT ?
            new AddedRemovableMediaViewSpecificationFacade(new LocalSharesViewSpecification(viewName, necessaryMIATypeIDs, optionalMIATypeIDs)) :
            new AddedRemovableMediaViewSpecificationFacade(new AllSharesViewSpecification(viewName, necessaryMIATypeIDs, optionalMIATypeIDs));
        // Dynamic screens remain null - browse media states don't provide dynamic filters
        AbstractScreenData screenData = currentStateId == Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT ?
            (AbstractScreenData) new LocalMediaNavigationScreenData(picd) : new BrowseMediaNavigationScreenData(picd);
        Sorting.Sorting browseDefaultSorting = new BrowseDefaultSorting();
        ICollection<Sorting.Sorting> availableSortings = new List<Sorting.Sorting>
          {
              browseDefaultSorting,
              new SortByTitle(),
              new SortByDate(),
              // We could offer sortings here which are specific for one media item type but which will cope with all three item types (and sort items of the three types in a defined order)
          };
        navigationData = new NavigationData(null, viewName, currentStateId,
            currentStateId, rootViewSpecification, screenData, null, browseDefaultSorting)
          {
              AvailableSortings = availableSortings
          };
      }
      SetNavigationData(navigationData, _currentNavigationContext);
    }

    /// <summary>
    /// Prepares the given workflow navigation <paramref name="context"/>, i.e. prepares the view data and the
    /// available filter criteria to be used in the menu.
    /// </summary>
    protected void PrepareState(NavigationContext context)
    {
      _currentNavigationContext = context;
      NavigationData navigationData = GetNavigationData(context, false);
      if (navigationData == null)
        PrepareRootState();
    }

    protected void ReleaseModelData()
    {
      _currentNavigationContext = null;
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MEDIA_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext);
      NavigationData navigationData = GetNavigationData(newContext, false);
      navigationData.Enable();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      NavigationData navigationData = GetNavigationData(oldContext, false);
      navigationData.Dispose();
      ReleaseModelData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext);
      if (push)
      {
        NavigationData navigationData = GetNavigationData(oldContext, false);
        navigationData.Disable();
        navigationData = GetNavigationData(newContext, false);
        navigationData.Enable();
      }
      else
      {
        NavigationData navigationData = GetNavigationData(oldContext, false);
        navigationData.Dispose();
        navigationData = GetNavigationData(newContext, false);
        navigationData.Enable();
      }
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Don't disable the current navigation data when we leave our model - the last navigation data must be
      // available in sub workflows, for example to make the GetMediaItemsFromCurrentView method work
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // The last navigation data was not disabled so we don't need to enable it here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      PrepareState(context);
      ICollection<WorkflowAction> dynamicWorkflowActions = NavigationData.DynamicWorkflowActions;
      if (dynamicWorkflowActions != null)
        foreach (WorkflowAction action in dynamicWorkflowActions)
          actions.Add(action.ActionId, action);
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      screen = NavigationData.CurrentScreenData.Screen;
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
