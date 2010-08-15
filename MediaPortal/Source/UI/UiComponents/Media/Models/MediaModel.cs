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
using System.Linq;
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

    // ID variables
    public static readonly Guid MEDIA_MODEL_ID = new Guid(MEDIA_MODEL_ID_STR);

    #endregion

    #region Protected fields

    // Screen data is stored in current navigation context
    protected NavigationContext _currentNavigationContext = null;

    // Play menu
    protected ItemsList _playMenuItems = null;
    // Choice dialog for media type for LocalMedia navigation
    protected ItemsList _mediaTypeChoiceMenuItems = null;

    #endregion

    /// <summary>
    /// Provides a list of items to be shown in the play menu.
    /// </summary>
    public ItemsList PlayMenuItems
    {
      get { return _playMenuItems; }
    }

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
        return (_currentNavigationContext.GetContextVariable(Consts.NAVIGATION_MODE_KEY, true) as MediaNavigationMode?) ?? MediaNavigationMode.LocalMedia;
      }
      internal set
      {
        if (_currentNavigationContext == null)
          return;
        _currentNavigationContext.SetContextVariable(Consts.NAVIGATION_MODE_KEY, value);
      }
    }

    /// <summary>
    /// Gets the navigation data which is set in the current workflow navigation context.
    /// </summary>
    public NavigationData NavigationData
    {
      get { return GetNavigationData(_currentNavigationContext); }
    }

    protected internal static NavigationData GetNavigationData(NavigationContext navigationContext)
    {
      return navigationContext.GetContextVariable(Consts.NAVIGATION_DATA_KEY, false) as NavigationData;
    }

    protected static void SetNavigationData(NavigationData navigationData, NavigationContext navigationContext)
    {
      navigationContext.SetContextVariable(Consts.NAVIGATION_DATA_KEY, navigationData);
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

    protected delegate IEnumerable<MediaItem> GetMediaItemsDlgt();

    protected internal void AddCurrentViewToPlaylist()
    {
      MediaNavigationMode mode = Mode;
      switch (mode)
      {
        case MediaNavigationMode.Music:
          CheckPlayMenu(AVType.Audio, GetMediaItemsFromCurrentView);
          break;
        case MediaNavigationMode.Movies:
        case MediaNavigationMode.Pictures:
          CheckPlayMenu(AVType.Video, GetMediaItemsFromCurrentView);
          break;
        case MediaNavigationMode.LocalMedia:
          // Albert, 2010-08-14: Is it possible to guess the AV type of the current view? We cannot derive the AV type from
          // the current view's media categories (at least not in the general case - we only know the default media categories)
          // IF it would be possible to guess the AV type, we didn't need to ask the user here.
          _mediaTypeChoiceMenuItems = new ItemsList
            {
                new ListItem(Consts.NAME_KEY, Consts.ADD_ALL_AUDIO_RES)
                  {
                      Command = new MethodDelegateCommand(() => CheckPlayMenu(AVType.Audio,
                          () => FilterMediaItemsFromCurrentView(new Guid[] {AudioAspect.Metadata.AspectId})))
                  },
                new ListItem(Consts.NAME_KEY, Consts.ADD_ALL_VIDEOS_RES)
                  {
                      Command = new MethodDelegateCommand(() => CheckPlayMenu(AVType.Video,
                          () => FilterMediaItemsFromCurrentView(new Guid[] {VideoAspect.Metadata.AspectId})))
                  },
                new ListItem(Consts.NAME_KEY, Consts.ADD_ALL_IMAGES_RES)
                  {
                      Command = new MethodDelegateCommand(() => CheckPlayMenu(AVType.Video,
                          () => FilterMediaItemsFromCurrentView(new Guid[] {PictureAspect.Metadata.AspectId})))
                  },
                new ListItem(Consts.NAME_KEY, Consts.ADD_VIDEOS_AND_IMAGES_RES)
                  {
                      Command = new MethodDelegateCommand(() => CheckPlayMenu(AVType.Video,
                          () => FilterMediaItemsFromCurrentView(new Guid[] {VideoAspect.Metadata.AspectId, PictureAspect.Metadata.AspectId})))
                  },
            };
          IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
          screenManager.ShowDialog(Consts.CHOOSE_AV_TYPE_DIALOG_SCREEN);
          break;
      }
    }

    protected IEnumerable<MediaItem> FilterMediaItemsFromCurrentView(ICollection<Guid> necessaryMediaItemAspectTypes)
    {
      NavigationData navigationData = NavigationData;
      if (navigationData == null)
        yield break;
      foreach (MediaItem mediaItem in navigationData.CurrentScreenData.GetAllMediaItems())
      {
        bool matches = true;
        foreach (Guid aspectType in necessaryMediaItemAspectTypes)
          if (!mediaItem.Aspects.ContainsKey(aspectType))
          {
            matches = false;
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

    /// <summary>
    /// Checks if we need to show a menu for playing all items of the current view and shows that
    /// menu or adds all current items to the playlist at once, starting playing, if no menu must be shown.
    /// </summary>
    /// <param name="avType">Media type of the given view to play.</param>
    /// <param name="getMediaItemsFunction">Function to be used to get the media items to be added to the playlist.</param>
    protected void CheckPlayMenu(AVType avType, GetMediaItemsDlgt getMediaItemsFunction)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      int numOpen = pcm.NumActivePlayerContexts;
      if (numOpen == 0)
      {
        PlayItems(getMediaItemsFunction, avType);
        return;
      }
      _playMenuItems = new ItemsList();
      int numAudio = pcm.NumPlayerContextsOfType(AVType.Audio);
      int numVideo = pcm.NumPlayerContextsOfType(AVType.Video);
      if (avType == AVType.Audio)
      {
        ListItem playItem = new ListItem(Consts.NAME_KEY, Consts.PLAY_AUDIO_ITEMS_RESOURCE)
          {
              Command = new MethodDelegateCommand(() => PlayItems(getMediaItemsFunction, avType))
          };
        _playMenuItems.Add(playItem);
        if (numAudio > 0)
        {
          ListItem enqueueItem = new ListItem(Consts.NAME_KEY, Consts.ENQUEUE_AUDIO_ITEMS_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItems(getMediaItemsFunction, avType, false, false, false))
            };
          _playMenuItems.Add(enqueueItem);
        }
        if (numVideo > 0)
        {
          ListItem playItemConcurrently = new ListItem(Consts.NAME_KEY, Consts.MUTE_VIDEO_PLAY_AUDIO_ITEMS_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItems(getMediaItemsFunction, avType, true, true, false))
            };
          _playMenuItems.Add(playItemConcurrently);
        }
      }
      else if (avType == AVType.Video)
      {
        ListItem playItem = new ListItem(Consts.NAME_KEY, Consts.PLAY_VIDEO_ITEMS_RESOURCE)
          {
              Command = new MethodDelegateCommand(() => PlayItems(getMediaItemsFunction, avType))
          };
        _playMenuItems.Add(playItem);
        if (numVideo > 0)
        {
          ListItem enqueueItem = new ListItem(Consts.NAME_KEY, Consts.ENQUEUE_VIDEO_ITEMS_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItems(getMediaItemsFunction, avType, false, false, false))
            };
          _playMenuItems.Add(enqueueItem);
        }
        if (numAudio > 0)
        {
          ListItem playItem_A = new ListItem(Consts.NAME_KEY, Consts.PLAY_VIDEO_ITEMS_MUTED_CONCURRENT_AUDIO_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItems(getMediaItemsFunction, avType, true, true, false))
            };
          _playMenuItems.Add(playItem_A);
        }
        if (numVideo > 0)
        {
          ListItem playItem_V = new ListItem(Consts.NAME_KEY, Consts.PLAY_VIDEO_ITEMS_PIP_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItems(getMediaItemsFunction, avType, true, true, true))
            };
          _playMenuItems.Add(playItem_V);
        }
      }
      else
      {
        IDialogManager dialogManager = ServiceRegistration.Get<IDialogManager>();
        dialogManager.ShowDialog(Consts.SYSTEM_INFORMATION_RESOURCE, Consts.CANNOT_PLAY_ITEMS_DIALOG_TEXT_RES, DialogType.OkDialog, false,
            DialogButtonType.Ok);
      }
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(Consts.PLAY_MENU_DIALOG_SCREEN);
    }

    /// <summary>
    /// Discards any currently playing items and plays the media items of type <paramref name="avType"/> returned by the given
    /// <paramref name="getMediaItemsFunction"/>
    /// </summary>
    /// <param name="getMediaItemsFunction">Function returning the media items to be played.</param>
    /// <param name="avType">AV type of media items returned.</param>
    protected static void PlayItems(GetMediaItemsDlgt getMediaItemsFunction, AVType avType)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
      PlayOrEnqueueItems(getMediaItemsFunction, avType, true, false, false);
    }

    protected static IPlayerContext PreparePlayerContext(AVType avType, bool play, bool concurrent, bool subordinatedVideo)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      string contextName;
      if (!GetPlayerContextNameForMediaType(avType, out contextName))
        return null;
      IPlayerContext pc = null;
      if (!play)
      {
        // !play means enqueue - so find our first player context of the correct media type
        IList<IPlayerContext> playerContexts = new List<IPlayerContext>(
            pcm.GetPlayerContextsByMediaModuleId(Consts.MEDIA_MODULE_ID).Where(playerContext => playerContext.AVType == avType));
        // In case the media type is audio, we have max. one player context of that type. In case media type is
        // video, we might have two. Which one is the correct one depends on parameter subordinatedVideo.
        pc = subordinatedVideo ? playerContexts.LastOrDefault() : playerContexts.FirstOrDefault();
      }
      if (pc == null)
        // No player context to reuse - so open a new one
        if (avType == AVType.Video)
          pc = pcm.OpenVideoPlayerContext(Consts.MEDIA_MODULE_ID, contextName, concurrent, subordinatedVideo,
              Consts.CURRENTLY_PLAYING_VIDEO_WORKFLOW_STATE_ID, Consts.FULLSCREEN_VIDEO_WORKFLOW_STATE_ID);
        else if (avType == AVType.Audio)
          pc = pcm.OpenAudioPlayerContext(Consts.MEDIA_MODULE_ID, contextName, concurrent,
              Consts.CURRENTLY_PLAYING_AUDIO_WORKFLOW_STATE_ID, Consts.FULLSCREEN_AUDIO_WORKFLOW_STATE_ID);
      if (pc == null)
        return null;
      if (play)
        pc.Playlist.Clear();
      return pc;
    }

    /// <summary>
    /// Depending on parameter <paramref name="play"/>, plays or enqueues the media items of type <paramref name="avType"/>
    /// returned by the given <paramref name="getMediaItemsFunction"/>.
    /// </summary>
    /// <param name="getMediaItemsFunction">Function returning the media items to be played.</param>
    /// <param name="avType">AV type of media items returned.</param>
    /// <param name="play">If <c>true</c>, plays the specified items, else enqueues it.</param>
    /// <param name="concurrent">If set to <c>true</c>, the items will be played concurrently to
    /// an already playing player. Else, all other players will be stopped first.</param>
    /// <param name="subordinatedVideo">If set to <c>true</c>, a video item will be played in PiP mode, if
    /// applicable.</param>
    protected static void PlayOrEnqueueItems(GetMediaItemsDlgt getMediaItemsFunction, AVType avType,
        bool play, bool concurrent, bool subordinatedVideo)
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext pc = PreparePlayerContext(avType, play, concurrent, subordinatedVideo);
      if (pc == null)
        return;

      // Always add items to playlist. This allows audio playlists as well as video playlists.
      pc.Playlist.AddAll(getMediaItemsFunction());
      // TODO: Save playlist in this model instance so that they are still able to be accessed later,
      // after the player has closed
      pc.CloseWhenFinished = true; // Has to be done before starting the media item, else the slot will not close in case of an error / when the media item cannot be played
      pc.Play();
      if (avType == AVType.Video)
        pcm.ShowFullscreenContent();
    }

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
      AVType avType = pcm.GetTypeOfMediaItem(item);
      int numAudio = pcm.NumPlayerContextsOfType(AVType.Audio);
      int numVideo = pcm.NumPlayerContextsOfType(AVType.Video);
      if (avType == AVType.Audio)
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
      else if (avType == AVType.Video)
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
        dialogManager.ShowDialog(Consts.SYSTEM_INFORMATION_RESOURCE, Consts.CANNOT_PLAY_ITEM_DIALOG_TEXT_RES, DialogType.OkDialog, false,
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

    public static bool GetPlayerContextNameForMediaType(AVType avType, out string contextName)
    {
      // No locking necessary
      if (avType == AVType.Video)
      {
        contextName = Consts.VIDEO_PICTURE_CONTEXT_NAME_RESOURCE;
        return true;
      }
      if (avType == AVType.Audio)
      {
        contextName = Consts.AUDIO_CONTEXT_NAME_RESOURCE;
        return true;
      }
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
      AVType avType = pcm.GetTypeOfMediaItem(item);
      IPlayerContext pc = PreparePlayerContext(avType, play, concurrent, subordinatedVideo);
      if (pc == null)
        return;

      // Always add items to playlist. This allows audio playlists as well as video playlists.
      pc.Playlist.Add(item);
      // TODO: Save playlist in this model instance so that they are still able to be accessed later,
      // after the player has closed
      pc.CloseWhenFinished = true; // Has to be done before starting the media item, else the slot will not close in case of an error / when the media item cannot be played
      pc.Play();
      if (avType == AVType.Video)
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
      if (currentStateId == Consts.MUSIC_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.Music;
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new MusicItem(mi)
          {
              Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryViewSpecification(Consts.MUSIC_VIEW_NAME_RESOURCE,
            null, Consts.NECESSARY_MUSIC_MIAS, null, true)
          {
              MaxNumItems = Consts.MAX_NUM_ITEMS_VISIBLE
          };
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
            currentStateId, rootViewSpecification, sd, availableScreens);
      }
      else if (currentStateId == Consts.MOVIES_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.Movies;
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new MovieItem(mi)
          {
              Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryViewSpecification(Consts.MOVIES_VIEW_NAME_RESOURCE,
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
              new MoviesSimpleSearchScreenData(picd),
          };
        navigationData = new NavigationData(Consts.MOVIES_VIEW_NAME_RESOURCE, currentStateId,
            currentStateId, rootViewSpecification, sd, availableScreens);
      }
      else if (currentStateId == Consts.PICTURES_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.Pictures;
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi => new PictureItem(mi)
          {
              Command = new MethodDelegateCommand(() => CheckPlayMenu(mi))
          };
        ViewSpecification rootViewSpecification = new MediaLibraryViewSpecification(Consts.PICTURES_VIEW_NAME_RESOURCE,
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
              new PicturesSimpleSearchScreenData(picd),
          };
        navigationData = new NavigationData(Consts.PICTURES_VIEW_NAME_RESOURCE, currentStateId,
            currentStateId, rootViewSpecification, sd, availableScreens);
      }
      else
      { // If we were called with a supported root state, we should be in state LOCAL_MEDIA_NAVIGATION_ROOT_STATE here
        if (currentStateId != Consts.LOCAL_MEDIA_NAVIGATION_ROOT_STATE)
        {
          // Error case: We cannot handle the given state
          ServiceRegistration.Get<ILogger>().Warn("MediaModel: Unknown root workflow state with ID '{0}', initializing local media navigation", currentStateId);
          // We simply use the local media mode as fallback for this case, so we go on
        }
        Mode = MediaNavigationMode.LocalMedia;
        AbstractItemsScreenData.PlayableItemCreatorDelegate picd = mi =>
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
      _playMenuItems = null;
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
