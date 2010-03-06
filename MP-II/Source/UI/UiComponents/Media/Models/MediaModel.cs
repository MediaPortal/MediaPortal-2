#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.MediaManagement.MLQueries;
using MediaPortal.UI.Views;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;
using UiComponents.Media.FilterCriteria;
using UiComponents.Media.Navigation;

namespace UiComponents.Media.Models
{
  /// <summary>
  /// Model which holds the GUI state for the current navigation in the media views.
  /// </summary>
  /// <remarks>
  /// The media model attends all media navigation workflow states. It can handle the local media navigation as well as
  /// media library based music, movies and pictures navigation. For each of those modes, there exists an own workflow
  /// state which represents the according root view. So when navigating to the root view state for the music navigation,
  /// this model will show the music navigation screen with a view representing the complete music contents of the media
  /// library, for example.
  /// </remarks>
  public class MediaModel : IWorkflowModel
  {
    protected delegate PlayableItem PlayableItemCreatorDelegate(MediaItem mi);

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
    protected const string NAVIGATION_MODE_KEY = "MediaModel: NAVIGATION_MODE";
    protected const string SUB_VIEWS_NAVIGATION_MODE_KEY = "MediaModel: SUB_VIEWS_NAVIGATION_MODE";
    protected const string PLAYABLE_ITEM_CREATOR_KEY = "MediaModel: PLAYABLE_ITEM_CREATOR";
    protected const string NECESSARY_MIA_TYPES_KEY = "MediaModel: NECESSARY_MIA_TYPES";
    protected const string OPTIONAL_MIA_TYPES_KEY = "MediaModel: OPTIONAL_MIA_TYPES";
    protected const string VIEW_KEY = "MediaModel: VIEW";
    protected const string ITEMS_KEY = "MediaModel: ITEMS";
    protected const string NUM_ITEMS_STR_KEY = "MediaModel: NUM_ITEMS_STR";
    protected const string ITEMSLIST_TITLE_KEY = "MediaModel: ITEMSLIST_TITLE";
    protected const string SIMPLE_SEARCH_TEXT_PROPERTY_KEY = "MediaModel: SIMPLE_SEARCH_TEXT_PROPERTY";
    protected const string HAS_PARENT_DIRECTORY_KEY = "MediaModel: HAS_PARENT_DIRECTORY";
    protected const string IS_ITEMS_VALID_KEY = "MediaModel: IS_VIEW_VALID";
    protected const string IS_ITEMS_EMPTY_KEY = "MediaModel: IS_VIEW_EMPTY";
    protected const string DYNAMIC_MODES_KEY = "MediaModel: DYNAMIC_MODES";
    protected const string SCREEN_KEY = "MediaModel: SCREEN";
    protected const string DYNAMIC_WORKFLOW_ACTIONS_KEY = "MediaModel: DYNAMIC_WORKFLOW_ACTIONS";

    // Keys for the ListItem's Labels in the ItemsLists
    public const string NAME_KEY = "Name";
    public const string MEDIA_ITEM_KEY = "MediaItem";
    public const string NUM_ITEMS_KEY = "NumItems";

    // Localization resource identifiers
    public const string PLAY_AUDIO_ITEM_RESOURCE = "[Media.PlayAudioItem]";
    public const string ENQUEUE_AUDIO_ITEM_RESOURCE = "[Media.EnqueueAudioItem]";
    public const string MUTE_VIDEO_PLAY_AUDIO_ITEM_RESOURCE = "[Media.MuteVideoAndPlayAudioItem]";

    public const string PLAY_VIDEO_ITEM_RESOURCE = "[Media.PlayVideoItem]";
    public const string ENQUEUE_VIDEO_ITEM_RESOURCE = "[Media.EnqueueVideoItem]";
    public const string PLAY_VIDEO_ITEM_MUTED_CONCURRENT_AUDIO_RESOURCE = "[Media.PlayVideoItemMutedConcurrentAudio]";
    public const string PLAY_VIDEO_ITEM_PIP_RESOURCE = "[Media.PlayVideoItemPIP]";

    public const string VIDEO_PLAYER_CONTEXT_NAME_RESOURCE = "[Media.VideoPlayerContextName]";
    public const string PICTURE_PLAYER_CONTEXT_NAME_RESOURCE = "[Media.PicturePlayerContextName]";
    public const string AUDIO_PLAYER_CONTEXT_NAME_RESOURCE = "[Media.AudioPlayerContextName]";

    public const string SYSTEM_INFORMATION_RESOURCE = "[System.Information]";
    public const string CANNOT_PLAY_ITEM_RESOURCE = "[Media.CannotPlayItemDialogText]";

    public const string NO_ITEMS_RESOURCE = "[Media.NoItems]";
    public const string ONE_ITEM_RESOURCE = "[Media.OneItem]";
    public const string N_ITEMS_RESOURCE = "[Media.NItems]";

    public const string LOCAL_MEDIA_ROOT_VIEW_NAME_RESOURCE = "[Media.LocalMediaRootViewName]";
    public const string MUSIC_VIEW_NAME_RESOURCE = "[Media.MusicRootViewName]";
    public const string MOVIES_VIEW_NAME_RESOURCE = "[Media.MoviesRootViewName]";
    public const string PICTURES_VIEW_NAME_RESOURCE = "[Media.PicturesRootViewName]";

