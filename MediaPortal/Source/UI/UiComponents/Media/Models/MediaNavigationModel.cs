#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.UI.Views;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
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
  /// media library based music, movies and pictures navigation. For each of those modes, there exists an own workflow
  /// state which represents the according root view. So when navigating to the root view state for the music navigation,
  /// this model will show the music navigation screen with a view representing the complete music contents of the media
  /// library, for example. For the navigation, it delegates to <see cref="AbstractScreenData"/> and its sub classes.
  /// </remarks>
  public class MediaNavigationModel : IWorkflowModel
  {
    #region Consts

    // Global ID definitions and references
    public const string MEDIA_MODEL_ID_STR = "4CDD601F-E280-43b9-AD0A-6D7B2403C856";

    // ID variables
    public static readonly Guid MEDIA_MODEL_ID = new Guid(MEDIA_MODEL_ID_STR);

    #endregion

    #region Protected fields

    // Screen data is stored in current navigation context
    protected NavigationContext _currentNavigationContext = null;

    // Choice dialog for media type for LocalMedia navigation
    protected ItemsList _mediaTypeChoiceMenuItems = null;

    #endregion

    #region Public members

    /// <summary>
    /// Provides a list of items to be shown in the choice dialog for the AV type.
    /// </summary>
    public ItemsList MediaTypeChoiceMenuItems
    {
      get { return _mediaTypeChoiceMenuItems; }
    }

    /// <summary>
    /// Gets the current media navigation mode.
    /// </summary>
    /// <remarks>
    /// The media navigation mode determines the media library part which is navigated: Music, Movies or Pictures. Another
    /// navigation mode is LocalMedia, which is completely decoupled from the media library.
    /// </remarks>
    public MediaNavigationMode Mode
    {
      get
      {
        if (_currentNavigationContext == null)
          return MediaNavigationMode.LocalMedia;
        return (_currentNavigationContext.GetContextVariable(Consts.KEY_NAVIGATION_MODE, true) as MediaNavigationMode?) ?? MediaNavigationMode.LocalMedia;
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

    /// <summary>
    /// Gets the information if there is a navigation data available.
    /// </summary>
    public static bool IsNavigationDataEnabled
    {
      get
      {
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        MediaNavigationModel model = (MediaNavigationModel) workflowManager.GetModel(MEDIA_MODEL_ID);
        NavigationData navigationData = model.NavigationData;
        return navigationData != null && navigationData.IsEnabled;
      }
    }

    /// <summary>
    /// Action which can be called from outside when there is an enabled navigation data present.
    /// </summary>
    public static void AddCurrentViewToPlaylist()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      MediaNavigationModel model = (MediaNavigationModel) workflowManager.GetModel(MEDIA_MODEL_ID);
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

    protected IEnumerable<MediaItem> FilterMediaItemsFromCurrentView(ICollection<Guid> consideredMediaItemAspectTypes)
    {
      NavigationData navigationData = NavigationData;
      if (navigationData == null)
        yield break;
      foreach (MediaItem mediaItem in navigationData.CurrentScreenData.GetAllMediaItems())
      {
        bool matches = false;
        foreach (Guid aspectType in consideredMediaItemAspectTypes)
          if (mediaItem.Aspects.ContainsKey(aspectType))
          {
            matches = true;
            break;
          }
        if (matches)
          yield return mediaItem;
      }
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
        case MediaNavigationMode.Music:
          PlayItemsModel.CheckQueryPlayAction(GetMediaItemsFromCurrentView, AVType.Audio);
          break;
        case MediaNavigationMode.Movies:
        case MediaNavigationMode.Pictures:
          PlayItemsModel.CheckQueryPlayAction(GetMediaItemsFromCurrentView, AVType.Video);
          break;
        case MediaNavigationMode.LocalMedia:
          // Albert, 2010-08-14: Is it possible to guess the AV type of the current view? We cannot derive the AV type from
          // the current view's media categories (at least not in the general case - we only know the default media categories)
          // IF it would be possible to guess the AV type, we didn't need to ask the user here.
          _mediaTypeChoiceMenuItems = new ItemsList
            {
                new ListItem(Consts.KEY_NAME, Consts.RES_ADD_ALL_AUDIO)
                  {
                      Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(() => FilterMediaItemsFromCurrentView(new Guid[] {AudioAspect.Metadata.AspectId}), AVType.Audio))
                  },
                new ListItem(Consts.KEY_NAME, Consts.RES_ADD_ALL_VIDEOS)
                  {
                      Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(() => FilterMediaItemsFromCurrentView(new Guid[] {VideoAspect.Metadata.AspectId}), AVType.Video))
                  },
                new ListItem(Consts.KEY_NAME, Consts.RES_ADD_ALL_IMAGES)
                  {
                      Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(() => FilterMediaItemsFromCurrentView(new Guid[] {PictureAspect.Metadata.AspectId}), AVType.Video))
                  },
                new ListItem(Consts.KEY_NAME, Consts.RES_ADD_VIDEOS_AND_IMAGES)
                  {
                      Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(() => FilterMediaItemsFromCurrentView(new Guid[] {VideoAspect.Metadata.AspectId, PictureAspect.Metadata.AspectId}), AVType.Video))
                  },
            };
          IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
          screenManager.ShowDialog(Consts.SCREEN_CHOOSE_AV_TYPE_DIALOG);
          break;
      }
    }

    protected void PrepareRootState()
    {
      // Initialize root media navigation state. We will set up all sub processes for each media model "part", i.e.
      // music, movies, pictures and local media.
      Guid currentStateId = _currentNavigationContext.WorkflowState.StateId;
      // The initial state ID determines the media model "part" to initialize: Local media, music, movies or pictures.
      // The media model part determines the media navigation mode and the view contents to be set.
      NavigationData navigationData;
      if (currentStateId == Consts.WF_STATE_ID_MUSIC_NAVIGATION_ROOT)
      {
        Mode = MediaNavigationMode.Music;
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new MusicItem(mi)
          {
              Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryViewSpecification(Consts.RES_MUSIC_VIEW_NAME,
            null, Consts.NECESSARY_MUSIC_MIAS, null, true)
          {
              MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
          };
        AbstractScreenData sd = new MusicFilterByAlbumScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new MusicShowItemsScreenData(picd),
              new MusicFilterByArtistScreenData(),
              sd, // C# doesn't like it to have an assignment inside a collection initializer
              new MusicFilterByGenreScreenData(),
              new MusicFilterByDecadeScreenData(),
              new MusicFilterBySystemScreenData(),
              new MusicSimpleSearchScreenData(picd),
            };
        navigationData = new NavigationData(Consts.RES_MUSIC_VIEW_NAME, currentStateId,
            currentStateId, rootViewSpecification, sd, availableScreens);
      }
      else if (currentStateId == Consts.WF_STATE_ID_MOVIES_NAVIGATION_ROOT)
      {
        Mode = MediaNavigationMode.Movies;
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new MovieItem(mi)
          {
              Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryViewSpecification(Consts.RES_MOVIES_VIEW_NAME,
            null, Consts.NECESSARY_MOVIE_MIAS, null, true)
          {
              MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
          };
        AbstractScreenData sd = new MoviesFilterByGenreScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new MoviesShowItemsScreenData(picd),
              new MoviesFilterByActorScreenData(),
              sd, // C# doesn't like it to have an assignment inside a collection initializer
              new MoviesFilterByYearScreenData(),
              new MoviesFilterBySystemScreenData(),
              new MoviesSimpleSearchScreenData(picd),
          };
        navigationData = new NavigationData(Consts.RES_MOVIES_VIEW_NAME, currentStateId,
            currentStateId, rootViewSpecification, sd, availableScreens);
      }
      else if (currentStateId == Consts.WF_STATE_ID_PICTURES_NAVIGATION_ROOT)
      {
        Mode = MediaNavigationMode.Pictures;
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new PictureItem(mi)
          {
              Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryViewSpecification(Consts.RES_PICTURES_VIEW_NAME,
            null, Consts.NECESSARY_PICTURE_MIAS, null, true)
          {
              MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
          };
        AbstractScreenData sd = new PicturesFilterByYearScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new PicturesShowItemsScreenData(picd),
              sd, // C# doesn't like it to have an assignment inside a collection initializer
              new PicturesFilterBySizeScreenData(),
              new PicturesFilterBySystemScreenData(),
              new PicturesSimpleSearchScreenData(picd),
          };
        navigationData = new NavigationData(Consts.RES_PICTURES_VIEW_NAME, currentStateId,
            currentStateId, rootViewSpecification, sd, availableScreens);
      }
      else
      { // If we were called with a supported root state, we should be in state WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT here
        if (currentStateId != Consts.WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT)
        {
          // Error case: We cannot handle the given state
          ServiceRegistration.Get<ILogger>().Warn("MediaNavigationModel: Unknown root workflow state with ID '{0}', initializing local media navigation", currentStateId);
          // We simply use the local media mode as fallback for this case, so we go on
        }
        Mode = MediaNavigationMode.LocalMedia;
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi =>
          {
            if (mi.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
              return new MusicItem(mi)
                {
                    Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
                };
            if (mi.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
              return new MovieItem(mi)
                {
                    Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
                };
            if (mi.Aspects.ContainsKey(PictureAspect.ASPECT_ID))
              return new PictureItem(mi)
                {
                    Command = new MethodDelegateCommand(() => PlayItemsModel.CheckQueryPlayAction(mi))
                };
            return null;
          };
        ViewSpecification rootViewSpecification = new LocalSharesViewSpecification(Consts.RES_LOCAL_MEDIA_ROOT_VIEW_NAME,
            new Guid[]
                {
                    ProviderResourceAspect.ASPECT_ID,
                    MediaAspect.ASPECT_ID,
                },
            new Guid[]
                {
                    AudioAspect.ASPECT_ID,
                    VideoAspect.ASPECT_ID,
                    PictureAspect.ASPECT_ID,
                });
        // Dynamic screens remain null - local media doesn't provide dynamic filters
        navigationData = new NavigationData(Consts.RES_LOCAL_MEDIA_ROOT_VIEW_NAME, currentStateId,
            currentStateId, rootViewSpecification, new LocalMediaNavigationScreenData(picd), null);
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
      NavigationData navigationData = NavigationData;
      if (navigationData == null)
        PrepareRootState();
    }

    protected void ReleaseModelData()
    {
      _currentNavigationContext = null;
      _mediaTypeChoiceMenuItems = null;
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

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
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
