#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.UiComponents.Media.General
{
  public class Consts
  {
    public const string STR_MODULE_ID_MEDIA = "53130C0E-D19C-4972-92F4-DB6665E51CBC";

    public const string STR_WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT = "387044A0-83BA-435d-B262-C90CF70A9858";
    public const string STR_WF_STATE_ID_LOCAL_MEDIA_NAVIGATION = "B393C6D8-9F37-4481-B403-0D5B17F52EC8";
    public const string STR_WF_STATE_ID_AUDIO_NAVIGATION_ROOT = "F2AAEBC6-BFB0-42c8-9C80-0A98BA67A7EB";
    public const string STR_WF_STATE_ID_VIDEOS_NAVIGATION_ROOT = "22ED8702-3887-4acb-ACB4-30965220AFF0";
    public const string STR_WF_STATE_ID_IMAGES_NAVIGATION_ROOT = "76019AEB-3445-4da9-9A10-63A87549A7CF";

    public const string STR_WF_STATE_ID_ADD_TO_PLAYLIST = "76CDF664-F49C-40a4-8108-E478AB199595";

    public const string STR_WF_STATE_ID_AUDIO_CURRENTLY_PLAYING = "4596B758-CE2B-4e31-9CB9-6C30215831ED";
    public const string STR_WF_STATE_ID_AUDIO_FULLSCREEN_CONTENT = "82E8C050-0318-41a3-86B8-FC14FB85338B";

    public const string STR_WF_STATE_ID_CURRENTLY_PLAYING_VIDEO = "5764A810-F298-4a20-BF84-F03D16F775B1";
    public const string STR_WF_STATE_ID_FULLSCREEN_VIDEO = "882C1142-8028-4112-A67D-370E6E483A33";

    public const string STR_WF_STATE_ID_SHOW_PLAYLIST = "95E38A80-234C-4494-9F7A-006D8E4D6FDA";
    public const string STR_WF_STATE_ID_EDIT_PLAYLIST = "078DCC03-AE75-4347-8C07-183605CDB1B7";

    public const string STR_WF_STATE_ID_PLAYLISTS_OVERVIEW = "4A0981A3-2051-46f7-89ED-2DD3A9237DE9";
    public const string STR_WF_STATE_ID_PLAYLIST_INFO = "00E50877-E3BF-4361-A57D-15F5B495FDEF";
    public const string STR_WF_STATE_ID_PLAYLISTS_REMOVE = "BF716CDF-638C-4716-98F8-935FA85BC4D8";
    public const string STR_WF_STATE_ID_PLAYLIST_SAVE_CHOOSE_LOCATION = "D41DC5C7-71B0-4bf5-AE8E-FE2F3BC04FF1";
    public const string STR_WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME = "A967AEC6-C470-4ef6-B034-F192983AA02E";

    public const string STR_WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL = "D9DB81D6-CD4E-47a3-9E3A-BD388BD1297E";
    public const string STR_WF_STATE_ID_PLAYLIST_SAVE_FAILED = "9588B122-D697-4f9e-B8ED-887E83843C8E";

    public const string STR_WF_STATE_ID_PLAY_OR_ENQUEUE_ITEMS = "D93C8FA5-130F-4b5e-BE0B-79D6200CE8D2";
    public const string STR_WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM = "B79E395B-2276-4cde-B4CC-BB4F3E201EFF";
    public const string STR_WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS = "895C4853-6D52-4c0f-9B16-B7DA789CBF6A";
    public const string STR_WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS = "9F73CA87-732F-4017-9B1D-11DAFEED7FEC";

    public static readonly Guid MODULE_ID_MEDIA = new Guid(STR_MODULE_ID_MEDIA);

    public static readonly Guid WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT);
    public static readonly Guid WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_LOCAL_MEDIA_NAVIGATION);
    public static readonly Guid WF_STATE_ID_AUDIO_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_AUDIO_NAVIGATION_ROOT);
    public static readonly Guid WF_STATE_ID_VIDEOS_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_VIDEOS_NAVIGATION_ROOT);
    public static readonly Guid WF_STATE_ID_IMAGES_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_IMAGES_NAVIGATION_ROOT);

    public static readonly Guid WF_STATE_ID_CURRENTLY_PLAYING_VIDEO = new Guid(STR_WF_STATE_ID_CURRENTLY_PLAYING_VIDEO);
    public static readonly Guid WF_STATE_ID_FULLSCREEN_VIDEO = new Guid(STR_WF_STATE_ID_FULLSCREEN_VIDEO);

    public static readonly Guid WF_STATE_ID_CURRENTLY_PLAYING_AUDIO = new Guid(STR_WF_STATE_ID_AUDIO_CURRENTLY_PLAYING);
    public static readonly Guid WF_STATE_ID_FULLSCREEN_AUDIO = new Guid(STR_WF_STATE_ID_AUDIO_FULLSCREEN_CONTENT);

    public static readonly Guid WF_STATE_ID_SHOW_PLAYLIST = new Guid(STR_WF_STATE_ID_SHOW_PLAYLIST);
    public static readonly Guid WF_STATE_ID_EDIT_PLAYLIST = new Guid(STR_WF_STATE_ID_EDIT_PLAYLIST);

    public static readonly Guid WF_STATE_ID_PLAYLISTS_OVERVIEW = new Guid(STR_WF_STATE_ID_PLAYLISTS_OVERVIEW);
    public static readonly Guid WF_STATE_ID_PLAYLIST_INFO = new Guid(STR_WF_STATE_ID_PLAYLIST_INFO);
    public static readonly Guid WF_STATE_ID_PLAYLISTS_REMOVE = new Guid(STR_WF_STATE_ID_PLAYLISTS_REMOVE);
    public static readonly Guid WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME = new Guid(STR_WF_STATE_ID_PLAYLIST_SAVE_EDIT_NAME);

    public static readonly Guid WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL = new Guid(STR_WF_STATE_ID_PLAYLIST_SAVE_SUCCESSFUL);
    public static readonly Guid WF_STATE_ID_PLAYLIST_SAVE_FAILED = new Guid(STR_WF_STATE_ID_PLAYLIST_SAVE_FAILED);

    public static readonly Guid WF_STATE_ID_PLAY_OR_ENQUEUE_ITEMS = new Guid(STR_WF_STATE_ID_PLAY_OR_ENQUEUE_ITEMS);
    public static readonly Guid WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM = new Guid(STR_WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM);
    public static readonly Guid WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS = new Guid(STR_WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS);
    public static readonly Guid WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS = new Guid(STR_WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS);

    // Localization resource identifiers
    public const string RES_PLAY_AUDIO_ITEM = "[Media.PlayAudioItem]";
    public const string RES_ENQUEUE_AUDIO_ITEM = "[Media.EnqueueAudioItem]";
    public const string RES_MUTE_VIDEO_PLAY_AUDIO_ITEM = "[Media.MuteVideoAndPlayAudioItem]";

    public const string RES_PLAY_VIDEO_IMAGE_ITEM = "[Media.PlayVideoImageItem]";
    public const string RES_ENQUEUE_VIDEO_IMAGE_ITEM = "[Media.EnqueueVideoImageItem]";
    public const string RES_PLAY_VIDEO_IMAGE_ITEM_MUTED_CONCURRENT_AUDIO = "[Media.PlayVideoImageItemMutedConcurrentAudio]";
    public const string RES_PLAY_VIDEO_IMAGE_ITEM_PIP = "[Media.PlayVideoImageItemPiP]";

    public const string RES_PLAY_AUDIO_ITEMS = "[Media.PlayAudioItems]";
    public const string RES_ENQUEUE_AUDIO_ITEMS = "[Media.EnqueueAudioItems]";
    public const string RES_MUTE_VIDEO_PLAY_AUDIO_ITEMS = "[Media.MuteVideoAndPlayAudioItems]";

    public const string RES_PLAY_VIDEO_IMAGE_ITEMS = "[Media.PlayVideoImageItems]";
    public const string RES_ENQUEUE_VIDEO_IMAGE_ITEMS = "[Media.EnqueueVideoImageItems]";
    public const string RES_PLAY_VIDEO_IMAGE_ITEMS_MUTED_CONCURRENT_AUDIO = "[Media.PlayVideoImageItemsMutedConcurrentAudio]";
    public const string RES_PLAY_VIDEO_IMAGE_ITEMS_PIP = "[Media.PlayVideoImageItemsPiP]";

    public const string RES_VIDEO_IMAGE_CONTEXT_NAME = "[Media.VideoImageContextName]";
    public const string RES_AUDIO_CONTEXT_NAME = "[Media.AudioContextName]";

    public const string RES_SYSTEM_INFORMATION = "[System.Information]";
    public const string RES_CANNOT_PLAY_ITEM_DIALOG_TEXT = "[Media.CannotPlayItemDialogText]";
    public const string RES_CANNOT_PLAY_ITEMS_DIALOG_TEXT = "[Media.CannotPlayItemsDialogText]";

    public const string RES_BROWSE_MEDIA_ROOT_VIEW_NAME = "[Media.BrowseMediaRootViewName]";
    public const string RES_LOCAL_MEDIA_ROOT_VIEW_NAME = "[Media.LocalMediaRootViewName]";
    public const string RES_AUDIO_VIEW_NAME = "[Media.AudioRootViewName]";
    public const string RES_VIDEOS_VIEW_NAME = "[Media.VideosRootViewName]";
    public const string RES_IMAGES_VIEW_NAME = "[Media.ImagesRootViewName]";
    public const string RES_SIMPLE_SEARCH_VIEW_NAME = "[Media.SimpleSearchViewName]";

    public const string RES_FILTER_BY_ARTIST_MENU_ITEM = "[Media.FilterByArtistMenuItem]";
    public const string RES_FILTER_BY_ALBUM_MENU_ITEM = "[Media.FilterByAlbumMenuItem]";
    public const string RES_FILTER_BY_AUDIO_GENRE_MENU_ITEM = "[Media.FilterByAudioGenreMenuItem]";
    public const string RES_FILTER_BY_DECADE_MENU_ITEM = "[Media.FilterByDecadeMenuItem]";
    public const string RES_FILTER_BY_IMAGE_YEAR_MENU_ITEM = "[Media.FilterByImageYearMenuItem]";
    public const string RES_FILTER_BY_VIDEO_YEAR_MENU_ITEM = "[Media.FilterByVideoYearMenuItem]";
    public const string RES_FILTER_BY_ACTOR_MENU_ITEM = "[Media.FilterByActorMenuItem]";
    public const string RES_FILTER_BY_VIDEO_GENRE_MENU_ITEM = "[Media.FilterByVideoGenreMenuItem]";
    public const string RES_FILTER_BY_SYSTEM_MENU_ITEM = "[Media.FilterBySystemMenuItem]";
    public const string RES_FILTER_BY_IMAGE_SIZE_MENU_ITEM = "[Media.FilterByImageSizeMenuItem]";
    public const string RES_SIMPLE_SEARCH_FILTER_MENU_ITEM = "[Media.SimpleSearchFilterMenuItem]";
    public const string RES_SHOW_ALL_AUDIO_ITEMS_MENU_ITEM = "[Media.ShowAllAudioItemsMenuItem]";
    public const string RES_SHOW_ALL_VIDEO_ITEMS_MENU_ITEM = "[Media.ShowAllVideoItemsMenuItem]";
    public const string RES_SHOW_ALL_IMAGE_ITEMS_MENU_ITEM = "[Media.ShowAllImageItemsMenuItem]";

    public const string RES_BROWSE_MEDIA_NAVIGATION_NAVBAR_DISPLAY_LABEL = "[Media.BrowseMediaNavigationNavbarDisplayLabel]";
    public const string RES_LOCAL_MEDIA_NAVIGATION_NAVBAR_DISPLAY_LABEL = "[Media.LocalMediaNavigationNavbarDisplayLabel]";
    public const string RES_FILTER_ARTIST_NAVBAR_DISPLAY_LABEL = "[Media.FilterArtistNavbarDisplayLabel]";
    public const string RES_FILTER_ALBUM_NAVBAR_DISPLAY_LABEL = "[Media.FilterAlbumNavbarDisplayLabel]";
    public const string RES_FILTER_AUDIO_GENRE_NAVBAR_DISPLAY_LABEL = "[Media.FilterAudioGenreNavbarDisplayLabel]";
    public const string RES_FILTER_DECADE_NAVBAR_DISPLAY_LABEL = "[Media.FilterDecadeNavbarDisplayLabel]";
    public const string RES_FILTER_IMAGE_YEAR_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageYearNavbarDisplayLabel]";
    public const string RES_FILTER_VIDEO_YEAR_NAVBAR_DISPLAY_LABEL = "[Media.FilterVideoYearNavbarDisplayLabel]";
    public const string RES_FILTER_ACTOR_NAVBAR_DISPLAY_LABEL = "[Media.FilterActorNavbarDisplayLabel]";
    public const string RES_FILTER_VIDEO_GENRE_NAVBAR_DISPLAY_LABEL = "[Media.FilterVideoGenreNavbarDisplayLabel]";
    public const string RES_FILTER_IMAGE_SIZE_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageSizeNavbarDisplayLabel]";
    public const string RES_FILTER_AUDIO_ITEMS_NAVBAR_DISPLAY_LABEL = "[Media.FilterAudioItemsNavbarDisplayLabel]";
    public const string RES_FILTER_VIDEO_ITEMS_NAVBAR_DISPLAY_LABEL = "[Media.FilterVideoItemsNavbarDisplayLabel]";
    public const string RES_FILTER_IMAGE_ITEMS_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageItemsNavbarDisplayLabel]";
    public const string RES_FILTER_SYSTEM_NAVBAR_DISPLAY_LABEL = "[Media.FilterSystemNavbarDisplayLabel]";

    public const string VALUE_EMPTY_TITLE = "[Media.ValueEmptyTitle]";

    public const string RES_IMAGE_FILTER_SMALL = "[Media.ImageFilterSmall]";
    public const string RES_IMAGE_FILTER_MEDIUM = "[Media.ImageFilterMedium]";
    public const string RES_IMAGE_FILTER_BIG = "[Media.ImageFilterBig]";

    public const string RES_MORE_THAN_MAX_ITEMS_HINT = "[Media.MoreThanMaxItemsHint]";
    public const string RES_MORE_THAN_MAX_ITEMS_BROWSE_HINT = "[Media.MoreThanMaxItemsBrowseHint]";
    public const string RES_MORE_THAN_MAX_ITEMS_SEARCH_RESULT_HINT = "[Media.MoreThanMaxItemsSearchResultHint]";
    public const string RES_LIST_BEING_BUILT_HINT = "[Media.ListIsBeingBuiltHint]";
    public const string RES_SEARCH_RESULT_BEING_BUILT_HINT = "[Media.SearchResultIsBeingBuiltHint]";
    public const string RES_VIEW_EMPTY = "[Media.ViewEmpty]";

    public const string RES_NO_ITEMS = "[Media.NoItems]";
    public const string RES_ONE_ITEM = "[Media.OneItem]";
    public const string RES_ONE_OF_ONE_ITEM = "[Media.OneOfOneItem]";
    public const string RES_N_OF_M_ITEMS = "[Media.NOfMItems]";
    public const string RES_N_ITEMS = "[Media.NItems]";

    public const string RES_ADD_ALL_AUDIO = "[Media.AddAllAudio]";
    public const string RES_ADD_ALL_VIDEOS = "[Media.AddAllVideo]";
    public const string RES_ADD_ALL_IMAGES = "[Media.AddAllImages]";
    public const string RES_ADD_VIDEOS_AND_IMAGES = "[Media.AddAllVideosAndImages]";
    public const string RES_N_ITEMS_ADDED = "[Media.NItemsAdded]";

    // DVD
    public const string RES_SUBTITLE_OFF = "[Media.SubtitleOff]";

    // Playlists
    public const string RES_AUDIO_PLAYLIST = "[Media.AudioPlaylistHeader]";
    public const string RES_VIDEO_IMAGE_PLAYLIST = "[Media.VideoImagePlaylistHeader]";
    public const string RES_PIP_PLAYLIST = "[Media.PiPPlaylistHeader]";

    public const string RES_SAVE_PLAYLIST_FAILED_TEXT = "[Media.SavePlaylistFailedText]";
    public const string RES_SAVE_PLAYLIST_FAILED_LOCAL_MEDIAITEMS_TEXT = "[Media.CannotSavePlaylistWithLocalMediaItems]";
    public const string RES_SAVE_PLAYLIST_FAILED_PLAYLIST_ALREADY_EXISTS = "[Media.SavePlaylistFailedAlreadyExists]";
    public const string RES_SAVE_PLAYLIST_SUCCESSFUL_TEXT = "[Media.SavePlaylistSuccessfulText]";

    public const string RES_PLAYLIST_LOAD_NO_PLAYLIST = "[Media.PlaylistLoadNoPlaylistText]";
    public const string RES_PLAYLIST_LOAD_ERROR_LOADING = "[Media.PlaylistLoadErrorLoadingPlaylist]";

    public const string RES_PLAYLIST_LOAD_ITEMS_MISSING_TITLE = "[Media.PlaylistLoadItemsMissingTitle]";
    public const string RES_PLAYLIST_LOAD_SOME_ITEMS_MISSING_TEXT = "[Media.PlaylistLoadSomeItemsMissingText]";
    public const string RES_PLAYLIST_LOAD_ALL_ITEMS_MISSING_TEXT = "[Media.PlaylistLoadAllItemsMissingText]";

    // View mode
    public const string RES_SWITCH_VIEW_MODE = "[Media.SwitchViewModeMenuItem]";

    public const string RES_SMALL_LIST = "[Media.SmallList]";
    public const string RES_MEDIUM_LIST = "[Media.MediumList]";
    public const string RES_LARGE_LIST = "[Media.LargeList]";
    public const string RES_LARGE_Grid = "[Media.LargeGrid]";

    // Screens
    public const string SCREEN_BROWSE_MEDIA_NAVIGATION = "BrowseMediaNavigation";
    public const string SCREEN_LOCAL_MEDIA_NAVIGATION = "LocalMediaNavigation";
    public const string SCREEN_AUDIO_SHOW_ITEMS = "AudioShowItems";
    public const string SCREEN_AUDIO_FILTER_BY_ARTIST = "AudioFilterByArtist";
    public const string SCREEN_AUDIO_FILTER_BY_ALBUM = "AudioFilterByAlbum";
    public const string SCREEN_AUDIO_FILTER_BY_GENRE = "AudioFilterByGenre";
    public const string SCREEN_AUDIO_FILTER_BY_DECADE = "AudioFilterByDecade";
    public const string SCREEN_AUDIO_FILTER_BY_SYSTEM = "AudioFilterBySystem";
    public const string SCREEN_AUDIO_SIMPLE_SEARCH = "AudioSimpleSearch";
    public const string SCREEN_VIDEOS_SHOW_ITEMS = "VideoShowItems";
    public const string SCREEN_VIDEOS_FILTER_BY_ACTOR = "VideoFilterByActor";
    public const string SCREEN_VIDEOS_FILTER_BY_GENRE = "VideoFilterByGenre";
    public const string SCREEN_VIDEOS_FILTER_BY_YEAR = "VideoFilterByYear";
    public const string SCREEN_VIDEOS_FILTER_BY_SYSTEM = "VideoFilterBySystem";
    public const string SCREEN_VIDEOS_SIMPLE_SEARCH = "VideoSimpleSearch";
    public const string SCREEN_IMAGE_SHOW_ITEMS = "ImageShowItems";
    public const string SCREEN_IMAGE_FILTER_BY_YEAR = "ImageFilterByYear";
    public const string SCREEN_IMAGE_FILTER_BY_SIZE = "ImageFilterBySize";
    public const string SCREEN_IMAGE_FILTER_BY_SYSTEM = "ImageFilterBySystem";
    public const string SCREEN_IMAGE_SIMPLE_SEARCH = "ImageSimpleSearch";
    public const string SCREEN_PLAY_MENU_DIALOG = "DialogPlayMenu";
    public const string SCREEN_CHOOSE_AV_TYPE_DIALOG = "DialogChooseAVType";

    public const string SCREEN_FULLSCREEN_AUDIO = "FullscreenContentAudio";
    public const string SCREEN_CURRENTLY_PLAYING_AUDIO = "CurrentlyPlayingAudio";

    public const string SCREEN_FULLSCREEN_VIDEO = "FullscreenContentVideo";
    public const string SCREEN_CURRENTLY_PLAYING_VIDEO = "CurrentlyPlayingVideo";
    public static string SCREEN_FULLSCREEN_IMAGE = "FullscreenContentImage";
    public static string SCREEN_CURRENTLY_PLAYING_IMAGE = "CurrentlyPlayingImage";
    public const string SCREEN_FULLSCREEN_DVD = "FullscreenContentDVD";
    public const string SCREEN_CURRENTLY_PLAYING_DVD = "CurrentlyPlayingDVD";

    public const string SCREEN_VIDEOCONTEXTMENU_DIALOG = "DialogVideoContextMenu";

    public const string DIALOG_ADD_TO_PLAYLIST_PROGRESS = "DialogAddToPlaylistProgress";

    public const string DIALOG_SWITCH_VIEW_MODE = "DialogSwitchViewMode";

    // Timespans
    public static TimeSpan TS_SEARCH_TEXT_TYPE = TimeSpan.FromMilliseconds(300);

    public static TimeSpan TS_VIDEO_INFO_TIMEOUT = TimeSpan.FromSeconds(5);

    public static TimeSpan TS_ADD_TO_PLAYLIST_UPDATE_DIALOG_THRESHOLD = TimeSpan.FromSeconds(3);

    public static TimeSpan TS_PLAYLIST_LOAD_ITEMS_MISSING_NOTIFICATION = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Denotes the "infinite" timespan, used for <see cref="System.Threading.Timer.Change(System.TimeSpan,System.TimeSpan)"/>
    /// method, for example.
    /// </summary>
    public readonly static TimeSpan TS_INFINITE = new TimeSpan(0, 0, 0, 0, -1);

    // Accessor keys for GUI communication
    public const string KEY_NAME = "Name";
    public const string KEY_SIMPLE_TITLE = "SimpleTitle";
    public const string KEY_SORT_STRING = "SortString";
    public const string KEY_TITLE = "Title";
    public const string KEY_ARTISTS = "Artists";
    public const string KEY_YEAR = "Year";
    public const string KEY_SIZE = "Size";
    public const string KEY_WIDTH = "Width";
    public const string KEY_HEIGHT = "Height";
    public const string KEY_EXTENSION = "Extension";
    public const string KEY_MIMETYPE = "MimeType";
    public const string KEY_RATING = "Rating";

    public const string KEY_MEDIA_ITEM = "MediaItem";
    public const string KEY_NUM_ITEMS = "NumItems";
    public const string KEY_DURATION = "Duration";
    public const string KEY_AUDIO_ENCODING = "AudioEncoding";
    public const string KEY_VIDEO_ENCODING = "VideoEncoding";

    public const string KEY_IS_CURRENT_ITEM = "IsCurrentItem";

    public const string KEY_NUMBERSTR = "NumberStr";
    public const string KEY_INDEX = "Playlist-Index";

    public const string KEY_IS_DOWN_BUTTON_FOCUSED = "IsDownButtonFocused";
    public const string KEY_IS_UP_BUTTON_FOCUSED = "IsUpButtonFocused";

    public const string KEY_PLAYLIST_TYPES = "PlaylistsType";
    public const string KEY_PLAYLIST_LOCATION = "PlaylistLocation";
    public const string KEY_PLAYLIST_AV_TYPE = "PlaylistAVType";
    public const string KEY_PLAYLIST_DATA = "PlaylistData";
    public const string KEY_MESSAGE = "Message";

    // Keys for workflow state variables
    public const string KEY_NAVIGATION_MODE = "MediaNavigationModel: NAVIGATION_MODE";
    public const string KEY_NAVIGATION_DATA = "MediaNavigationModel: NAVIGATION_DATA";

    public static float DEFAULT_PIP_HEIGHT = 108;
    public static float DEFAULT_PIP_WIDTH = 192;

    public static int ADD_TO_PLAYLIST_UPDATE_INTERVAL = 50;

    public static readonly Guid[] NECESSARY_VIDEO_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_AUDIO_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          AudioAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_IMAGE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          ImageAspect.ASPECT_ID,
      };

    public static readonly string MEDIA_SKIN_SETTINGS_REGISTRATION_PATH = "/Media/SkinSettings";
    public static readonly string MEDIA_SKIN_SETTINGS_REGISTRATION_OPTIONAL_TYPES_PATH = "OptionalMIATypes";

    public const int MAX_NUM_ITEMS_VISIBLE = 5000;
  }
}