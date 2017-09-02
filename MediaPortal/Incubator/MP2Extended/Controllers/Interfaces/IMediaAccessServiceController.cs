using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Web;
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

    WebMediaServiceDescription GetServiceDescription();


    WebBoolResult TestConnection();


    WebMediaItem GetMediaItem(WebMediaType type, Guid id);


    //IList<WebSearchResult> Search(string text);


    //IList<WebSearchResult> SearchResultsByRange(string text, int start, int end);


    WebDictionary<string> GetExternalMediaInfo(WebMediaType type, Guid id);


    IList<WebDiskSpaceInformation> GetLocalDiskInformation(string filter = null);

    #endregion

    #region Movies

    WebIntResult GetMovieCount();


    IList<WebMovieBasic> GetMoviesBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMovieDetailed> GetMoviesDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMovieBasic> GetMoviesBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMovieDetailed> GetMoviesDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMovieBasic GetMovieBasicById(Guid id);


    WebMovieDetailed GetMovieDetailedById(Guid id);


    IList<WebGenre> GetMovieGenres(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebGenre> GetMovieGenresByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetMovieGenresCount(string filter = null);


    IList<WebCategory> GetMovieCategories(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebActor> GetMovieActors(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebActor> GetMovieActorsByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetMovieActorCount(string filter = null);

    #endregion

    #region Music

    WebIntResult GetMusicAlbumCount(string filter = null);


    IList<WebMusicAlbumBasic> GetMusicAlbumsBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicAlbumBasic> GetMusicAlbumsBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicAlbumBasic> GetMusicAlbumsBasicForArtist(Guid id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMusicAlbumBasic GetMusicAlbumBasicById(Guid id);


    WebIntResult GetMusicArtistCount(string filter = null);


    IList<WebMusicArtistBasic> GetMusicArtistsBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicArtistBasic> GetMusicArtistsBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMusicArtistBasic GetMusicArtistBasicById(Guid id);


    IList<WebMusicArtistDetailed> GetMusicArtistsDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicArtistDetailed> GetMusicArtistsDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMusicArtistDetailed GetMusicArtistDetailedById(Guid id);


    WebIntResult GetMusicTrackCount(string filter = null);


    IList<WebMusicTrackBasic> GetMusicTracksBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackDetailed> GetMusicTracksDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackBasic> GetMusicTracksBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackDetailed> GetMusicTracksDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackBasic> GetMusicTracksBasicForAlbum(Guid id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackBasic> GetMusicTracksBasicForArtist(Guid id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackDetailed> GetMusicTracksDetailedForAlbum(Guid id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackDetailed> GetMusicTracksDetailedForArtist(Guid id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMusicTrackBasic GetMusicTrackBasicById(Guid id);


    WebMusicTrackDetailed GetMusicTrackDetailedById(Guid id);


    IList<WebGenre> GetMusicGenres(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebGenre> GetMusicGenresByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetMusicGenresCount(string filter = null);


    IList<WebCategory> GetMusicCategories(string filter = null);

    #endregion

    #region OnlineVideos

    List<WebOnlineVideosVideo> GetOnlineVideosCategoryVideos(string id);

    List<WebOnlineVideosGlobalSite> GetOnlineVideosGlobalSites(string filter, WebSortField? sort, WebSortOrder? order);

    List<WebOnlineVideosSiteCategory> GetOnlineVideosSiteCategories(string id);

    List<WebOnlineVideosSite> GetOnlineVideosSites(string filter, WebSortField? sort, WebSortOrder? order);

    List<WebOnlineVideosSiteSetting> GetOnlineVideosSiteSettings(string id);

    List<WebOnlineVideosSiteCategory> GetOnlineVideosSubCategories(string id);

    List<string> GetOnlineVideosVideoUrls(string id);

    WebBoolResult SetOnlineVideosSiteSetting(string siteId, string property, string value);

    #endregion

    #region Pictures

    WebIntResult GetPictureCount();


    IList<WebPictureBasic> GetPicturesBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebPictureBasic> GetPicturesBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebPictureDetailed> GetPicturesDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebPictureDetailed> GetPicturesDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebPictureBasic GetPictureBasicById(Guid id);


    WebPictureDetailed GetPictureDetailedById(Guid id);


    IList<WebCategory> GetPictureCategories();


    IList<WebCategory> GetPictureSubCategories(Guid id, string filter = null);


    IList<WebPictureBasic> GetPicturesBasicByCategory(string id);


    IList<WebPictureDetailed> GetPicturesDetailedByCategory(Guid id, string filter = null);

    #endregion

    #region TVShows

    WebIntResult GetTVEpisodeCount();


    WebIntResult GetTVEpisodeCountForTVShow(Guid id);


    WebIntResult GetTVEpisodeCountForSeason(Guid id);


    WebIntResult GetTVShowCount(string filter = null);


    WebIntResult GetTVSeasonCountForTVShow(Guid id);


    IList<WebTVShowBasic> GetTVShowsBasic(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVShowDetailed> GetTVShowsDetailed(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVShowBasic> GetTVShowsBasicByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVShowDetailed> GetTVShowsDetailedByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebTVShowBasic GetTVShowBasicById(Guid id);


    WebTVShowDetailed GetTVShowDetailedById(Guid id);


    IList<WebTVSeasonBasic> GetTVSeasonsBasicForTVShow(Guid id, string filter = null, WebSortField? sort = WebSortField.TVSeasonNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVSeasonDetailed> GetTVSeasonsDetailedForTVShow(Guid id, WebSortField? sort = WebSortField.TVSeasonNumber, WebSortOrder? order = WebSortOrder.Asc);


    WebTVSeasonBasic GetTVSeasonBasicById(Guid id);


    WebTVSeasonDetailed GetTVSeasonDetailedById(Guid id);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasic(WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailed(string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasicByRange(int start, int end, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedByRange(int start, int end, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShow(Guid id, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShow(Guid id, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShowByRange(Guid id, int start, int end, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShowByRange(Guid id, int start, int end, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasicForSeason(Guid id, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForSeason(Guid id, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    WebTVEpisodeBasic GetTVEpisodeBasicById(Guid id);


    WebTVEpisodeDetailed GetTVEpisodeDetailedById(Guid id);


    IList<WebCategory> GetTVShowCategories(string filter = null);


    IList<WebGenre> GetTVShowGenres(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebGenre> GetTVShowGenresByRange(int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetTVShowGenresCount();


    IList<WebActor> GetTVShowActors(string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebActor> GetTVShowActorsByRange(int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetTVShowActorCount(string filter = null);

    #endregion

    #region Filesystem

    WebIntResult GetFileSystemDriveCount();


    IList<WebDriveBasic> GetFileSystemDrives(WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebDriveBasic> GetFileSystemDrivesByRange(int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFolderBasic> GetFileSystemFolders(string id, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFolderBasic> GetFileSystemFoldersByRange(string id, int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFileBasic> GetFileSystemFiles(string id, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFileBasic> GetFileSystemFilesByRange(string id, int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFilesystemItem> GetFileSystemFilesAndFolders(string id, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFilesystemItem> GetFileSystemFilesAndFoldersByRange(string id, int start, int end, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetFileSystemFilesAndFoldersCount(string id);


    WebIntResult GetFileSystemFilesCount(string id);


    WebIntResult GetFileSystemFoldersCount(string id);


    WebDriveBasic GetFileSystemDriveBasicById(string id);


    WebFolderBasic GetFileSystemFolderBasicById(string id);


    WebFileBasic GetFileSystemFileBasicById(string id);

    #endregion

    #region Files

    IList<WebArtwork> GetArtwork(WebMediaType type, Guid id);


    IList<string> GetPathList(WebMediaType mediatype, WebFileType filetype, Guid id);


    WebFileInfo GetFileInfo(WebMediaType mediatype, WebFileType filetype, Guid id, int offset);


    WebBoolResult IsLocalFile(WebMediaType mediatype, WebFileType filetype, Guid id, int offset);


    [WebGet(BodyStyle = WebMessageBodyStyle.Bare)]
    Stream RetrieveFile(WebMediaType mediatype, WebFileType filetype, Guid id, int offset);

    #endregion

    #region Playlist

    IList<WebPlaylist> GetPlaylists();


    IList<WebPlaylistItem> GetPlaylistItems(Guid playlistId, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebPlaylistItem> GetPlaylistItemsByRange(Guid playlistId, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetPlaylistItemsCount(Guid playlistId, string filter = null);


    WebBoolResult AddPlaylistItem(Guid playlistId, WebMediaType type, Guid id, int? position);


    WebBoolResult ClearAndAddPlaylistItems(Guid playlistId, WebMediaType type, int? position, List<Guid> ids);


    WebBoolResult AddPlaylistItems(Guid playlistId, WebMediaType type, int? position, List<Guid> ids);


    WebBoolResult RemovePlaylistItem(Guid playlistId, int position);


    WebBoolResult RemovePlaylistItems(Guid playlistId, string positions);


    WebBoolResult MovePlaylistItem(Guid playlistId, int oldPosition, int newPosition);


    WebStringResult CreatePlaylist(string playlistName);


    WebBoolResult DeletePlaylist(Guid playlistId);

    #endregion

    #region Filters

    WebIntResult GetFilterValuesCount(WebMediaType mediaType, string filterField, string op = null, int? limit = null);


    IList<string> GetFilterValues(WebMediaType mediaType, string filterField, string op = null, int? limit = null, WebSortOrder? order = WebSortOrder.Asc);


    IList<string> GetFilterValuesByRange(int start, int end, WebMediaType mediaType, string filterField, string op = null, int? limit = null, WebSortOrder? order = WebSortOrder.Asc);


    WebStringResult CreateFilterString(string field, string op, string value, string conjunction);


    IList<WebFilterOperator> GetFilterOperators();

    #endregion
  }
}