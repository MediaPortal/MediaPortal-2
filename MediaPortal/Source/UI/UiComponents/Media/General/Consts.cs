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
    public const string STR_WF_STATE_ID_SERIES_NAVIGATION_ROOT = "30F57CBA-459C-4202-A587-09FFF5098251";
    public const string STR_WF_STATE_ID_MOVIES_NAVIGATION_ROOT = "312016AA-3DF6-4C1D-B8F7-44D34C456FFE";

    public const string STR_WF_STATE_ID_LATEST_MEDIA = "60CD1874-1752-4486-9DF1-82B7BDF635A6";

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
    public const string STR_WF_STATE_ID_CHECK_MEDIA_ITEM_ACTION = "EEBCCAE6-59F8-4AF3-95FF-FC14A5B3CD62";
    public const string STR_WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM = "B79E395B-2276-4cde-B4CC-BB4F3E201EFF";
    public const string STR_WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS = "895C4853-6D52-4c0f-9B16-B7DA789CBF6A";
    public const string STR_WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS = "9F73CA87-732F-4017-9B1D-11DAFEED7FEC";

    public const string STR_WF_STATE_ID_CHECK_RESUME_SINGLE_ITEM = "04138763-42E6-49F1-BA51-EE3A9BAA835D";

    public static readonly Guid MODULE_ID_MEDIA = new Guid(STR_MODULE_ID_MEDIA);

    public static readonly Guid WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT);
    public static readonly Guid WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_LOCAL_MEDIA_NAVIGATION);
    public static readonly Guid WF_STATE_ID_AUDIO_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_AUDIO_NAVIGATION_ROOT);
    public static readonly Guid WF_STATE_ID_VIDEOS_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_VIDEOS_NAVIGATION_ROOT);
    public static readonly Guid WF_STATE_ID_IMAGES_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_IMAGES_NAVIGATION_ROOT);
    public static readonly Guid WF_STATE_ID_SERIES_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_SERIES_NAVIGATION_ROOT);
    public static readonly Guid WF_STATE_ID_MOVIES_NAVIGATION_ROOT = new Guid(STR_WF_STATE_ID_MOVIES_NAVIGATION_ROOT);

    /// <summary>
    /// Contains a list of all default navigation root states.
    /// </summary>
    public static readonly Guid[] WF_STATE_IDS_MEDIA_ROOTS =
                                    {
                                      WF_STATE_ID_BROWSE_MEDIA_NAVIGATION_ROOT,
                                      WF_STATE_ID_LOCAL_MEDIA_NAVIGATION_ROOT,
                                      WF_STATE_ID_AUDIO_NAVIGATION_ROOT,
                                      WF_STATE_ID_VIDEOS_NAVIGATION_ROOT,
                                      WF_STATE_ID_IMAGES_NAVIGATION_ROOT,
                                      WF_STATE_ID_SERIES_NAVIGATION_ROOT,
                                      WF_STATE_ID_MOVIES_NAVIGATION_ROOT
                                    };

    public static readonly Guid WF_STATE_ID_LATEST_MEDIA = new Guid(STR_WF_STATE_ID_LATEST_MEDIA);

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
    public static readonly Guid WF_STATE_ID_CHECK_MEDIA_ITEM_ACTION = new Guid(STR_WF_STATE_ID_CHECK_MEDIA_ITEM_ACTION);
    public static readonly Guid WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM = new Guid(STR_WF_STATE_ID_CHECK_QUERY_PLAYACTION_SINGLE_ITEM);
    public static readonly Guid WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS = new Guid(STR_WF_STATE_ID_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS);
    public static readonly Guid WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS = new Guid(STR_WF_STATE_ID_QUERY_AV_TYPE_CHECK_QUERY_PLAYACTION_MULTIPLE_ITEMS);

    public static readonly Guid WF_STATE_ID_CHECK_RESUME_SINGLE_ITEM = new Guid(STR_WF_STATE_ID_CHECK_RESUME_SINGLE_ITEM);

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

    // Resume playback
    public static string RES_PLAYBACK_RESUME = "[Media.PlaybackResume]";
    public static string RES_PLAYBACK_FROMSTART = "[Media.PlaybackFromStart]";

    public const string RES_VIDEO_IMAGE_CONTEXT_NAME = "[Media.VideoImageContextName]";
    public const string RES_AUDIO_CONTEXT_NAME = "[Media.AudioContextName]";

    public const string RES_SYSTEM_INFORMATION = "[System.Information]";
    public const string RES_CANNOT_PLAY_ITEM_DIALOG_TEXT = "[Media.CannotPlayItemDialogText]";
    public const string RES_CANNOT_PLAY_ITEMS_DIALOG_TEXT = "[Media.CannotPlayItemsDialogText]";

    public const string RES_CLIENT_SHARES_VIEW_NAME = "[Media.ClientSharesViewName]";
    public const string RES_SERVER_SHARES_VIEW_NAME = "[Media.ServerSharesViewName]";

    public const string RES_BROWSE_MEDIA_ROOT_VIEW_NAME = "[Media.BrowseMediaRootViewName]";
    public const string RES_LOCAL_MEDIA_ROOT_VIEW_NAME = "[Media.LocalMediaRootViewName]";
    public const string RES_AUDIO_VIEW_NAME = "[Media.AudioRootViewName]";
    public const string RES_VIDEOS_VIEW_NAME = "[Media.VideosRootViewName]";
    public const string RES_SERIES_VIEW_NAME = "[Media.SeriesRootViewName]";
    public const string RES_MOVIES_VIEW_NAME = "[Media.MoviesRootViewName]";
    public const string RES_IMAGES_VIEW_NAME = "[Media.ImagesRootViewName]";
    public const string RES_SIMPLE_SEARCH_VIEW_NAME = "[Media.SimpleSearchViewName]";


    public const string RES_VALUE_EMPTY_TITLE = "[Media.ValueEmptyTitle]";

    public const string RES_VALUE_UNWATCHED = "[Media.Unwatched]";
    public const string RES_VALUE_WATCHED = "[Media.Watched]";

    public const string RES_IMAGE_FILTER_SMALL = "[Media.ImageFilterSmall]";
    public const string RES_IMAGE_FILTER_MEDIUM = "[Media.ImageFilterMedium]";
    public const string RES_IMAGE_FILTER_BIG = "[Media.ImageFilterBig]";
    public const string RES_DISC_NUMBER_FILTER = "[Media.DiscNumberFilter]";

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

    public const string RES_ADD_TO_PLAYLIST_RES = "[Media.AddAllToPlaylist]";
    public const string RES_NO_ITEMS_TO_ADD_HEADER = "[Media.NoItemsToAddHeader]";
    public const string RES_NO_ITEMS_TO_ADD_TEXT = "[Media.NoItemsToAddText]";

    public const string RES_ADD_ALL_AUDIO = "[Media.AddAllAudio]";
    public const string RES_ADD_ALL_VIDEOS = "[Media.AddAllVideo]";
    public const string RES_ADD_ALL_IMAGES = "[Media.AddAllImages]";
    public const string RES_ADD_VIDEOS_AND_IMAGES = "[Media.AddAllVideosAndImages]";
    public const string RES_N_ITEMS_ADDED = "[Media.NItemsAdded]";

    public const string RES_LOCAL_MEDIA_MENU_ITEM = "[Media.LocalMediaMenuItem]";
    public const string RES_BROWSE_MEDIA_MENU_ITEM = "[Media.BrowseMediaMenuItem]";
    public const string RES_AUDIO_MENU_ITEM = "[Media.AudioMenuItem]";
    public const string RES_VIDEOS_MENU_ITEM = "[Media.VideosMenuItem]";
    public const string RES_IMAGES_MENU_ITEM = "[Media.ImagesMenuItem]";
    public const string RES_SERIES_MENU_ITEM = "[Media.SeriesMenuItem]";
    public const string RES_SERIES_SEASON_MENU_ITEM = "[Media.SeriesSeasonMenuItem]";
    public const string RES_LATEST_MEDIA_MENU_ITEM = "[Media.LatestMediaMenuItem]";

    public const string RES_ADD_TO_PLAYLIST_MENU_ITEM = "[Media.ManagePlaylists]";
    public const string RES_SAVE_PLAYLIST = "[Media.SavePlaylistAction]";
    public const string RES_SAVE_CURRENT_PLAYLIST = "[Media.SaveCurrentPlaylistAction]";
    public const string RES_SHOW_AUDIO_PLAYLIST = "[Media.ShowAudioPlaylistAction]";
    public const string RES_SHOW_VIDEO_IMAGE_PLAYLIST = "[Media.ShowVideoImagePlaylistAction]";
    public const string RES_SHOW_PIP_PLAYLIST = "[Media.ShowPiPPlaylistAction]";
    public const string RES_SWITCH_TO_BROWSE_ML_VIEW = "[Media.SwitchToBrowseMLView]";
    public const string RES_SWITCH_TO_LOCAL_MEDIA_VIEW = "[Media.SwitchToLocalMediaView]";

    public const string RES_SWITCH_TO_BROWSE_SHARE_VIEW = "[Media.SwitchToBrowseShareView]";
    public const string RES_SWITCH_TO_MEDIALIBRARY_VIEW = "[Media.SwitchToMediaLibraryView]";

    public const string RES_CONCURRENT_PLAYER_OPEN_STRATEGY_TEMPLATE = "[Media.OpenPlayerStrategy.{0}]";

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

    public const string RES_PLAYMODE_SUFFIX = "[Media.PlayMode.";
    public const string RES_PLAYMODE_PREFIX = "]";
    public const string RES_REPEATMODE_SUFFIX = "[Media.RepeatMode.";
    public const string RES_REPEATMODE_PREFIX = "]";

    public const string RES_PLAYMODE_CONTINUOUS = "[Media.PlayMode.Continuous]";
    public const string RES_PLAYMODE_SHUFFLE = "[Media.PlayMode.Shuffle]";

    public const string RES_REPEATMODE_NONE = "[Media.RepeatMode.None]";
    public const string RES_REPEATMODE_ALL = "[Media.RepeatMode.All]";
    public const string RES_REPEATMODE_ONE = "[Media.RepeatMode.One]";

    // View mode
    public const string RES_SWITCH_VIEW_MODE = "[Media.SwitchViewModeMenuItem]";

    public const string RES_SMALL_LIST = "[Media.SmallList]";
    public const string RES_MEDIUM_LIST = "[Media.MediumList]";
    public const string RES_LARGE_LIST = "[Media.LargeList]";
    public const string RES_LARGE_GRID = "[Media.LargeGrid]";
    public const string RES_LARGE_COVER = "[Media.CoverLayout]";

    //   Common strings
    public const string RES_COMMON_BY_TITLE_MENU_ITEM = "[Media.TitleMenuItem]";
    public const string RES_COMMON_BY_SORT_TITLE_MENU_ITEM = "[Media.SortTitleMenuItem]";
    public const string RES_COMMON_BY_NAME_MENU_ITEM = "[Media.NameMenuItem]";
    public const string RES_COMMON_BY_ALBUM_TRACK_MENU_ITEM = "[Media.AlbumTrackMenuItem]";
    public const string RES_COMMON_BY_GENRE_MENU_ITEM = "[Media.GenreMenuItem]";
    public const string RES_COMMON_BY_ALBUM_MENU_ITEM = "[Media.AlbumMenuItem]";
    public const string RES_COMMON_BY_TRACK_MENU_ITEM = "[Media.TrackMenuItem]";
    public const string RES_COMMON_BY_ARTIST_MENU_ITEM = "[Media.ArtistMenuItem]";
    public const string RES_COMMON_BY_YEAR_MENU_ITEM = "[Media.YearMenuItem]";
    public const string RES_COMMON_BY_DISC_NUMBER_MENU_ITEM = "[Media.DiscNumberMenuItem]";
    public const string RES_COMMON_BY_SIZE_MENU_ITEM = "[Media.SizeMenuItem]";
    public const string RES_COMMON_BY_SYSTEM_MENU_ITEM = "[Media.SystemMenuItem]";
    public const string RES_COMMON_BY_ACTOR_MENU_ITEM = "[Media.ActorMenuItem]";
    public const string RES_COMMON_BY_DIRECTOR_MENU_ITEM = "[Media.DirectorMenuItem]";
    public const string RES_COMMON_BY_WRITER_MENU_ITEM = "[Media.WriterMenuItem]";
    public const string RES_COMMON_BY_DURATION_MENU_ITEM = "[Media.DurationMenuItem]";
    public const string RES_COMMON_BY_ASPECT_RATIO_MENU_ITEM = "[Media.AspectRatioMenuItem]";
    public const string RES_COMMON_BY_FIRST_AIRED_DATE_MENU_ITEM = "[Media.AiredDateMenuItem]";
    public const string RES_COMMON_BY_ADDED_DATE_MENU_ITEM = "[Media.AddedDateMenuItem]";
    public const string RES_COMMON_BY_EPISODE_MENU_ITEM = "[Media.EpisodeMenuItem]";
    public const string RES_COMMON_BY_DVD_EPISODE_MENU_ITEM = "[Media.DVDEpisodeMenuItem]";
    public const string RES_COMMON_BY_CHARACTER_MENU_ITEM = "[Media.CharacterMenuItem]";
    public const string RES_COMMON_BY_COMPOSER_MENU_ITEM = "[Media.ComposerMenuItem]";
    public const string RES_COMMON_BY_TV_NETWORK_MENU_ITEM = "[Media.TVNetworkMenuItem]";
    public const string RES_COMMON_BY_PRODUCTION_STUDIO_MENU_ITEM = "[Media.CompanyMenuItem]";
    public const string RES_COMMON_BY_MUSIC_LABEL_MENU_ITEM = "[Media.MusicLabelMenuItem]";
    public const string RES_COMMON_BY_ALBUM_ARTIST_MENU_ITEM = "[Media.AlbumArtistMenuItem]";
    public const string RES_COMMON_BY_ALBUM_LABEL_MENU_ITEM = "[Media.AlbumLabelMenuItem]";
    public const string RES_COMMON_BY_DECADE_MENU_ITEM = "[Media.DecadeMenuItem]";
    public const string RES_COMMON_BY_VIDEO_PLAYCOUNT_MENU_ITEM = "[Media.VideoPlayCountMenuItem]";
    public const string RES_COMMON_BY_AUDIO_LANG_MENU_ITEM = "[Media.AudioLanguageMenuItem]";
    public const string RES_COMMON_BY_IMAGE_COUNTRY_MENU_ITEM = "[Media.ImageCountryMenuItem]";
    public const string RES_COMMON_BY_IMAGE_STATE_MENU_ITEM = "[Media.ImageStateMenuItem]";
    public const string RES_COMMON_BY_IMAGE_CITY_MENU_ITEM = "[Media.ImageCityMenuItem]";
    public const string RES_COMMON_BY_MOVIES_COLLECTION_MENU_ITEM = "[Media.MoviesCollectionMenuItem]";
    public const string RES_COMMON_BY_SERIES_NAME_MENU_ITEM = "[Media.SeriesNameMenuItem]";
    public const string RES_COMMON_BY_SERIES_SEASON_MENU_ITEM = "[Media.SeriesSeasonMenuItem]";
    public const string RES_COMMON_BY_SERIES_EPISODE_MENU_ITEM = "[Media.SeriesEpisodeMenuItem]";

    public const string RES_COMMON_SHOW_ALL_MENU_ITEM = "[Media.ShowAllMenuItem]";


    // Filter
    public const string RES_SIMPLE_SEARCH_FILTER_MENU_ITEM = "[Media.SimpleSearchFilterMenuItem]";

    public const string RES_BROWSE_MEDIA_NAVIGATION_NAVBAR_DISPLAY_LABEL = "[Media.BrowseMediaNavigationNavbarDisplayLabel]";
    public const string RES_LOCAL_MEDIA_NAVIGATION_NAVBAR_DISPLAY_LABEL = "[Media.LocalMediaNavigationNavbarDisplayLabel]";
    public const string RES_FILTER_ARTIST_NAVBAR_DISPLAY_LABEL = "[Media.FilterArtistNavbarDisplayLabel]";
    public const string RES_FILTER_COMPOSER_NAVBAR_DISPLAY_LABEL = "[Media.FilterComposerNavbarDisplayLabel]";
    public const string RES_FILTER_ALBUM_ARTIST_NAVBAR_DISPLAY_LABEL = "[Media.FilterAlbumArtistNavbarDisplayLabel]";
    public const string RES_FILTER_ALBUM_LABEL_NAVBAR_DISPLAY_LABEL = "[Media.FilterAlbumLabelNavbarDisplayLabel]";
    public const string RES_FILTER_ALBUM_NAVBAR_DISPLAY_LABEL = "[Media.FilterAlbumNavbarDisplayLabel]";
    public const string RES_FILTER_AUDIO_GENRE_NAVBAR_DISPLAY_LABEL = "[Media.FilterAudioGenreNavbarDisplayLabel]";
    public const string RES_FILTER_AUDIO_DISC_NUMBER_NAVBAR_DISPLAY_LABEL = "[Media.FilterAudioDiscNumberNavbarDisplayLabel]";
    public const string RES_FILTER_DECADE_NAVBAR_DISPLAY_LABEL = "[Media.FilterDecadeNavbarDisplayLabel]";
    public const string RES_FILTER_IMAGE_YEAR_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageYearNavbarDisplayLabel]";
    public const string RES_FILTER_VIDEO_YEAR_NAVBAR_DISPLAY_LABEL = "[Media.FilterVideoYearNavbarDisplayLabel]";
    public const string RES_FILTER_ACTOR_NAVBAR_DISPLAY_LABEL = "[Media.FilterActorNavbarDisplayLabel]";
    public const string RES_FILTER_CHARACTER_NAVBAR_DISPLAY_LABEL = "[Media.FilterCharacterNavbarDisplayLabel]";
    public const string RES_FILTER_DIRECTOR_NAVBAR_DISPLAY_LABEL = "[Media.FilterDirectorNavbarDisplayLabel]";
    public const string RES_FILTER_WRITER_NAVBAR_DISPLAY_LABEL = "[Media.FilterWriterNavbarDisplayLabel]";
    public const string RES_FILTER_COMPANY_NAVBAR_DISPLAY_LABEL = "[Media.FilterCompanyNavbarDisplayLabel]";
    public const string RES_FILTER_TV_NETWORK_NAVBAR_DISPLAY_LABEL = "[Media.FilterTvNetworkNavbarDisplayLabel]";
    public const string RES_FILTER_VIDEO_GENRE_NAVBAR_DISPLAY_LABEL = "[Media.FilterVideoGenreNavbarDisplayLabel]";
    public const string RES_FILTER_VIDEO_PLAYCOUNT_NAVBAR_DISPLAY_LABEL = "[Media.FilterVideoPlayCountDisplayLabel]";
    public const string RES_FILTER_IMAGE_SIZE_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageSizeNavbarDisplayLabel]";
    public const string RES_FILTER_AUDIO_ITEMS_NAVBAR_DISPLAY_LABEL = "[Media.FilterAudioItemsNavbarDisplayLabel]";
    public const string RES_FILTER_VIDEO_ITEMS_NAVBAR_DISPLAY_LABEL = "[Media.FilterVideoItemsNavbarDisplayLabel]";
    public const string RES_FILTER_IMAGE_ITEMS_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageItemsNavbarDisplayLabel]";
    public const string RES_FILTER_IMAGE_COUNTRY_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageCountryNavbarDisplayLabel]";
    public const string RES_FILTER_IMAGE_STATE_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageStateNavbarDisplayLabel]";
    public const string RES_FILTER_IMAGE_CITY_NAVBAR_DISPLAY_LABEL = "[Media.FilterImageCityNavbarDisplayLabel]";

    public const string RES_FILTER_AUDIO_LANG_NAVBAR_DISPLAY_LABEL = "[Media.FilterAudioLanguagesNavbarDisplayLabel]";
    public const string RES_FILTER_SYSTEM_NAVBAR_DISPLAY_LABEL = "[Media.FilterSystemNavbarDisplayLabel]";
    public const string RES_FILTER_SERIES_ITEMS_NAVBAR_DISPLAY_LABEL = "[Media.FilterSeriesItemsNavbarDisplayLabel]";
    public const string RES_FILTER_SERIES_SEASON_NAVBAR_DISPLAY_LABEL = "[Media.FilterSeriesSeasonNavbarDisplayLabel]";
    public const string RES_FILTER_MOVIES_NAVBAR_DISPLAY_LABEL = "[Media.FilterMoviesNavbarDisplayLabel]";
    public const string RES_FILTER_MOVIES_COLLECTION_NAVBAR_DISPLAY_LABEL = "[Media.FilterMoviesCollectionNavbarDisplayLabel]";


    // Sorting
    public const string RES_SWITCH_SORTING = "[Media.SwitchSortingMenuItem]";
    public const string RES_SORTING_BROWSE_DEFAULT = "[Media.SortingBrowseDefault]";
    public const string RES_SORT_BY_DATE = "[Media.SortByDate]";                         //no string defined

    // Grouping
    public const string RES_SWITCH_GROUPING = "[Media.SwitchGroupingMenuItem]";
    public const string RES_NO_GROUPING = "[Media.NoGrouping]";
    public const string RES_GROUP_BY_DATE = "[Media.GroupByDate]";
    public const string RES_GROUPING_BROWSE_DEFAULT = "[Media.GroupingBrowseDefault]";   //no string defined

    // Filter
    public const string RES_SWITCH_FILTER = "[Media.SwitchFilterMenuItem]";



    // Image and video size thresholds
    /// <summary>
    /// Smaller than this value in pixels in any direction is considered "small".
    /// </summary>
    public const int SMALL_SIZE_THRESHOLD = 640;

    /// <summary>
    /// Bigger than this value in pixels in any direction is considered "big".
    /// </summary>
    public const int BIG_SIZE_THRESHOLD = 1200;

    // Screens
    public const string SCREEN_BROWSE_MEDIA_NAVIGATION = "BrowseMediaNavigation";
    public const string SCREEN_LOCAL_MEDIA_NAVIGATION = "LocalMediaNavigation";
    public const string SCREEN_AUDIO_SHOW_ITEMS = "AudioShowItems";
    public const string SCREEN_AUDIO_FILTER_BY_ARTIST = "AudioFilterByArtist";
    public const string SCREEN_AUDIO_FILTER_BY_COMPOSER = "AudioFilterByComposer";
    public const string SCREEN_AUDIO_FILTER_BY_ALBUM_ARTIST = "AudioFilterByAlbumArtist";
    public const string SCREEN_AUDIO_FILTER_BY_ALBUM_LABEL = "AudioFilterByAlbumLabel";
    public const string SCREEN_AUDIO_FILTER_BY_ALBUM = "AudioFilterByAlbum";
    public const string SCREEN_AUDIO_FILTER_BY_GENRE = "AudioFilterByGenre";
    public const string SCREEN_AUDIO_FILTER_BY_DECADE = "AudioFilterByDecade";
    public const string SCREEN_AUDIO_FILTER_BY_DISC_NUMBER = "AudioFilterByDiscNumber";
    public const string SCREEN_AUDIO_FILTER_BY_SYSTEM = "AudioFilterBySystem";
    public const string SCREEN_AUDIO_SIMPLE_SEARCH = "AudioSimpleSearch";
    public const string SCREEN_VIDEOS_SHOW_ITEMS = "VideoShowItems";
    public const string SCREEN_VIDEOS_FILTER_BY_CHARACTER = "VideoFilterByCharacter";
    public const string SCREEN_VIDEOS_FILTER_BY_ACTOR = "VideoFilterByActor";
    public const string SCREEN_VIDEOS_FILTER_BY_DIRECTOR = "VideoFilterByDirector";
    public const string SCREEN_VIDEOS_FILTER_BY_WRITER = "VideoFilterByWriter";
    public const string SCREEN_VIDEOS_FILTER_BY_GENRE = "VideoFilterByGenre";
    public const string SCREEN_VIDEOS_FILTER_BY_YEAR = "VideoFilterByYear";
    public const string SCREEN_VIDEOS_FILTER_BY_SYSTEM = "VideoFilterBySystem";
    public const string SCREEN_VIDEOS_FILTER_BY_AUDIO_LANG = "VideoFilterByLanguage";
    public const string SCREEN_VIDEOS_FILTER_BY_PLAYCOUNT = "VideoFilterByPlayCount";
    public const string SCREEN_MOVIES_SHOW_ITEMS = "MoviesShowItems";
    public const string SCREEN_MOVIES_FILTER_BY_COLLECTION = "MovieFilterByCollection";
    public const string SCREEN_MOVIES_FILTER_BY_ACTOR = "MovieFilterByActor";
    public const string SCREEN_MOVIES_FILTER_BY_CHARACTER = "MovieFilterByCharacter";
    public const string SCREEN_MOVIES_FILTER_BY_DIRECTOR = "MovieFilterByDirector";
    public const string SCREEN_MOVIES_FILTER_BY_WRITER = "MovieFilterByWriter";
    public const string SCREEN_MOVIES_FILTER_BY_COMPANY = "MovieFilterByCompany";
    public const string SCREEN_MOVIES_FILTER_BY_GENRE = "MovieFilterByGenre";
    public const string SCREEN_SERIES_SHOW_ITEMS = "SeriesShowItems";
    public const string SCREEN_SERIES_FILTER_BY_NAME = "SeriesFilterByName";
    public const string SCREEN_SERIES_FILTER_BY_SEASON = "SeriesFilterBySeason";
    public const string SCREEN_SERIES_FILTER_BY_ACTOR = "SeriesFilterByActor";
    public const string SCREEN_SERIES_FILTER_BY_CHARACTER = "SeriesFilterByCharacter";
    public const string SCREEN_SERIES_FILTER_BY_COMPANY = "SeriesFilterByCompany";
    public const string SCREEN_SERIES_FILTER_BY_NETWORK = "SeriesFilterByTvNetwork";
    public const string SCREEN_SERIES_EPISODE_FILTER_BY_ACTOR = "SeriesEpisodeFilterByActor";
    public const string SCREEN_SERIES_EPISODE_FILTER_BY_CHARACTER = "SeriesEpisodeFilterByCharacter";
    public const string SCREEN_SERIES_EPISODE_FILTER_BY_DIRECTOR = "SeriesEpisodeFilterByDirector";
    public const string SCREEN_SERIES_EPISODE_FILTER_BY_WRITER = "SeriesEpisodeFilterByWriter";
    public const string SCREEN_SERIES_FILTER_BY_GENRE = "SeriesFilterByGenre";
    public const string SCREEN_VIDEOS_SIMPLE_SEARCH = "VideoSimpleSearch";
    public const string SCREEN_IMAGE_SHOW_ITEMS = "ImageShowItems";
    public const string SCREEN_IMAGE_FILTER_BY_YEAR = "ImageFilterByYear";
    public const string SCREEN_IMAGE_FILTER_BY_SIZE = "ImageFilterBySize";
    public const string SCREEN_IMAGE_FILTER_BY_SYSTEM = "ImageFilterBySystem";
    public const string SCREEN_IMAGE_FILTER_BY_COUNTRY = "ImageFilterByCountry";
    public const string SCREEN_IMAGE_FILTER_BY_STATE = "ImageFilterByState";
    public const string SCREEN_IMAGE_FILTER_BY_CITY = "ImageFilterByCity";

    public const string SCREEN_IMAGE_SIMPLE_SEARCH = "ImageSimpleSearch";

    public const string SCREEN_FULLSCREEN_AUDIO = "FullscreenContentAudio";
    public const string SCREEN_CURRENTLY_PLAYING_AUDIO = "CurrentlyPlayingAudio";

    public const string SCREEN_FULLSCREEN_VIDEO = "FullscreenContentVideo";
    public const string SCREEN_CURRENTLY_PLAYING_VIDEO = "CurrentlyPlayingVideo";
    public const string SCREEN_FULLSCREEN_IMAGE = "FullscreenContentImage";
    public const string SCREEN_CURRENTLY_PLAYING_IMAGE = "CurrentlyPlayingImage";
    public const string SCREEN_FULLSCREEN_DVD = "FullscreenContentDVD";
    public const string SCREEN_CURRENTLY_PLAYING_DVD = "CurrentlyPlayingDVD";

    public const string DIALOG_PLAY_MENU = "DialogPlayMenu";

    public const string DIALOG_MEDIAITEM_ACTION_MENU = "DialogMediaItemActionMenu";

    public const string DIALOG_CHOOSE_AV_TYPE = "DialogChooseAVType";

    public const string DIALOG_VIDEOCONTEXTMENU = "DialogVideoContextMenu";

    public const string DIALOG_ADD_TO_PLAYLIST_PROGRESS = "DialogAddToPlaylistProgress";

    public const string DIALOG_SWITCH_VIEW_MODE = "DialogSwitchViewMode";

    public const string DIALOG_SWITCH_SORTING = "DialogSwitchSorting";

    public const string DIALOG_SWITCH_GROUPING = "DialogSwitchGrouping";

    public const string DIALOG_SWITCH_FILTER = "DialogSwitchFilter";

    // Timespans
    public static TimeSpan TS_SEARCH_TEXT_TYPE = TimeSpan.FromMilliseconds(300);

    public static TimeSpan TS_VIDEO_INFO_TIMEOUT = TimeSpan.FromSeconds(5);

    public static TimeSpan TS_ADD_TO_PLAYLIST_UPDATE_DIALOG_THRESHOLD = TimeSpan.FromSeconds(3);

    public static TimeSpan TS_WAIT_FOR_PLAYLISTS_UPDATE = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Denotes the "infinite" timespan, used for <see cref="System.Threading.Timer.Change(System.TimeSpan,System.TimeSpan)"/>
    /// method, for example.
    /// </summary>
    public static readonly TimeSpan TS_INFINITE = new TimeSpan(0, 0, 0, 0, -1);

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
    public const string KEY_PLAYCOUNT = "PlayCount";
    public const string KEY_FORMAT = "Format";
    public const string KEY_LANGUAGE = "Language";
    public const string KEY_DEFAULT = "Default";
    public const string KEY_FORCED = "Forced";
    public const string KEY_PARTS = "Parts";
    public const string KEY_SET = "Set";
    public const string KEY_SET_NAME = "SetName";
    public const string KEY_BITRATE = "BitRate";
    public const string KEY_ASPECTRATIO = "AspctRatio";
    public const string KEY_FPS = "FPS";
    public const string KEY_CHANNELS = "Channels";
    public const string KEY_SAMPLERATE = "SampleRate";
    public const string KEY_ID = "Id";
    public const string KEY_VIRTUAL = "Virtual";

    public const string KEY_MEDIA_ITEM = "MediaItem";
    public const string KEY_MEDIA_ITEM_ACTION = "MediaItemAction";
    public const string KEY_NUM_ITEMS = "NumItems";
    public const string KEY_DURATION = "Duration";
    public const string KEY_WATCH_PERCENTAGE = "WatchPercentage";
    public const string KEY_AUDIO_ENCODING = "AudioEncoding";
    public const string KEY_VIDEO_ENCODING = "VideoEncoding";

    public const string KEY_ALBUM = "Album";
    public const string KEY_NUM_TRACKS = "NumTracks";
    public const string KEY_TOTAL_TRACKS = "TotalTracks";
    public const string KEY_AVAIL_TRACKS = "AvailTracks";

    public const string KEY_ACTOR = "Actor";
    public const string KEY_STORY_PLOT = "StoryPlot";
    public const string KEY_DESCRIPTION = "Description";

    public const string KEY_SERIES_NAME = "SeriesName";
    public const string KEY_SERIES_SEASON = "SeriesSeason";
    public const string KEY_SERIES_EPISODE_NUM = "SeriesEpisodeNum";
    public const string KEY_SERIES_DVD_EPISODE_NUM = "SeriesDVDEpisodeNum";
    public const string KEY_SERIES_EPISODE_NAME = "SeriesEpisodeName";
    public const string KEY_NUM_EPISODES = "NumEpisodes";
    public const string KEY_NUM_SEASONS = "NumSeasons";
    public const string KEY_TOTAL_EPISODES = "TotalEpisodes";
    public const string KEY_TOTAL_SEASONS = "TotalSeasons";
    public const string KEY_AVAIL_EPISODES = "AvailEpisodes";
    public const string KEY_AVAIL_SEASONS = "AvailSeasons";

    public const string KEY_MOVIE_COLLECTION = "MovieCollection";
    public const string KEY_MOVIE_YEAR = "MovieYear";
    public const string KEY_NUM_MOVIES = "NumMovies";
    public const string KEY_TOTAL_MOVIES = "TotalMovies";
    public const string KEY_AVAIL_MOVIES = "AvailMovies";

    public const string KEY_IS_CURRENT_ITEM = "IsCurrentItem";

    public const string KEY_NUMBERSTR = "NumberStr";
    public const string KEY_INDEX = "Playlist-Index";

    public const string KEY_IS_DOWN_BUTTON_FOCUSED = "IsDownButtonFocused";
    public const string KEY_IS_UP_BUTTON_FOCUSED = "IsUpButtonFocused";

    public const string KEY_PLAYLIST_TYPES = "PlaylistsType";
    public const string KEY_PLAYLIST_LOCATION = "PlaylistLocation";
    public const string KEY_PLAYLIST_AV_TYPE = "PlaylistAVType";
    public const string KEY_PLAYLIST_NUM_ITEMS = "PlaylistNumItems";
    public const string KEY_PLAYLIST_DATA = "PlaylistData";
    public const string KEY_MESSAGE = "Message";

    public const string KEY_DISABLE_EDIT_MODE = "DisableEditMode";

    public const string KEY_PLAYMODE = "PlayMode";
    public const string KEY_REPEATMODE = "RepeatMode";

    public const string KEY_LAYOUT_TYPE = "LayoutType";
    public const string KEY_LAYOUT_SIZE = "LayoutSize";

    public const string KEY_SORTING = "Sorting";
    public const string KEY_GROUPING = "Grouping";

    public const string KEY_FILTER = "Filter";

    // Custom constants used for NavigationData
    public const string USE_BROWSE_MODE = "{UseBrowseMode}";

    // Keys for workflow state variables
    public const string KEY_NAVIGATION_MODE = "MediaNavigationModel: NAVIGATION_MODE";
    public const string KEY_NAVIGATION_DATA = "MediaNavigationModel: NAVIGATION_DATA";

    public static float DEFAULT_PIP_HEIGHT = 108;
    public static float DEFAULT_PIP_WIDTH = 192;

    public static int ADD_TO_PLAYLIST_UPDATE_INTERVAL = 50;

    public static readonly Guid[] NECESSARY_BROWSING_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_LOCAL_BROWSING_MIAS = new Guid[]
      {
          AudioAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
          ImageAspect.ASPECT_ID,
          VideoStreamAspect.ASPECT_ID,
          VideoAudioStreamAspect.ASPECT_ID,
          SubtitleAspect.ASPECT_ID,
          ImporterAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_MEDIA_LIBRARY_BROWSING_MIAS = new Guid[]
      {
          MovieAspect.ASPECT_ID,
          EpisodeAspect.ASPECT_ID,
          AudioAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
          ImageAspect.ASPECT_ID,
          VideoStreamAspect.ASPECT_ID,
          VideoAudioStreamAspect.ASPECT_ID,
          SubtitleAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_MOVIE_GENRE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
          MovieAspect.ASPECT_ID
      };

    public static readonly Guid[] NECESSARY_SERIES_GENRE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
          SeriesAspect.ASPECT_ID
      };

    public static readonly Guid[] NECESSARY_AUDIO_GENRE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
          AudioAspect.ASPECT_ID
      };

    public static readonly Guid[] NECESSARY_VIDEO_GENRE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID
      };

    public static readonly Guid[] NECESSARY_VIDEO_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
          ImporterAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_VIDEO_MIAS = new Guid[]
      {
          VideoStreamAspect.ASPECT_ID,
          VideoAudioStreamAspect.ASPECT_ID,
          SubtitleAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_SERIES_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          SeriesAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_SERIES_MIAS = new Guid[]
      {
          GenreAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_SEASON_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          SeasonAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_SEASON_MIAS = null;

    public static readonly Guid[] NECESSARY_EPISODE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
          EpisodeAspect.ASPECT_ID,
          ImporterAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_EPISODE_MIAS = new Guid[]
      {
          VideoStreamAspect.ASPECT_ID,
          VideoAudioStreamAspect.ASPECT_ID,
          SubtitleAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_MOVIE_COLLECTION_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          MovieCollectionAspect.ASPECT_ID
      };

    public static readonly Guid[] OPTIONAL_MOVIE_COLLECTION_MIAS = null;

    public static readonly Guid[] NECESSARY_MOVIES_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
          MovieAspect.ASPECT_ID,
          ImporterAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_MOVIES_MIAS = new Guid[]
      {
          VideoStreamAspect.ASPECT_ID,
          VideoAudioStreamAspect.ASPECT_ID,
          SubtitleAspect.ASPECT_ID,
          GenreAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_ALBUM_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          AudioAlbumAspect.ASPECT_ID
      };

    public static readonly Guid[] OPTIONAL_ALBUM_MIAS = new Guid[]
      {
          GenreAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_AUDIO_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          AudioAspect.ASPECT_ID,
          ImporterAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_AUDIO_MIAS = new Guid[]
      {
          GenreAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_IMAGE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          ImageAspect.ASPECT_ID,
          ImporterAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_IMAGE_MIAS = null;

    public static readonly Guid[] NECESSARY_PERSON_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          PersonAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_PERSON_MIAS = null;

    public static readonly Guid[] NECESSARY_CHARACTER_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          CharacterAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_CHARACTER_MIAS = null;

    public static readonly Guid[] NECESSARY_COMPANY_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          CompanyAspect.ASPECT_ID,
      };

    public static readonly Guid[] OPTIONAL_COMPANY_MIAS = null;

    public static readonly string MEDIA_SKIN_SETTINGS_REGISTRATION_PATH = "/Media/SkinSettings";
    public static readonly string MEDIA_SKIN_SETTINGS_REGISTRATION_OPTIONAL_TYPES_PATH = "OptionalMIATypes";

    public const string FILTERS_WORKFLOW_CATEGORY = "a-Filters";

    public const int MAX_NUM_ITEMS_VISIBLE = 5000;
  }
}
