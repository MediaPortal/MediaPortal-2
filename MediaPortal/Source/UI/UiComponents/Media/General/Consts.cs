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
using MediaPortal.Core.MediaManagement.DefaultItemAspects;

namespace MediaPortal.UiComponents.Media.General
{
  public class Consts
  {
    // Localization resource identifiers
    public const string PLAY_AUDIO_ITEM_RESOURCE = "[Media.PlayAudioItem]";
    public const string ENQUEUE_AUDIO_ITEM_RESOURCE = "[Media.EnqueueAudioItem]";
    public const string MUTE_VIDEO_PLAY_AUDIO_ITEM_RESOURCE = "[Media.MuteVideoAndPlayAudioItem]";

    public const string PLAY_VIDEO_ITEM_RESOURCE = "[Media.PlayVideoItem]";
    public const string ENQUEUE_VIDEO_ITEM_RESOURCE = "[Media.EnqueueVideoItem]";
    public const string PLAY_VIDEO_ITEM_MUTED_CONCURRENT_AUDIO_RESOURCE = "[Media.PlayVideoItemMutedConcurrentAudio]";
    public const string PLAY_VIDEO_ITEM_PIP_RESOURCE = "[Media.PlayVideoItemPiP]";

    public const string VIDEO_PLAYER_CONTEXT_NAME_RESOURCE = "[Media.VideoPlayerContextName]";
    public const string PICTURE_PLAYER_CONTEXT_NAME_RESOURCE = "[Media.PicturePlayerContextName]";
    public const string AUDIO_PLAYER_CONTEXT_NAME_RESOURCE = "[Media.AudioPlayerContextName]";

    public const string SYSTEM_INFORMATION_RESOURCE = "[System.Information]";
    public const string CANNOT_PLAY_ITEM_RESOURCE = "[Media.CannotPlayItemDialogText]";

    public const string LOCAL_MEDIA_ROOT_VIEW_NAME_RESOURCE = "[Media.LocalMediaRootViewName]";
    public const string MUSIC_VIEW_NAME_RESOURCE = "[Media.MusicRootViewName]";
    public const string MOVIES_VIEW_NAME_RESOURCE = "[Media.MoviesRootViewName]";
    public const string PICTURES_VIEW_NAME_RESOURCE = "[Media.PicturesRootViewName]";
    public const string SIMPLE_SEARCH_VIEW_NAME_RESOURCE = "[Media.SimpleSearchViewName]";

    public const string FILTER_BY_ARTIST_MENU_ITEM_RES = "[Media.FilterByArtistMenuItem]";
    public const string FILTER_BY_ALBUM_MENU_ITEM_RES = "[Media.FilterByAlbumMenuItem]";
    public const string FILTER_BY_MUSIC_GENRE_MENU_ITEM_RES = "[Media.FilterByMusicGenreMenuItem]";
    public const string FILTER_BY_DECADE_MENU_ITEM_RES = "[Media.FilterByDecadeMenuItem]";
    public const string FILTER_BY_PICTURE_YEAR_MENU_ITEM_RES = "[Media.FilterByPictureYearMenuItem]";
    public const string FILTER_BY_MOVIE_YEAR_MENU_ITEM_RES = "[Media.FilterByMovieYearMenuItem]";
    public const string FILTER_BY_ACTOR_MENU_ITEM_RES = "[Media.FilterByActorMenuItem]";
    public const string FILTER_BY_MOVIE_GENRE_MENU_ITEM_RES = "[Media.FilterByMovieGenreMenuItem]";
    public const string FILTER_BY_PICTURE_SIZE_MENU_ITEM_RES = "[Media.FilterByPictureSizeMenuItem]";
    public const string SIMPLE_SEARCH_FILTER_MENU_ITEM_RES = "[Media.SimpleSearchFilterMenuItem]";
    public const string SHOW_ALL_MUSIC_ITEMS_MENU_ITEM_RES = "[Media.ShowAllMusicItemsMenuItem]";
    public const string SHOW_ALL_MOVIE_ITEMS_MENU_ITEM_RES = "[Media.ShowAllMovieItemsMenuItem]";
    public const string SHOW_ALL_PICTURE_ITEMS_MENU_ITEM_RES = "[Media.ShowAllPictureItemsMenuItem]";

