#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class FanArtTVWrapper : ApiWrapper<FanArtMovieThumb, string>
  {
    protected FanArtTVApiV3 _fanArtTvHandler;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath, bool useHttps)
    {
      _fanArtTvHandler = new FanArtTVApiV3("82daed8035a4ad8fd868f70d5ee2012e", cachePath, useHttps);
      SetDefaultLanguage(FanArtTVApiV3.DefaultLanguage);
      SetCachePath(cachePath);
      return true;
    }

    #region Update

    public override Task<bool> UpdateFromOnlineMovieAsync(MovieInfo movie, string language, bool cacheOnly)
    {
      return Task.FromResult(movie.MovieDbId > 0 || !string.IsNullOrEmpty(movie.ImdbId));
    }

    public override Task<bool> UpdateFromOnlineSeriesAsync(SeriesInfo series, string language, bool cacheOnly)
    {
      return Task.FromResult(series.TvdbId > 0);
    }

    public override Task<bool> UpdateFromOnlineSeriesSeasonAsync(SeasonInfo season, string language, bool cacheOnly)
    {
      return Task.FromResult(season.SeriesTvdbId > 0);
    }

    public override Task<bool> UpdateFromOnlineSeriesEpisodeAsync(EpisodeInfo episode, string language, bool cacheOnly)
    {
      return Task.FromResult(episode.SeriesTvdbId > 0);
    }

    public override Task<bool> UpdateFromOnlineMusicTrackAsync(TrackInfo track, string language, bool cacheOnly)
    {
      return Task.FromResult(!string.IsNullOrEmpty(track.MusicBrainzId));
    }

    public override Task<bool> UpdateFromOnlineMusicTrackAlbumAsync(AlbumInfo album, string language, bool cacheOnly)
    {
      return Task.FromResult(!string.IsNullOrEmpty(album.MusicBrainzGroupId));
    }

    public override Task<bool> UpdateFromOnlineMusicTrackAlbumCompanyAsync(AlbumInfo albumInfo, CompanyInfo company, string language, bool cacheOnly)
    {
      return Task.FromResult(!string.IsNullOrEmpty(company.MusicBrainzId));
    }

    public override Task<bool> UpdateFromOnlineMusicTrackAlbumPersonAsync(AlbumInfo albumInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return Task.FromResult(!string.IsNullOrEmpty(person.MusicBrainzId));
    }

    public override Task<bool> UpdateFromOnlineMusicTrackPersonAsync(TrackInfo trackInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return Task.FromResult(!string.IsNullOrEmpty(person.MusicBrainzId));
    }

    #endregion

    #region FanArt

    public override Task<ApiWrapperImageCollection<FanArtMovieThumb>> GetFanArtAsync<T>(T infoObject, string language, string fanartMediaType)
    {
      if (fanartMediaType == FanArtMediaTypes.Movie)
        return GetMovieFanArtAsync(infoObject as MovieInfo, language);
      if (fanartMediaType == FanArtMediaTypes.Series)
        return GetSeriesFanArtAsync(infoObject.AsSeries(), language);
      if (fanartMediaType == FanArtMediaTypes.SeriesSeason)
        return GetSeasonFanArtAsync(infoObject.AsSeason(), language);
      if (fanartMediaType == FanArtMediaTypes.Artist)
        return GetArtistFanArtAsync(infoObject as PersonInfo);
      if (fanartMediaType == FanArtMediaTypes.MusicLabel)
        return GetMusicLabelFanArtAsync(infoObject as CompanyInfo);
      if (fanartMediaType == FanArtMediaTypes.Album)
        return GetAlbumFanArtAsync(infoObject.AsAlbum());
      return Task.FromResult<ApiWrapperImageCollection<FanArtMovieThumb>>(null);
    }

    public override Task<bool> DownloadFanArtAsync(string id, FanArtMovieThumb image, string folderPath)
    {
      return _fanArtTvHandler.DownloadImageAsync(id, image, folderPath);
    }

    protected async Task<ApiWrapperImageCollection<FanArtMovieThumb>> GetMovieFanArtAsync(MovieInfo movie, string language)
    {
      if (movie == null || movie.MovieDbId < 1)
        return null;
      // Download all image information, filter later!
      FanArtMovieThumbs thumbs = await _fanArtTvHandler.GetMovieThumbsAsync(movie.MovieDbId.ToString()).ConfigureAwait(false);
      if (thumbs == null)
        return null;
      ApiWrapperImageCollection<FanArtMovieThumb> images = new ApiWrapperImageCollection<FanArtMovieThumb>();
      images.Id = movie.MovieDbId.ToString();
      if (thumbs.MovieFanArt != null) images.Backdrops.AddRange(SortByLanguageAndLikes(thumbs.MovieFanArt));
      if (thumbs.MovieBanners != null) images.Banners.AddRange(SortByLanguageAndLikes(thumbs.MovieBanners));
      if (thumbs.MoviePosters != null) images.Posters.AddRange(SortByLanguageAndLikes(thumbs.MoviePosters));
      if (thumbs.MovieCDArt != null) images.DiscArt.AddRange(SortByLanguageAndLikes(thumbs.MovieCDArt));
      if (thumbs.HDMovieClearArt != null) images.ClearArt.AddRange(SortByLanguageAndLikes(thumbs.HDMovieClearArt));
      if (thumbs.HDMovieLogos != null) images.Logos.AddRange(SortByLanguageAndLikes(thumbs.HDMovieLogos));
      if (thumbs.MovieThumbnails != null) images.Thumbnails.AddRange(SortByLanguageAndLikes(thumbs.MovieThumbnails));
      return images;
    }

    protected async Task<ApiWrapperImageCollection<FanArtMovieThumb>> GetSeriesFanArtAsync(SeriesInfo series, string language)
    {
      if (series == null || series.TvdbId < 1)
        return null;
      // Download all image information, filter later!
      FanArtTVThumbs thumbs = await _fanArtTvHandler.GetSeriesThumbsAsync(series.TvdbId.ToString()).ConfigureAwait(false);
      if (thumbs == null)
        return null;
      ApiWrapperImageCollection<FanArtMovieThumb> images = new ApiWrapperImageCollection<FanArtMovieThumb>();
      images.Id = series.TvdbId.ToString();
      if (thumbs.SeriesFanArt != null) images.Backdrops.AddRange(SortByLanguageAndLikes(thumbs.SeriesFanArt));
      if (thumbs.SeriesBanners != null) images.Banners.AddRange(SortByLanguageAndLikes(thumbs.SeriesBanners));
      if (thumbs.SeriesPosters != null) images.Posters.AddRange(SortByLanguageAndLikes(thumbs.SeriesPosters));
      if (thumbs.HDSeriesClearArt != null) images.ClearArt.AddRange(SortByLanguageAndLikes(thumbs.HDSeriesClearArt));
      if (thumbs.HDSeriesLogos != null) images.Logos.AddRange(SortByLanguageAndLikes(thumbs.HDSeriesLogos));
      if (thumbs.SeriesThumbnails != null) images.Thumbnails.AddRange(SortByLanguageAndLikes(thumbs.SeriesThumbnails));
      return images;
    }

    protected async Task<ApiWrapperImageCollection<FanArtMovieThumb>> GetSeasonFanArtAsync(SeasonInfo season, string language)
    {
      if (season == null || season.SeriesTvdbId < 1 || !season.SeasonNumber.HasValue)
        return null;
      int seasonNo = season.SeasonNumber.Value;
      // Download all image information, filter later!
      FanArtTVThumbs thumbs = await _fanArtTvHandler.GetSeriesThumbsAsync(season.SeriesTvdbId.ToString()).ConfigureAwait(false);
      if (thumbs == null)
        return null;
      ApiWrapperImageCollection<FanArtMovieThumb> images = new ApiWrapperImageCollection<FanArtMovieThumb>();
      images.Id = season.SeriesTvdbId.ToString();
      if (thumbs.SeasonBanners != null) images.Banners.AddRange(SortBySeasonNumberLanguageAndLikes(seasonNo, thumbs.SeasonBanners));
      if (thumbs.SeasonPosters != null) images.Posters.AddRange(SortBySeasonNumberLanguageAndLikes(seasonNo, thumbs.SeasonPosters));
      if (thumbs.SeasonThumbnails != null) images.Thumbnails.AddRange(SortBySeasonNumberLanguageAndLikes(seasonNo, thumbs.SeasonThumbnails));
      return images;
    }

    protected async Task<ApiWrapperImageCollection<FanArtMovieThumb>> GetArtistFanArtAsync(PersonInfo person)
    {
      if (person == null || string.IsNullOrEmpty(person.MusicBrainzId))
        return null;
      // Download all image information, filter later!
      FanArtArtistThumbs thumbs = await _fanArtTvHandler.GetArtistThumbsAsync(person.MusicBrainzId).ConfigureAwait(false);
      if (thumbs == null)
        return null;
      ApiWrapperImageCollection<FanArtMovieThumb> images = new ApiWrapperImageCollection<FanArtMovieThumb>();
      images.Id = person.MusicBrainzId;
      if (thumbs.ArtistFanart != null) images.Backdrops.AddRange(SortByLikes(thumbs.ArtistFanart));
      if (thumbs.ArtistBanners != null) images.Banners.AddRange(SortByLikes(thumbs.ArtistBanners));
      if (thumbs.HDArtistLogos != null) images.Logos.AddRange(SortByLikes(thumbs.HDArtistLogos));
      if (thumbs.ArtistThumbnails != null) images.Thumbnails.AddRange(SortByLikes(thumbs.ArtistThumbnails));
      return images;
    }

    protected async Task<ApiWrapperImageCollection<FanArtMovieThumb>> GetMusicLabelFanArtAsync(CompanyInfo company)
    {
      if (company == null || string.IsNullOrEmpty(company.MusicBrainzId))
        return null;
      // Download all image information, filter later!
      FanArtLabelThumbs thumbs = await _fanArtTvHandler.GetLabelThumbsAsync(company.MusicBrainzId).ConfigureAwait(false);
      if (thumbs == null)
        return null;
      ApiWrapperImageCollection<FanArtMovieThumb> images = new ApiWrapperImageCollection<FanArtMovieThumb>();
      images.Id = company.MusicBrainzId;
      if (thumbs.LabelLogos != null) images.Logos.AddRange(SortByLikes(thumbs.LabelLogos));
      return images;
    }

    protected async Task<ApiWrapperImageCollection<FanArtMovieThumb>> GetAlbumFanArtAsync(AlbumInfo album)
    {
      if (album == null || string.IsNullOrEmpty(album.MusicBrainzDiscId))
        return null;
      // Download all image information, filter later!
      FanArtAlbumDetails albumDetails = await _fanArtTvHandler.GetAlbumThumbsAsync(album.MusicBrainzGroupId).ConfigureAwait(false);
      if (albumDetails == null || albumDetails.Albums == null)
        return null;
      FanArtAlbumThumbs thumbs;
      if (!albumDetails.Albums.TryGetValue(album.MusicBrainzDiscId, out thumbs))
        return null;
      ApiWrapperImageCollection<FanArtMovieThumb> images = new ApiWrapperImageCollection<FanArtMovieThumb>();
      images.Id = album.MusicBrainzDiscId;
      if (thumbs.AlbumCovers != null) images.Covers.AddRange(SortByLikes(thumbs.AlbumCovers));
      if (thumbs.CDArts != null) images.DiscArt.AddRange(SortByLikes(thumbs.CDArts));
      return images;
    }

    protected IEnumerable<FanArtMovieThumb> SortByLikes(IEnumerable<FanArtThumb> thumbs)
    {
      return thumbs.OrderByDescending(b => b.Likes).Select(b => new FanArtMovieThumb(b));
    }

    protected IEnumerable<FanArtMovieThumb> SortByLanguageAndLikes(IEnumerable<FanArtMovieThumb> thumbs)
    {
      return thumbs.OrderBy(b => string.IsNullOrEmpty(b.Language)).ThenByDescending(b => b.Likes);
    }

    protected IEnumerable<FanArtMovieThumb> SortBySeasonNumberLanguageAndLikes(int season, IEnumerable<FanArtSeasonThumb> thumbs)
    {
      return SortByLanguageAndLikes(thumbs.Where(b => !b.Season.HasValue || b.Season.Value == season));
    }

    #endregion
  }
}
