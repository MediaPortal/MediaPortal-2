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
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Utilities;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

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

    public override bool SearchMovie(MovieInfo movieSearch, string language, out List<MovieInfo> movies)
    {
      movies = null;
      List<OmDbSearchItem> foundMovies = _omDbHandler.SearchMovie(movieSearch.MovieName.Text, 
        movieSearch.ReleaseDate.HasValue ? movieSearch.ReleaseDate.Value.Year : 0);
      if (foundMovies == null) return false;
      movies = foundMovies.Select(m => new MovieInfo()
      {
        ImdbId = m.ImdbID,
        MovieName = new SimpleTitle(m.Title, true),
        ReleaseDate = m.Year.HasValue ? new DateTime(m.Year.Value, 1, 1) : default(DateTime?),
      }).ToList();

      return movies.Count > 0;
    }

    public override bool SearchSeriesEpisode(EpisodeInfo episodeSearch, string language, out List<EpisodeInfo> episodes)
    {
      episodes = null;
      SeriesInfo seriesSearch = null;
      if(string.IsNullOrEmpty(episodeSearch.SeriesImdbId))
      {
        seriesSearch = episodeSearch.CloneBasicInstance<SeriesInfo>();
        if (!SearchSeriesUniqueAndUpdate(seriesSearch, language))
          return false;
        episodeSearch.CopyIdsFrom(seriesSearch);
      }

      if (!string.IsNullOrEmpty(episodeSearch.SeriesImdbId) && episodeSearch.SeasonNumber.HasValue)
      {
        OmDbSeason season = _omDbHandler.GetSeriesSeason(episodeSearch.SeriesImdbId, episodeSearch.SeasonNumber.Value, false);
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
        info.EpisodeNumbers.AddRange(episodeSearch.EpisodeNumbers);
        episodes.Add(info);
        return true;
      }

      return episodes != null;
    }

    public override bool SearchSeries(SeriesInfo seriesSearch, string language, out List<SeriesInfo> series)
    {
      series = null;
      List<OmDbSearchItem> foundSeries = _omDbHandler.SearchSeries(seriesSearch.SeriesName.Text,
        seriesSearch.FirstAired.HasValue ? seriesSearch.FirstAired.Value.Year : 0);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = _omDbHandler.SearchSeries(seriesSearch.AlternateName, 
          seriesSearch.FirstAired.HasValue ? seriesSearch.FirstAired.Value.Year : 0);
      if (foundSeries == null && seriesSearch.FirstAired.HasValue)
        foundSeries = _omDbHandler.SearchSeries(seriesSearch.SeriesName.Text, 0);
      if (foundSeries == null && seriesSearch.FirstAired.HasValue && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = _omDbHandler.SearchSeries(seriesSearch.AlternateName, 0);
      if (foundSeries == null) return false;
      series = foundSeries.Select(s => new SeriesInfo()
      {
        ImdbId = s.ImdbID,
        SeriesName = new SimpleTitle(s.Title, true),
        FirstAired = s.Year.HasValue ? new DateTime(s.Year.Value, 1, 1) : default(DateTime?),
      }).ToList();
      return series.Count > 0;
    }

    #endregion

    #region Update

    public override bool UpdateFromOnlineMovie(MovieInfo movie, string language, bool cacheOnly)
    {
      OmDbMovie movieDetail = null;
      if (!string.IsNullOrEmpty(movie.ImdbId))
        movieDetail = _omDbHandler.GetMovie(movie.ImdbId, cacheOnly);
      if (movieDetail == null) return false;

      movie.ImdbId = movieDetail.ImdbID;
      movie.MovieName = new SimpleTitle(movieDetail.Title, true);
      movie.Summary = new SimpleTitle(movieDetail.Plot, true);
      movie.Certification = movieDetail.Rated;

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
        movie.Awards = awards;
      }

      if (movieDetail.ImdbRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref movie.Rating, new SimpleRating(movieDetail.ImdbVotes, movieDetail.ImdbVotes));
      }
      if (movieDetail.TomatoRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref movie.Rating, new SimpleRating(movieDetail.TomatoRating, movieDetail.TomatoTotalReviews));
      }
      if (movieDetail.TomatoUserRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref movie.Rating, new SimpleRating(movieDetail.TomatoUserRating, movieDetail.TomatoUserTotalReviews));
      }

      movie.Genres = movieDetail.Genres.Select(s => new GenreInfo { Name = s }).ToList();

      //Only use these if absolutely necessary because there is no way to ID them
      if (movie.Actors.Count == 0)
        movie.Actors = ConvertToPersons(movieDetail.Actors, PersonAspect.OCCUPATION_ACTOR, movieDetail.Title);
      if (movie.Writers.Count == 0)
        movie.Writers = ConvertToPersons(movieDetail.Writers, PersonAspect.OCCUPATION_WRITER, movieDetail.Title);
      if (movie.Directors.Count == 0)
        movie.Directors = ConvertToPersons(movieDetail.Directors, PersonAspect.OCCUPATION_DIRECTOR, movieDetail.Title);

      return true;
    }

    public override bool UpdateFromOnlineSeries(SeriesInfo series, string language, bool cacheOnly)
    {
      OmDbSeries seriesDetail = null;
      if (!string.IsNullOrEmpty(series.ImdbId))
        seriesDetail = _omDbHandler.GetSeries(series.ImdbId, cacheOnly);
      if (seriesDetail == null) return false;

      series.ImdbId = seriesDetail.ImdbID;

      series.SeriesName = new SimpleTitle(seriesDetail.Title, true);
      series.FirstAired = seriesDetail.Released;
      series.Description = new SimpleTitle(seriesDetail.Plot, true);
      if (seriesDetail.EndYear.HasValue)
      {
        series.IsEnded = true;
      }
      series.Certification = seriesDetail.Rated;

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
        series.Awards = awards;
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
      series.Genres = seriesDetail.Genres.Select(s => new GenreInfo { Name = s }).ToList();

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

    public override bool UpdateFromOnlineSeriesSeason(SeasonInfo season, string language, bool cacheOnly)
    {
      OmDbSeason seasonDetail = null;
      if (!string.IsNullOrEmpty(season.SeriesImdbId) && season.SeasonNumber.HasValue)
        seasonDetail = _omDbHandler.GetSeriesSeason(season.SeriesImdbId, season.SeasonNumber.Value, cacheOnly);
      if (seasonDetail == null) return false;

      season.SeriesName = new SimpleTitle(seasonDetail.Title, true);
      season.FirstAired = seasonDetail.Episodes != null && seasonDetail.Episodes.Count > 0 ? seasonDetail.Episodes[0].Released : default(DateTime?);
      season.SeasonNumber = seasonDetail.SeasonNumber;
      season.TotalEpisodes = seasonDetail.Episodes.Count;

      return true;
    }

    public override bool UpdateFromOnlineSeriesEpisode(EpisodeInfo episode, string language, bool cacheOnly)
    {
      List<EpisodeInfo> episodeDetails = new List<EpisodeInfo>();
      OmDbEpisode episodeDetail = null;
      
      if (!string.IsNullOrEmpty(episode.SeriesImdbId) && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
      {
        OmDbSeason seasonDetail = _omDbHandler.GetSeriesSeason(episode.SeriesImdbId, 1, cacheOnly);

        foreach (int episodeNumber in episode.EpisodeNumbers)
        {
          episodeDetail = _omDbHandler.GetSeriesEpisode(episode.SeriesImdbId, episode.SeasonNumber.Value, episodeNumber, cacheOnly);
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
            Genres = episodeDetail.Genres.Select(s => new GenreInfo { Name = s }).ToList(),
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