    public const string LOCAL_MEDIA_NAVIGATION_NAVBAR_DISPLAY_LABEL_RES = "[Media.LocalMediaNavigationNavbarDisplayLabel]";
    public const string FILTER_ARTIST_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterArtistNavbarDisplayLabel]";
    public const string FILTER_ALBUM_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterAlbumNavbarDisplayLabel]";
    public const string FILTER_MUSIC_GENRE_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterMusicGenreNavbarDisplayLabel]";
    public const string FILTER_DECADE_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterDecadeNavbarDisplayLabel]";
    public const string FILTER_PICTURE_YEAR_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterPictureYearNavbarDisplayLabel]";
    public const string FILTER_MOVIE_YEAR_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterMovieYearNavbarDisplayLabel]";
    public const string FILTER_ACTOR_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterActorNavbarDisplayLabel]";
    public const string FILTER_MOVIE_GENRE_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterMovieGenreNavbarDisplayLabel]";
    public const string FILTER_PICTURE_SIZE_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterPictureSizeNavbarDisplayLabel]";
    public const string FILTER_MUSIC_ITEMS_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterMusicItemsNavbarDisplayLabel]";
    public const string FILTER_MOVIE_ITEMS_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterMovieItemsNavbarDisplayLabel]";
    public const string FILTER_PICTURE_ITEMS_NAVBAR_DISPLAY_LABEL_RES = "[Media.FilterPictureItemsNavbarDisplayLabel]";

    public const string MORE_THAN_MAX_ITEMS_HINT_RES = "[Media.MoreThanMaxItemsHint]";
    public const string MORE_THAN_MAX_ITEMS_SEARCH_RESULT_HINT_RES = "[Media.MoreThanMaxItemsSearchResultHint]";
    public const string LIST_BEING_BUILT_HINT_RES = "[Media.ListIsBeingBuiltHint]";
    public const string SEARCH_RESULT_BEING_BUILT_HINT_RES = "[Media.SearchResultIsBeingBuiltHint]";
    public const string VIEW_EMPTY_RES = "[Media.ViewEmpty]";

    public const string NO_ITEMS_RES = "[Media.NoItems]";
    public const string ONE_ITEM_RES = "[Media.OneItem]";
    public const string N_ITEMS_RES = "[Media.NItems]";

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

    // Timespans
    public static readonly TimeSpan SEARCH_TEXT_TYPE_TIMESPAN = new TimeSpan(0, 0, 0, 0, 300);

    /// <summary>
    /// Denotes the "infinite" timespan, used for <see cref="System.Threading.Timer.Change(System.TimeSpan,System.TimeSpan)"/>
    /// method, for example.
    /// </summary>
    public readonly static TimeSpan INFINITE_TIMESPAN = new TimeSpan(0, 0, 0, 0, -1);

    // Accessor keys for GUI communication
    public const string NAME_KEY = "Name";
    public const string MEDIA_ITEM_KEY = "MediaItem";
    public const string NUM_ITEMS_KEY = "NumItems";
    public const string LENGTH_KEY = "Length";

    public static readonly Guid[] NECESSARY_MOVIE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          VideoAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_MUSIC_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          AudioAspect.ASPECT_ID,
      };

    public static readonly Guid[] NECESSARY_PICTURE_MIAS = new Guid[]
      {
          ProviderResourceAspect.ASPECT_ID,
          MediaAspect.ASPECT_ID,
          PictureAspect.ASPECT_ID,
      };

    public const int MAX_NUM_ITEMS_VISIBLE = 500;
  }
}