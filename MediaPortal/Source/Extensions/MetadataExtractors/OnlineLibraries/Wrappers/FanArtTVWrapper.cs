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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3;
using MediaPortal.Extensions.OnlineLibraries.Libraries.FanArtTVV3.Data;

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

    public override bool UpdateFromOnlineMovie(MovieInfo movie, string language, bool cacheOnly)
    {
      if (movie.MovieDbId > 0 || !string.IsNullOrEmpty(movie.ImdbId))
        return true;
      return false;
    }

    public override bool UpdateFromOnlineSeries(SeriesInfo series, string language, bool cacheOnly)
    {
      if (series.TvdbId > 0)
        return true;
      return false;
    }

    public override bool UpdateFromOnlineSeriesSeason(SeasonInfo season, string language, bool cacheOnly)
    {
      if (season.SeriesTvdbId > 0)
        return true;
      return false;
    }

    public override bool UpdateFromOnlineSeriesEpisode(EpisodeInfo episode, string language, bool cacheOnly)
    {
      if (episode.SeriesTvdbId > 0)
        return true;
      return false;
    }

    public override bool UpdateFromOnlineMusicTrack(TrackInfo track, string language, bool cacheOnly)
    {
      if (!string.IsNullOrEmpty(track.MusicBrainzId))
        return true;
      return false;
    }

    public override bool UpdateFromOnlineMusicTrackAlbum(AlbumInfo album, string language, bool cacheOnly)
    {
      if (!string.IsNullOrEmpty(album.MusicBrainzGroupId))
        return true;
      return false;
    }

    public override bool UpdateFromOnlineMusicTrackAlbumCompany(AlbumInfo albumInfo, CompanyInfo company, string language, bool cacheOnly)
    {
      if (!string.IsNullOrEmpty(company.MusicBrainzId))
        return true;
      return false;
    }

    public override bool UpdateFromOnlineMusicTrackAlbumPerson(AlbumInfo albumInfo, PersonInfo person, string language, bool cacheOnly)
    {
      if (!string.IsNullOrEmpty(person.MusicBrainzId))
        return true;
      return false;
    }

    public override bool UpdateFromOnlineMusicTrackPerson(TrackInfo trackInfo, PersonInfo person, string language, bool cacheOnly)
    {
      if (!string.IsNullOrEmpty(person.MusicBrainzId))
        return true;
      return false;
    }

    #endregion

    #region FanArt

    public override bool GetFanArt<T>(T infoObject, string language, string fanartMediaType, out ApiWrapperImageCollection<FanArtMovieThumb> images)
    {
      images = new ApiWrapperImageCollection<FanArtMovieThumb>();

      try
      {
        if (fanartMediaType == FanArtMediaTypes.Movie)
        {
          FanArtMovieThumbs imgs = null;
          MovieInfo movie = infoObject as MovieInfo;
          if (movie != null && movie.MovieDbId > 0)
          {
            // Download all image information, filter later!
            imgs = _fanArtTvHandler.GetMovieThumbs(movie.MovieDbId.ToString());
          }

          if (imgs != null)
          {
            images.Id = movie.MovieDbId.ToString();
            if (imgs.MovieFanArt != null) images.Backdrops.AddRange(imgs.MovieFanArt.OrderByDescending(b => b.Likes).ToList());
            if (imgs.MovieBanners != null) images.Banners.AddRange(imgs.MovieBanners.OrderByDescending(b => b.Likes).ToList());
            if (imgs.MoviePosters != null) images.Posters.AddRange(imgs.MoviePosters.OrderByDescending(b => b.Likes).ToList());
            if (imgs.MovieCDArt != null) images.DiscArt.AddRange(imgs.MovieCDArt.OrderByDescending(b => b.Likes).ToList());
            if (imgs.HDMovieClearArt != null) images.ClearArt.AddRange(imgs.HDMovieClearArt.OrderByDescending(b => b.Likes).ToList());
            if (imgs.HDMovieLogos != null) images.Logos.AddRange(imgs.HDMovieLogos.OrderByDescending(b => b.Likes).ToList());
            if (imgs.MovieThumbnails != null) images.Thumbnails.AddRange(imgs.MovieThumbnails.OrderByDescending(b => b.Likes).ToList());
            return true;
          }
        }
        else if (fanartMediaType == FanArtMediaTypes.Series)
        {
          FanArtTVThumbs imgs = null;
          EpisodeInfo episode = infoObject as EpisodeInfo;
          SeasonInfo season = infoObject as SeasonInfo;
          SeriesInfo series = infoObject as SeriesInfo;
          if (series == null && season != null)
          {
            series = season.CloneBasicInstance<SeriesInfo>();
          }
          if (series == null && episode != null)
          {
            series = episode.CloneBasicInstance<SeriesInfo>();
          }
          if (series != null && series.TvdbId > 0)
          {
            // Download all image information, filter later!
            imgs = _fanArtTvHandler.GetSeriesThumbs(series.TvdbId.ToString());
          }

          if (imgs != null)
          {
            images.Id = series.TvdbId.ToString();
            if (imgs.SeriesFanArt != null) images.Backdrops.AddRange(imgs.SeriesFanArt.OrderByDescending(b => b.Likes).ToList());
            if (imgs.SeriesBanners != null) images.Banners.AddRange(imgs.SeriesBanners.OrderByDescending(b => b.Likes).ToList());
            if (imgs.SeriesPosters != null) images.Posters.AddRange(imgs.SeriesPosters.OrderByDescending(b => b.Likes).ToList());
            if (imgs.HDSeriesClearArt != null) images.ClearArt.AddRange(imgs.HDSeriesClearArt.OrderByDescending(b => b.Likes).ToList());
            if (imgs.HDSeriesLogos != null) images.Logos.AddRange(imgs.HDSeriesLogos.OrderByDescending(b => b.Likes).ToList());
            if (imgs.SeriesThumbnails != null) images.Thumbnails.AddRange(imgs.SeriesThumbnails.OrderByDescending(b => b.Likes).ToList());
            return true;
          }
        }
        else if (fanartMediaType == FanArtMediaTypes.SeriesSeason)
        {
          FanArtTVThumbs imgs = null;
          int seasonNo = 0;
          EpisodeInfo episode = infoObject as EpisodeInfo;
          SeasonInfo season = infoObject as SeasonInfo;
          if (season == null && episode != null)
          {
            season = episode.CloneBasicInstance<SeasonInfo>();
          }
          if (season != null && season.SeriesTvdbId > 0 && season.SeasonNumber.HasValue)
          {
            // Download all image information, filter later!
            imgs = _fanArtTvHandler.GetSeriesThumbs(season.SeriesTvdbId.ToString());
            seasonNo = season.SeasonNumber.Value;
          }

          if (imgs != null)
          {
            images.Id = season.SeriesTvdbId.ToString();
            if (imgs.SeasonBanners != null) images.Banners.AddRange(imgs.SeasonBanners.FindAll(b => !b.Season.HasValue || b.Season == seasonNo).
              OrderByDescending(b => b.Likes).ToList());
            if (imgs.SeasonPosters != null) images.Posters.AddRange(imgs.SeasonPosters.FindAll(b => !b.Season.HasValue || b.Season == seasonNo).
              OrderByDescending(b => b.Likes).ToList());
            if (imgs.SeasonThumbnails != null) images.Thumbnails.AddRange(imgs.SeasonThumbnails.FindAll(b => !b.Season.HasValue || b.Season == seasonNo).
              OrderByDescending(b => b.Likes).ToList());
            return true;
          }
        }
        else if (fanartMediaType == FanArtMediaTypes.Artist)
        {
          FanArtArtistThumbs imgs = null;
          PersonInfo person = infoObject as PersonInfo;
          if (person != null && !string.IsNullOrEmpty(person.MusicBrainzId))
          {
            // Download all image information, filter later!
            imgs = _fanArtTvHandler.GetArtistThumbs(person.MusicBrainzId);
          }

          if (imgs != null)
          {
            images.Id = person.MusicBrainzId;
            if (imgs.ArtistFanart != null) images.Backdrops.AddRange(imgs.ArtistFanart.OrderByDescending(b => b.Likes).Select(b => new FanArtMovieThumb(b)).ToList());
            if (imgs.ArtistBanners != null) images.Banners.AddRange(imgs.ArtistBanners.OrderByDescending(b => b.Likes).Select(b => new FanArtMovieThumb(b)).ToList());
            if (imgs.HDArtistLogos != null) images.Logos.AddRange(imgs.HDArtistLogos.OrderByDescending(b => b.Likes).Select(b => new FanArtMovieThumb(b)).ToList());
            if (imgs.ArtistThumbnails != null) images.Thumbnails.AddRange(imgs.ArtistThumbnails.OrderByDescending(b => b.Likes).Select(b => new FanArtMovieThumb(b)).ToList());
            return true;
          }
        }
        else if (fanartMediaType == FanArtMediaTypes.MusicLabel)
        {
          FanArtLabelThumbs imgs = null;
          CompanyInfo company = infoObject as CompanyInfo;
          if (company != null && !string.IsNullOrEmpty(company.MusicBrainzId))
          {
            // Download all image information, filter later!
            imgs = _fanArtTvHandler.GetLabelThumbs(company.MusicBrainzId);
          }

          if (imgs != null)
          {
            images.Id = company.MusicBrainzId;
            if (imgs.LabelLogos != null) images.Logos.AddRange(imgs.LabelLogos.OrderByDescending(b => b.Likes).Select(b => new FanArtMovieThumb(b)).ToList());
            return true;
          }
        }
        else if (fanartMediaType == FanArtMediaTypes.Album)
        {
          FanArtAlbumDetails imgs = null;
          string albumId = null;
          TrackInfo track = infoObject as TrackInfo;
          AlbumInfo album = infoObject as AlbumInfo;
          if (album == null && track != null)
          {
            album = track.CloneBasicInstance<AlbumInfo>();
          }
          if (album != null && !string.IsNullOrEmpty(album.MusicBrainzGroupId))
          {
            // Download all image information, filter later!
            imgs = _fanArtTvHandler.GetAlbumThumbs(album.MusicBrainzGroupId);
            albumId = album.MusicBrainzGroupId;
          }

          if (imgs != null)
          {
            images.Id = albumId;
            if (imgs.Albums != null && imgs.Albums.ContainsKey(albumId) && imgs.Albums[albumId].AlbumCovers != null)
              images.Covers.AddRange(imgs.Albums[albumId].AlbumCovers.OrderByDescending(b => b.Likes).Select(b => new FanArtMovieThumb(b)).ToList());
            if (imgs.Albums != null && imgs.Albums.ContainsKey(albumId) && imgs.Albums[albumId].CDArts != null)
              images.DiscArt.AddRange(imgs.Albums[albumId].CDArts.OrderByDescending(b => b.Likes).Select(b => new FanArtMovieThumb(b)).ToList());
            return true;
          }
        }
        else
        {
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception downloading images", ex);
      }
      return false;
    }

    public override bool DownloadFanArt(string id, FanArtMovieThumb image, string folderPath)
    {
      return _fanArtTvHandler.DownloadImage(id, image, folderPath);
    }

    #endregion
  }
}
