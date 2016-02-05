using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Controllers.Interfaces;
using MediaPortal.Plugins.MP2Extended.MAS;
using MediaPortal.Plugins.MP2Extended.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.MAS.Music;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.FileSystem;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Filter;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Movie;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Music;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Playlist;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.TvShow;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using Microsoft.AspNet.Mvc;

namespace MediaPortal.Plugins.MP2Extended.Controllers.json
{
  [Route("[Controller]/json/[Action]")]
  public class MediaAccessServiceController : Controller, IMediaAccessServiceController
  {

    #region General
    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMediaServiceDescription GetServiceDescription()
    {
      return new GetServiceDescription().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult TestConnection()
    {
      return new WebBoolResult { Result = true };
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMediaItem GetMediaItem(WebMediaType type, Guid id)
    {
      return new GetMediaItem().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebDictionary<string> GetExternalMediaInfo(WebMediaType type, Guid id)
    {
      return new GetExternalMediaInfo().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebDiskSpaceInformation> GetLocalDiskInformation(string filter)
    {
      return new GetLocalDiskInformation().Process();
    }

    #endregion

    #region Movies

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMovieCount()
    {
      return new GetMovieCount().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMovieBasic> GetMoviesBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMoviesBasic().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMovieDetailed> GetMoviesDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMovieBasic> GetMoviesBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMoviesBasicByRange().Process(start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMovieDetailed> GetMoviesDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMoviesDetailedByRange().Process(start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMovieBasic GetMovieBasicById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMovieDetailed GetMovieDetailedById(Guid id)
    {
      return new GetMovieDetailedById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetMovieGenres(WebSortField? sort, WebSortOrder? order)
    {
      return new GetMovieGenres().Process(sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetMovieGenresByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMovieGenresCount(string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetMovieCategories(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebActor> GetMovieActors(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMovieActors().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebActor> GetMovieActorsByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMovieActorCount(string filter)
    {
      return new GetMovieActorCount().Process(filter);
    }

    #endregion

    #region Music

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMusicAlbumCount(string filter)
    {
      return new GetMusicAlbumCount().Process(filter);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicAlbumBasic> GetMusicAlbumsBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMusicAlbumsBasic().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicAlbumBasic> GetMusicAlbumsBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMusicAlbumsBasicByRange().Process(start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicAlbumBasic> GetMusicAlbumsBasicForArtist(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicAlbumBasic GetMusicAlbumBasicById(Guid id)
    {
      return new GetMusicAlbumBasicById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMusicArtistCount(string filter)
    {
      return new GetMusicArtistCount().Process(filter);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicArtistBasic> GetMusicArtistsBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMusicArtistsBasic().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicArtistBasic> GetMusicArtistsBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMusicArtistsBasicByRange().Process(start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicArtistBasic GetMusicArtistBasicById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicArtistDetailed> GetMusicArtistsDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicArtistDetailed> GetMusicArtistsDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicArtistDetailed GetMusicArtistDetailedById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMusicTrackCount(string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackBasic> GetMusicTracksBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackDetailed> GetMusicTracksDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackBasic> GetMusicTracksBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackDetailed> GetMusicTracksDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackBasic> GetMusicTracksBasicForAlbum(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetMusicTracksBasicForAlbum().Process(id, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackBasic> GetMusicTracksBasicForArtist(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackDetailed> GetMusicTracksDetailedForAlbum(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebMusicTrackDetailed> GetMusicTracksDetailedForArtist(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicTrackBasic GetMusicTrackBasicById(Guid id)
    {
      return new GetMusicTrackBasicById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebMusicTrackDetailed GetMusicTrackDetailedById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    //[ResponseCache(CacheProfileName = "nonCriticalApiCalls")]
    public IList<WebGenre> GetMusicGenres(WebSortField? sort, WebSortOrder? order)
    {
      return new GetMusicGenres().Process(sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetMusicGenresByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetMusicGenresCount(string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetMusicCategories(string filter)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region OnlineVideos

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public List<WebOnlineVideosVideo> GetOnlineVideosCategoryVideos(string id)
    {
      return new GetOnlineVideosCategoryVideos().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public List<WebOnlineVideosGlobalSite> GetOnlineVideosGlobalSites(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetOnlineVideosGlobalSites().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public List<WebOnlineVideosSiteCategory> GetOnlineVideosSiteCategories(string id)
    {
      return new GetOnlineVideosSiteCategories().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public List<WebOnlineVideosSite> GetOnlineVideosSites(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetOnlineVideosSites().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public List<WebOnlineVideosSiteSetting> GetOnlineVideosSiteSettings(string id)
    {
      return new GetOnlineVideosSiteSettings().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public List<WebOnlineVideosSiteCategory> GetOnlineVideosSubCategories(string id)
    {
      return new GetOnlineVideosSubCategories().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public List<string> GetOnlineVideosVideoUrls(string id)
    {
      return new GetOnlineVideosVideoUrls().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult SetOnlineVideosSiteSetting(string siteId, string property, string value)
    {
      return new SetOnlineVideosSiteSetting().Process(siteId, property, value);
    }

    #endregion

    #region Pictures

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetPictureCount()
    {
      return new GetPictureCount().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureBasic> GetPicturesBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetPicturesBasic().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureBasic> GetPicturesBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureDetailed> GetPicturesDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetPicturesDetailed().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureDetailed> GetPicturesDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebPictureBasic GetPictureBasicById(Guid id)
    {
      return new GetPictureBasicById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebPictureDetailed GetPictureDetailedById(Guid id)
    {
      return new GetPictureDetailedById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetPictureCategories()
    {
      return new GetPictureCategories().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetPictureSubCategories(Guid id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureBasic> GetPicturesBasicByCategory(string id)
    {
      return new GetPicturesBasicByCategory().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPictureDetailed> GetPicturesDetailedByCategory(Guid id, string filter)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region TVShows

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVEpisodeCount()
    {
      return new GetTVEpisodeCount().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVEpisodeCountForTVShow(Guid id)
    {
      return new GetTVEpisodeCountForTVShow().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVEpisodeCountForSeason(Guid id)
    {
      return new GetTVEpisodeCountForSeason().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVShowCount(string filter)
    {
      return new GetTVShowCount().Process(filter);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVSeasonCountForTVShow(Guid id)
    {
      return new GetTVSeasonCountForTVShow().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVShowBasic> GetTVShowsBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVShowsBasic().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVShowDetailed> GetTVShowsDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVShowsDetailed().Process(filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVShowBasic> GetTVShowsBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVShowsBasicByRange().Process(start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVShowDetailed> GetTVShowsDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVShowsDetailedRange().Process(start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVShowBasic GetTVShowBasicById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVShowDetailed GetTVShowDetailedById(Guid id)
    {
      return new GetTVShowDetailedById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVSeasonBasic> GetTVSeasonsBasicForTVShow(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVSeasonsBasicForTVShow().Process(id, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVSeasonDetailed> GetTVSeasonsDetailedForTVShow(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVSeasonsDetailedForTVShow().Process(id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVSeasonBasic GetTVSeasonBasicById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVSeasonDetailed GetTVSeasonDetailedById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasic(WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVEpisodesBasic().Process(sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasicByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVEpisodesBasicByRange().Process(start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVEpisodesDetailedByRange().Process(start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShow(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVEpisodesBasicForTVShow().Process(id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShow(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasicForTVShowByRange(Guid id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForTVShowByRange(Guid id, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeBasic> GetTVEpisodesBasicForSeason(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVEpisodesBasicForSeason().Process(id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebTVEpisodeDetailed> GetTVEpisodesDetailedForSeason(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVEpisodesDetailedForSeason().Process(id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVEpisodeBasic GetTVEpisodeBasicById(Guid id)
    {
      return new GetTVEpisodeBasicById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebTVEpisodeDetailed GetTVEpisodeDetailedById(Guid id)
    {
      return new GetTVEpisodeDetailedById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebCategory> GetTVShowCategories(string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetTVShowGenres(WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVShowGenres().Process(sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebGenre> GetTVShowGenresByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetTVShowGenresByRange().Process(start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVShowGenresCount()
    {
      return new GetTVShowGenresCount().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebActor> GetTVShowActors(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebActor> GetTVShowActorsByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetTVShowActorCount(string filter)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Filesystem

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFileSystemDriveCount()
    {
      return new GetFileSystemDriveCount().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebDriveBasic> GetFileSystemDrives(WebSortField? sort, WebSortOrder? order)
    {
      return new GetFileSystemDrives().Process(sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebDriveBasic> GetFileSystemDrivesByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetFileSystemDrivesByRange().Process(start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFolderBasic> GetFileSystemFolders(string id, WebSortField? sort, WebSortOrder? order)
    {
      return new GetFileSystemFolders().Process(id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFolderBasic> GetFileSystemFoldersByRange(string id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetFileSystemFoldersByRange().Process(id, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFileBasic> GetFileSystemFiles(string id, WebSortField? sort, WebSortOrder? order)
    {
      return new GetFileSystemFiles().Process(id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFileBasic> GetFileSystemFilesByRange(string id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetFileSystemFilesByRange().Process(id, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFilesystemItem> GetFileSystemFilesAndFolders(string id, WebSortField? sort, WebSortOrder? order)
    {
      return new GetFileSystemFilesAndFolders().Process(id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFilesystemItem> GetFileSystemFilesAndFoldersByRange(string id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return new GetFileSystemFilesAndFoldersByRange().Process(id, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFileSystemFilesAndFoldersCount(string id)
    {
      return new GetFileSystemFilesAndFoldersCount().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFileSystemFilesCount(string id)
    {
      return new GetFileSystemFilesCount().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFileSystemFoldersCount(string id)
    {
      return new GetFileSystemFoldersCount().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebDriveBasic GetFileSystemDriveBasicById(string id)
    {
      return new GetFileSystemDriveBasicById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebFolderBasic GetFileSystemFolderBasicById(string id)
    {
      return new GetFileSystemFolderBasicById().Process(id);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebFileBasic GetFileSystemFileBasicById(string id)
    {
      return new GetFileSystemFileBasicById().Process(id);
    }

    #endregion

    #region Files

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebArtwork> GetArtwork(WebMediaType type, Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<string> GetPathList(WebMediaType mediatype, WebFileType filetype, Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebFileInfo GetFileInfo(WebMediaType mediatype, WebFileType filetype, Guid id, int offset)
    {
      return new GetFileInfo().Process(mediatype, filetype, id, offset);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult IsLocalFile(WebMediaType mediatype, WebFileType filetype, Guid id, int offset)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public Stream RetrieveFile(WebMediaType mediatype, WebFileType filetype, Guid id, int offset)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Playlist

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPlaylist> GetPlaylists()
    {
      return new GetPlaylists().Process();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPlaylistItem> GetPlaylistItems(Guid playlistId, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebPlaylistItem> GetPlaylistItemsByRange(Guid playlistId, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetPlaylistItemsCount(Guid playlistId, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult AddPlaylistItem(Guid playlistId, WebMediaType type, Guid id, int? position)
    {
      return new AddPlaylistItem().Process(playlistId, type, id, position);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult ClearAndAddPlaylistItems(Guid playlistId, WebMediaType type, int? position, List<Guid> ids)
    {
      return new ClearAndAddPlaylistItems().Process(playlistId, type, position, ids);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult AddPlaylistItems(Guid playlistId, WebMediaType type, int? position, List<Guid> ids)
    {
      return new AddPlaylistItems().Process(playlistId, type, position, ids);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult RemovePlaylistItem(Guid playlistId, int position)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult RemovePlaylistItems(Guid playlistId, string positions)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult MovePlaylistItem(Guid playlistId, int oldPosition, int newPosition)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebStringResult CreatePlaylist(string playlistName)
    {
      return new CreatePlaylist().Process(playlistName);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebBoolResult DeletePlaylist(Guid playlistId)
    {
      return new DeletePlaylist().Process(playlistId);
    }

    #endregion

    #region Filters

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebIntResult GetFilterValuesCount(WebMediaType mediaType, string filterField, string op, int? limit)
    {
      return new GetFilterValuesCount().Process(mediaType, filterField, op, limit);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<string> GetFilterValues(WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
      return new GetFilterValues().Process(mediaType, filterField, op, limit, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<string> GetFilterValuesByRange(int start, int end, WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
      return new GetFilterValuesByRange().Process(start, end, mediaType, filterField, op, limit, order);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public WebStringResult CreateFilterString(string field, string op, string value, string conjunction)
    {
      return new CreateFilterString().Process(field, op, value, conjunction);
    }

    [HttpGet]
    [ApiExplorerSettings(GroupName = "MediaAccessService")]
    public IList<WebFilterOperator> GetFilterOperators()
    {
      return new GetFilterOperators().Process();
    }

    #endregion
  }
}
