using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web.Script.Services;
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

namespace MediaPortal.Plugins.MP2Extended.Controllers.json
{
  [RoutePrefix("MPExtended/MediaAccessService/json")]
  [Route("{action}")]
  [Authorize]
  public class MediaAccessServiceController : ApiController, IMediaAccessServiceController
  {
    #region General

    [HttpGet]
    [ApiExplorerSettings]
    [AllowAnonymous]
    public async Task<WebMediaServiceDescription> GetServiceDescription()
    {
      return await new GetServiceDescription().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    [AllowAnonymous]
    public Task<WebBoolResult> TestConnection()
    {
      return Task.FromResult(new WebBoolResult { Result = true });
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMediaItem> GetMediaItem(WebMediaType type, Guid id)
    {
      return await new GetMediaItem().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebDictionary<string>> GetExternalMediaInfo(WebMediaType type, Guid id)
    {
      return await new GetExternalMediaInfo().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebDiskSpaceInformation>> GetLocalDiskInformation(string filter)
    {
      return await new GetLocalDiskInformation().ProcessAsync(Request.GetOwinContext());
    }

    #endregion

    #region Movies

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetMovieCount()
    {
      return await new GetMovieCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMovieBasic>> GetMoviesBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMoviesBasic().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMovieDetailed>> GetMoviesDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMoviesDetailed().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMovieBasic>> GetMoviesBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMoviesBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMovieDetailed>> GetMoviesDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMoviesDetailedByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMovieBasic> GetMovieBasicById(Guid id)
    {
      return await new GetMovieBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMovieDetailed> GetMovieDetailedById(Guid id)
    {
      return await new GetMovieDetailedById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebGenre>> GetMovieGenres(WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMovieGenres().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebGenre>> GetMovieGenresByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMovieGenresByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetMovieGenresCount(string filter)
    {
      return await new GetMovieGenresCount().ProcessAsync(Request.GetOwinContext(), filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebCategory>> GetMovieCategories(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebActor>> GetMovieActors(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMovieActors().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebActor>> GetMovieActorsByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMovieActorsByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetMovieActorCount(string filter)
    {
      return await new GetMovieActorCount().ProcessAsync(Request.GetOwinContext(), filter);
    }

    #endregion

    #region Music

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetMusicAlbumCount(string filter)
    {
      return await new GetMusicAlbumCount().ProcessAsync(Request.GetOwinContext(), filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicAlbumBasic>> GetMusicAlbumsBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMusicAlbumsBasic().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicAlbumBasic>> GetMusicAlbumsBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMusicAlbumsBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicAlbumBasic>> GetMusicAlbumsBasicForArtist(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMusicAlbumBasic> GetMusicAlbumBasicById(Guid id)
    {
      return await new GetMusicAlbumBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetMusicArtistCount(string filter)
    {
      return await new GetMusicArtistCount().ProcessAsync(Request.GetOwinContext(), filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicArtistBasic>> GetMusicArtistsBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMusicArtistsBasic().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicArtistBasic>> GetMusicArtistsBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMusicArtistsBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMusicArtistBasic> GetMusicArtistBasicById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicArtistDetailed>> GetMusicArtistsDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicArtistDetailed>> GetMusicArtistsDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMusicArtistDetailed> GetMusicArtistDetailedById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetMusicTrackCount(string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicTrackBasic>> GetMusicTracksBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicTrackDetailed>> GetMusicTracksDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicTrackBasic>> GetMusicTracksBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicTrackDetailed>> GetMusicTracksDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicTrackBasic>> GetMusicTracksBasicForAlbum(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMusicTracksBasicForAlbum().ProcessAsync(Request.GetOwinContext(), id, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicTrackBasic>> GetMusicTracksBasicForArtist(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicTrackDetailed>> GetMusicTracksDetailedForAlbum(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebMusicTrackDetailed>> GetMusicTracksDetailedForArtist(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMusicTrackBasic> GetMusicTrackBasicById(Guid id)
    {
      return await new GetMusicTrackBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebMusicTrackDetailed> GetMusicTrackDetailedById(Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebGenre>> GetMusicGenres(WebSortField? sort, WebSortOrder? order)
    {
      return await new GetMusicGenres().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebGenre>> GetMusicGenresByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetMusicGenresCount(string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebCategory>> GetMusicCategories(string filter)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region OnlineVideos

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebOnlineVideosVideo>> GetOnlineVideosCategoryVideos(string id)
    {
      return await new GetOnlineVideosCategoryVideos().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebOnlineVideosGlobalSite>> GetOnlineVideosGlobalSites(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetOnlineVideosGlobalSites().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebOnlineVideosSiteCategory>> GetOnlineVideosSiteCategories(string id)
    {
      return await new GetOnlineVideosSiteCategories().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebOnlineVideosSite>> GetOnlineVideosSites(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetOnlineVideosSites().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebOnlineVideosSiteSetting>> GetOnlineVideosSiteSettings(string id)
    {
      return await new GetOnlineVideosSiteSettings().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebOnlineVideosSiteCategory>> GetOnlineVideosSubCategories(string id)
    {
      return await new GetOnlineVideosSubCategories().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<string>> GetOnlineVideosVideoUrls(string id)
    {
      return await new GetOnlineVideosVideoUrls().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> SetOnlineVideosSiteSetting(string siteId, string property, string value)
    {
      return await new SetOnlineVideosSiteSetting().ProcessAsync(Request.GetOwinContext(), siteId, property, value);
    }

    #endregion

    #region Pictures

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetPictureCount()
    {
      return await new GetPictureCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPictureBasic>> GetPicturesBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetPicturesBasic().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPictureBasic>> GetPicturesBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPictureDetailed>> GetPicturesDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetPicturesDetailed().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPictureDetailed>> GetPicturesDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebPictureBasic> GetPictureBasicById(Guid id)
    {
      return await new GetPictureBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebPictureDetailed> GetPictureDetailedById(Guid id)
    {
      return await new GetPictureDetailedById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebCategory>> GetPictureCategories()
    {
      return await new GetPictureCategories().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebCategory>> GetPictureSubCategories(Guid id, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPictureBasic>> GetPicturesBasicByCategory(string id)
    {
      return await new GetPicturesBasicByCategory().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPictureDetailed>> GetPicturesDetailedByCategory(Guid id, string filter)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region TVShows

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetTVEpisodeCount()
    {
      return await new GetTVEpisodeCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetTVEpisodeCountForTVShow(Guid id)
    {
      return await new GetTVEpisodeCountForTVShow().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetTVEpisodeCountForSeason(Guid id)
    {
      return await new GetTVEpisodeCountForSeason().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetTVShowCount(string filter)
    {
      return await new GetTVShowCount().ProcessAsync(Request.GetOwinContext(), filter);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetTVSeasonCountForTVShow(Guid id)
    {
      return await new GetTVSeasonCountForTVShow().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVShowBasic>> GetTVShowsBasic(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVShowsBasic().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVShowDetailed>> GetTVShowsDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVShowsDetailed().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVShowBasic>> GetTVShowsBasicByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVShowsBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVShowDetailed>> GetTVShowsDetailedByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVShowsDetailedRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebTVShowBasic> GetTVShowBasicById(Guid id)
    {
      return await new GetTVShowBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebTVShowDetailed> GetTVShowDetailedById(Guid id)
    {
      return await new GetTVShowDetailedById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVSeasonBasic>> GetTVSeasonsBasicForTVShow(Guid id, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVSeasonsBasicForTVShow().ProcessAsync(Request.GetOwinContext(), id, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVSeasonDetailed>> GetTVSeasonsDetailedForTVShow(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVSeasonsDetailedForTVShow().ProcessAsync(Request.GetOwinContext(), id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebTVSeasonBasic> GetTVSeasonBasicById(Guid id)
    {
      return await new GetTVSeasonBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebTVSeasonDetailed> GetTVSeasonDetailedById(Guid id)
    {
      return await new GetTVSeasonDetailedById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasic(WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesBasic().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailed(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesDetailed().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasicByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesBasicByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailedByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesDetailedByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasicForTVShow(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesBasicForTVShow().ProcessAsync(Request.GetOwinContext(), id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailedForTVShow(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesDetailedForTVShow().ProcessAsync(Request.GetOwinContext(), id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasicForTVShowByRange(Guid id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesBasicForTVShowByRange().ProcessAsync(Request.GetOwinContext(), id, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailedForTVShowByRange(Guid id, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesDetailedForTVShowByRange().ProcessAsync(Request.GetOwinContext(), id, start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeBasic>> GetTVEpisodesBasicForSeason(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesBasicForSeason().ProcessAsync(Request.GetOwinContext(), id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebTVEpisodeDetailed>> GetTVEpisodesDetailedForSeason(Guid id, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVEpisodesDetailedForSeason().ProcessAsync(Request.GetOwinContext(), id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebTVEpisodeBasic> GetTVEpisodeBasicById(Guid id)
    {
      return await new GetTVEpisodeBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebTVEpisodeDetailed> GetTVEpisodeDetailedById(Guid id)
    {
      return await new GetTVEpisodeDetailedById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebCategory>> GetTVShowCategories(string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebGenre>> GetTVShowGenres(WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVShowGenres().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebGenre>> GetTVShowGenresByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVShowGenresByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetTVShowGenresCount()
    {
      return await new GetTVShowGenresCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebActor>> GetTVShowActors(string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVShowActors().ProcessAsync(Request.GetOwinContext(), filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebActor>> GetTVShowActorsByRange(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetTVShowActorsByRange().ProcessAsync(Request.GetOwinContext(), start, end, filter, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetTVShowActorCount(string filter)
    {
      return await new GetTVShowActorCount().ProcessAsync(Request.GetOwinContext(), filter);
    }

    #endregion

    #region Filesystem

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetFileSystemDriveCount()
    {
      return await new GetFileSystemDriveCount().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebDriveBasic>> GetFileSystemDrives(WebSortField? sort, WebSortOrder? order)
    {
      return await new GetFileSystemDrives().ProcessAsync(Request.GetOwinContext(), sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebDriveBasic>> GetFileSystemDrivesByRange(int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetFileSystemDrivesByRange().ProcessAsync(Request.GetOwinContext(), start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebFolderBasic>> GetFileSystemFolders(string id, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetFileSystemFolders().ProcessAsync(Request.GetOwinContext(), id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebFolderBasic>> GetFileSystemFoldersByRange(string id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetFileSystemFoldersByRange().ProcessAsync(Request.GetOwinContext(), id, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebFileBasic>> GetFileSystemFiles(string id, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetFileSystemFiles().ProcessAsync(Request.GetOwinContext(), id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebFileBasic>> GetFileSystemFilesByRange(string id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetFileSystemFilesByRange().ProcessAsync(Request.GetOwinContext(), id, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebFilesystemItem>> GetFileSystemFilesAndFolders(string id, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetFileSystemFilesAndFolders().ProcessAsync(Request.GetOwinContext(), id, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebFilesystemItem>> GetFileSystemFilesAndFoldersByRange(string id, int start, int end, WebSortField? sort, WebSortOrder? order)
    {
      return await new GetFileSystemFilesAndFoldersByRange().ProcessAsync(Request.GetOwinContext(), id, start, end, sort, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetFileSystemFilesAndFoldersCount(string id)
    {
      return await new GetFileSystemFilesAndFoldersCount().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetFileSystemFilesCount(string id)
    {
      return await new GetFileSystemFilesCount().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetFileSystemFoldersCount(string id)
    {
      return await new GetFileSystemFoldersCount().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebDriveBasic> GetFileSystemDriveBasicById(string id)
    {
      return await new GetFileSystemDriveBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebFolderBasic> GetFileSystemFolderBasicById(string id)
    {
      return await new GetFileSystemFolderBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebFileBasic> GetFileSystemFileBasicById(string id)
    {
      return await new GetFileSystemFileBasicById().ProcessAsync(Request.GetOwinContext(), id);
    }

    #endregion

    #region Files

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebArtwork>> GetArtwork(WebMediaType type, Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<string>> GetPathList(WebMediaType mediatype, WebFileType filetype, Guid id)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebFileInfo> GetFileInfo(WebMediaType mediatype, WebFileType filetype, Guid id, int offset)
    {
      return await new GetFileInfo().ProcessAsync(Request.GetOwinContext(), mediatype, filetype, id, offset);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> IsLocalFile(WebMediaType mediatype, WebFileType filetype, Guid id, int offset)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<Stream> RetrieveFile(WebMediaType mediatype, WebFileType filetype, Guid id, int offset)
    {
      throw new NotImplementedException();
    }

    #endregion

    #region Playlist

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPlaylist>> GetPlaylists()
    {
      return await new GetPlaylists().ProcessAsync(Request.GetOwinContext());
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPlaylistItem>> GetPlaylistItems(Guid playlistId, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebPlaylistItem>> GetPlaylistItemsByRange(Guid playlistId, int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetPlaylistItemsCount(Guid playlistId, string filter)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> AddPlaylistItem(Guid playlistId, WebMediaType type, Guid id, int? position)
    {
      return await new AddPlaylistItem().ProcessAsync(Request.GetOwinContext(), playlistId, type, id, position);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> ClearAndAddPlaylistItems(Guid playlistId, WebMediaType type, int? position, List<Guid> ids)
    {
      return await new ClearAndAddPlaylistItems().ProcessAsync(Request.GetOwinContext(), playlistId, type, position, ids);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> AddPlaylistItems(Guid playlistId, WebMediaType type, int? position, List<Guid> ids)
    {
      return await new AddPlaylistItems().ProcessAsync(Request.GetOwinContext(), playlistId, type, position, ids);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> RemovePlaylistItem(Guid playlistId, int position)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> RemovePlaylistItems(Guid playlistId, string positions)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> MovePlaylistItem(Guid playlistId, int oldPosition, int newPosition)
    {
      throw new NotImplementedException();
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebStringResult> CreatePlaylist(string playlistName)
    {
      return await new CreatePlaylist().ProcessAsync(Request.GetOwinContext(), playlistName);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebBoolResult> DeletePlaylist(Guid playlistId)
    {
      return await new DeletePlaylist().ProcessAsync(Request.GetOwinContext(), playlistId);
    }

    #endregion

    #region Filters

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebIntResult> GetFilterValuesCount(WebMediaType mediaType, string filterField, string op, int? limit)
    {
      return await new GetFilterValuesCount().ProcessAsync(Request.GetOwinContext(), mediaType, filterField, op, limit);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<string>> GetFilterValues(WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
      return await new GetFilterValues().ProcessAsync(Request.GetOwinContext(), mediaType, filterField, op, limit, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<string>> GetFilterValuesByRange(int start, int end, WebMediaType mediaType, string filterField, string op, int? limit, WebSortOrder? order)
    {
      return await new GetFilterValuesByRange().ProcessAsync(Request.GetOwinContext(), start, end, mediaType, filterField, op, limit, order);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<WebStringResult> CreateFilterString(string field, string op, string value, string conjunction)
    {
      return await new CreateFilterString().ProcessAsync(Request.GetOwinContext(), field, op, value, conjunction);
    }

    [HttpGet]
    [ApiExplorerSettings]
    public async Task<IList<WebFilterOperator>> GetFilterOperators()
    {
      return await new GetFilterOperators().ProcessAsync(Request.GetOwinContext());
    }

    #endregion
  }
}
