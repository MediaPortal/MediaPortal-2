using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Controllers.Interfaces;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended.Controllers
{
  [Route("[Controller]/json/[Action]")]
  public class MediaAccessServiceController : Controller, IMediaAccessServiceController
  {
    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMediaServiceDescription GetServiceDescription()
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult TestConnection()
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMediaItem GetMediaItem(int? provider, WebMediaType type, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebDictionary<string> GetExternalMediaInfo(int? provider, WebMediaType type, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebDiskSpaceInformation> GetLocalDiskInformation(string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMovieCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMovieBasic> GetMoviesBasic(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMovieDetailed> GetMoviesDetailed(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMovieBasic> GetMoviesBasicByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMovieDetailed> GetMoviesDetailedByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMovieBasic GetMovieBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMovieDetailed GetMovieDetailedById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetMovieGenres(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetMovieGenresByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMovieGenresCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetMovieCategories(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebActor> GetMovieActors(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebActor> GetMovieActorsByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMovieActorCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMusicAlbumCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicAlbumBasic> GetMusicAlbumsBasic(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicAlbumBasic> GetMusicAlbumsBasicByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicAlbumBasic> GetMusicAlbumsBasicForArtist(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicAlbumBasic GetMusicAlbumBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMusicArtistCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicArtistBasic> GetMusicArtistsBasic(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicArtistBasic> GetMusicArtistsBasicByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicArtistBasic GetMusicArtistBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicArtistDetailed> GetMusicArtistsDetailed(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicArtistDetailed> GetMusicArtistsDetailedByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicArtistDetailed GetMusicArtistDetailedById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMusicTrackCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackBasic> GetMusicTracksBasic(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackDetailed> GetMusicTracksDetailed(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackBasic> GetMusicTracksBasicByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackDetailed> GetMusicTracksDetailedByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackBasic> GetMusicTracksBasicForAlbum(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackBasic> GetMusicTracksBasicForArtist(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackDetailed> GetMusicTracksDetailedForAlbum(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackDetailed> GetMusicTracksDetailedForArtist(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicTrackBasic GetMusicTrackBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicTrackDetailed GetMusicTrackDetailedById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetMusicGenres(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetMusicGenresByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMusicGenresCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetMusicCategories(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetPictureCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureBasic> GetPicturesBasic(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureBasic> GetPicturesBasicByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureDetailed> GetPicturesDetailed(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureDetailed> GetPicturesDetailedByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebPictureBasic GetPictureBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebPictureDetailed GetPictureDetailedById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetPictureCategories(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetPictureSubCategories(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureBasic> GetPicturesBasicByCategory(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureDetailed> GetPicturesDetailedByCategory(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVEpisodeCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVEpisodeCountForTVShow(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVEpisodeCountForSeason(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVShowCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVSeasonCountForTVShow(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVShowBasic> GetTVShowsBasic(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVShowDetailed> GetTVShowsDetailed(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVShowBasic> GetTVShowsBasicByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVShowDetailed> GetTVShowsDetailedByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVShowBasic GetTVShowBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVShowDetailed GetTVShowDetailedById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVSeasonBasic> GetTVSeasonsBasicForTVShow(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVSeasonDetailed> GetTVSeasonsDetailedForTVShow(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVSeasonBasic GetTVSeasonBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVSeasonDetailed GetTVSeasonDetailedById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasic(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailed(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasicByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShow(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShow(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShowByRange(int? provider, string id, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShowByRange(int? provider, string id, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasicForSeason(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForSeason(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVEpisodeBasic GetTVEpisodeBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVEpisodeDetailed GetTVEpisodeDetailedById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetTVShowCategories(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetTVShowGenres(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetTVShowGenresByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVShowGenresCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebActor> GetTVShowActors(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebActor> GetTVShowActorsByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVShowActorCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFileSystemDriveCount(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebDriveBasic> GetFileSystemDrives(int? provider, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebDriveBasic> GetFileSystemDrivesByRange(int? provider, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFolderBasic> GetFileSystemFolders(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFolderBasic> GetFileSystemFoldersByRange(int? provider, string id, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFileBasic> GetFileSystemFiles(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFileBasic> GetFileSystemFilesByRange(int? provider, string id, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFilesystemItem> GetFileSystemFilesAndFolders(int? provider, string id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFilesystemItem> GetFileSystemFilesAndFoldersByRange(int? provider, string id, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFileSystemFilesAndFoldersCount(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFileSystemFilesCount(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFileSystemFoldersCount(int? provider, string id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebDriveBasic GetFileSystemDriveBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebFolderBasic GetFileSystemFolderBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebFileBasic GetFileSystemFileBasicById(int? provider, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebArtwork> GetArtwork(int? provider, WebMediaType type, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<string> GetPathList(int? provider, WebMediaType mediatype, WebFileType filetype, string id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebFileInfo GetFileInfo(int? provider, WebMediaType mediatype, WebFileType filetype, string id, int offset)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult IsLocalFile(int? provider, WebMediaType mediatype, WebFileType filetype, string id, int offset)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public Stream RetrieveFile(int? provider, WebMediaType mediatype, WebFileType filetype, string id, int offset)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPlaylist> GetPlaylists(int? provider, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPlaylistItem> GetPlaylistItems(int? provider, string playlistId, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPlaylistItem> GetPlaylistItemsByRange(int? provider, string playlistId, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetPlaylistItemsCount(int? provider, string playlistId, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult AddPlaylistItem(int? provider, string playlistId, WebMediaType type, string id, int? position)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult ClearAndAddPlaylistItems(int? provider, string playlistId, WebMediaType type, int? position, string ids)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult AddPlaylistItems(int? provider, string playlistId, WebMediaType type, int? position, string ids)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult RemovePlaylistItem(int? provider, string playlistId, int position)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult RemovePlaylistItems(int? provider, string playlistId, string positions)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult MovePlaylistItem(int? provider, string playlistId, int oldPosition, int newPosition)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebStringResult CreatePlaylist(int? provider, string playlistName)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult DeletePlaylist(int? provider, string playlistId)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFilterValuesCount(int? provider, WebMediaType mediaType, string filterField, string op, int? limit)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<string> GetFilterValues(int? provider, WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<string> GetFilterValuesByRange(int? provider, int start, int end, WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebStringResult CreateFilterString(string field, string op, string value, string conjunction)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFilterOperator> GetFilterOperators()
    {
      throw new NotImplementedException();
    }
  }
}
