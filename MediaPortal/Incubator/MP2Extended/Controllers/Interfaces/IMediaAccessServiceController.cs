#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;

namespace MediaPortal.Plugins.MP2Extended.Controllers.Interfaces
{
  public interface IMediaAccessServiceController
  {
    #region General

    Task<WebMediaServiceDescription> GetServiceDescription();
    Task<WebBoolResult> TestConnection();
    Task<WebMediaItem> GetMediaItem(WebMediaType type, string id);
    //Task<IList<WebSearchResult>> Search(string text);
    //Task<IList<WebSearchResult>> SearchResultsByRange(string text, int start, int end);
    Task<WebDictionary<string>> GetExternalMediaInfo(WebMediaType type, string id);
    Task<IList<WebDiskSpaceInformation>> GetLocalDiskInformation(string filter = null);

    #endregion

    #region Movies

    Task<WebIntResult> GetMovieCount();
    Task<IList<WebMovieBasic>> GetMoviesBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMovieDetailed>> GetMoviesDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMovieBasic>> GetMoviesBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMovieDetailed>> GetMoviesDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebMovieBasic> GetMovieBasicById(string id);
    Task<WebMovieDetailed> GetMovieDetailedById(string id);
    Task<IList<WebGenre>> GetMovieGenres(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebGenre>> GetMovieGenresByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebIntResult> GetMovieGenresCount(string filter = null);
    //Task<IList<WebCategory>> GetMovieCategories(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebActor>> GetMovieActors(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebActor>> GetMovieActorsByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebIntResult> GetMovieActorCount(string filter = null);

    #endregion

    #region Music

    Task<WebIntResult> GetMusicAlbumCount(string filter = null);
    Task<IList<WebMusicAlbumBasic>> GetMusicAlbumsBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicAlbumBasic>> GetMusicAlbumsBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicAlbumBasic>> GetMusicAlbumsBasicForArtist(string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebMusicAlbumBasic> GetMusicAlbumBasicById(string id);
    Task<WebIntResult> GetMusicArtistCount(string filter = null);
    Task<IList<WebMusicArtistBasic>> GetMusicArtistsBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicArtistBasic>> GetMusicArtistsBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebMusicArtistBasic> GetMusicArtistBasicById(string id);
    Task<IList<WebMusicArtistDetailed>> GetMusicArtistsDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicArtistDetailed>> GetMusicArtistsDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebMusicArtistDetailed> GetMusicArtistDetailedById(string id);
    Task<WebIntResult> GetMusicTrackCount(string filter = null);
    Task<IList<WebMusicTrackBasic>> GetMusicTracksBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicTrackDetailed>> GetMusicTracksDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicTrackBasic>> GetMusicTracksBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicTrackDetailed>> GetMusicTracksDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicTrackBasic>> GetMusicTracksBasicForAlbum(string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicTrackBasic>> GetMusicTracksBasicForArtist(string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicTrackDetailed>> GetMusicTracksDetailedForAlbum(string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebMusicTrackDetailed>> GetMusicTracksDetailedForArtist(string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebMusicTrackBasic> GetMusicTrackBasicById(string id);
    Task<WebMusicTrackDetailed> GetMusicTrackDetailedById(string id);
    Task<IList<WebGenre>> GetMusicGenres(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebGenre>> GetMusicGenresByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebIntResult> GetMusicGenresCount(string filter = null);
    //Task<IList<WebCategory>> GetMusicCategories(string filter = null);

    #endregion

    #region OnlineVideos

    Task<IList<WebOnlineVideosVideo>> GetOnlineVideosCategoryVideos(string id);
    Task<IList<WebOnlineVideosGlobalSite>> GetOnlineVideosGlobalSites(string filter, WebSortField? sort, WebSortOrder? order);
    Task<IList<WebOnlineVideosSiteCategory>> GetOnlineVideosSiteCategories(string id);
    Task<IList<WebOnlineVideosSite>> GetOnlineVideosSites(string filter, WebSortField? sort, WebSortOrder? order);
    Task<IList<WebOnlineVideosSiteSetting>> GetOnlineVideosSiteSettings(string id);
    Task<IList<WebOnlineVideosSiteCategory>> GetOnlineVideosSubCategories(string id);
    Task<IList<string>> GetOnlineVideosVideoUrls(string id);
    Task<WebBoolResult> SetOnlineVideosSiteSetting(string siteId, string property, string value);

    #endregion

    #region Pictures

    Task<WebIntResult> GetPictureCount();
    Task<IList<WebPictureBasic>> GetPicturesBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebPictureBasic>> GetPicturesBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebPictureDetailed>> GetPicturesDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebPictureDetailed>> GetPicturesDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebPictureBasic> GetPictureBasicById(string id);
    Task<WebPictureDetailed> GetPictureDetailedById(string id);
    //Task<IList<WebCategory>> GetPictureCategories();
    //Task<IList<WebCategory>> GetPictureSubCategories(string id, string filter = null);
    Task<IList<WebPictureBasic>> GetPicturesBasicByCategory(string id);
    Task<IList<WebPictureDetailed>> GetPicturesDetailedByCategory(string id);

    #endregion

    #region TVShows

    Task<WebIntResult> GetTVEpisodeCount();
    Task<WebIntResult> GetTVEpisodeCountForTVShow(string id);
    Task<WebIntResult> GetTVEpisodeCountForSeason(string id);
    Task<WebIntResult> GetTVShowCount(string filter = null);
    Task<WebIntResult> GetTVSeasonCountForTVShow(string id);
    Task<IList<WebTVShowBasic>> GetTVShowsBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVShowDetailed>> GetTVShowsDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVShowBasic>> GetTVShowsBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVShowDetailed>> GetTVShowsDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebTVShowBasic> GetTVShowBasicById(string id);
    Task<WebTVShowDetailed> GetTVShowDetailedById(string id);
    Task<IList<WebTVSeasonBasic>> GetTVSeasonsBasicForTVShow(string id, string filter = null, WebSortField? sort = WebSortField.TVSeasonNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVSeasonDetailed>> GetTVSeasonsDetailedForTVShow(string id, WebSortField? sort = WebSortField.TVSeasonNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebTVSeasonBasic> GetTVSeasonBasicById(string id);
    Task<WebTVSeasonDetailed> GetTVSeasonDetailedById(string id);
    Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasic(WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailed(string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasicByRange(int start, int end, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailedByRange(int start, int end, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasicForTVShow(string id, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailedForTVShow(string id, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasicForTVShowByRange(string id, int start, int end, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailedForTVShowByRange(string id, int start, int end, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasicForSeason(string id, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailedForSeason(string id, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebTVEpisodeBasic> GetTVEpisodeBasicById(string id);
    Task<WebTVEpisodeDetailed> GetTVEpisodeDetailedById(string id);
    //Task<IList<WebCategory>> GetTVShowCategories(string filter = null);
    Task<IList<WebGenre>> GetTVShowGenres(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebGenre>> GetTVShowGenresByRange(int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebIntResult> GetTVShowGenresCount();
    Task<IList<WebActor>> GetTVShowActors(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebActor>> GetTVShowActorsByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebIntResult> GetTVShowActorCount(string filter = null);

    #endregion

    #region Filesystem

    Task<WebIntResult> GetFileSystemDriveCount();
    Task<IList<WebDriveBasic>> GetFileSystemDrives(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebDriveBasic>> GetFileSystemDrivesByRange(int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebFolderBasic>> GetFileSystemFolders(string id, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebFolderBasic>> GetFileSystemFoldersByRange(string id, int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebFileBasic>> GetFileSystemFiles(string id, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebFileBasic>> GetFileSystemFilesByRange(string id, int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebFilesystemItem>> GetFileSystemFilesAndFolders(string id, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebFilesystemItem>> GetFileSystemFilesAndFoldersByRange(string id, int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebIntResult> GetFileSystemFilesAndFoldersCount(string id);
    Task<WebIntResult> GetFileSystemFilesCount(string id);
    Task<WebIntResult> GetFileSystemFoldersCount(string id);
    Task<WebDriveBasic> GetFileSystemDriveBasicById(string id);
    Task<WebFolderBasic> GetFileSystemFolderBasicById(string id);
    Task<WebFileBasic> GetFileSystemFileBasicById(string id);

    #endregion

    #region Files

    Task<IList<WebArtwork>> GetArtwork(WebMediaType type, string id);
    Task<IList<string>> GetPathList(WebMediaType mediatype, WebFileType filetype, string id);
    Task<WebFileInfo> GetFileInfo(WebMediaType mediatype, WebFileType filetype, string id, int offset);
    Task<WebBoolResult> IsLocalFile(WebMediaType mediatype, WebFileType filetype, string id, int offset);
    Task<Stream> RetrieveFile(WebMediaType mediatype, WebFileType filetype, string id, int offset);

    #endregion

    #region Playlist

    Task<IList<WebPlaylist>> GetPlaylists();
    Task<IList<WebPlaylistItem>> GetPlaylistItems(string playlistId, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<WebPlaylistItem>> GetPlaylistItemsByRange(string playlistId, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebIntResult> GetPlaylistItemsCount(string playlistId, string filter = null);
    Task<WebBoolResult> AddPlaylistItem(string playlistId, WebMediaType type, string id, int? position);
    Task<WebBoolResult> ClearAndAddPlaylistItems(string playlistId, WebMediaType type, int? position, List<string> ids);
    Task<WebBoolResult> AddPlaylistItems(string playlistId, WebMediaType type, int? position, List<string> ids);
    Task<WebBoolResult> RemovePlaylistItem(string playlistId, int position);
    Task<WebBoolResult> RemovePlaylistItems(string playlistId, string positions);
    Task<WebBoolResult> MovePlaylistItem(string playlistId, int oldPosition, int newPosition);
    Task<WebStringResult> CreatePlaylist(string playlistName);
    Task<WebBoolResult> DeletePlaylist(string playlistId);

    #endregion

    #region Filters

    Task<WebIntResult> GetFilterValuesCount(WebMediaType mediaType, string filterField, string op = null, int? limit = null);
    Task<IList<string>> GetFilterValues(WebMediaType mediaType, string filterField, string op = null, int? limit = null, WebSortOrder? order = WebSortOrder.Asc);
    Task<IList<string>> GetFilterValuesByRange(int start, int end, WebMediaType mediaType, string filterField, string op = null, int? limit = null, WebSortOrder? order = WebSortOrder.Asc);
    Task<WebStringResult> CreateFilterString(string field, string op, string value, string conjunction);
    Task<IList<WebFilterOperator>> GetFilterOperators();

    #endregion
  }
}
