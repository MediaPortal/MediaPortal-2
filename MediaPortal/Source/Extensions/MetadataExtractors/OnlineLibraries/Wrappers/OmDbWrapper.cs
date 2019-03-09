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
using MediaPortal.Common.Certifications;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class OmDbWrapper : ApiWrapper<object, string>
  {
    protected OmDbApiV1 _omDbHandler;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath, bool useHttps)
    {
      _omDbHandler = new OmDbApiV1(cachePath, useHttps);
      return true;
    }

    #region Search

    public override async Task<List<MovieInfo>> SearchMovieAsync(MovieInfo movieSearch, string language)
    {
      List<OmDbSearchItem> foundMovies = await _omDbHandler.SearchMovieAsync(movieSearch.MovieName.Text, 
        movieSearch.ReleaseDate.HasValue ? movieSearch.ReleaseDate.Value.Year : 0).ConfigureAwait(false);
      if (foundMovies == null || foundMovies.Count == 0) return null;
      return foundMovies.Select(m => new MovieInfo()
      {
        ImdbId = m.ImdbID,
        MovieName = new SimpleTitle(m.Title, true),
        ReleaseDate = m.Year.HasValue ? new DateTime(m.Year.Value, 1, 1) : default(DateTime?),
      }).ToList();
    }

    public override async Task<List<EpisodeInfo>> SearchSeriesEpisodeAsync(EpisodeInfo episodeSearch, string language)
    {
      SeriesInfo seriesSearch = null;
      if(string.IsNullOrEmpty(episodeSearch.SeriesImdbId))
      {
        seriesSearch = episodeSearch.CloneBasicInstance<SeriesInfo>();
        if (!await SearchSeriesUniqueAndUpdateAsync(seriesSearch, language))
          return null;
        episodeSearch.CopyIdsFrom(seriesSearch);
      }

      List<EpisodeInfo> episodes = null;
      if (!string.IsNullOrEmpty(episodeSearch.SeriesImdbId) && episodeSearch.SeasonNumber.HasValue)
      {
        OmDbSeason season = await _omDbHandler.GetSeriesSeasonAsync(episodeSearch.SeriesImdbId, episodeSearch.SeasonNumber.Value, false).ConfigureAwait(false);
        if (season != null && season.Episodes != null)
        {
          foreach (OmDbSeasonEpisode episode in season.Episodes)
          {
            if ((episode.EpisodeNumber.HasValue  && episodeSearch.EpisodeNumbers.Contains(episode.EpisodeNumber.Value)) || episodeSearch.EpisodeNumbers.Count == 0)
            {
              if (episodes == null)
                episodes = new List<EpisodeInfo>();

              EpisodeInfo info = new EpisodeInfo()
              {
                ImdbId = episode.ImdbID,
                SeriesName = new SimpleTitle(season.Title, true),
                SeasonNumber = episodeSearch.SeasonNumber.Value,
                EpisodeName = new SimpleTitle(episode.Title, false),
              };
              if(episode.EpisodeNumber.HasValue)
                info.EpisodeNumbers.Add(episode.EpisodeNumber.Value);
              info.CopyIdsFrom(episodeSearch.CloneBasicInstance<SeriesInfo>());
              episodes.Add(info);
            }
          }
        }
      }

      if (episodes == null)
      {
        episodes = new List<EpisodeInfo>();
        EpisodeInfo info = new EpisodeInfo()
        {
          SeriesName = seriesSearch == null ? episodeSearch.SeriesName : seriesSearch.SeriesName,
          SeasonNumber = episodeSearch.SeasonNumber,
          EpisodeName = episodeSearch.EpisodeName,
        };
        info.CopyIdsFrom(seriesSearch);
        info.EpisodeNumbers = info.EpisodeNumbers.Union(episodeSearch.EpisodeNumbers).ToList();
        episodes.Add(info);
      }

      return episodes;
    }

    public override async Task<List<SeriesInfo>> SearchSeriesAsync(SeriesInfo seriesSearch, string language)
    {
      List<OmDbSearchItem> foundSeries = await _omDbHandler.SearchSeriesAsync(seriesSearch.SeriesName.Text,
        seriesSearch.FirstAired.HasValue ? seriesSearch.FirstAired.Value.Year : 0).ConfigureAwait(false);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = await _omDbHandler.SearchSeriesAsync(seriesSearch.AlternateName, 
          seriesSearch.FirstAired.HasValue ? seriesSearch.FirstAired.Value.Year : 0).ConfigureAwait(false);
      if (foundSeries == null && seriesSearch.FirstAired.HasValue)
        foundSeries = await _omDbHandler.SearchSeriesAsync(seriesSearch.SeriesName.Text, 0).ConfigureAwait(false);
      if (foundSeries == null && seriesSearch.FirstAired.HasValue && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = await _omDbHandler.SearchSeriesAsync(seriesSearch.AlternateName, 0).ConfigureAwait(false);
      if (foundSeries == null) return null;
      List<SeriesInfo> series = foundSeries.Select(s => new SeriesInfo()
      {
        ImdbId = s.ImdbID,
        SeriesName = new SimpleTitle(s.Title, true),
        FirstAired = s.Year.HasValue ? new DateTime(s.Year.Value, 1, 1) : default(DateTime?),
      }).ToList();
      return series;
    }

    #endregion

    #region Update

    public override async Task<bool> UpdateFromOnlineMovieAsync(MovieInfo movie, string language, bool cacheOnly)
    {
      try
      {
        OmDbMovie movieDetail = null;
        if (!string.IsNullOrEmpty(movie.ImdbId))
          movieDetail = await _omDbHandler.GetMovie(movie.ImdbId, cacheOnly).ConfigureAwait(false);
        if (movieDetail == null) return false;

        movie.ImdbId = movieDetail.ImdbID;
        movie.MovieName = new SimpleTitle(movieDetail.Title, true);
        movie.Summary = new SimpleTitle(movieDetail.Plot, true);

        CertificationMapping cert = null;
        if (CertificationMapper.TryFindMovieCertification(movieDetail.Rated, out cert))
        {
          movie.Certification = cert.CertificationId;
        }

        movie.Revenue = movieDetail.Revenue.HasValue ? movieDetail.Revenue.Value : 0;
        movie.Runtime = movieDetail.Runtime.HasValue ? movieDetail.Runtime.Value : 0;
        movie.ReleaseDate = movieDetail.Released;

        List<string> awards = new List<string>();
        if (!string.IsNullOrEmpty(movieDetail.Awards))
        {
          if (movieDetail.Awards.IndexOf("Won ", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            movieDetail.Awards.IndexOf(" Oscar", StringComparison.InvariantCultureIgnoreCase) >= 0)
          {
            awards.Add("Oscar");
          }
          if (movieDetail.Awards.IndexOf("Won ", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            movieDetail.Awards.IndexOf(" Golden Globe", StringComparison.InvariantCultureIgnoreCase) >= 0)
          {
            awards.Add("Golden Globe");
          }
          movie.Awards = awards.ToList();
        }

        if (movieDetail.ImdbRating.HasValue)
        {
          MetadataUpdater.SetOrUpdateRatings(ref movie.Rating, new SimpleRating(movieDetail.ImdbRating, movieDetail.ImdbVotes));
        }
        if (movieDetail.TomatoRating.HasValue)
        {
          MetadataUpdater.SetOrUpdateRatings(ref movie.Rating, new SimpleRating(movieDetail.TomatoRating, movieDetail.TomatoTotalReviews));
        }
        if (movieDetail.TomatoUserRating.HasValue)
        {
          MetadataUpdater.SetOrUpdateRatings(ref movie.Rating, new SimpleRating(movieDetail.TomatoUserRating, movieDetail.TomatoUserTotalReviews));
        }

        movie.Genres = movieDetail.Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList();

        //Only use these if absolutely necessary because there is no way to ID them
        if (movie.Actors.Count == 0)
          movie.Actors = ConvertToPersons(movieDetail.Actors, PersonAspect.OCCUPATION_ACTOR, movieDetail.Title);
        if (movie.Writers.Count == 0)
          movie.Writers = ConvertToPersons(movieDetail.Writers, PersonAspect.OCCUPATION_WRITER, movieDetail.Title);
        if (movie.Directors.Count == 0)
          movie.Directors = ConvertToPersons(movieDetail.Directors, PersonAspect.OCCUPATION_DIRECTOR, movieDetail.Title);

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Exception while processing movie {0}", ex, movie.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesAsync(SeriesInfo series, string language, bool cacheOnly)
    {
      try
      {
        OmDbSeries seriesDetail = null;
        if (!string.IsNullOrEmpty(series.ImdbId))
          seriesDetail = await _omDbHandler.GetSeriesAsync(series.ImdbId, cacheOnly).ConfigureAwait(false);
        if (seriesDetail == null) return false;

        series.ImdbId = seriesDetail.ImdbID;

        series.SeriesName = new SimpleTitle(seriesDetail.Title, true);
        series.FirstAired = seriesDetail.Released;
        series.Description = new SimpleTitle(seriesDetail.Plot, true);
        if (seriesDetail.EndYear.HasValue)
        {
          series.IsEnded = true;
        }

        CertificationMapping cert = null;
        if (CertificationMapper.TryFindMovieCertification(seriesDetail.Rated, out cert))
        {
          series.Certification = cert.CertificationId;
        }

        List<string> awards = new List<string>();
        if (!string.IsNullOrEmpty(seriesDetail.Awards))
        {
          if (seriesDetail.Awards.IndexOf("Won ", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            seriesDetail.Awards.IndexOf(" Oscar", StringComparison.InvariantCultureIgnoreCase) >= 0)
          {
            awards.Add("Oscar");
          }
          if (seriesDetail.Awards.IndexOf("Won ", StringComparison.InvariantCultureIgnoreCase) >= 0 ||
            seriesDetail.Awards.IndexOf(" Golden Globe", StringComparison.InvariantCultureIgnoreCase) >= 0)
          {
            awards.Add("Golden Globe");
          }
          series.Awards = awards.ToList();
        }

        if (seriesDetail.ImdbRating.HasValue)
        {
          MetadataUpdater.SetOrUpdateRatings(ref series.Rating, new SimpleRating(seriesDetail.ImdbRating, seriesDetail.ImdbVotes));
        }
        if (seriesDetail.TomatoRating.HasValue)
        {
          MetadataUpdater.SetOrUpdateRatings(ref series.Rating, new SimpleRating(seriesDetail.TomatoRating, seriesDetail.TomatoTotalReviews));
        }
        if (seriesDetail.TomatoUserRating.HasValue)
        {
          MetadataUpdater.SetOrUpdateRatings(ref series.Rating, new SimpleRating(seriesDetail.TomatoUserRating, seriesDetail.TomatoUserTotalReviews));
        }
        series.Genres = seriesDetail.Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList();

        //Only use these if absolutely necessary because there is no way to ID them
        if (seriesDetail.Actors == null || seriesDetail.Actors.Count == 0)
          series.Actors = ConvertToPersons(seriesDetail.Actors, PersonAspect.OCCUPATION_ACTOR, null, seriesDetail.Title);

        //Episode listing is currently not optimal
        //OmDbSeason seasonDetails = null;
        //OmDbSeasonEpisode nextEpisode = null;
        //int seasonNumber = 1;
        //while (true)
        //{
        //  seasonDetails = _omDbHandler.GetSeriesSeason(series.ImdbId, seasonNumber, cacheOnly);
        //  if (seasonDetails != null)
        //  {
        //    SeasonInfo seasonInfo = new SeasonInfo()
        //    {
        //      SeriesImdbId = seriesDetail.ImdbID,
        //      SeriesName = new SimpleTitle(seriesDetail.Title, true),
        //      SeasonNumber = seasonDetails.SeasonNumber,
        //      FirstAired = seasonDetails.Episodes.First().Released,
        //      TotalEpisodes = seasonDetails.Episodes.Count
        //    };
        //    if (!series.Seasons.Contains(seasonInfo))
        //      series.Seasons.Add(seasonInfo);

        //    foreach (OmDbSeasonEpisode episodeDetail in seasonDetails.Episodes)
        //    {
        //      if (episodeDetail.EpisodeNumber <= 0) continue;

        //      EpisodeInfo info = new EpisodeInfo()
        //      {
        //        ImdbId = episodeDetail.ImdbID,

        //        SeriesImdbId = seriesDetail.ImdbID,
        //        SeriesName = new SimpleTitle(seriesDetail.Title, true),
        //        SeriesFirstAired = series.FirstAired,

        //        SeasonNumber = seasonNumber,
        //        EpisodeNumbers = new List<int>(new int[] { episodeDetail.EpisodeNumber }),
        //        FirstAired = episodeDetail.Released,
        //        EpisodeName = new SimpleTitle(episodeDetail.Title, true),
        //      };

        //      series.Episodes.Add(info);

        //      if (nextEpisode == null && episodeDetail.Released > DateTime.Now)
        //      {
        //        series.NextEpisodeName = new SimpleTitle(episodeDetail.Title, true);
        //        series.NextEpisodeAirDate = episodeDetail.Released;
        //        series.NextEpisodeSeasonNumber = seasonDetails.SeasonNumber;
        //        series.NextEpisodeNumber = episodeDetail.EpisodeNumber;
        //      }
        //    }
        //    seasonNumber++;
        //  }
        //  else
        //  {
        //    break;
        //  }
        //}
        series.TotalSeasons = series.Seasons.Count;
        series.TotalEpisodes = series.Episodes.Count;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Exception while processing series {0}", ex, series.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesSeasonAsync(SeasonInfo season, string language, bool cacheOnly)
    {
      try
      {
        OmDbSeason seasonDetail = null;
        if (!string.IsNullOrEmpty(season.SeriesImdbId) && season.SeasonNumber.HasValue)
          seasonDetail = await _omDbHandler.GetSeriesSeasonAsync(season.SeriesImdbId, season.SeasonNumber.Value, cacheOnly).ConfigureAwait(false);
        if (seasonDetail == null) return false;

        season.SeriesName = new SimpleTitle(seasonDetail.Title, true);
        season.FirstAired = seasonDetail.Episodes != null && seasonDetail.Episodes.Count > 0 ? seasonDetail.Episodes[0].Released : default(DateTime?);
        season.SeasonNumber = seasonDetail.SeasonNumber;
        season.TotalEpisodes = seasonDetail.Episodes.Count;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Exception while processing season {0}", ex, season.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesEpisodeAsync(EpisodeInfo episode, string language, bool cacheOnly)
    {
      try
      {
        List<EpisodeInfo> episodeDetails = new List<EpisodeInfo>();
        OmDbEpisode episodeDetail = null;

        if (!string.IsNullOrEmpty(episode.SeriesImdbId) && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
        {
          OmDbSeason seasonDetail = await _omDbHandler.GetSeriesSeasonAsync(episode.SeriesImdbId, 1, cacheOnly).ConfigureAwait(false);

          foreach (int episodeNumber in episode.EpisodeNumbers)
          {
            episodeDetail = await _omDbHandler.GetSeriesEpisodeAsync(episode.SeriesImdbId, episode.SeasonNumber.Value, episodeNumber, cacheOnly).ConfigureAwait(false);
            if (episodeDetail == null) continue;
            if (episodeDetail.EpisodeNumber <= 0) continue;

            EpisodeInfo info = new EpisodeInfo()
            {
              ImdbId = episodeDetail.ImdbID,

              SeriesImdbId = episodeDetail.ImdbSeriesID,
              SeriesName = new SimpleTitle(seasonDetail.Title, true),
              SeriesFirstAired = seasonDetail != null && seasonDetail.Episodes != null && seasonDetail.Episodes.Count > 0 ?
                seasonDetail.Episodes[0].Released : default(DateTime?),

              SeasonNumber = episodeDetail.SeasonNumber,
              EpisodeNumbers = episodeDetail.EpisodeNumber.HasValue ? new List<int>(new int[] { episodeDetail.EpisodeNumber.Value }) : null,
              FirstAired = episodeDetail.Released,
              EpisodeName = new SimpleTitle(episodeDetail.Title, true),
              Summary = new SimpleTitle(episodeDetail.Plot, true),
              Genres = episodeDetail.Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList(),
            };

            if (episodeDetail.ImdbRating.HasValue)
            {
              MetadataUpdater.SetOrUpdateRatings(ref info.Rating, new SimpleRating(episodeDetail.ImdbRating, episodeDetail.ImdbVotes));
            }
            if (episodeDetail.TomatoRating.HasValue)
            {
              MetadataUpdater.SetOrUpdateRatings(ref info.Rating, new SimpleRating(episodeDetail.TomatoRating, episodeDetail.TomatoTotalReviews));
            }
            if (episodeDetail.TomatoUserRating.HasValue)
            {
              MetadataUpdater.SetOrUpdateRatings(ref info.Rating, new SimpleRating(episodeDetail.TomatoUserRating, episodeDetail.TomatoUserTotalReviews));
            }

            //Only use these if absolutely necessary because there is no way to ID them
            if (episode.Actors == null || episode.Actors.Count == 0)
              info.Actors = ConvertToPersons(episodeDetail.Actors, PersonAspect.OCCUPATION_ARTIST, episodeDetail.Title, seasonDetail.Title);
            if (episode.Directors == null || episode.Directors.Count == 0)
              info.Directors = ConvertToPersons(episodeDetail.Writers, PersonAspect.OCCUPATION_DIRECTOR, episodeDetail.Title, seasonDetail.Title);
            if (episode.Writers == null || episode.Writers.Count == 0)
              info.Writers = ConvertToPersons(episodeDetail.Directors, PersonAspect.OCCUPATION_WRITER, episodeDetail.Title, seasonDetail.Title);

            episodeDetails.Add(info);
          }
        }
        if (episodeDetails.Count > 1)
        {
          SetMultiEpisodeDetails(episode, episodeDetails);
          return true;
        }
        else if (episodeDetails.Count > 0)
        {
          SetEpisodeDetails(episode, episodeDetails[0]);
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("OmDbWrapper: Exception while processing episode {0}", ex, episode.ToString());
        return false;
      }
    }

    #endregion

    #region Convert

    private List<PersonInfo> ConvertToPersons(List<string> names, string occupation, string media, string parentMedia = null)
    {
      if (names == null || names.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (string name in names)
        retValue.Add(new PersonInfo() { Name = name, Occupation = occupation, Order = sortOrder++, MediaName = media, ParentMediaName = parentMedia });
      return retValue;
    }

    #endregion
  }
}
