#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
    public bool Init(string cachePath)
    {
      _omDbHandler = new OmDbApiV1(cachePath);
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
        MovieName = m.Title,
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
        seriesSearch = episodeSearch.CloneBasicSeries();
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
            if (episodeSearch.EpisodeNumbers.Contains(episode.EpisodeNumber) || episodeSearch.EpisodeNumbers.Count == 0)
            {
              if (episodes == null)
                episodes = new List<EpisodeInfo>();

              EpisodeInfo info = new EpisodeInfo()
              {
                ImdbId = episode.ImdbID,
                SeriesName = season.Title,
                SeasonNumber = episodeSearch.SeasonNumber.Value,
                EpisodeName = new LanguageText(episode.Title, false),
              };
              info.EpisodeNumbers.Add(episode.EpisodeNumber);
              info.CopyIdsFrom(episodeSearch.CloneBasicSeries());
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
          SeriesName = seriesSearch.SeriesName,
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
      if (foundSeries == null) return false;
      series = foundSeries.Select(s => new SeriesInfo()
      {
        ImdbId = s.ImdbID,
        SeriesName = s.Title,
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
      movie.MovieName = new LanguageText(movieDetail.Title, true);
      movie.Summary = new LanguageText(movieDetail.Plot, true);
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
        movie.TotalRating = movieDetail.ImdbVotes ?? 0;
        movie.RatingCount = movieDetail.ImdbVotes ?? 0;
      }
      if (movieDetail.TomatoRating.HasValue)
      {
        movie.TotalRating = movieDetail.TomatoRating ?? 0;
        movie.RatingCount = movieDetail.TomatoTotalReviews ?? 0;
      }
      if (movieDetail.TomatoUserRating.HasValue)
      {
        movie.TotalRating = movieDetail.TomatoUserRating ?? 0;
        movie.RatingCount = movieDetail.TomatoUserTotalReviews ?? 0;
      }

      movie.Genres = movieDetail.Genres;

      //Only use these if absolutely necessary because there is no way to ID them
      if (movie.Actors.Count == 0)
        movie.Actors = ConvertToPersons(movieDetail.Actors, PersonAspect.OCCUPATION_ACTOR);
      if (movie.Writers.Count == 0)
        movie.Writers = ConvertToPersons(movieDetail.Writers, PersonAspect.OCCUPATION_WRITER);
      if (movie.Directors.Count == 0)
        movie.Directors = ConvertToPersons(movieDetail.Directors, PersonAspect.OCCUPATION_DIRECTOR);

      return true;
    }

    public override bool UpdateFromOnlineSeries(SeriesInfo series, string language, bool cacheOnly)
    {
      OmDbSeries seriesDetail = null;
      if (!string.IsNullOrEmpty(series.ImdbId))
        seriesDetail = _omDbHandler.GetSeries(series.ImdbId, cacheOnly);
      if (seriesDetail == null) return false;

      series.ImdbId = seriesDetail.ImdbID;

      series.SeriesName = new LanguageText(seriesDetail.Title, true);
      series.FirstAired = seriesDetail.Released;
      series.Description = new LanguageText(seriesDetail.Plot, true);
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
        series.TotalRating = seriesDetail.ImdbRating ?? 0;
        series.RatingCount = seriesDetail.ImdbVotes ?? 0;
      }
      else if (seriesDetail.TomatoRating.HasValue)
      {
        series.TotalRating = seriesDetail.TomatoRating ?? 0;
        series.RatingCount = seriesDetail.TomatoTotalReviews ?? 0;
      }
      else if (seriesDetail.TomatoUserRating.HasValue)
      {
        series.TotalRating = seriesDetail.TomatoUserRating ?? 0;
        series.RatingCount = seriesDetail.TomatoUserTotalReviews ?? 0;
      }
      series.Genres = seriesDetail.Genres;

      //Only use these if absolutely necessary because there is no way to ID them
      if (seriesDetail.Actors == null || seriesDetail.Actors.Count == 0)
        series.Actors = ConvertToPersons(seriesDetail.Actors, PersonAspect.OCCUPATION_ACTOR);

      OmDbSeason seasonDetails = null;
      int seasonNumber = 1;
      while(true)
      {
        seasonDetails = _omDbHandler.GetSeriesSeason(series.ImdbId, seasonNumber, cacheOnly);
        if (seasonDetails != null)
        {
          OmDbSeasonEpisode episodeDetails = seasonDetails.Episodes.Where(e => e.Released > DateTime.Now).FirstOrDefault();
          if (episodeDetails == null)
          {
            seasonNumber++;
            continue;
          }
          if (episodeDetails != null)
          {
            series.NextEpisodeName = new LanguageText(episodeDetails.Title, true);
            series.NextEpisodeAirDate = episodeDetails.Released;
            series.NextEpisodeSeasonNumber = seasonDetails.SeasonNumber;
            series.NextEpisodeNumber = episodeDetails.EpisodeNumber;
          }
        }
        break;
      }

      return true;
    }

    public override bool UpdateFromOnlineSeriesSeason(SeasonInfo season, string language, bool cacheOnly)
    {
      OmDbSeason seasonDetail = null;
      if (!string.IsNullOrEmpty(season.SeriesImdbId) && season.SeasonNumber.HasValue)
        seasonDetail = _omDbHandler.GetSeriesSeason(season.SeriesImdbId, season.SeasonNumber.Value, cacheOnly);
      if (seasonDetail == null) return false;

      season.SeriesName = new LanguageText(seasonDetail.Title, true);
      season.FirstAired = seasonDetail.Episodes != null && seasonDetail.Episodes.Count > 0 ? seasonDetail.Episodes[0].Released : default(DateTime?);
      season.SeasonNumber = seasonDetail.SeasonNumber;

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
          if (episodeDetail == null) return false;

          EpisodeInfo info = new EpisodeInfo()
          {
            ImdbId = episodeDetail.ImdbID,

            SeriesImdbId = episodeDetail.ImdbSeriesID,
            SeriesName = new LanguageText(seasonDetail.Title, true),
            SeriesFirstAired = seasonDetail != null && seasonDetail.Episodes != null && seasonDetail.Episodes.Count > 0 ? 
              seasonDetail.Episodes[0].Released : default(DateTime?),

            SeasonNumber = episodeDetail.SeasonNumber,
            EpisodeNumbers = new List<int>(new int[] { episodeDetail.EpisodeNumber }),
            FirstAired = episodeDetail.Released,
            EpisodeName = new LanguageText(episodeDetail.Title, true),
            Summary = new LanguageText(episodeDetail.Plot, true),
            Genres = episodeDetail.Genres,
        };

        if (episodeDetail.ImdbRating.HasValue)
        {
            info.TotalRating = episodeDetail.ImdbVotes ?? 0;
            info.RatingCount = episodeDetail.ImdbVotes ?? 0;
        }
        if (episodeDetail.TomatoRating.HasValue)
        {
            info.TotalRating = episodeDetail.TomatoRating ?? 0;
            info.RatingCount = episodeDetail.TomatoTotalReviews ?? 0;
        }
        if (episodeDetail.TomatoUserRating.HasValue)
        {
            info.TotalRating = episodeDetail.TomatoUserRating ?? 0;
            info.RatingCount = episodeDetail.TomatoUserTotalReviews ?? 0;
        }

          //Only use these if absolutely necessary because there is no way to ID them
          if (episode.Actors == null || episode.Actors.Count == 0)
            info.Actors = ConvertToPersons(episodeDetail.Actors, PersonAspect.OCCUPATION_ARTIST);
          if (episode.Directors == null || episode.Directors.Count == 0)
            info.Directors = ConvertToPersons(episodeDetail.Writers, PersonAspect.OCCUPATION_DIRECTOR);
          if (episode.Writers == null || episode.Writers.Count == 0)
            info.Writers = ConvertToPersons(episodeDetail.Directors, PersonAspect.OCCUPATION_WRITER);

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

    private List<PersonInfo> ConvertToPersons(List<string> names, string occupation)
    {
      if (names == null || names.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (string name in names)
        retValue.Add(new PersonInfo() { Name = name, Occupation = occupation, Order = sortOrder++ });
      return retValue;
    }

    #endregion
  }
}
