using System.Collections.Generic;
using System.IO;
using System.ServiceModel.Web;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;

namespace MediaPortal.Plugins.MP2Extended.Controllers.Interfaces
{
  public interface IMediaAccessServiceController
  {
    #region Global

    WebMediaServiceDescription GetServiceDescription();


    WebBoolResult TestConnection();


    WebMediaItem GetMediaItem(int? provider, WebMediaType type, string id);


    //IList<WebSearchResult> Search(string text);


    //IList<WebSearchResult> SearchResultsByRange(string text, int start, int end);


    WebDictionary<string> GetExternalMediaInfo(int? provider, WebMediaType type, string id);


    IList<WebDiskSpaceInformation> GetLocalDiskInformation(string filter = null);

    #endregion

    #region Movies

    WebIntResult GetMovieCount(int? provider, string filter = null);


    IList<WebMovieBasic> GetMoviesBasic(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMovieDetailed> GetMoviesDetailed(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMovieBasic> GetMoviesBasicByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMovieDetailed> GetMoviesDetailedByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMovieBasic GetMovieBasicById(int? provider, string id);


    WebMovieDetailed GetMovieDetailedById(int? provider, string id);


    IList<WebGenre> GetMovieGenres(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebGenre> GetMovieGenresByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetMovieGenresCount(int? provider, string filter = null);


    IList<WebCategory> GetMovieCategories(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebActor> GetMovieActors(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebActor> GetMovieActorsByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetMovieActorCount(int? provider, string filter = null);

    #endregion

    #region Music

    WebIntResult GetMusicAlbumCount(int? provider, string filter = null);


    IList<WebMusicAlbumBasic> GetMusicAlbumsBasic(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicAlbumBasic> GetMusicAlbumsBasicByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicAlbumBasic> GetMusicAlbumsBasicForArtist(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMusicAlbumBasic GetMusicAlbumBasicById(int? provider, string id);


    WebIntResult GetMusicArtistCount(int? provider, string filter = null);


    IList<WebMusicArtistBasic> GetMusicArtistsBasic(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicArtistBasic> GetMusicArtistsBasicByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMusicArtistBasic GetMusicArtistBasicById(int? provider, string id);


    IList<WebMusicArtistDetailed> GetMusicArtistsDetailed(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicArtistDetailed> GetMusicArtistsDetailedByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMusicArtistDetailed GetMusicArtistDetailedById(int? provider, string id);


    WebIntResult GetMusicTrackCount(int? provider, string filter = null);


    IList<WebMusicTrackBasic> GetMusicTracksBasic(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackDetailed> GetMusicTracksDetailed(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackBasic> GetMusicTracksBasicByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackDetailed> GetMusicTracksDetailedByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackBasic> GetMusicTracksBasicForAlbum(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackBasic> GetMusicTracksBasicForArtist(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackDetailed> GetMusicTracksDetailedForAlbum(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebMusicTrackDetailed> GetMusicTracksDetailedForArtist(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebMusicTrackBasic GetMusicTrackBasicById(int? provider, string id);


    WebMusicTrackDetailed GetMusicTrackDetailedById(int? provider, string id);


    IList<WebGenre> GetMusicGenres(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebGenre> GetMusicGenresByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetMusicGenresCount(int? provider, string filter = null);


    IList<WebCategory> GetMusicCategories(int? provider, string filter = null);

    #endregion

    #region Pictures

    WebIntResult GetPictureCount(int? provider, string filter = null);


    IList<WebPictureBasic> GetPicturesBasic(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebPictureBasic> GetPicturesBasicByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebPictureDetailed> GetPicturesDetailed(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebPictureDetailed> GetPicturesDetailedByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebPictureBasic GetPictureBasicById(int? provider, string id);


    WebPictureDetailed GetPictureDetailedById(int? provider, string id);


    IList<WebCategory> GetPictureCategories(int? provider, string filter = null);


    IList<WebCategory> GetPictureSubCategories(int? provider, string id, string filter = null);


    IList<WebPictureBasic> GetPicturesBasicByCategory(int? provider, string id, string filter = null);


    IList<WebPictureDetailed> GetPicturesDetailedByCategory(int? provider, string id, string filter = null);

    #endregion

    #region TVShows

    WebIntResult GetTVEpisodeCount(int? provider, string filter = null);


    WebIntResult GetTVEpisodeCountForTVShow(int? provider, string id, string filter = null);


    WebIntResult GetTVEpisodeCountForSeason(int? provider, string id, string filter = null);


    WebIntResult GetTVShowCount(int? provider, string filter = null);


    WebIntResult GetTVSeasonCountForTVShow(int? provider, string id, string filter = null);


    IList<WebTVShowBasic> GetTVShowsBasic(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVShowDetailed> GetTVShowsDetailed(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVShowBasic> GetTVShowsBasicByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVShowDetailed> GetTVShowsDetailedByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebTVShowBasic GetTVShowBasicById(int? provider, string id);


    WebTVShowDetailed GetTVShowDetailedById(int? provider, string id);


    IList<WebTVSeasonBasic> GetTVSeasonsBasicForTVShow(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.TVSeasonNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVSeasonDetailed> GetTVSeasonsDetailedForTVShow(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.TVSeasonNumber, WebSortOrder? order = WebSortOrder.Asc);


    WebTVSeasonBasic GetTVSeasonBasicById(int? provider, string id);


    WebTVSeasonDetailed GetTVSeasonDetailedById(int? provider, string id);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasic(int? provider, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailed(int? provider, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasicByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShow(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShow(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShowByRange(int? provider, string id, int start, int end, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShowByRange(int? provider, string id, int start, int end, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeBasic> GetTVEpisodesBasicForSeason(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForSeason(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.TVEpisodeNumber, WebSortOrder? order = WebSortOrder.Asc);


    WebTVEpisodeBasic GetTVEpisodeBasicById(int? provider, string id);


    WebTVEpisodeDetailed GetTVEpisodeDetailedById(int? provider, string id);


    IList<WebCategory> GetTVShowCategories(int? provider, string filter = null);


    IList<WebGenre> GetTVShowGenres(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebGenre> GetTVShowGenresByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetTVShowGenresCount(int? provider, string filter = null);


    IList<WebActor> GetTVShowActors(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebActor> GetTVShowActorsByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetTVShowActorCount(int? provider, string filter = null);

    #endregion

    #region Filesystem

    WebIntResult GetFileSystemDriveCount(int? provider, string filter = null);


    IList<WebDriveBasic> GetFileSystemDrives(int? provider, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebDriveBasic> GetFileSystemDrivesByRange(int? provider, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFolderBasic> GetFileSystemFolders(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFolderBasic> GetFileSystemFoldersByRange(int? provider, string id, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFileBasic> GetFileSystemFiles(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFileBasic> GetFileSystemFilesByRange(int? provider, string id, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFilesystemItem> GetFileSystemFilesAndFolders(int? provider, string id, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebFilesystemItem> GetFileSystemFilesAndFoldersByRange(int? provider, string id, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetFileSystemFilesAndFoldersCount(int? provider, string id, string filter = null);


    WebIntResult GetFileSystemFilesCount(int? provider, string id, string filter = null);


    WebIntResult GetFileSystemFoldersCount(int? provider, string id, string filter = null);


    WebDriveBasic GetFileSystemDriveBasicById(int? provider, string id);


    WebFolderBasic GetFileSystemFolderBasicById(int? provider, string id);


    WebFileBasic GetFileSystemFileBasicById(int? provider, string id);

    #endregion

    #region Files

    IList<WebArtwork> GetArtwork(int? provider, WebMediaType type, string id);


    IList<string> GetPathList(int? provider, WebMediaType mediatype, WebFileType filetype, string id);


    WebFileInfo GetFileInfo(int? provider, WebMediaType mediatype, WebFileType filetype, string id, int offset);


    WebBoolResult IsLocalFile(int? provider, WebMediaType mediatype, WebFileType filetype, string id, int offset);


    [WebGet(BodyStyle = WebMessageBodyStyle.Bare)]
    Stream RetrieveFile(int? provider, WebMediaType mediatype, WebFileType filetype, string id, int offset);

    #endregion

    #region Playlist

    IList<WebPlaylist> GetPlaylists(int? provider, string filter = null);


    IList<WebPlaylistItem> GetPlaylistItems(int? provider, string playlistId, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    IList<WebPlaylistItem> GetPlaylistItemsByRange(int? provider, string playlistId, int start, int end, string filter = null, WebSortField? sort = WebSortField.Title, WebSortOrder? order = WebSortOrder.Asc);


    WebIntResult GetPlaylistItemsCount(int? provider, string playlistId, string filter = null);


    WebBoolResult AddPlaylistItem(int? provider, string playlistId, WebMediaType type, string id, int? position);


    WebBoolResult ClearAndAddPlaylistItems(int? provider, string playlistId, WebMediaType type, int? position, string ids);


    WebBoolResult AddPlaylistItems(int? provider, string playlistId, WebMediaType type, int? position, string ids);


    WebBoolResult RemovePlaylistItem(int? provider, string playlistId, int position);


    WebBoolResult RemovePlaylistItems(int? provider, string playlistId, string positions);


    WebBoolResult MovePlaylistItem(int? provider, string playlistId, int oldPosition, int newPosition);


    WebStringResult CreatePlaylist(int? provider, string playlistName);


    WebBoolResult DeletePlaylist(int? provider, string playlistId);

    #endregion

    #region Filters

    WebIntResult GetFilterValuesCount(int? provider, WebMediaType mediaType, string filterField, string op = null, int? limit = null);


    IList<string> GetFilterValues(int? provider, WebMediaType mediaType, string filterField, string op = null, int? limit = null, WebSortOrder? order = WebSortOrder.Asc);


    IList<string> GetFilterValuesByRange(int? provider, int start, int end, WebMediaType mediaType, string filterField, string op = null, int? limit = null, WebSortOrder? order = WebSortOrder.Asc);


    WebStringResult CreateFilterString(string field, string op, string value, string conjunction);


    IList<WebFilterOperator> GetFilterOperators();

    #endregion
  }
}