    public const string MEDIA_ITEMS_VIEW_TITLE_RESOURCE = "[Media.MediaItemsViewTitle]";
    public const string SIMPLE_SEARCH_TITLE_RESOURCE = "[Media.SimpleSearchTitle]";
    public const string FILTER_BY_ARTIST_MODE_RESOURCE = "[Media.FilterByArtistMode]";
    public const string FILTER_BY_ALBUM_MODE_RESOURCE = "[Media.FilterByAlbumMode]";
    public const string FILTER_BY_MUSIC_GENRE_MODE_RESOURCE = "[Media.FilterByMusicGenreMode]";
    public const string FILTER_BY_DECADE_MODE_RESOURCE = "[Media.FilterByDecadeMode]";
    public const string FILTER_BY_YEAR_MODE_RESOURCE = "[Media.FilterByYearMode]";
    public const string FILTER_BY_ACTOR_MODE_RESOURCE = "[Media.FilterByActorMode]";
    public const string FILTER_BY_MOVIE_GENRE_MODE_RESOURCE = "[Media.FilterByMovieGenreMode]";
    public const string FILTER_BY_PICTURE_SIZE_MODE_RESOURCE = "[Media.FilterByPictureSizeMode]";
    public const string SIMPLE_SEARCH_FILTER_MODE_RESOURCE = "[Media.SimpleSearchFilterMode]";
    public const string SHOW_ALL_MUSIC_ITEMS_MODE_RESOURCE = "[Media.ShowAllMusicItemsMode]";
    public const string SHOW_ALL_MOVIE_ITEMS_MODE_RESOURCE = "[Media.ShowAllMovieItemsMode]";
    public const string SHOW_ALL_PICTURE_ITEMS_MODE_RESOURCE = "[Media.ShowAllPictureItemsMode]";

    public static readonly IDictionary<MediaNavigationMode, string> DYNAMIC_ACTION_TITLES =
        new Dictionary<MediaNavigationMode, string>
      {
        {MediaNavigationMode.LocalMedia, null},
        {MediaNavigationMode.MusicShowItems, SHOW_ALL_MUSIC_ITEMS_MODE_RESOURCE},
        {MediaNavigationMode.MusicFilterByArtist, FILTER_BY_ARTIST_MODE_RESOURCE},
        {MediaNavigationMode.MusicFilterByAlbum, FILTER_BY_ALBUM_MODE_RESOURCE},
        {MediaNavigationMode.MusicFilterByGenre, FILTER_BY_MUSIC_GENRE_MODE_RESOURCE},
        {MediaNavigationMode.MusicFilterByDecade, FILTER_BY_DECADE_MODE_RESOURCE},
        {MediaNavigationMode.MusicSimpleSearch, SIMPLE_SEARCH_FILTER_MODE_RESOURCE},
        {MediaNavigationMode.MoviesShowItems, SHOW_ALL_MOVIE_ITEMS_MODE_RESOURCE},
        {MediaNavigationMode.MoviesFilterByActor, FILTER_BY_ACTOR_MODE_RESOURCE},
        {MediaNavigationMode.MoviesFilterByGenre, FILTER_BY_MOVIE_GENRE_MODE_RESOURCE},
        {MediaNavigationMode.MoviesFilterByYear, FILTER_BY_YEAR_MODE_RESOURCE},
        {MediaNavigationMode.MoviesSimpleSearch, SIMPLE_SEARCH_FILTER_MODE_RESOURCE},
        {MediaNavigationMode.PicturesShowItems, SHOW_ALL_PICTURE_ITEMS_MODE_RESOURCE},
        {MediaNavigationMode.PicturesFilterByYear, FILTER_BY_YEAR_MODE_RESOURCE},
        {MediaNavigationMode.PicturesFilterBySize, FILTER_BY_PICTURE_SIZE_MODE_RESOURCE},
        {MediaNavigationMode.PicturesSimpleSearch, SIMPLE_SEARCH_FILTER_MODE_RESOURCE},
      };

    protected static readonly Guid[] NECESSARY_MUSIC_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          MusicAspect.ASPECT_ID,
      };

