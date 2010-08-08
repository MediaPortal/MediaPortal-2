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
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2.  If not, see <http://www.gnu.org/licenses/>.
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
  public class MediaModel : IWorkflowModel
  {
    #region Consts

    // Global ID definitions and references
    public const string MEDIA_MODEL_ID_STR = "4CDD601F-E280-43b9-AD0A-6D7B2403C856";

    public const string MUSIC_MODULE_ID_STR = "53130C0E-D19C-4972-92F4-DB6665E51CBC";
    public const string VIDEO_MODULE_ID_STR = "BB5362E4-3723-4a11-A989-A4B01ECCEB14";
    public const string PICTURE_MODULE_ID_STR = "DE0A2E53-1898-4e50-B27F-8652C3D11EDF";

    public const string LOCAL_MEDIA_NAVIGATION_ROOT_STATE_STR = "B393C6D8-9F37-4481-B403-0D5B17F52EC8";
    public const string MUSIC_NAVIGATION_ROOT_STATE_STR = "F2AAEBC6-BFB0-42c8-9C80-0A98BA67A7EB";
    public const string MOVIES_NAVIGATION_ROOT_STATE_STR = "22ED8702-3887-4acb-ACB4-30965220AFF0";
    public const string PICTURES_NAVIGATION_ROOT_STATE_STR = "76019AEB-3445-4da9-9A10-63A87549A7CF";

    // ID variables
    public static readonly Guid MUSIC_MODULE_ID = new Guid(MUSIC_MODULE_ID_STR);
    public static readonly Guid VIDEO_MODULE_ID = new Guid(VIDEO_MODULE_ID_STR);
    public static readonly Guid PICTURE_MODULE_ID = new Guid(PICTURE_MODULE_ID_STR);

    public static readonly Guid LOCAL_MEDIA_NAVIGATION_ROOT_STATE = new Guid(LOCAL_MEDIA_NAVIGATION_ROOT_STATE_STR);
    public static readonly Guid MUSIC_NAVIGATION_ROOT_STATE = new Guid(MUSIC_NAVIGATION_ROOT_STATE_STR);
    public static readonly Guid MOVIES_NAVIGATION_ROOT_STATE = new Guid(MOVIES_NAVIGATION_ROOT_STATE_STR);
    public static readonly Guid PICTURES_NAVIGATION_ROOT_STATE = new Guid(PICTURES_NAVIGATION_ROOT_STATE_STR);

    public static readonly Guid CURRENTLY_PLAYING_VIDEO_WORKFLOW_STATE_ID = VideoPlayerModel.CURRENTLY_PLAYING_STATE_ID;
    public static readonly Guid FULLSCREEN_VIDEO_WORKFLOW_STATE_ID = VideoPlayerModel.FULLSCREEN_CONTENT_STATE_ID;

    public static readonly Guid CURRENTLY_PLAYING_AUDIO_WORKFLOW_STATE_ID = AudioPlayerModel.CURRENTLY_PLAYING_STATE_ID;
    public static readonly Guid FULLSCREEN_AUDIO_WORKFLOW_STATE_ID = AudioPlayerModel.FULLSCREEN_CONTENT_STATE_ID;

    // Keys for workflow state variables
    public const string NAVIGATION_MODE_KEY = "MediaModel: NAVIGATION_MODE";
    internal const string NAVIGATION_DATA_KEY = "MediaModel: NAVIGATION_DATA";

    #endregion

    #region Protected fields

    // Screen data is stored in current navigation context
    protected NavigationContext _currentNavigationContext = null;

    // Play menu
    protected ItemsList _playMenuItems = null;

    #endregion

    /// <summary>
    /// Provides a list of items to be shown in the play menu.
    /// </summary>
    public ItemsList PlayMenuItems
    {
      get { return _playMenuItems; }
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
        return (_currentNavigationContext.GetContextVariable(NAVIGATION_MODE_KEY, false) as MediaNavigationMode?) ?? MediaNavigationMode.LocalMedia;
      }
      internal set
      {
        if (_currentNavigationContext == null)
          return;
        _currentNavigationContext.SetContextVariable(NAVIGATION_MODE_KEY, value);
      }
    }

    /// <summary>
    /// Gets the navigation data which is set in the current workflow navigation context.
    /// </summary>
    public NavigationData NavigationData
    {
      get { return GetNavigationData(_currentNavigationContext); }
      set  { SetNavigationData(value, _currentNavigationContext); }
    }

    protected NavigationData GetNavigationData(NavigationContext navigationContext)
    {
      return navigationContext.GetContextVariable(NAVIGATION_DATA_KEY, false) as NavigationData;
    }

    protected void SetNavigationData(NavigationData navigationData, NavigationContext navigationContext)
    {
      navigationContext.SetContextVariable(NAVIGATION_DATA_KEY, navigationData);
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item of the media contents view.
    /// Depending on the item type, we will navigate to the choosen view, play the choosen item or filter by the item.
    /// </summary>
    /// <param name="item">The choosen item. This item should be one of the items in the
    /// <see cref="AbstractScreenData.Items"/> list from the <see cref="Models.NavigationData.CurrentScreenData"/> screen
    /// data.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      if (item.Command != null)
        item.Command.Execute();
    }

    #region Protected members

    /// <summary>
    /// Checks if we need to show a menu for playing the specified <paramref name="item"/> and shows that
    /// menu or plays the item, if no menu must be shown.
    /// </summary>
    /// <param name="item">The item which was selected to play.</param>
    protected void CheckPlayMenu(MediaItem item)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      int numOpen = pcm.NumActivePlayerContexts;
      if (numOpen == 0)
      {
        PlayItem(item);
        return;
      }
      _playMenuItems = new ItemsList();
      PlayerContextType mediaType = pcm.GetTypeOfMediaItem(item);
      int numAudio = pcm.NumPlayerContextsOfMediaType(PlayerContextType.Audio);
      int numVideo = pcm.NumPlayerContextsOfMediaType(PlayerContextType.Video);
      if (mediaType == PlayerContextType.Audio)
      {
        ListItem playItem = new ListItem(Consts.NAME_KEY, Consts.PLAY_AUDIO_ITEM_RESOURCE)
          {
              Command = new MethodDelegateCommand(() => PlayItem(item))
          };
        _playMenuItems.Add(playItem);
        if (numAudio > 0)
        {
          ListItem enqueueItem = new ListItem(Consts.NAME_KEY, Consts.ENQUEUE_AUDIO_ITEM_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, false, false, false))
            };
          _playMenuItems.Add(enqueueItem);
        }
        if (numVideo > 0)
        {
          ListItem playItemConcurrently = new ListItem(Consts.NAME_KEY, Consts.MUTE_VIDEO_PLAY_AUDIO_ITEM_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, true, false))
            };
          _playMenuItems.Add(playItemConcurrently);
        }
      }
      else if (mediaType == PlayerContextType.Video)
      {
        ListItem playItem = new ListItem(Consts.NAME_KEY, Consts.PLAY_VIDEO_ITEM_RESOURCE)
          {
              Command = new MethodDelegateCommand(() => PlayItem(item))
          };
        _playMenuItems.Add(playItem);
        if (numVideo > 0)
        {
          ListItem enqueueItem = new ListItem(Consts.NAME_KEY, Consts.ENQUEUE_VIDEO_ITEM_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, false, false, false))
            };
          _playMenuItems.Add(enqueueItem);
        }
        if (numAudio > 0)
        {
          ListItem playItem_A = new ListItem(Consts.NAME_KEY, Consts.PLAY_VIDEO_ITEM_MUTED_CONCURRENT_AUDIO_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, true, false))
            };
          _playMenuItems.Add(playItem_A);
        }
        if (numVideo > 0)
        {
          ListItem playItem_V = new ListItem(Consts.NAME_KEY, Consts.PLAY_VIDEO_ITEM_PIP_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, true, true))
            };
          _playMenuItems.Add(playItem_V);
        }
      }
      else
      {
        IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
        dialogManager.ShowDialog(Consts.SYSTEM_INFORMATION_RESOURCE, Consts.CANNOT_PLAY_ITEM_RESOURCE, DialogType.OkDialog, false,
            DialogButtonType.Ok);
      }
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.PLAY_MENU_DIALOG_SCREEN);
    }

    /// <summary>
    /// Provides a callable method for the play menu dialog to select one of the items representing
    /// a user choice of a player slot.
    /// </summary>
    /// <param name="item">The item which has an attached <see cref="ListItem.Command"/>.</param>
    public void ExecuteMenuItem(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }

    /// <summary>
    /// Discards any currently playing items and plays the specified media <paramref name="item"/>.
    /// </summary>
    /// <param name="item">Media item to be played.</param>
    protected static void PlayItem(MediaItem item)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
      PlayOrEnqueueItem(item, true, false, false);
    }

    protected static IPlayerContext GetPlayerContextByMediaType(PlayerContextType mediaType, bool concurrent)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext result;
      if (mediaType == PlayerContextType.Video && concurrent || mediaType == PlayerContextType.Audio)
        // Concurrent video & audio - search in reverse order
        for (int i = 1; i >= 0; i--)
        {
          if ((result = pcm.GetPlayerContext(i)) != null && result.MediaType == mediaType)
            return result;
        }
      else // Non-concurrent video - search in normal order
        for (int i = 0; i < 2; i++)
        {
          if ((result = pcm.GetPlayerContext(i)) != null && result.MediaType == PlayerContextType.Video)
            return result;
        }
      return null;
    }

    public static bool GetModuleIdAndNameForMediaItem(MediaItem item, out Guid moduleId, out string contextName)
    {
      // No locking necessary
      if (item.Aspects.ContainsKey(VideoAspect.Metadata.AspectId))
      {
        moduleId = VIDEO_MODULE_ID;
        contextName = Consts.VIDEO_PLAYER_CONTEXT_NAME_RESOURCE;
        return true;
      }
      if (item.Aspects.ContainsKey(PictureAspect.Metadata.AspectId))
      {
        moduleId = PICTURE_MODULE_ID;
        contextName = Consts.PICTURE_PLAYER_CONTEXT_NAME_RESOURCE;
        return true;
      }
      if (item.Aspects.ContainsKey(AudioAspect.Metadata.AspectId))
      {
        moduleId = MUSIC_MODULE_ID;
        contextName = Consts.AUDIO_PLAYER_CONTEXT_NAME_RESOURCE;
        return true;
      }
      moduleId = new Guid();
      contextName = null;
      return false;
    }

    /// <summary>
    /// Depending on parameter <paramref name="play"/>, plays or enqueues the specified media <paramref name="item"/>.
    /// </summary>
    /// <param name="item">Media item to be played.</param>
    /// <param name="play">If <c>true</c>, plays the specified <paramref name="item"/>, else enqueues it.</param>
    /// <param name="concurrent">If set to <c>true</c>, the <paramref name="item"/> will be played concurrently to
    /// an already playing player. Else, all other players will be stopped first.</param>
    /// <param name="subordinatedVideo">If set to <c>true</c>, a video item will be played in PiP mode, if
    /// applicable.</param>
    protected static void PlayOrEnqueueItem(MediaItem item, bool play, bool concurrent, bool subordinatedVideo)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      PlayerContextType mediaType = pcm.GetTypeOfMediaItem(item);
      Guid moduleId;
      string contextName;
      if (!GetModuleIdAndNameForMediaItem(item, out moduleId, out contextName))
        return;
      IPlayerContext pc = null;
      if (!play)
        // !play means enqueue - so find our first player context of the correct media type
        foreach (IPlayerContext mmPC in pcm.GetPlayerContextsByMediaModuleId(moduleId))
          if (mmPC.MediaType == mediaType)
          {
            pc = mmPC;
            break;
          }
      if (pc == null)
        // No player context to reuse - so open a new one
        if (mediaType == PlayerContextType.Video)
          pc = pcm.OpenVideoPlayerContext(moduleId, contextName, concurrent, subordinatedVideo,
              CURRENTLY_PLAYING_VIDEO_WORKFLOW_STATE_ID, FULLSCREEN_VIDEO_WORKFLOW_STATE_ID);
        else if (mediaType == PlayerContextType.Audio)
          pc = pcm.OpenAudioPlayerContext(moduleId, contextName, concurrent,
              CURRENTLY_PLAYING_AUDIO_WORKFLOW_STATE_ID, FULLSCREEN_AUDIO_WORKFLOW_STATE_ID);
      if (pc == null)
        return;
      if (play)
        pc.Playlist.Clear();

      // Always add items to playlist. This allows audio playlists as well as video playlists.
      pc.Playlist.Add(item);
      // TODO: Save playlist in this model instance so that they are still able to be accessed later,
      // after the player has closed
      pc.CloseWhenFinished = true; // Has to be done before starting the media item, else the slot will not close in case of an error / when the media item cannot be played
      pc.Play();
      if (mediaType == PlayerContextType.Video)
        pcm.ShowFullscreenContent();
    }

    protected void PrepareRootState()
    {
      // Initialize root media navigation state. We will set up all sub processes for each media model "part", i.e.
      // music, movies, pictures and local media.
      Guid currentStateId = _currentNavigationContext.WorkflowState.StateId;
      // The initial state ID determines the media model "part" to initialize: Local media, music, movies or pictures.
      // The media model part determines the media navigation mode and the view contents to be set.
      NavigationData navigationData;
      if (currentStateId == MUSIC_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.Music;
        ItemsScreenData.PlayableItemCreatorDelegate picd = mi => new MusicItem(mi)
          {
              Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
          };
        ViewSpecification rootViewSpecification = StackedFiltersMLVS.CreateRootViewSpecification(Consts.MUSIC_VIEW_NAME_RESOURCE,
            Consts.NECESSARY_MUSIC_MIAS, null, null, true);
        AbstractScreenData sd = new MusicFilterByAlbumScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new MusicShowItemsScreenData(picd),
              new MusicFilterByArttistScreenData(),
              sd, // C# doesn't like it to have an assignment inside a collection initializer
              new MusicFilterByGenreScreenData(),
              new MusicFilterByDecadeScreenData(),
              new MusicSimpleSearchScreenData(picd),
            };
        navigationData = new NavigationData(Consts.MUSIC_VIEW_NAME_RESOURCE, currentStateId,
            rootViewSpecification, sd, availableScreens);
      }
      else if (currentStateId == MOVIES_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.Movies;
        ItemsScreenData.PlayableItemCreatorDelegate picd = mi => new MovieItem(mi)
          {
              Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
          };
        ViewSpecification rootViewSpecification = StackedFiltersMLVS.CreateRootViewSpecification(Consts.MOVIES_VIEW_NAME_RESOURCE,
            Consts.NECESSARY_MOVIE_MIAS, null, null, true);
        AbstractScreenData sd = new MoviesFilterByGenreScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new MoviesShowItemsScreenData(picd),
              new MoviesFilterByActorScreenData(),
              sd, // C# doesn't like it to have an assignment inside a collection initializer
              new MoviesFilterByYearScreenData(),
              new MoviesSimpleSearchScreenData(picd),
          };
        navigationData = new NavigationData(Consts.MOVIES_VIEW_NAME_RESOURCE, currentStateId,
            rootViewSpecification, sd, availableScreens);
      }
      else if (currentStateId == PICTURES_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.Pictures;
        ItemsScreenData.PlayableItemCreatorDelegate picd = mi => new PictureItem(mi)
          {
              Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
          };
        ViewSpecification rootViewSpecification = StackedFiltersMLVS.CreateRootViewSpecification(Consts.PICTURES_VIEW_NAME_RESOURCE,
            Consts.NECESSARY_PICTURE_MIAS, null, null, true);
        AbstractScreenData sd = new PicturesFilterByYearScreenData();
        ICollection<AbstractScreenData> availableScreens = new List<AbstractScreenData>
            {
              new PicturesShowItemsScreenData(picd),
              sd, // C# doesn't like it to have an assignment inside a collection initializer
              new PicturesFilterBySizeScreenData(),
              new PicturesSimpleSearchScreenData(picd),
          };
        navigationData = new NavigationData(Consts.PICTURES_VIEW_NAME_RESOURCE, currentStateId,
            rootViewSpecification, sd, availableScreens);
      }
      else
      { // If we were called with a supported root state, we should be in state LOCAL_MEDIA_NAVIGATION_ROOT_STATE here
        if (currentStateId != LOCAL_MEDIA_NAVIGATION_ROOT_STATE)
        {
          // Error case: We cannot handle the given state
          ServiceRegistration.Get<ILogger>().Warn("MediaModel: Unknown root workflow state with ID '{0}', initializing local media navigation", currentStateId);
          // We simply use the local media mode as fallback for this case, so we go on
        }
        Mode = MediaNavigationMode.LocalMedia;
        ItemsScreenData.PlayableItemCreatorDelegate picd = mi =>
          {
            if (mi.Aspects.ContainsKey(AudioAspect.ASPECT_ID))
              return new MusicItem(mi)
                {
                    Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
                };
            if (mi.Aspects.ContainsKey(VideoAspect.ASPECT_ID))
              return new MovieItem(mi)
                {
                    Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
                };
            if (mi.Aspects.ContainsKey(PictureAspect.ASPECT_ID))
              return new PictureItem(mi)
                {
                    Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
                };
            return null;
          };
        ViewSpecification rootViewSpecification = new LocalSharesViewSpecification(Consts.LOCAL_MEDIA_ROOT_VIEW_NAME_RESOURCE,
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
        navigationData = new NavigationData(Consts.LOCAL_MEDIA_ROOT_VIEW_NAME_RESOURCE, currentStateId,
            rootViewSpecification, new LocalMediaNavigationScreenData(picd), null);
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
      _playMenuItems = null;
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MEDIA_MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext);
      NavigationData navigationData = GetNavigationData(newContext);
      navigationData.Enable();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      NavigationData navigationData = GetNavigationData(oldContext);
      navigationData.Dispose();
      ReleaseModelData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext);
      if (push)
      {
        NavigationData navigationData = GetNavigationData(oldContext);
        navigationData.Disable();
        navigationData = GetNavigationData(newContext);
        navigationData.Enable();
      }
      else
      {
        NavigationData navigationData = GetNavigationData(oldContext);
        navigationData.Dispose();
        navigationData = GetNavigationData(newContext);
        navigationData.Enable();
      }
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      NavigationData navigationData = GetNavigationData(oldContext);
      navigationData.Disable();
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      _currentNavigationContext = newContext;
      NavigationData navigationData = GetNavigationData(newContext);
      navigationData.Enable();
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
