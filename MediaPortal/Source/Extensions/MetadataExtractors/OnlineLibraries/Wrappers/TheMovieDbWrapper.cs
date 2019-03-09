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
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Utilities;
using MediaPortal.Common.Certifications;
using System.Threading.Tasks;
using MediaPortal.Common.Services.GenreConverter;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class TheMovieDbWrapper : ApiWrapper<ImageItem, string>
  {
    protected MovieDbApiV3 _movieDbHandler;
    protected TimeSpan _cacheTimeout = TimeSpan.FromHours(23.5);
    private bool _movieMode = true;

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath, bool useHttps, bool movieMode)
    {
      _movieDbHandler = new MovieDbApiV3("1e3f311b50e6ca53bbc3fcade2214b5e", cachePath, useHttps);
      SetDefaultLanguage(MovieDbApiV3.DefaultLanguage);
      SetCachePath(cachePath);
      _movieMode = movieMode;
      return true;
    }

    #region Search

    public override async Task<List<MovieInfo>> SearchMovieAsync(MovieInfo movieSearch, string language)
    {
      List<MovieSearchResult> foundMovies = await _movieDbHandler.SearchMovieAsync(movieSearch.MovieName.Text, language).ConfigureAwait(false);
      if (foundMovies == null) return null;
      if (foundMovies.Count == 0 && !string.IsNullOrEmpty(movieSearch.OriginalName))
      {
        foundMovies = await _movieDbHandler.SearchMovieAsync(movieSearch.OriginalName, language).ConfigureAwait(false);
        if (foundMovies == null) return null;
      }
      return foundMovies.Count > 0 ? foundMovies.Select(m => new MovieInfo()
      {
        MovieDbId = m.Id,
        MovieName = new SimpleTitle(m.Title, false),
        OriginalName = m.OriginalTitle,
        ReleaseDate = m.ReleaseDate,
      }).ToList() : null;
    }

    public override async Task<List<EpisodeInfo>> SearchSeriesEpisodeAsync(EpisodeInfo episodeSearch, string language)
    {
      language = language ?? PreferredLanguage;

      SeriesInfo seriesSearch = null;
      if (episodeSearch.SeriesMovieDbId <= 0 && !string.IsNullOrEmpty(episodeSearch.SeriesImdbId) && episodeSearch.SeriesTvdbId <= 0 && episodeSearch.SeriesTvRageId <= 0)
      {
        seriesSearch = episodeSearch.CloneBasicInstance<SeriesInfo>();
        if (!await SearchSeriesUniqueAndUpdateAsync(seriesSearch, language).ConfigureAwait(false))
          return null;
        episodeSearch.CopyIdsFrom(seriesSearch);
      }

      List<EpisodeInfo> episodes = null;
      if ((episodeSearch.SeriesMovieDbId > 0 || !string.IsNullOrEmpty(episodeSearch.SeriesImdbId) || episodeSearch.SeriesTvdbId > 0 || episodeSearch.SeriesTvRageId > 0) && episodeSearch.SeasonNumber.HasValue)
      {
        Season season = null;
        if(episodeSearch.SeriesMovieDbId > 0)
          season = await _movieDbHandler.GetSeriesSeasonAsync(episodeSearch.SeriesMovieDbId, episodeSearch.SeasonNumber.Value, language, false).ConfigureAwait(false);
        if (season == null && !string.IsNullOrEmpty(episodeSearch.SeriesImdbId))
        {
          var results = await _movieDbHandler.FindSeriesByImdbIdAsync(episodeSearch.SeriesImdbId, language);
          if (results.Count == 1)
            season = await _movieDbHandler.GetSeriesSeasonAsync(results.First().Id, episodeSearch.SeasonNumber.Value, language, false).ConfigureAwait(false);
        }
        if (season == null && episodeSearch.SeriesTvdbId > 0)
        {
          var results = await _movieDbHandler.FindSeriesByTvDbIdAsync(episodeSearch.SeriesTvdbId, language);
          if (results.Count == 1)
            season = await _movieDbHandler.GetSeriesSeasonAsync(results.First().Id, episodeSearch.SeasonNumber.Value, language, false).ConfigureAwait(false);
        }
        if (season == null && episodeSearch.SeriesTvRageId > 0)
        {
          var results = await _movieDbHandler.FindSeriesByTvRageIdAsync(episodeSearch.SeriesTvRageId, language);
          if (results.Count == 1)
            season = await _movieDbHandler.GetSeriesSeasonAsync(results.First().Id, episodeSearch.SeasonNumber.Value, language, false).ConfigureAwait(false);
        }
        if (season != null && season.Episodes != null)
        {
          foreach (SeasonEpisode episode in season.Episodes)
          {
            if (episodeSearch.EpisodeNumbers.Contains(episode.EpisodeNumber) || episodeSearch.EpisodeNumbers.Count == 0)
            {
              if (episodes == null)
                episodes = new List<EpisodeInfo>();

              EpisodeInfo info = new EpisodeInfo()
              {
                SeriesName = seriesSearch?.SeriesName ?? episodeSearch.SeriesName,
                SeasonNumber = episode.SeasonNumber,
                EpisodeName = new SimpleTitle(episode.Name, false),
              };
              info.EpisodeNumbers.Add(episode.EpisodeNumber);
              info.CopyIdsFrom(seriesSearch);
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
          SeriesName = seriesSearch?.SeriesName ?? episodeSearch.SeriesName,
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
      language = language ?? PreferredLanguage;
      
      List<SeriesSearchResult> foundSeries = await _movieDbHandler.SearchSeriesAsync(seriesSearch.SeriesName.Text, language).ConfigureAwait(false);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.OriginalName))
        foundSeries = await _movieDbHandler.SearchSeriesAsync(seriesSearch.OriginalName, language).ConfigureAwait(false);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = await _movieDbHandler.SearchSeriesAsync(seriesSearch.AlternateName, language).ConfigureAwait(false);
      if (foundSeries == null) return null;
      return foundSeries.Select(s => new SeriesInfo()
      {
        MovieDbId = s.Id,
        SeriesName = new SimpleTitle(s.Name, true),
        OriginalName = s.OriginalName,
        FirstAired = s.FirstAirDate,
      }).ToList();
    }

    public override async Task<List<PersonInfo>> SearchPersonAsync(PersonInfo personSearch, string language)
    {
      language = language ?? PreferredLanguage;

      List<PersonSearchResult> foundPersons = await _movieDbHandler.SearchPersonAsync(personSearch.Name, language).ConfigureAwait(false);
      if (foundPersons == null) return null;
      return foundPersons.Select(p => new PersonInfo()
      {
        MovieDbId = p.Id,
        Name = p.Name,
      }).ToList();
    }

    public override async Task<List<CompanyInfo>> SearchCompanyAsync(CompanyInfo companySearch, string language)
    {
      language = language ?? PreferredLanguage;
      
      List<CompanySearchResult> foundCompanies = await _movieDbHandler.SearchCompanyAsync(companySearch.Name, language).ConfigureAwait(false);
      if (foundCompanies == null) return null;
      return foundCompanies.Select(p => new CompanyInfo()
      {
        MovieDbId = p.Id,
        Name = p.Name,
      }).ToList();
    }

    #endregion

    #region Update

    public override async Task<bool> UpdateFromOnlineMovieAsync(MovieInfo movie, string language, bool cacheOnly)
    {
      try
      {
        bool cacheIncomplete = false;
        language = language ?? PreferredLanguage;

        Movie movieDetail = null;
        if (movie.MovieDbId > 0)
          movieDetail = await _movieDbHandler.GetMovieAsync(movie.MovieDbId, language, cacheOnly).ConfigureAwait(false);
        if (movieDetail == null && !string.IsNullOrEmpty(movie.ImdbId))
          movieDetail = await _movieDbHandler.GetMovieAsync(movie.ImdbId, language, cacheOnly).ConfigureAwait(false);
        if (movieDetail == null && cacheOnly == false)
        {
          if (!string.IsNullOrEmpty(movie.ImdbId))
          {
            List<IdResult> ids = await _movieDbHandler.FindMovieByImdbId(movie.ImdbId, language).ConfigureAwait(false);
            if (ids != null && ids.Count > 0)
              movieDetail = await _movieDbHandler.GetMovieAsync(ids[0].Id, language, false).ConfigureAwait(false);
          }
        }
        if (movieDetail == null) return false;

        movie.MovieDbId = movieDetail.Id;
        movie.ImdbId = movieDetail.ImdbId;
        movie.Budget = movieDetail.Budget ?? 0;
        movie.CollectionMovieDbId = movieDetail.Collection != null ? movieDetail.Collection.Id : 0;
        movie.CollectionName = movieDetail.Collection != null ? movieDetail.Collection.Name : null;
        movie.Genres = ConvertToMovieGenreIds(movieDetail.Genres);
        movie.MovieName = new SimpleTitle(movieDetail.Title, false);
        movie.OriginalName = movieDetail.OriginalTitle;
        movie.Summary = new SimpleTitle(movieDetail.Overview, false);
        movie.Popularity = movieDetail.Popularity ?? 0;
        movie.ProductionCompanies = ConvertToCompanies(movieDetail.ProductionCompanies, CompanyAspect.COMPANY_PRODUCTION);
        movie.Rating = new SimpleRating(movieDetail.Rating, movieDetail.RatingCount);
        movie.ReleaseDate = movieDetail.ReleaseDate;
        movie.Revenue = movieDetail.Revenue ?? 0;
        movie.Tagline = movieDetail.Tagline;
        movie.Runtime = movieDetail.Runtime ?? 0;

        MovieCasts movieCasts = await _movieDbHandler.GetMovieCastCrewAsync(movieDetail.Id, language, cacheOnly).ConfigureAwait(false);
        if (cacheOnly && movieCasts == null)
          cacheIncomplete = true;
        if (movieCasts != null)
        {
          movie.Actors = ConvertToPersons(movieCasts.Cast, PersonAspect.OCCUPATION_ACTOR, movieDetail.Title);
          movie.Writers = ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Author").ToList(), PersonAspect.OCCUPATION_WRITER, movieDetail.Title);
          movie.Directors = ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Director").ToList(), PersonAspect.OCCUPATION_DIRECTOR, movieDetail.Title);
          movie.Characters = ConvertToCharacters(movieCasts.Cast, movieDetail.Title);
        }

        return !cacheIncomplete;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing movie {0}", ex, movie.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMovieCollectionAsync(MovieCollectionInfo collection, string language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        MovieCollection collectionDetail = null;
        if (collection.MovieDbId > 0)
          collectionDetail = await _movieDbHandler.GetCollectionAsync(collection.MovieDbId, language, cacheOnly).ConfigureAwait(false);
        if (collectionDetail == null) return false;

        collection.MovieDbId = collectionDetail.Id;
        collection.CollectionName = new SimpleTitle(collectionDetail.Name, false);
        collection.Movies = ConvertToMovies(collectionDetail, collectionDetail.Movies);
        collection.TotalMovies = collectionDetail.Movies.Count;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing movie collection {0}", ex, collection.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMoviePersonAsync(MovieInfo movieInfo, PersonInfo person, string language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        Person personDetail = null;
        if (person.MovieDbId > 0)
          personDetail = await _movieDbHandler.GetPersonAsync(person.MovieDbId, language, cacheOnly).ConfigureAwait(false);
        if (personDetail == null && cacheOnly == false)
        {
          List<IdResult> ids = null;

          if (!string.IsNullOrEmpty(person.ImdbId))
            ids = await _movieDbHandler.FindPersonByImdbIdAsync(person.ImdbId, language).ConfigureAwait(false);
          if (personDetail == null && person.TvRageId > 0)
            ids = await _movieDbHandler.FindPersonByTvRageIdAsync(person.TvRageId, language).ConfigureAwait(false);

          if (ids != null && ids.Count > 0)
            personDetail = await _movieDbHandler.GetPersonAsync(ids[0].Id, language, false).ConfigureAwait(false);
        }
        if (personDetail == null) return false;

        person.MovieDbId = personDetail.PersonId;
        person.Name = personDetail.Name;
        person.TvRageId = personDetail.ExternalId.TvRageId ?? 0;
        person.ImdbId = personDetail.ExternalId.ImDbId;
        person.Biography = personDetail.Biography;
        person.DateOfBirth = personDetail.DateOfBirth;
        person.DateOfDeath = personDetail.DateOfDeath;
        person.Orign = personDetail.PlaceOfBirth;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing person {0}", ex, person.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMovieCharacterAsync(MovieInfo movieInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      try
      {
        bool cacheIncomplete = false;
        language = language ?? PreferredLanguage;

        if (movieInfo.MovieDbId <= 0)
          return false;

        MovieCasts movieCasts = await _movieDbHandler.GetMovieCastCrewAsync(movieInfo.MovieDbId, language, cacheOnly).ConfigureAwait(false);
        if (cacheOnly && movieCasts == null)
          cacheIncomplete = true;
        if (movieCasts != null)
        {
          List<CharacterInfo> characters = ConvertToCharacters(movieCasts.Cast);
          int index = characters.IndexOf(character);
          if (index >= 0)
          {
            character.ActorMovieDbId = characters[index].ActorMovieDbId;
            character.ActorName = characters[index].ActorName;
            character.Name = characters[index].Name;
            character.Order = characters[index].Order;

            return true;
          }
        }

        return !cacheIncomplete;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing character {0}", ex, character.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineMovieCompanyAsync(MovieInfo movieInfo, CompanyInfo company, string language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        if (company.Type != CompanyAspect.COMPANY_PRODUCTION)
          return false;
        Company companyDetail = null;
        if (company.MovieDbId > 0)
          companyDetail = await _movieDbHandler.GetCompanyAsync(company.MovieDbId, language, cacheOnly).ConfigureAwait(false);
        if (companyDetail == null) return false;

        company.MovieDbId = companyDetail.Id;
        company.Name = companyDetail.Name;
        company.Description = new SimpleTitle(companyDetail.Description, false);

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing company {0}", ex, company.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesAsync(SeriesInfo series, string language, bool cacheOnly)
    {
      try
      {
        bool cacheIncomplete = false;
        language = language ?? PreferredLanguage;

        Series seriesDetail = null;
        if (series.MovieDbId > 0)
          seriesDetail = await _movieDbHandler.GetSeriesAsync(series.MovieDbId, language, cacheOnly).ConfigureAwait(false);
        if (seriesDetail == null && cacheOnly == false)
        {
          List<IdResult> ids = null;

          if (!string.IsNullOrEmpty(series.ImdbId))
            ids = await _movieDbHandler.FindSeriesByImdbIdAsync(series.ImdbId, language).ConfigureAwait(false);
          if (ids == null && series.TvdbId > 0)
            ids = await _movieDbHandler.FindSeriesByTvDbIdAsync(series.TvdbId, language).ConfigureAwait(false);
          if (ids == null && series.TvRageId > 0)
            ids = await _movieDbHandler.FindSeriesByTvRageIdAsync(series.TvRageId, language).ConfigureAwait(false);

          if (ids != null && ids.Count > 0)
            seriesDetail = await _movieDbHandler.GetSeriesAsync(ids[0].Id, language, false).ConfigureAwait(false);
        }
        if (seriesDetail == null) return false;

        series.MovieDbId = seriesDetail.Id;
        series.TvdbId = seriesDetail.ExternalId.TvDbId ?? 0;
        series.TvRageId = seriesDetail.ExternalId.TvRageId ?? 0;
        series.ImdbId = seriesDetail.ExternalId.ImDbId;

        series.SeriesName = new SimpleTitle(seriesDetail.Name, false);
        series.OriginalName = seriesDetail.OriginalName;
        series.FirstAired = seriesDetail.FirstAirDate;
        series.Description = new SimpleTitle(seriesDetail.Overview, false);
        series.Popularity = seriesDetail.Popularity ?? 0;
        series.Rating = new SimpleRating(seriesDetail.Rating, seriesDetail.RatingCount);
        series.Genres = ConvertToSeriesGenreIds(seriesDetail.Genres);
        series.Networks = ConvertToCompanies(seriesDetail.Networks, CompanyAspect.COMPANY_TV_NETWORK);
        series.ProductionCompanies = ConvertToCompanies(seriesDetail.ProductionCompanies, CompanyAspect.COMPANY_PRODUCTION);
        if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
          series.IsEnded = true;
        }
        if (seriesDetail.ContentRatingResults.Results.Count > 0)
        {
          var cert = seriesDetail.ContentRatingResults.Results.Where(c => c.CountryId == "US").FirstOrDefault();
          if (cert != null)
          {
            CertificationMapping certification = null;
            if (CertificationMapper.TryFindMovieCertification(cert.ContentRating, out certification))
            {
              series.Certification = certification.CertificationId;
            }
          }
          else
          {
            cert = seriesDetail.ContentRatingResults.Results.FirstOrDefault();
            if (cert != null)
            {
              CertificationMapping certification = null;
              if (CertificationMapper.TryFindMovieCertification(cert.ContentRating, out certification))
              {
                series.Certification = certification.CertificationId;
              }
            }
          }
        }

        MovieCasts seriesCast = await _movieDbHandler.GetSeriesCastCrewAsync(seriesDetail.Id, language, cacheOnly).ConfigureAwait(false);
        if (cacheOnly && seriesCast == null)
          cacheIncomplete = true;
        if (seriesCast != null)
        {
          series.Actors = ConvertToPersons(seriesCast.Cast, PersonAspect.OCCUPATION_ACTOR, null, seriesDetail.Name);
          series.Characters = ConvertToCharacters(seriesCast.Cast, null, seriesDetail.Name);
        }

        SeasonEpisode nextEpisode = null;
        foreach (SeriesSeason season in seriesDetail.Seasons)
        {
          Season currentSeason = await _movieDbHandler.GetSeriesSeasonAsync(seriesDetail.Id, season.SeasonNumber, language, cacheOnly).ConfigureAwait(false);
          if (cacheOnly && currentSeason == null)
            cacheIncomplete = true;
          if (currentSeason != null)
          {
            SeasonInfo seasonInfo = new SeasonInfo()
            {
              MovieDbId = currentSeason.SeasonId,
              ImdbId = currentSeason.ExternalId.ImDbId,
              TvdbId = currentSeason.ExternalId.TvDbId ?? 0,
              TvRageId = currentSeason.ExternalId.TvRageId ?? 0,

              SeriesMovieDbId = seriesDetail.Id,
              SeriesImdbId = seriesDetail.ExternalId.ImDbId,
              SeriesTvdbId = seriesDetail.ExternalId.TvDbId ?? 0,
              SeriesTvRageId = seriesDetail.ExternalId.TvRageId ?? 0,
              SeriesName = new SimpleTitle(seriesDetail.Name, false),

              FirstAired = currentSeason.AirDate,
              Description = new SimpleTitle(currentSeason.Overview, false),
              TotalEpisodes = currentSeason.Episodes.Count,
              SeasonNumber = currentSeason.SeasonNumber
            };
            series.Seasons.Add(seasonInfo);

            foreach (SeasonEpisode episodeDetail in currentSeason.Episodes)
            {
              EpisodeInfo episodeInfo = new EpisodeInfo()
              {
                SeriesMovieDbId = seriesDetail.Id,
                SeriesImdbId = seriesDetail.ExternalId.ImDbId,
                SeriesTvdbId = seriesDetail.ExternalId.TvDbId ?? 0,
                SeriesTvRageId = seriesDetail.ExternalId.TvRageId ?? 0,
                SeriesName = new SimpleTitle(seriesDetail.Name, false),
                SeriesFirstAired = seriesDetail.FirstAirDate,

                SeasonNumber = episodeDetail.SeasonNumber,
                EpisodeNumbers = new List<int>(new int[] { episodeDetail.EpisodeNumber }),
                FirstAired = episodeDetail.AirDate,
                Rating = new SimpleRating(episodeDetail.Rating, episodeDetail.RatingCount),
                EpisodeName = new SimpleTitle(episodeDetail.Name, false),
                Summary = new SimpleTitle(episodeDetail.Overview, false),
                Genres = ConvertToSeriesGenreIds(seriesDetail.Genres)
              };

              episodeInfo.Actors = new List<PersonInfo>();
              episodeInfo.Characters = new List<CharacterInfo>();
              if (seriesCast != null)
              {
                episodeInfo.Actors.AddRange(ConvertToPersons(seriesCast.Cast, PersonAspect.OCCUPATION_ACTOR, episodeDetail.Name, seriesDetail.Name));
                episodeInfo.Characters.AddRange(ConvertToCharacters(seriesCast.Cast, episodeDetail.Name, seriesDetail.Name));
              }
              //info.Actors.AddRange(ConvertToPersons(episodeDetail.GuestStars, PersonAspect.OCCUPATION_ACTOR));
              //info.Characters.AddRange(ConvertToCharacters(episodeDetail.GuestStars));
              episodeInfo.Directors = ConvertToPersons(episodeDetail.Crew.Where(p => p.Job == "Director").ToList(), PersonAspect.OCCUPATION_DIRECTOR, episodeDetail.Name, seriesDetail.Name);
              episodeInfo.Writers = ConvertToPersons(episodeDetail.Crew.Where(p => p.Job == "Writer").ToList(), PersonAspect.OCCUPATION_WRITER, episodeDetail.Name, seriesDetail.Name);

              series.Episodes.Add(episodeInfo);

              if (nextEpisode == null && episodeDetail.AirDate > DateTime.Now)
              {
                nextEpisode = episodeDetail;
                series.NextEpisodeName = new SimpleTitle(nextEpisode.Name, false);
                series.NextEpisodeAirDate = nextEpisode.AirDate;
                series.NextEpisodeSeasonNumber = nextEpisode.SeasonNumber;
                series.NextEpisodeNumber = nextEpisode.EpisodeNumber;
              }
            }
          }
        }

        series.TotalSeasons = series.Seasons.Count;
        series.TotalEpisodes = series.Episodes.Count;

        return !cacheIncomplete;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing series {0}", ex, series.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesSeasonAsync(SeasonInfo season, string language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        Series seriesDetail = null;
        Season seasonDetail = null;
        if (season.SeriesMovieDbId > 0)
          seriesDetail = await _movieDbHandler.GetSeriesAsync(season.SeriesMovieDbId, language, cacheOnly).ConfigureAwait(false);
        if (seriesDetail == null && !string.IsNullOrEmpty(season.SeriesImdbId))
        {
          var results = await _movieDbHandler.FindSeriesByImdbIdAsync(season.SeriesImdbId, language);
          if (results.Count == 1)
            seriesDetail = await _movieDbHandler.GetSeriesAsync(results.First().Id, language, cacheOnly).ConfigureAwait(false);
        }
        if (seriesDetail == null && season.SeriesTvdbId > 0)
        {
          var results = await _movieDbHandler.FindSeriesByTvDbIdAsync(season.SeriesTvdbId, language);
          if (results.Count == 1)
            seriesDetail = await _movieDbHandler.GetSeriesAsync(results.First().Id, language, cacheOnly).ConfigureAwait(false);
        }
        if (seriesDetail == null && season.SeriesTvRageId > 0)
        {
          var results = await _movieDbHandler.FindSeriesByTvRageIdAsync(season.SeriesTvRageId, language);
          if (results.Count == 1)
            seriesDetail = await _movieDbHandler.GetSeriesAsync(results.First().Id, language, cacheOnly).ConfigureAwait(false);
        }
        if (seriesDetail == null) return false;
        if (season.SeriesMovieDbId > 0 && season.SeasonNumber.HasValue)
          seasonDetail = await _movieDbHandler.GetSeriesSeasonAsync(season.SeriesMovieDbId, season.SeasonNumber.Value, language, cacheOnly).ConfigureAwait(false);
        if (seasonDetail == null && season.TvdbId > 0)
        {
          var results = await _movieDbHandler.FindSeriesSeasonByTvDbIdAsync(season.TvdbId, language);
          if (results.Count == 1)
            seasonDetail = await _movieDbHandler.GetSeriesSeasonAsync(results.First().Id, season.SeasonNumber.Value, language, cacheOnly).ConfigureAwait(false);
        }
        if (seasonDetail == null && season.TvRageId > 0)
        {
          var results = await _movieDbHandler.FindSeriesSeasonByTvRageIdAsync(season.TvRageId, language);
          if (results.Count == 1)
            seasonDetail = await _movieDbHandler.GetSeriesSeasonAsync(results.First().Id, season.SeasonNumber.Value, language, cacheOnly).ConfigureAwait(false);
        }
        if (seasonDetail == null) return false;

        season.MovieDbId = seasonDetail.SeasonId;
        season.TvdbId = seasonDetail.ExternalId.TvDbId ?? 0;
        season.TvRageId = seasonDetail.ExternalId.TvRageId ?? 0;
        season.ImdbId = seasonDetail.ExternalId.ImDbId;

        season.SeriesMovieDbId = seriesDetail.Id;
        season.SeriesImdbId = seriesDetail.ExternalId.ImDbId;
        season.SeriesTvdbId = seriesDetail.ExternalId.TvDbId ?? 0;
        season.SeriesTvRageId = seriesDetail.ExternalId.TvRageId ?? 0;

        season.SeriesName = new SimpleTitle(seriesDetail.Name, false);
        season.FirstAired = seasonDetail.AirDate;
        season.Description = new SimpleTitle(seasonDetail.Overview, false);
        season.SeasonNumber = seasonDetail.SeasonNumber;
        season.TotalEpisodes = seasonDetail.Episodes.Count;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing season {0}", ex, season.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesEpisodeAsync(EpisodeInfo episode, string language, bool cacheOnly)
    {
      try
      {
        bool cacheIncomplete = false;
        language = language ?? PreferredLanguage;

        List<EpisodeInfo> episodeDetails = new List<EpisodeInfo>();
        Episode episodeDetail = null;
        Series seriesDetail = null;
        MovieCasts seriesCast = null;

        if ((episode.SeriesMovieDbId > 0 || !string.IsNullOrEmpty(episode.SeriesImdbId) || episode.SeriesTvdbId > 0 || episode.SeriesTvRageId > 0) && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
        {
          if (episode.SeriesMovieDbId > 0)
            seriesDetail = await _movieDbHandler.GetSeriesAsync(episode.SeriesMovieDbId, language, cacheOnly).ConfigureAwait(false);
          if (seriesDetail == null && !string.IsNullOrEmpty(episode.SeriesImdbId))
          {
            var results = await _movieDbHandler.FindSeriesByImdbIdAsync(episode.SeriesImdbId, language);
            if (results.Count == 1)
              seriesDetail = await _movieDbHandler.GetSeriesAsync(results.First().Id, language, cacheOnly).ConfigureAwait(false);
          }
          if (seriesDetail == null && episode.SeriesTvdbId > 0)
          {
            var results = await _movieDbHandler.FindSeriesByTvDbIdAsync(episode.SeriesTvdbId, language);
            if (results.Count == 1)
              seriesDetail = await _movieDbHandler.GetSeriesAsync(results.First().Id, language, cacheOnly).ConfigureAwait(false);
          }
          if (seriesDetail == null && episode.SeriesTvRageId > 0)
          {
            var results = await _movieDbHandler.FindSeriesByTvRageIdAsync(episode.SeriesTvRageId, language);
            if (results.Count == 1)
              seriesDetail = await _movieDbHandler.GetSeriesAsync(results.First().Id, language, cacheOnly).ConfigureAwait(false);
          }
          if (seriesDetail == null) return false;
          seriesCast = await _movieDbHandler.GetSeriesCastCrewAsync(seriesDetail.Id, language, cacheOnly).ConfigureAwait(false);
          if (cacheOnly && seriesCast == null)
            cacheIncomplete = true;

          foreach (int episodeNumber in episode.EpisodeNumbers)
          {
            episodeDetail = await _movieDbHandler.GetSeriesEpisodeAsync(seriesDetail.Id, episode.SeasonNumber.Value, episodeNumber, language, cacheOnly).ConfigureAwait(false);
            if (episodeDetail == null) continue;

            EpisodeInfo info = new EpisodeInfo()
            {
              MovieDbId = episodeDetail.Id,
              ImdbId = episodeDetail.ExternalId.ImDbId,
              TvdbId = episodeDetail.ExternalId.TvDbId ?? 0,
              TvRageId = episodeDetail.ExternalId.TvRageId ?? 0,

              SeriesMovieDbId = seriesDetail.Id,
              SeriesImdbId = seriesDetail.ExternalId.ImDbId,
              SeriesTvdbId = seriesDetail.ExternalId.TvDbId ?? 0,
              SeriesTvRageId = seriesDetail.ExternalId.TvRageId ?? 0,
              SeriesName = new SimpleTitle(seriesDetail.Name, false),
              SeriesFirstAired = seriesDetail.FirstAirDate,

              SeasonNumber = episodeDetail.SeasonNumber,
              EpisodeNumbers = new List<int>(new int[] { episodeDetail.EpisodeNumber }),
              FirstAired = episodeDetail.AirDate,
              Rating = new SimpleRating(episodeDetail.Rating, episodeDetail.RatingCount),
              EpisodeName = new SimpleTitle(episodeDetail.Name, false),
              Summary = new SimpleTitle(episodeDetail.Overview, false),
              Genres = ConvertToSeriesGenreIds(seriesDetail.Genres)
            };

            info.Actors = new List<PersonInfo>();
            info.Characters = new List<CharacterInfo>();
            if (seriesCast != null)
            {
              info.Actors.AddRange(ConvertToPersons(seriesCast.Cast, PersonAspect.OCCUPATION_ACTOR, episodeDetail.Name, seriesDetail.Name));
              info.Characters.AddRange(ConvertToCharacters(seriesCast.Cast, episodeDetail.Name, seriesDetail.Name));
            }
            //info.Actors.AddRange(ConvertToPersons(episodeDetail.GuestStars, PersonAspect.OCCUPATION_ACTOR));
            //info.Characters.AddRange(ConvertToCharacters(episodeDetail.GuestStars));
            info.Directors = ConvertToPersons(episodeDetail.Crew.Where(p => p.Job == "Director").ToList(), PersonAspect.OCCUPATION_DIRECTOR, episodeDetail.Name, seriesDetail.Name);
            info.Writers = ConvertToPersons(episodeDetail.Crew.Where(p => p.Job == "Writer").ToList(), PersonAspect.OCCUPATION_WRITER, episodeDetail.Name, seriesDetail.Name);

            episodeDetails.Add(info);
          }
        }
        if (episodeDetails.Count > 1)
        {
          SetMultiEpisodeDetails(episode, episodeDetails);
          return !cacheIncomplete;
        }
        else if (episodeDetails.Count > 0)
        {
          SetEpisodeDetails(episode, episodeDetails[0]);
          return !cacheIncomplete;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing episode {0}", ex, episode.ToString());
        return false;
      }
    }

    public override Task<bool> UpdateFromOnlineSeriesPersonAsync(SeriesInfo seriesInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineMoviePersonAsync(null, person, language, cacheOnly);
    }

    public override Task<bool> UpdateFromOnlineSeriesEpisodePersonAsync(EpisodeInfo episodeInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineMoviePersonAsync(null, person, language, cacheOnly);
    }

    public override async Task<bool> UpdateFromOnlineSeriesCompanyAsync(SeriesInfo seriesInfo, CompanyInfo company, string language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        if (company.Type == CompanyAspect.COMPANY_PRODUCTION)
        {
          return await UpdateFromOnlineMovieCompanyAsync(null, company, language, cacheOnly).ConfigureAwait(false);
        }
        else if (company.Type == CompanyAspect.COMPANY_TV_NETWORK)
        {
          Network companyDetail = null;
          if (company.MovieDbId > 0)
            companyDetail = await _movieDbHandler.GetNetworkAsync(company.MovieDbId, language, cacheOnly).ConfigureAwait(false);
          if (companyDetail == null) return false;

          company.MovieDbId = companyDetail.Id;
          company.Name = companyDetail.Name;

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing company {0}", ex, company.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesCharacterAsync(SeriesInfo seriesInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      try
      {
        bool cacheIncomplete = false;
        language = language ?? PreferredLanguage;

        if (seriesInfo.MovieDbId <= 0)
          return false;

        MovieCasts seriesCast = await _movieDbHandler.GetSeriesCastCrewAsync(seriesInfo.MovieDbId, language, cacheOnly).ConfigureAwait(false);
        if (cacheOnly && seriesCast == null)
          cacheIncomplete = true;
        if (seriesCast != null)
        {
          List<CharacterInfo> characters = ConvertToCharacters(seriesCast.Cast);
          int index = characters.IndexOf(character);
          if (index >= 0)
          {
            character.ActorMovieDbId = characters[index].ActorMovieDbId;
            character.ActorName = characters[index].ActorName;
            character.Name = characters[index].Name;
            character.Order = characters[index].Order;

            return true;
          }
        }

        return !cacheIncomplete;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TheMovieDbWrapper: Exception while processing character {0}", ex, character.ToString());
        return false;
      }
    }

    public override Task<bool> UpdateFromOnlineSeriesEpisodeCharacterAsync(EpisodeInfo episodeInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesCharacterAsync(episodeInfo.CloneBasicInstance<SeriesInfo>(), character, language, cacheOnly);
    }

    #endregion

    #region Convert

    private List<MovieInfo> ConvertToMovies(MovieCollection movieCollection, List<MovieSearchResult> movies)
    {
      if (movies == null || movies.Count == 0)
        return new List<MovieInfo>();

      List<MovieInfo> retValue = new List<MovieInfo>();
      foreach (MovieSearchResult movie in movies)
      {
        retValue.Add(new MovieInfo()
        {
          CollectionMovieDbId = movieCollection.Id,
          CollectionName = new SimpleTitle(movieCollection.Name, false),

          MovieDbId = movie.Id,
          MovieName = new SimpleTitle(movie.Title, false),
          OriginalName = movie.OriginalTitle,
          ReleaseDate = movie.ReleaseDate,
          Order = retValue.Count
        });
      }
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<CrewItem> crew, string occupation, string media = null, string parentMedia = null)
    {
      if (crew == null || crew.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (CrewItem person in crew)
      {
        retValue.Add(new PersonInfo()
        {
          MovieDbId = person.PersonId,
          Name = person.Name,
          Occupation = occupation,
          MediaName = media,
          ParentMediaName = parentMedia
        });
      }
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<CastItem> cast, string occupation, string media = null, string parentMedia = null)
    {
      if (cast == null || cast.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (CastItem person in cast)
      {
        retValue.Add(new PersonInfo()
        {
          MovieDbId = person.PersonId,
          Name = person.Name,
          Occupation = occupation,
          Order = person.Order,
          MediaName = media
        });
      }
      return retValue;
    }

    private List<CharacterInfo> ConvertToCharacters(List<CastItem> characters, string media = null, string parentMedia = null)
    {
      if (characters == null || characters.Count == 0)
        return new List<CharacterInfo>();

      List<CharacterInfo> retValue = new List<CharacterInfo>();
      foreach (CastItem person in characters)
        retValue.Add(new CharacterInfo()
        {
          ActorMovieDbId = person.PersonId,
          ActorName = person.Name,
          Name = person.Character,
          Order = person.Order,
          MediaName = media,
          ParentMediaName = parentMedia
        });
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanies(List<ProductionCompany> companies, string type)
    {
      if (companies == null || companies.Count == 0)
        return new List<CompanyInfo>();

      List<CompanyInfo> retValue = new List<CompanyInfo>();
      foreach (ProductionCompany company in companies)
      {
        retValue.Add(new CompanyInfo()
        {
          MovieDbId = company.Id,
          Name = company.Name,
          Type = type
        });
      }
      return retValue;
    }

    private List<GenreInfo> ConvertToMovieGenreIds(List<Genre> genres)
    {
      List<GenreInfo> movieGenres = new List<GenreInfo>();
      foreach (Genre genre in genres)
      {
        if (genre.Id == 28)
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Action, Name = genre.Name });
        else if (genre.Id == 12)            
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Adventure, Name = genre.Name });
        else if (genre.Id == 16)              
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Animation, Name = genre.Name });
        else if (genre.Id == 35)              
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Comedy, Name = genre.Name });
        else if (genre.Id == 80)              
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Crime, Name = genre.Name });
        else if (genre.Id == 99)              
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Documentary, Name = genre.Name });
        else if (genre.Id == 18)             
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Drama, Name = genre.Name });
        else if (genre.Id == 10751)           
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Family, Name = genre.Name });
        else if (genre.Id == 14)              
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Fantasy, Name = genre.Name });
        else if (genre.Id == 36)             
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.History, Name = genre.Name });
        else if (genre.Id == 27)              
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Horror, Name = genre.Name });
        else if (genre.Id == 10402)           
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Music, Name = genre.Name });
        else if (genre.Id == 9648)            
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Mystery, Name = genre.Name });
        else if (genre.Id == 10749)           
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Romance, Name = genre.Name });
        else if (genre.Id == 878)             
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.SciFi, Name = genre.Name });
        else if (genre.Id == 10770)            
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.TvMovie, Name = genre.Name });
        else if (genre.Id == 53)              
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Thriller, Name = genre.Name });
        else if (genre.Id == 10752)           
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.War, Name = genre.Name });
        else if (genre.Id == 37)               
          movieGenres.Add(new GenreInfo { Id = (int)VideoGenre.Western, Name = genre.Name });
      }
      return movieGenres;
    }

    private List<GenreInfo> ConvertToSeriesGenreIds(List<Genre> genres)
    {
      List<GenreInfo> seriesGenres = new List<GenreInfo>();
      foreach (Genre genre in genres)
      {
        if (genre.Id == 10759)
        {
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Action, Name = genre.Name });
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Adventure, Name = genre.Name });
        }
        else if (genre.Id == 16)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Animation, Name = genre.Name });
        else if (genre.Id == 35)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Comedy, Name = genre.Name });
        else if (genre.Id == 80)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Crime, Name = genre.Name });
        else if (genre.Id == 99)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Documentary, Name = genre.Name });
        else if (genre.Id == 18)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Drama, Name = genre.Name });
        else if (genre.Id == 10751)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Family, Name = genre.Name });
        else if (genre.Id == 10762)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Kids, Name = genre.Name });
        else if (genre.Id == 14)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Fantasy, Name = genre.Name });
        else if (genre.Id == 10763)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.News, Name = genre.Name });
        else if (genre.Id == 10764)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Reality, Name = genre.Name });
        else if (genre.Id == 10765)
        {
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.SciFi, Name = genre.Name });
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Fantasy, Name = genre.Name });
        }
        else if (genre.Id == 9648)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Mystery, Name = genre.Name });
        else if (genre.Id == 10766)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Soap, Name = genre.Name });
        else if (genre.Id == 10767)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Talk, Name = genre.Name });
        else if (genre.Id == 10768)
        {
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.War, Name = genre.Name });
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Politics, Name = genre.Name });
        }
        else if (genre.Id == 37)
          seriesGenres.Add(new GenreInfo { Id = (int)VideoGenre.Western, Name = genre.Name });
      }
      return seriesGenres;
    }

    #endregion

    #region FanArt

    public override async Task<ApiWrapperImageCollection<ImageItem>> GetFanArtAsync<T>(T infoObject, string language, string fanartMediaType)
    {
      ImageCollection imgs = null;
      if (fanartMediaType == FanArtMediaTypes.MovieCollection)
        imgs = await GetMovieCollectionImages(infoObject.AsMovieCollection()).ConfigureAwait(false);
      else if (fanartMediaType == FanArtMediaTypes.Movie)
        imgs = await GetMovieImages(infoObject as MovieInfo).ConfigureAwait(false);
      else if (fanartMediaType == FanArtMediaTypes.Series)
        imgs = await GetSeriesImages(infoObject.AsSeries()).ConfigureAwait(false);
      else if (fanartMediaType == FanArtMediaTypes.SeriesSeason)
        imgs = await GetSeasonImages(infoObject.AsSeason()).ConfigureAwait(false);
      else if (fanartMediaType == FanArtMediaTypes.Episode)
        imgs = await GetEpisodeImages(infoObject as EpisodeInfo).ConfigureAwait(false);
      else if (fanartMediaType == FanArtMediaTypes.Actor || fanartMediaType == FanArtMediaTypes.Director || fanartMediaType == FanArtMediaTypes.Writer)
        imgs = await GetPersonImages(infoObject as PersonInfo).ConfigureAwait(false);
      else
        return null;

      if (imgs != null)
      {
        ApiWrapperImageCollection<ImageItem> images = new ApiWrapperImageCollection<ImageItem>();
        if (imgs.Id > 0) images.Id = imgs.Id.ToString();
        if (imgs.Backdrops != null) images.Backdrops.AddRange(imgs.Backdrops.OrderBy(b => string.IsNullOrEmpty(b.Language)));
        if (imgs.Covers != null) images.Covers.AddRange(imgs.Covers.OrderBy(b => string.IsNullOrEmpty(b.Language)));
        if (imgs.Posters != null) images.Posters.AddRange(imgs.Posters.OrderBy(b => string.IsNullOrEmpty(b.Language)));
        if (imgs.Profiles != null) images.Thumbnails.AddRange(imgs.Profiles.OrderBy(b => string.IsNullOrEmpty(b.Language)));
        if (imgs.Stills != null) images.Thumbnails.AddRange(imgs.Stills.OrderBy(b => string.IsNullOrEmpty(b.Language)));
        return images;
      }
      return null;
    }

    public override Task<bool> DownloadFanArtAsync(string id, ImageItem image, string folderPath)
    {
      return _movieDbHandler.DownloadImageAsync(id, image, folderPath);
    }

    protected Task<ImageCollection> GetMovieCollectionImages(MovieCollectionInfo collection)
    {
      if (collection == null || collection.MovieDbId < 1)
        return Task.FromResult<ImageCollection>(null);
      // Download all image information, filter later!
      return _movieDbHandler.GetMovieCollectionImagesAsync(collection.MovieDbId, null);
    }

    protected Task<ImageCollection> GetMovieImages(MovieInfo movie)
    {
      if (movie == null || movie.MovieDbId < 1)
        return Task.FromResult<ImageCollection>(null);
      // Download all image information, filter later!
      return _movieDbHandler.GetMovieImagesAsync(movie.MovieDbId, null);
    }

    protected Task<ImageCollection> GetSeriesImages(SeriesInfo series)
    {
      if (series == null || series.MovieDbId < 1)
        return Task.FromResult<ImageCollection>(null);
      // Download all image information, filter later!
      return _movieDbHandler.GetSeriesImagesAsync(series.MovieDbId, null);
    }

    protected Task<ImageCollection> GetSeasonImages(SeasonInfo season)
    {
      if (season == null || season.SeriesMovieDbId < 1 || !season.SeasonNumber.HasValue)
        return Task.FromResult<ImageCollection>(null);
      // Download all image information, filter later!
      return _movieDbHandler.GetSeriesSeasonImagesAsync(season.SeriesMovieDbId, season.SeasonNumber.Value, null);
    }

    protected Task<ImageCollection> GetEpisodeImages(EpisodeInfo episode)
    {
      if (episode == null || episode.SeriesMovieDbId < 1 || !episode.SeasonNumber.HasValue || episode.EpisodeNumbers.Count == 0)
        return Task.FromResult<ImageCollection>(null);
      // Download all image information, filter later!
      return _movieDbHandler.GetSeriesEpisodeImagesAsync(episode.SeriesMovieDbId, episode.SeasonNumber.Value, episode.FirstEpisodeNumber, null);
    }

    protected Task<ImageCollection> GetPersonImages(PersonInfo person)
    {
      if (person == null || person.MovieDbId < 1)
        return Task.FromResult<ImageCollection>(null);
      // Download all image information, filter later!
      return _movieDbHandler.GetPersonImagesAsync(person.MovieDbId, null);
    }

    #endregion

    #region Cache

    /// <summary>
    /// Updates the local available information with updated ones from online source.
    /// </summary>
    /// <returns></returns>
    public override bool RefreshCache(DateTime lastRefresh)
    {
      if (DateTime.Now - lastRefresh <= _cacheTimeout)
        return false;

      try
      {
        DateTime startTime = DateTime.Now;
        int page = 1;
        List<int> changedItems = new List<int>();
        ChangeCollection changes;
        if (_movieMode)
        {
          //Refresh movies
          startTime = DateTime.Now;
          page = 1;
          changedItems.Clear();
          changes = _movieDbHandler.GetMovieChanges(page, lastRefresh);
          foreach (Change change in changes.Changes)
            if (change.Id.HasValue)
            changedItems.Add(change.Id.Value);
          while (page < changes.TotalPages)
          {
            page++;
            changes = _movieDbHandler.GetMovieChanges(page, lastRefresh);
            foreach (Change change in changes.Changes)
              if (change.Id.HasValue)
                changedItems.Add(change.Id.Value);
          }
          foreach (int movieId in changedItems)
            _movieDbHandler.DeleteMovieCache(movieId);
          FireCacheUpdateFinished(startTime, DateTime.Now, UpdateType.Movie, changedItems.Select(i => i.ToString()).ToList());

          //Refresh persons
          startTime = DateTime.Now;
          page = 1;
          changedItems.Clear();
          changes = _movieDbHandler.GetPersonChanges(page, lastRefresh);
          foreach (Change change in changes.Changes)
            if (change.Id.HasValue)
              changedItems.Add(change.Id.Value);
          while (page < changes.TotalPages)
          {
            page++;
            changes = _movieDbHandler.GetPersonChanges(page, lastRefresh);
            foreach (Change change in changes.Changes)
              if (change.Id.HasValue)
                changedItems.Add(change.Id.Value);
          }
          foreach (int movieId in changedItems)
            _movieDbHandler.DeletePersonCache(movieId);
          FireCacheUpdateFinished(startTime, DateTime.Now, UpdateType.Person, changedItems.Select(i => i.ToString()).ToList());
        }
        else
        {
          //Refresh series
          startTime = DateTime.Now;
          page = 1;
          changedItems.Clear();
          changes = _movieDbHandler.GetSeriesChanges(page, lastRefresh);
          foreach (Change change in changes.Changes)
            if (change.Id.HasValue)
              changedItems.Add(change.Id.Value);
          while (page < changes.TotalPages)
          {
            page++;
            changes = _movieDbHandler.GetSeriesChanges(page, lastRefresh);
            foreach (Change change in changes.Changes)
              if (change.Id.HasValue)
                changedItems.Add(change.Id.Value);
          }
          foreach (int movieId in changedItems)
            _movieDbHandler.DeleteSeriesCache(movieId);
          FireCacheUpdateFinished(startTime, DateTime.Now, UpdateType.Series, changedItems.Select(i => i.ToString()).ToList());
        }
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("TheMovieDbWrapper: Error updating cache", ex);
        return false;
      }
    }

    #endregion
  }
}