    protected static readonly Guid[] NECESSARY_MOVIE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          MovieAspect.ASPECT_ID,
      };

    protected static readonly Guid[] NECESSARY_PICTURE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          PictureAspect.ASPECT_ID,
      };

    public const string FILTERS_WORKFLOW_CATEGORY = "a-Filters";
    public const string STATIC_ACTIONS_WORKFLOW_CATEGORY = "b-Static";

    // Screens
    public const string LOCAL_MEDIA_NAVIGATION_SCREEN = "LocalMediaNavigation";
    public const string MUSIC_SHOW_ITEMS_SCREEN = "MusicShowItems";
    public const string MUSIC_FILTER_BY_ARTIST_SCREEN = "MusicFilterByArtist";
    public const string MUSIC_FILTER_BY_ALBUM_SCREEN = "MusicFilterByAlbum";
    public const string MUSIC_FILTER_BY_GENRE_SCREEN = "MusicFilterByGenre";
    public const string MUSIC_FILTER_BY_DECADE_SCREEN = "MusicFilterByDecade";
    public const string MUSIC_SIMPLE_SEARCH_SCREEN = "MusicSimpleSearch";
    public const string MOVIES_SHOW_ITEMS_SCREEN = "MoviesShowItems";
    public const string MOVIES_FILTER_BY_ACTOR_SCREEN = "MoviesFilterByActor";
    public const string MOVIES_FILTER_BY_GENRE_SCREEN = "MoviesFilterByGenre";
    public const string MOVIES_FILTER_BY_YEAR_SCREEN = "MoviesFilterByYear";
    public const string MOVIES_SIMPLE_SEARCH_SCREEN = "MoviesSimpleSearch";
    public const string PICTURES_SHOW_ITEMS_SCREEN = "PicturesShowItems";
    public const string PICTURES_FILTER_BY_YEAR_SCREEN = "PicturesFilterByYear";
    public const string PICTURES_FILTER_BY_SIZE_SCREEN = "PicturesFilterBySize";
    public const string PICTURES_SIMPLE_SEARCH_SCREEN = "PicturesSimpleSearch";
    public const string PLAY_MENU_DIALOG_SCREEN = "DialogPlayMenu";

    public static readonly TimeSpan SEARCH_TEXT_TYPE_TIMESPAN = new TimeSpan(0, 0, 0, 0, 300);

    /// <summary>
    /// Denotes the "infinite" timespan, used for <see cref="System.Threading.Timer.Change(System.TimeSpan,System.TimeSpan)"/>
    /// method, for example.
    /// </summary>
    public readonly static TimeSpan INFINITE_TIMESPAN = new TimeSpan(0, 0, 0, 0, -1);


    #endregion

    #region Protected fields

    // Screen data is stored in current navigation context
    protected NavigationContext _currentNavigationContext = null;

    // Play menu
    protected ItemsList _playMenuItems = null;

    protected Timer _searchTimer;

    #endregion

    public MediaModel()
    {
      _searchTimer = new Timer(OnSearchTimerElapsed, null, Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Gets the current media navigation mode.
    /// </summary>
    /// <remarks>
    /// The media navigation mode changes during navigation through the media library or through the local media provider
    /// structure. Each time the user chooses another kind of items filter, the mode changes and the current screen reflects
    /// the mode.
    /// </remarks>
    public MediaNavigationMode Mode
    {
      get { return GetFromCurrentContext(NAVIGATION_MODE_KEY, false, MediaNavigationMode.LocalMedia); }
      internal set { SetInCurrentContext(NAVIGATION_MODE_KEY, value); }
    }

    /// <summary>
    /// Gets the screen which is currently being shown by this model.
    /// </summary>
    public string Screen
    {
      get { return GetFromCurrentContext<string>(SCREEN_KEY, false); }
      internal set { SetInCurrentContext(SCREEN_KEY, value); }
    }

    /// <summary>
    /// Provides the data of the view currently shown.
    /// </summary>
    public View CurrentView
    {
      get { return GetFromCurrentContext<View>(VIEW_KEY, false); }
      internal set { SetInCurrentContext(VIEW_KEY, value); }
    }

    /// <summary>
    /// In case the current screen shows local media, music, movies or pictures, this property provides a list
    /// with the sub views and media items of the current view. In case the current screen shows a list of filter
    /// value of a choosen filter criteria, this property provides a list of available filter values.
    /// </summary>
    public ItemsList Items
    {
      get { return GetFromCurrentContext<ItemsList>(ITEMS_KEY, false); }
      internal set { SetInCurrentContext(ITEMS_KEY, value); }
    }

    /// <summary>
    /// Gets a string like "No items", "One item" or "10 items". The number reflects the number of of items in the
    /// <see cref="Items"/> list.
    /// </summary>
    public string NumItemsStr
    {
      get
      {
        return GetFromCurrentContext<string>(NUM_ITEMS_STR_KEY, false);
      }
      internal set { SetInCurrentContext(NUM_ITEMS_STR_KEY, value); }
    }

    /// <summary>
    /// Gets the title to be displayed for the current <see cref="Items"/> list.
    /// </summary>
    /// <remarks>
    /// The contents of this property is like "Media items" or "Simple search".
    /// </remarks>
    public string ItemsListTitle
    {
      get { return GetFromCurrentContext<string>(ITEMSLIST_TITLE_KEY, false); }
      internal set { SetInCurrentContext(ITEMSLIST_TITLE_KEY, value); }
    }

    /// <summary>
    /// Gets the information whether the current view has a navigatable parent view.
    /// </summary>
    /// <remarks>
    /// In case this property is <c>true</c>, the view should provide a means to navigate to the parent view.
    /// </remarks>
    public bool HasParentDirectory
    {
      get { return GetFromCurrentContext(HAS_PARENT_DIRECTORY_KEY, false, false); }
      internal set { SetInCurrentContext(HAS_PARENT_DIRECTORY_KEY, value); }
    }

    /// <summary>
    /// Gets the information whether the current view is valid, i.e. its content could be built and <see cref="Items"/>
    /// contains the items of the current view.
    /// </summary>
    public bool IsItemsValid
    {
      get { return GetFromCurrentContext(IS_ITEMS_VALID_KEY, false, false); }
      internal set { SetInCurrentContext(IS_ITEMS_VALID_KEY, value); }
    }

    /// <summary>
    /// Gets the information whether the current view is empty, i.e. <see cref="Items"/>'s count is <c>0</c>.
    /// </summary>
    public bool IsItemsEmpty
    {
      get { return GetFromCurrentContext(IS_ITEMS_EMPTY_KEY, false, false); }
      internal set { SetInCurrentContext(IS_ITEMS_EMPTY_KEY, value); }
    }

    /// <summary>
    /// Gets the contents of the text edit field containing the current search text for the simple search.
    /// </summary>
    public string SimpleSearchText
    {
      get { return (string) SimpleSearchTextProperty.GetValue(); }
      internal set { SimpleSearchTextProperty.SetValue(value); }
    }

    public AbstractProperty SimpleSearchTextProperty
    {
      get { return GetFromCurrentContext<AbstractProperty>(SIMPLE_SEARCH_TEXT_PROPERTY_KEY, false); }
      internal set { SetInCurrentContext(SIMPLE_SEARCH_TEXT_PROPERTY_KEY, value); }
    }

    /// <summary>
    /// Gets a collection of available media navigation modes this model can be switched to in the current navigation state.
    /// </summary>
    /// <remarks>
    /// During the media navigation, the user can choose several filters to narrow the result set of media items to be shown.
    /// Once a filter is choosen, it doesn't make sense that the same kind of filter is applied again, so that media
    /// navigation mode will be removed from the collection of available modes in derived media navigation states.
    /// </remarks>
    public ICollection<MediaNavigationMode> AvailableDynamicModes
    {
      get { return GetFromCurrentContext<ICollection<MediaNavigationMode>>(DYNAMIC_MODES_KEY, false); }
      internal set { SetInCurrentContext(DYNAMIC_MODES_KEY, value); }
    }

    /// <summary>
    /// Gets a collection of workflow actions to be shown in the menu which reflect the current
    /// <see cref="AvailableDynamicModes"/>.
    /// </summary>
    public ICollection<WorkflowAction> DynamicWorkflowActions
    {
      get { return GetFromCurrentContext<ICollection<WorkflowAction>>(DYNAMIC_WORKFLOW_ACTIONS_KEY, false); }
      internal set { SetInCurrentContext(DYNAMIC_WORKFLOW_ACTIONS_KEY, value); }
    }

    /// <summary>
    /// Provides a list of items to be shown in the play menu.
    /// </summary>
    public ItemsList PlayMenuItems
    {
      get { return _playMenuItems; }
    }

    /// <summary>
    /// Provides a callable method for the skin to select an item of the media contents view.
    /// Depending on the item type, we will navigate to the choosen view, play the choosen item or filter by the item.
    /// </summary>
    /// <param name="item">The choosen item. This item should be one of the items in the <see cref="Items"/> list.</param>
    public void Select(ListItem item)
    {
      if (item == null)
        return;
      if (item.Command != null)
        item.Command.Execute();
    }

    #region Protected members

    /// <summary>
    /// If in a shown view there are sub views, clicking on them will switch this media model to the media navigation
    /// mode which is given by this property. This property will be inherited from predecessor navigation contexts.
    /// </summary>
    protected MediaNavigationMode SubViewsNavigationMode
    {
      get { return GetFromCurrentContext(SUB_VIEWS_NAVIGATION_MODE_KEY, true, MediaNavigationMode.LocalMedia); }
      set { SetInCurrentContext(SUB_VIEWS_NAVIGATION_MODE_KEY, value); }
    }

    /// <summary>
    /// Delegate function to be used to wrap a playable media item should into a ListItem.
    /// This property will be inherited from predecessor navigation contexts.
    /// </summary>
    protected PlayableItemCreatorDelegate PlayableItemCreator
    {
      get { return GetFromCurrentContext<PlayableItemCreatorDelegate>(PLAYABLE_ITEM_CREATOR_KEY, true); }
      set { SetInCurrentContext(PLAYABLE_ITEM_CREATOR_KEY, value); }
    }

    protected Guid[] NecessaryMIATypes
    {
      get { return GetFromCurrentContext<Guid[]>(NECESSARY_MIA_TYPES_KEY, true); }
      set { SetInCurrentContext(NECESSARY_MIA_TYPES_KEY, value); }
    }

    protected Guid[] OptionalMIATypes
    {
      get { return GetFromCurrentContext<Guid[]>(OPTIONAL_MIA_TYPES_KEY, true); }
      set { SetInCurrentContext(OPTIONAL_MIA_TYPES_KEY, value); }
    }

    protected MediaNavigationMode? GetMode()
    {
      return _currentNavigationContext.GetContextVariable(NAVIGATION_MODE_KEY, false) as MediaNavigationMode?;
    }

    protected T GetFromCurrentContext<T>(string contextVariableName, bool inheritFromPredecessor) where T : class
    {
      return _currentNavigationContext == null ? null :
          _currentNavigationContext.GetContextVariable(contextVariableName, inheritFromPredecessor) as T;
    }

    protected T GetFromCurrentContext<T>(string contextVariableName, bool inheritFromPredecessor, T defaultValue) where T : struct
    {
      if (_currentNavigationContext == null)
        return defaultValue;
      T? result = _currentNavigationContext.GetContextVariable(contextVariableName, inheritFromPredecessor) as T?;
      return result.HasValue ? result.Value : defaultValue;
    }

    protected void SetInCurrentContext<T>(string contextVariableName, T value)
    {
      if (_currentNavigationContext == null)
        return;
      _currentNavigationContext.SetContextVariable(contextVariableName, value);
    }

    /// <summary>
    /// Does the actual work of navigating to the specifield view. This will exchange our
    /// <see cref="CurrentView"/> to the specified <paramref name="view"/> and push a state onto
    /// the workflow manager's navigation stack.
    /// </summary>
    /// <param name="navigationMode">Mode to be used for the navigation. Determines the screen to be used.</param>
    /// <param name="view">View to navigate to.</param>
    protected static void NavigateToView(MediaNavigationMode navigationMode, View view)
    {
      WorkflowState newState = WorkflowState.CreateTransientState(
          "View: " + view.DisplayName, view.DisplayName, null, true, WorkflowType.Workflow);
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      IDictionary<string, object> variables = new Dictionary<string, object>
        {
            {NAVIGATION_MODE_KEY, navigationMode},
            {VIEW_KEY, view},
            {DYNAMIC_MODES_KEY, null}
        };
      workflowManager.NavigatePushTransient(newState, null, variables);
    }

    /// <summary>
    /// Checks if we need to show a menu for playing the specified <paramref name="item"/> and shows that
    /// menu or plays the item, if no menu must be shown.
    /// </summary>
    /// <param name="item">The item which was selected to play.</param>
    protected void CheckPlayMenu(MediaItem item)
    {
      IPlayerContextManager pcm = ServiceScope.Get<IPlayerContextManager>();
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
        ListItem playItem = new ListItem(NAME_KEY, PLAY_AUDIO_ITEM_RESOURCE)
          {
              Command = new MethodDelegateCommand(() => PlayItem(item))
          };
        _playMenuItems.Add(playItem);
        if (numAudio > 0)
        {
          ListItem enqueueItem = new ListItem(NAME_KEY, ENQUEUE_AUDIO_ITEM_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, false, false, false))
            };
          _playMenuItems.Add(enqueueItem);
        }
        if (numVideo > 0)
        {
          ListItem playItemConcurrently = new ListItem(NAME_KEY, MUTE_VIDEO_PLAY_AUDIO_ITEM_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, true, false))
            };
          _playMenuItems.Add(playItemConcurrently);
        }
      }
      else if (mediaType == PlayerContextType.Video)
      {
        ListItem playItem = new ListItem(NAME_KEY, PLAY_VIDEO_ITEM_RESOURCE)
          {
              Command = new MethodDelegateCommand(() => PlayItem(item))
          };
        _playMenuItems.Add(playItem);
        if (numVideo > 0)
        {
          ListItem enqueueItem = new ListItem(NAME_KEY, ENQUEUE_VIDEO_ITEM_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, false, false, false))
            };
          _playMenuItems.Add(enqueueItem);
        }
        if (numAudio > 0)
        {
          ListItem playItem_A = new ListItem(NAME_KEY, PLAY_VIDEO_ITEM_MUTED_CONCURRENT_AUDIO_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, true, false))
            };
          _playMenuItems.Add(playItem_A);
        }
        if (numVideo > 0)
        {
          ListItem playItem_V = new ListItem(NAME_KEY, PLAY_VIDEO_ITEM_PIP_RESOURCE)
            {
                Command = new MethodDelegateCommand(() => PlayOrEnqueueItem(item, true, true, true))
            };
          _playMenuItems.Add(playItem_V);
        }
      }
      else
      {
        IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
        dialogManager.ShowDialog(SYSTEM_INFORMATION_RESOURCE, CANNOT_PLAY_ITEM_RESOURCE, DialogType.OkDialog, false,
            DialogButtonType.Ok);
      }
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.ShowDialog(PLAY_MENU_DIALOG_SCREEN);
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
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
      PlayOrEnqueueItem(item, true, false, false);
    }

    protected static IPlayerContext GetPlayerContextByMediaType(PlayerContextType mediaType, bool concurrent)
    {
      IPlayerContextManager pcm = ServiceScope.Get<IPlayerContextManager>();
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
      if (item.Aspects.ContainsKey(MovieAspect.Metadata.AspectId))
      {
        moduleId = VIDEO_MODULE_ID;
        contextName = VIDEO_PLAYER_CONTEXT_NAME_RESOURCE;
        return true;
      }
      if (item.Aspects.ContainsKey(PictureAspect.Metadata.AspectId))
      {
        moduleId = PICTURE_MODULE_ID;
        contextName = PICTURE_PLAYER_CONTEXT_NAME_RESOURCE;
        return true;
      }
      if (item.Aspects.ContainsKey(MusicAspect.Metadata.AspectId))
      {
        moduleId = MUSIC_MODULE_ID;
        contextName = AUDIO_PLAYER_CONTEXT_NAME_RESOURCE;
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
    /// <param name="subordinatedVideo">If set to <c>true</c>, a video item will be played in PIP mode, if
    /// applicable.</param>
    protected static void PlayOrEnqueueItem(MediaItem item, bool play, bool concurrent, bool subordinatedVideo)
    {
      IPlayerContextManager pcm = ServiceScope.Get<IPlayerContextManager>();
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

    protected string BuildNumItemsStr(int numItems)
    {
      return numItems == 0 ? NO_ITEMS_RESOURCE : (numItems == 1 ? ONE_ITEM_RESOURCE :
          LocalizationHelper.Translate(N_ITEMS_RESOURCE, numItems));
    }

    /// <summary>
    /// Updates the GUI data for a media items view screen which reflects the data of the given <paramref name="view"/>.
    /// </summary>
    /// <remarks>
    /// Updates the properties <see cref="Items"/>, <see cref="ItemsListTitle"/>, <see cref="IsItemsEmpty"/> and
    /// <see cref="IsItemsValid"/>.
    /// </remarks>
    /// <param name="title">Title to be written to the <see cref="ItemsListTitle"/> GUI property.</param>
    /// <param name="view">View contents to be filled into the <see cref="Items"/> GUI property.</param>
    /// <param name="createNewList">If set to <c>true</c>, this method will re-create the <see cref="Items"/> list,
    /// else it will reuse it.</param>
    protected void ReloadMediaItems(string title, View view, bool createNewList)
    {
      // We need to create a new items list because the reloading of items takes place while the old
      // screen still shows the old items
      ItemsList items;
      if (createNewList)
        items = new ItemsList();
      else
      {
        items = Items;
        items.Clear();
      }
      // TODO: Add the items in a separate job while the UI already shows the new screen
      HasParentDirectory = view.ParentView != null;
      if (view.IsValid)
      {
        IsItemsValid = true;
        // Add items for sub views
        List<ListItem> viewsList = new List<ListItem>();
        MediaNavigationMode subViewsNavigationMode = SubViewsNavigationMode;
        PlayableItemCreatorDelegate picd = PlayableItemCreator;
        foreach (View subView in view.SubViews)
        {
          NavigationItem item = new NavigationItem(subViewsNavigationMode, subView, null);
          View sv = subView;
          item.Command = new MethodDelegateCommand(() => NavigateToView(subViewsNavigationMode, sv));
          viewsList.Add(item);
        }
        viewsList.Sort((v1, v2) => string.Compare(v1[NavigationItem.KEY_NAME], v2[NavigationItem.KEY_NAME]));
        CollectionUtils.AddAll(items, viewsList);
        List<ListItem> itemsList = new List<ListItem>();
        foreach (MediaItem childItem in view.MediaItems)
        {
          PlayableItem item = picd(childItem);
          if (item == null)
            continue;
          item.Command = new MethodDelegateCommand(() => CheckPlayMenu(item.MediaItem));
          itemsList.Add(item);
        }
        itemsList.Sort((i1, i2) => string.Compare(i1[PlayableItem.KEY_NAME], i2[PlayableItem.KEY_NAME]));
        CollectionUtils.AddAll(items, itemsList);
        IsItemsEmpty = items.Count == 0;
      }
      else
      {
        IsItemsEmpty = false;
        IsItemsValid = false;
      }
      Items = items;
      NumItemsStr = BuildNumItemsStr(items.Count);
      Items.FireChange();
      ItemsListTitle = title;
    }

    /// <summary>
    /// Updates the GUI data for a filter values selection screen which reflects the available filter values of
    /// the given view specification <paramref name="currentVS"/> for the given filter <paramref name="criterion"/>.
    /// </summary>
    /// <remarks>
    /// Updates the properties <see cref="Items"/>, <see cref="ItemsListTitle"/>, <see cref="IsItemsEmpty"/> and
    /// <see cref="IsItemsValid"/>.
    /// </remarks>
    /// <param name="title">Title to be written to the <see cref="ItemsListTitle"/> GUI property.</param>
    /// <param name="currentMode">Media navigation mode which is currently active. This mode will be removed
    /// from the remaining navigation modes in the menu items displayed in the next workflow states.</param>
    /// <param name="currentVS">View specification of the view to be filtered in the current screen.</param>
    /// <param name="criterion">Filter criterion to be used to build the filter values.</param>
    protected void CreateFilterValuesList(string title, MediaNavigationMode currentMode, StackedFiltersMLVS currentVS,
        MLFilterCriterion criterion)
    {
      ItemsList items = new ItemsList();
      ICollection<MediaNavigationMode> remainingDynamicModes = new List<MediaNavigationMode>(AvailableDynamicModes);
      remainingDynamicModes.Remove(currentMode);

      try
      {
        List<FilterValue> filterValues = new List<FilterValue>(criterion.GetAvailableValues(NECESSARY_MUSIC_MIAS,
            BooleanCombinationFilter.CombineFilters(BooleanOperator.And, currentVS.Filters)));
        filterValues.Sort((f1, f2) => string.Compare(f1.Title, f2.Title));
        foreach (FilterValue filterValue in filterValues)
        {
          string filterTitle = filterValue.Title;
          StackedFiltersMLVS subVS = currentVS.CreateSubViewSpecification(filterTitle, filterValue.Filter);
          ListItem filterValueItem = new ListItem(NAME_KEY, filterTitle)
            {
                Command = new MethodDelegateCommand(() =>
                    {
                      WorkflowState state = WorkflowState.CreateTransientState(filterTitle, filterTitle, null, false,
                          WorkflowType.Workflow);
                      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
                      workflowManager.NavigatePushTransient(state, null, new Dictionary<string, object>
                          {
                            {NAVIGATION_MODE_KEY, remainingDynamicModes.FirstOrDefault()},
                            {VIEW_KEY, subVS.BuildRootView()},
                            {DYNAMIC_MODES_KEY, remainingDynamicModes}
                          });
                    })
            };
          if (filterValue.HasNumItems)
            filterValueItem.SetLabel(NUM_ITEMS_KEY, filterValue.NumItems.ToString());
          items.Add(filterValueItem);
        }
        IsItemsValid = true;
        IsItemsEmpty = items.Count == 0;
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Warn("MediaModel: Error creating filter values list", e);
        IsItemsValid = false;
        IsItemsEmpty = true;
        throw;
      }
      Items = items;
      NumItemsStr = BuildNumItemsStr(items.Count);
      ItemsListTitle = title;
    }

    void OnSimpleSearchTextChanged(AbstractProperty prop, object oldValue)
    {
      _searchTimer.Change(SEARCH_TEXT_TYPE_TIMESPAN, INFINITE_TIMESPAN);
    }

    void OnSearchTimerElapsed(object sender)
    {
      View cv = CurrentView;
      if (cv == null)
        return;
      StackedFiltersMLVS sfmlvs = cv.Specification as StackedFiltersMLVS;
      View view = new SimpleTextSearchViewSpecification(SIMPLE_SEARCH_TITLE_RESOURCE, SimpleSearchText,
          BooleanCombinationFilter.CombineFilters(BooleanOperator.And, sfmlvs.Filters), NecessaryMIATypes, OptionalMIATypes, true, true).BuildRootView();
      ReloadMediaItems(SIMPLE_SEARCH_TITLE_RESOURCE, view, false);
    }

    protected void InitializeSimpleSearch()
    {
      AbstractProperty sstProperty = SimpleSearchTextProperty;
      if (sstProperty == null)
      {
        sstProperty = new WProperty(typeof(string), string.Empty);
        sstProperty.Attach(OnSimpleSearchTextChanged);
        SimpleSearchTextProperty = sstProperty;
      }

      // First load all media items of the current view
      ReloadMediaItems(SIMPLE_SEARCH_TITLE_RESOURCE, CurrentView, true);
    }

    protected void ReloadMediaItems(View view, bool createNewList)
    {
      ReloadMediaItems(MEDIA_ITEMS_VIEW_TITLE_RESOURCE, view, createNewList);
    }

    protected void ReloadArtists(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_ARTIST_MODE_RESOURCE, MediaNavigationMode.MusicFilterByArtist, sfmlvs, new SimpleMLFilterCriterion(MusicAspect.ATTR_ARTISTS));
    }

    protected void ReloadAlbums(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_ALBUM_MODE_RESOURCE, MediaNavigationMode.MusicFilterByAlbum, sfmlvs, new SimpleMLFilterCriterion(MusicAspect.ATTR_ALBUM));
    }

    protected void ReloadMusicGenres(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_MUSIC_GENRE_MODE_RESOURCE, MediaNavigationMode.MusicFilterByGenre, sfmlvs, new SimpleMLFilterCriterion(MusicAspect.ATTR_GENRES));
    }

    protected void ReloadDecades(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_DECADE_MODE_RESOURCE, MediaNavigationMode.MusicFilterByDecade, sfmlvs, new FilterByDecadeCriterion());
    }

    protected void ReloadActors(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_ACTOR_MODE_RESOURCE, MediaNavigationMode.MoviesFilterByActor, sfmlvs, new SimpleMLFilterCriterion(MovieAspect.ATTR_ACTORS));
    }

    protected void ReloadMovieGenres(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_MOVIE_GENRE_MODE_RESOURCE, MediaNavigationMode.MoviesFilterByGenre, sfmlvs, new SimpleMLFilterCriterion(MovieAspect.ATTR_GENRE));
    }

    protected void ReloadMovieYears(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_YEAR_MODE_RESOURCE, MediaNavigationMode.MoviesFilterByYear, sfmlvs, new FilterByYearCriterion());
    }

    protected void ReloadPictureYears(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_YEAR_MODE_RESOURCE, MediaNavigationMode.PicturesFilterByYear, sfmlvs, new FilterByYearCriterion());
    }

    protected void ReloadPictureSizes(StackedFiltersMLVS sfmlvs)
    {
      CreateFilterValuesList(FILTER_BY_PICTURE_SIZE_MODE_RESOURCE, MediaNavigationMode.PicturesFilterBySize, sfmlvs, new FilterByPictureSizeCriterion());
    }

    protected void PrepareRootState()
    {
      // Initialize root media navigation state. We will set up all sub processes for each media model "part", i.e.
      // music, movies, pictures and local media.
      // For each step through the media navigation, we divide variables to be set into three categories:
      // 0) Variables which are constant over the media model part (PlayableItemCreator, SubViewsNavigationMode,
      //    NecessaryMIAs, OptionalMIAs)
      // 1) Variables which are initialized by the previous navigation (media navigation mode, view, available dynamic modes)
      // 2) Variables which are dependent from variables of (1): Workflow actions for filter criteria (depend on available
      //    dynamic modes), screen and screen data like media items/filter values (depend on media navigation mode and view)
      // For the first media model state, we need to initialize the variables (1). Dependent variables (2) will be updated
      // later by method PrepareScreenVariablesForMode
      Guid currentStateId = _currentNavigationContext.WorkflowState.StateId;
      ViewSpecification rootViewSpecification;
      ICollection<MediaNavigationMode> availableDynamicModes = null;
      // The initial state ID determines the media model "part" to initialize: Local media, music, movies or pictures.
      // The media model part determines the media navigation mode and the view contents to be set.
      if (currentStateId == MUSIC_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.MusicFilterByAlbum; // We just initialize an arbitrary mode for the music part; maybe we should make the initial mode configurable
        SubViewsNavigationMode = MediaNavigationMode.MusicFilterByAlbum;
        PlayableItemCreator = mi => new MusicItem(mi);
        rootViewSpecification = StackedFiltersMLVS.CreateRootViewSpecification(MUSIC_VIEW_NAME_RESOURCE,
            NecessaryMIATypes = NECESSARY_MUSIC_MIAS, OptionalMIATypes = null, null, true);
          availableDynamicModes = new List<MediaNavigationMode>
            {
                MediaNavigationMode.MusicShowItems,
                MediaNavigationMode.MusicFilterByArtist,
                MediaNavigationMode.MusicFilterByAlbum,
                MediaNavigationMode.MusicFilterByGenre,
                MediaNavigationMode.MusicFilterByDecade,
                MediaNavigationMode.MusicSimpleSearch,
            };
      }
      else if (currentStateId == MOVIES_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.MoviesShowItems; // We just initialize an arbitrary mode for the movies part; maybe we should make the initial mode configurable
        SubViewsNavigationMode = MediaNavigationMode.MoviesShowItems;
        PlayableItemCreator = mi => new MovieItem(mi);
        rootViewSpecification = StackedFiltersMLVS.CreateRootViewSpecification(MOVIES_VIEW_NAME_RESOURCE,
            NecessaryMIATypes = NECESSARY_MOVIE_MIAS, OptionalMIATypes = null, null, true);
          availableDynamicModes = new List<MediaNavigationMode>
          {
              MediaNavigationMode.MoviesShowItems,
              MediaNavigationMode.MoviesFilterByActor,
              MediaNavigationMode.MoviesFilterByGenre,
              MediaNavigationMode.MoviesFilterByYear,
              MediaNavigationMode.MoviesSimpleSearch,
          };
      }
      else if (currentStateId == PICTURES_NAVIGATION_ROOT_STATE)
      {
        Mode = MediaNavigationMode.PicturesShowItems; // We just initialize an arbitrary mode for the pictures part; maybe we should make the initial mode configurable
        SubViewsNavigationMode = MediaNavigationMode.PicturesShowItems;
        PlayableItemCreator = mi => new PictureItem(mi);
        rootViewSpecification = StackedFiltersMLVS.CreateRootViewSpecification(PICTURES_VIEW_NAME_RESOURCE,
            NecessaryMIATypes = NECESSARY_PICTURE_MIAS, OptionalMIATypes = null, null, true);
          availableDynamicModes = new List<MediaNavigationMode>
          {
              MediaNavigationMode.PicturesShowItems,
              MediaNavigationMode.PicturesFilterByYear,
              MediaNavigationMode.PicturesFilterBySize,
              MediaNavigationMode.PicturesSimpleSearch,
          };
      }
      else
      { // If we were called with a supported root state, we should be in state LOCAL_MEDIA_NAVIGATION_ROOT_STATE here
        if (currentStateId != LOCAL_MEDIA_NAVIGATION_ROOT_STATE)
        {
          // Error case: We cannot handle the given state
          ServiceScope.Get<ILogger>().Warn("MediaModel: Unknown root workflow state with ID '{0}'", currentStateId);
          // We simply use the local media mode as fallback for this case, so we go on
        }
        Mode = MediaNavigationMode.LocalMedia;
        SubViewsNavigationMode = MediaNavigationMode.LocalMedia;
        PlayableItemCreator = mi =>
          {
            if (mi.Aspects.ContainsKey(MusicAspect.ASPECT_ID))
              return new MusicItem(mi);
            else if (mi.Aspects.ContainsKey(MovieAspect.ASPECT_ID))
              return new MovieItem(mi);
            else if (mi.Aspects.ContainsKey(PictureAspect.ASPECT_ID))
              return new PictureItem(mi);
            else
              return null;
          };
        rootViewSpecification = new LocalSharesViewSpecification(LOCAL_MEDIA_ROOT_VIEW_NAME_RESOURCE,
            NecessaryMIATypes = new Guid[]
                {
                    ProviderResourceAspect.ASPECT_ID,
                    MediaAspect.ASPECT_ID,
                },
            OptionalMIATypes = new Guid[]
                {
                    MusicAspect.ASPECT_ID,
                    MovieAspect.ASPECT_ID,
                    PictureAspect.ASPECT_ID,
                });
        // Dynamic modes remain null - local media doesn't provide dynamic filters
      }
      CurrentView = rootViewSpecification.BuildRootView();
      if (availableDynamicModes != null)
        AvailableDynamicModes = availableDynamicModes;
    }

    protected void PrepareScreenVariablesForMode()
    {
      // Initialize a new sub state for media navigation here.
      // 1) Variables which need to be already set:
      //  - Media navigation mode
      //  - View
      //  - Available dynamic modes
      // 2) Variables which will be initialized here and which depend on variables of (1):
      //  - Screen and screen data like media items/filter values (depend on media model mode and view)
      MediaNavigationMode? mode = GetMode();
      if (!mode.HasValue)
      {
        ServiceScope.Get<ILogger>().Error("MediaModel: Media navigation mode isn't initialized; cannot prepare necessary variables");
        return;
      }

      string screen = null;
      try
      {
        View view = CurrentView;
        // Prepare screen & screen data for the current media navigation mode
        if (mode.Value == MediaNavigationMode.LocalMedia)
        {
          screen = LOCAL_MEDIA_NAVIGATION_SCREEN;
          ReloadMediaItems(view, true);
        }
        else
        {
          StackedFiltersMLVS sfmlvs = view.Specification as StackedFiltersMLVS;
          if (sfmlvs == null)
          {
            ServiceScope.Get<ILogger>().Error("MediaModel: Wrong type of media library view '{0}'", view);
            return;
          }
          switch (mode.Value)
          {
            // case MediaNavigationMode.LocalMedia: handled in "if" statement above
            case MediaNavigationMode.MusicShowItems:
              screen = MUSIC_SHOW_ITEMS_SCREEN;
              ReloadMediaItems(view, true);
              break;
            case MediaNavigationMode.MusicFilterByArtist:
              screen = MUSIC_FILTER_BY_ARTIST_SCREEN;
              ReloadArtists(sfmlvs);
              break;
            case MediaNavigationMode.MusicFilterByAlbum:
              screen = MUSIC_FILTER_BY_ALBUM_SCREEN;
              ReloadAlbums(sfmlvs);
              break;
            case MediaNavigationMode.MusicFilterByGenre:
              screen = MUSIC_FILTER_BY_GENRE_SCREEN;
              ReloadMusicGenres(sfmlvs);
              break;
            case MediaNavigationMode.MusicFilterByDecade:
              screen = MUSIC_FILTER_BY_DECADE_SCREEN;
              ReloadDecades(sfmlvs);
              break;
            case MediaNavigationMode.MusicSimpleSearch:
              screen = MUSIC_SIMPLE_SEARCH_SCREEN;
              InitializeSimpleSearch();
              break;
            case MediaNavigationMode.MoviesShowItems:
              screen = MOVIES_SHOW_ITEMS_SCREEN;
              ReloadMediaItems(view, true);
              break;
            case MediaNavigationMode.MoviesFilterByActor:
              screen = MOVIES_FILTER_BY_ACTOR_SCREEN;
              ReloadActors(sfmlvs);
              break;
            case MediaNavigationMode.MoviesFilterByGenre:
              screen = MOVIES_FILTER_BY_GENRE_SCREEN;
              ReloadMovieGenres(sfmlvs);
              break;
            case MediaNavigationMode.MoviesFilterByYear:
              screen = MOVIES_FILTER_BY_YEAR_SCREEN;
              ReloadMovieYears(sfmlvs);
              break;
            case MediaNavigationMode.MoviesSimpleSearch:
              screen = MOVIES_SIMPLE_SEARCH_SCREEN;
              InitializeSimpleSearch();
              break;
            case MediaNavigationMode.PicturesShowItems:
              screen = PICTURES_SHOW_ITEMS_SCREEN;
              ReloadMediaItems(view, true);
              break;
            case MediaNavigationMode.PicturesFilterByYear:
              screen = PICTURES_FILTER_BY_YEAR_SCREEN;
              ReloadPictureYears(sfmlvs);
              break;
            case MediaNavigationMode.PicturesFilterBySize:
              screen = PICTURES_FILTER_BY_SIZE_SCREEN;
              ReloadPictureSizes(sfmlvs);
              break;
            case MediaNavigationMode.PicturesSimpleSearch:
              screen = PICTURES_SIMPLE_SEARCH_SCREEN;
              InitializeSimpleSearch();
              break;
            default:
              ServiceScope.Get<ILogger>().Error("MediaModel: Unsupported media navigation mode '{0}'", mode.Value);
              return;
          }
        }
      }
      catch (Exception e)
      {
        ServiceScope.Get<ILogger>().Error("Error loading screen data", e);
      }
      Screen = screen;
    }

    protected void PrepareWorkflowActions()
    {
      // Initialize a new sub state for media navigation here.
      // 1) Variables which need to be already set:
      //  - Media navigation mode
      //  - Available dynamic modes
      // 2) Variables which will be initialized here and which depend on variables of (1):
      //  - Workflow actions for filter criteria (depend on available dynamic modes)
      ICollection<MediaNavigationMode> availableDynamicModes = AvailableDynamicModes;
      if (availableDynamicModes == null || availableDynamicModes.Count == 0)
        return;
      ICollection<WorkflowAction> dynamicWorkflowActions = new List<WorkflowAction>(availableDynamicModes.Count);
      WorkflowState state = _currentNavigationContext.WorkflowState;
      int ct = 0;
      foreach (MediaNavigationMode mode in availableDynamicModes)
      {
        MediaNavigationMode newMode = mode; // Necessary to be used in closure
        WorkflowAction action = new MethodDelegateAction(Guid.NewGuid(),
            state.Name + "->" + mode, state.StateId,
            LocalizationHelper.CreateResourceString(DYNAMIC_ACTION_TITLES[mode]), () =>
              {
                Mode = newMode;
                PrepareScreenVariablesForMode();
                SwitchToCurrentScreen();
              })
          {
              DisplayCategory = FILTERS_WORKFLOW_CATEGORY,
              SortOrder = ct++.ToString(), // Sort in the order we have built up the filters
          };
        dynamicWorkflowActions.Add(action);
      }
      DynamicWorkflowActions = dynamicWorkflowActions;
    }

    protected void SwitchToCurrentScreen()
    {
      ServiceScope.Get<IScreenManager>().ShowScreen(Screen);
    }

    /// <summary>
    /// Prepares the given workflow navigation <paramref name="context"/>, i.e. prepares the view data and the
    /// available filter criteria to be used in the menu.
    /// </summary>
    protected void PrepareState(NavigationContext context)
    {
      _currentNavigationContext = context;
      MediaNavigationMode? mode = GetMode();
      if (!mode.HasValue)
        // If mode != null, the current state is already prepared,
        // i.e. we have already been in the initial state or we have initialized our variables by setting them in the workflow
        // state when navigating to it
        PrepareRootState();
      string screen = Screen;
      if (screen == null)
        // If screen != null, dependent variables of this state are already prepared
        PrepareScreenVariablesForMode();
      if (DynamicWorkflowActions == null)
        PrepareWorkflowActions();
    }

    protected void DisposeData()
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
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      DisposeData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Don't dispose state data here - when coming back to our last state ("oldContext"), we will restore our
      // last screen (which is stored in the context object)
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext);
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      PrepareState(context);
      ICollection<WorkflowAction> dynamicWorkflowActions = DynamicWorkflowActions;
      if (dynamicWorkflowActions != null)
        foreach (WorkflowAction action in dynamicWorkflowActions)
          actions.Add(action.ActionId, action);
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      screen = Screen;
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
