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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Genres;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using System;
using System.Collections.Generic;
using System.Linq;

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

    public override bool SearchMovie(MovieInfo movieSearch, string language, out List<MovieInfo> movies)
    {
      movies = null;
      List<MovieSearchResult> foundMovies = _movieDbHandler.SearchMovie(movieSearch.MovieName.Text, language);
      if (foundMovies == null) return false;
      movies = foundMovies.Select(m => new MovieInfo()
      {
        MovieDbId = m.Id,
        MovieName = new SimpleTitle(m.Title, false),
        OriginalName = m.OriginalTitle,
        ReleaseDate = m.ReleaseDate,
      }).ToList();

      if (movies.Count == 0 && !string.IsNullOrEmpty(movieSearch.OriginalName))
      {
        foundMovies = _movieDbHandler.SearchMovie(movieSearch.OriginalName, language);
        if (foundMovies == null) return false;
        movies = foundMovies.Select(m => new MovieInfo()
        {
          MovieDbId = m.Id,
          MovieName = new SimpleTitle(m.Title, false),
          OriginalName = m.OriginalTitle,
          ReleaseDate = m.ReleaseDate,
        }).ToList();
      }
      return movies.Count > 0;
    }

    public override bool SearchSeriesEpisode(EpisodeInfo episodeSearch, string language, out List<EpisodeInfo> episodes)
    {
      language = language ?? PreferredLanguage;

      episodes = null;
      SeriesInfo seriesSearch = null;
      if (episodeSearch.SeriesMovieDbId <= 0)
      {
        seriesSearch = episodeSearch.CloneBasicInstance<SeriesInfo>();
        if (!SearchSeriesUniqueAndUpdate(seriesSearch, language))
          return false;
        episodeSearch.CopyIdsFrom(seriesSearch);
      }

      if (episodeSearch.SeriesMovieDbId > 0 && episodeSearch.SeasonNumber.HasValue)
      {
        Season season = _movieDbHandler.GetSeriesSeason(episodeSearch.SeriesMovieDbId, episodeSearch.SeasonNumber.Value, language, false);
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
                SeriesName = seriesSearch.SeriesName,
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
      language = language ?? PreferredLanguage;

      series = null;
      List<SeriesSearchResult> foundSeries = _movieDbHandler.SearchSeries(seriesSearch.SeriesName.Text, language);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.OriginalName))
        foundSeries = _movieDbHandler.SearchSeries(seriesSearch.OriginalName, language);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = _movieDbHandler.SearchSeries(seriesSearch.AlternateName, language);
      if (foundSeries == null) return false;
      series = foundSeries.Select(s => new SeriesInfo()
      {
        MovieDbId = s.Id,
        SeriesName = new SimpleTitle(s.Name, true),
        OriginalName = s.OriginalName,
        FirstAired = s.FirstAirDate,
      }).ToList();
      return series.Count > 0;
    }

    public override bool SearchPerson(PersonInfo personSearch, string language, out List<PersonInfo> persons)
    {
      language = language ?? PreferredLanguage;

      persons = null;
      List<PersonSearchResult> foundPersons = _movieDbHandler.SearchPerson(personSearch.Name, language);
      if (foundPersons == null) return false;
      persons = foundPersons.Select(p => new PersonInfo()
      {
        MovieDbId = p.Id,
        Name = p.Name,
      }).ToList();
      return persons.Count > 0;
    }

    public override bool SearchCompany(CompanyInfo companySearch, string language, out List<CompanyInfo> companies)
    {
      language = language ?? PreferredLanguage;

      companies = null;
      List<CompanySearchResult> foundCompanies = _movieDbHandler.SearchCompany(companySearch.Name, language);
      if (foundCompanies == null) return false;
      companies = foundCompanies.Select(p => new CompanyInfo()
      {
        MovieDbId = p.Id,
        Name = p.Name,
      }).ToList();
      return companies.Count > 0;
    }

    #endregion

    #region Update

    public override bool UpdateFromOnlineMovie(MovieInfo movie, string language, bool cacheOnly)
    {
      bool cacheIncomplete = false;
      language = language ?? PreferredLanguage;

      Movie movieDetail = null;
      if (movie.MovieDbId > 0)
        movieDetail = _movieDbHandler.GetMovie(movie.MovieDbId, language, cacheOnly);
      if (movieDetail == null && !string.IsNullOrEmpty(movie.ImdbId))
        movieDetail = _movieDbHandler.GetMovie(movie.ImdbId, language, cacheOnly);
      if (movieDetail == null && cacheOnly == false)
      {
        if (!string.IsNullOrEmpty(movie.ImdbId))
        {
          List<IdResult> ids = _movieDbHandler.FindMovieByImdbId(movie.ImdbId, language);
          if (ids != null && ids.Count > 0)
            movieDetail = _movieDbHandler.GetMovie(ids[0].Id, language, false);
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

      MovieCasts movieCasts = _movieDbHandler.GetMovieCastCrew(movieDetail.Id, language, cacheOnly);
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

    public override bool UpdateFromOnlineMovieCollection(MovieCollectionInfo collection, string language, bool cacheOnly)
    {
      language = language ?? PreferredLanguage;

      MovieCollection collectionDetail = null;
      if (collection.MovieDbId > 0)
        collectionDetail = _movieDbHandler.GetCollection(collection.MovieDbId, language, cacheOnly);
      if (collectionDetail == null) return false;

      collection.MovieDbId = collectionDetail.Id;
      collection.CollectionName = new SimpleTitle(collectionDetail.Name, false);
      collection.Movies = ConvertToMovies(collectionDetail, collectionDetail.Movies);
      collection.TotalMovies = collectionDetail.Movies.Count;

      return true;
    }

    public override bool UpdateFromOnlineMoviePerson(MovieInfo movieInfo, PersonInfo person, string language, bool cacheOnly)
    {
      language = language ?? PreferredLanguage;

      Person personDetail = null;
      if (person.MovieDbId > 0)
        personDetail = _movieDbHandler.GetPerson(person.MovieDbId, language, cacheOnly);
      if (personDetail == null && cacheOnly == false)
      {
        List<IdResult> ids = null;

        if (!string.IsNullOrEmpty(person.ImdbId))
          ids = _movieDbHandler.FindPersonByImdbId(person.ImdbId, language);
        if (personDetail == null && person.TvRageId > 0)
          ids = _movieDbHandler.FindPersonByTvRageId(person.TvRageId, language);

        if (ids != null && ids.Count > 0)
          personDetail = _movieDbHandler.GetPerson(ids[0].Id, language, false);
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

    public override bool UpdateFromOnlineMovieCharacter(MovieInfo movieInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      bool cacheIncomplete = false;
      language = language ?? PreferredLanguage;

      if (movieInfo.MovieDbId <= 0)
        return false;

      MovieCasts movieCasts = _movieDbHandler.GetMovieCastCrew(movieInfo.MovieDbId, language, cacheOnly);
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

    public override bool UpdateFromOnlineMovieCompany(MovieInfo movieInfo, CompanyInfo company, string language, bool cacheOnly)
    {
      language = language ?? PreferredLanguage;

      if (company.Type != CompanyAspect.COMPANY_PRODUCTION)
        return false;
      Company companyDetail = null;
      if (company.MovieDbId > 0)
        companyDetail = _movieDbHandler.GetCompany(company.MovieDbId, language, cacheOnly);
      if (companyDetail == null) return false;

      company.MovieDbId = companyDetail.Id;
      company.Name = companyDetail.Name;
      company.Description = new SimpleTitle(companyDetail.Description, false);

      return true;
    }

    public override bool UpdateFromOnlineSeries(SeriesInfo series, string language, bool cacheOnly)
    {
      bool cacheIncomplete = false;
      language = language ?? PreferredLanguage;

      Series seriesDetail = null;
      if (series.MovieDbId > 0)
        seriesDetail = _movieDbHandler.GetSeries(series.MovieDbId, language, cacheOnly);
      if (seriesDetail == null && cacheOnly == false)
      {
        List<IdResult> ids = null;

        if (!string.IsNullOrEmpty(series.ImdbId))
          ids = _movieDbHandler.FindSeriesByImdbId(series.ImdbId, language);
        if (ids == null && series.TvdbId > 0)
          ids = _movieDbHandler.FindSeriesByTvDbId(series.TvdbId, language);
        if (ids == null && series.TvRageId > 0)
          ids = _movieDbHandler.FindSeriesByTvRageId(series.TvRageId, language);

        if (ids != null && ids.Count > 0)
          seriesDetail = _movieDbHandler.GetSeries(ids[0].Id, language, false);
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
        var cert = seriesDetail.ContentRatingResults.Results.Where(c => c.CountryId == "US").First();
        if (cert != null)
          series.Certification = cert.ContentRating;
      }

      MovieCasts seriesCast = _movieDbHandler.GetSeriesCastCrew(seriesDetail.Id, language, cacheOnly);
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
        Season currentSeason = _movieDbHandler.GetSeriesSeason(seriesDetail.Id, season.SeasonNumber, language, cacheOnly);
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

    public override bool UpdateFromOnlineSeriesSeason(SeasonInfo season, string language, bool cacheOnly)
    {
      language = language ?? PreferredLanguage;

      Series seriesDetail = null;
      Season seasonDetail = null;
      if (season.SeriesMovieDbId > 0)
        seriesDetail = _movieDbHandler.GetSeries(season.SeriesMovieDbId, language, cacheOnly);
      if (seriesDetail == null) return false;
      if (season.SeriesMovieDbId > 0 && season.SeasonNumber.HasValue)
        seasonDetail = _movieDbHandler.GetSeriesSeason(season.SeriesMovieDbId, season.SeasonNumber.Value, language, cacheOnly);
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

    public override bool UpdateFromOnlineSeriesEpisode(EpisodeInfo episode, string language, bool cacheOnly)
    {
      bool cacheIncomplete = false;
      language = language ?? PreferredLanguage;

      List<EpisodeInfo> episodeDetails = new List<EpisodeInfo>();
      Episode episodeDetail = null;
      Series seriesDetail = null;
      MovieCasts seriesCast = null;

      if (episode.SeriesMovieDbId > 0 && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
      {
        seriesDetail = _movieDbHandler.GetSeries(episode.SeriesMovieDbId, language, cacheOnly);
        if (seriesDetail == null) return false;
        seriesCast = _movieDbHandler.GetSeriesCastCrew(episode.SeriesMovieDbId, language, cacheOnly);
        if (cacheOnly && seriesCast == null)
          cacheIncomplete = true;

        foreach (int episodeNumber in episode.EpisodeNumbers)
        {
          episodeDetail = _movieDbHandler.GetSeriesEpisode(episode.SeriesMovieDbId, episode.SeasonNumber.Value, episodeNumber, language, cacheOnly);
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

    public override bool UpdateFromOnlineSeriesPerson(SeriesInfo seriesInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineMoviePerson(null, person, language, cacheOnly);
    }

    public override bool UpdateFromOnlineSeriesEpisodePerson(EpisodeInfo episodeInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineMoviePerson(null, person, language, cacheOnly);
    }

    public override bool UpdateFromOnlineSeriesCompany(SeriesInfo seriesInfo, CompanyInfo company, string language, bool cacheOnly)
    {
      language = language ?? PreferredLanguage;

      if (company.Type == CompanyAspect.COMPANY_PRODUCTION)
      {
        return UpdateFromOnlineMovieCompany(null, company, language, cacheOnly);
      }
      else if (company.Type == CompanyAspect.COMPANY_TV_NETWORK)
      {
        Network companyDetail = null;
        if (company.MovieDbId > 0)
          companyDetail = _movieDbHandler.GetNetwork(company.MovieDbId, language, cacheOnly);
        if (companyDetail == null) return false;

        company.MovieDbId = companyDetail.Id;
        company.Name = companyDetail.Name;

        return true;
      }
      return false;
    }

    public override bool UpdateFromOnlineSeriesCharacter(SeriesInfo seriesInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      bool cacheIncomplete = false;
      language = language ?? PreferredLanguage;

      if (seriesInfo.MovieDbId <= 0)
        return false;

      MovieCasts seriesCast = _movieDbHandler.GetSeriesCastCrew(seriesInfo.MovieDbId, language, cacheOnly);
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

    public override bool UpdateFromOnlineSeriesEpisodeCharacter(EpisodeInfo episodeInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesCharacter(episodeInfo.CloneBasicInstance<SeriesInfo>(), character, language, cacheOnly);
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
          movieGenres.Add(new GenreInfo { Id = MovieGenre.ACTION, Name = genre.Name });
        else if (genre.Id == 12)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.ADVENTURE, Name = genre.Name });
        else if (genre.Id == 16)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.ANIMATION, Name = genre.Name });
        else if (genre.Id == 35)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.COMEDY, Name = genre.Name });
        else if (genre.Id == 80)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.CRIME, Name = genre.Name });
        else if (genre.Id == 99)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.DOCUMENTARY, Name = genre.Name });
        else if (genre.Id == 18)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.DRAMA, Name = genre.Name });
        else if (genre.Id == 10751)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.FAMILY, Name = genre.Name });
        else if (genre.Id == 14)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.FANTASY, Name = genre.Name });
        else if (genre.Id == 36)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.HISTORY, Name = genre.Name });
        else if (genre.Id == 27)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.HORROR, Name = genre.Name });
        else if (genre.Id == 10402)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.MUSIC, Name = genre.Name });
        else if (genre.Id == 9648)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.MYSTERY, Name = genre.Name });
        else if (genre.Id == 10749)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.ROMANCE, Name = genre.Name });
        else if (genre.Id == 878)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.SCIENCE_FICTION, Name = genre.Name });
        else if (genre.Id == 10770)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.TV_MOVIE, Name = genre.Name });
        else if (genre.Id == 53)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.THRILLER, Name = genre.Name });
        else if (genre.Id == 10752)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.WAR, Name = genre.Name });
        else if (genre.Id == 37)
          movieGenres.Add(new GenreInfo { Id = MovieGenre.WESTERN, Name = genre.Name });
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
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.ACTION, Name = genre.Name });
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.ADVENTURE, Name = genre.Name });
        }
        else if (genre.Id == 16)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.ANIMATION, Name = genre.Name });
        else if (genre.Id == 35)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.COMEDY, Name = genre.Name });
        else if (genre.Id == 80)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.CRIME, Name = genre.Name });
        else if (genre.Id == 99)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.DOCUMENTARY, Name = genre.Name });
        else if (genre.Id == 18)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.DRAMA, Name = genre.Name });
        else if (genre.Id == 10751)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.FAMILY, Name = genre.Name });
        else if (genre.Id == 10762)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.KIDS, Name = genre.Name });
        else if (genre.Id == 14)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.FANTASY, Name = genre.Name });
        else if (genre.Id == 10763)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.NEWS, Name = genre.Name });
        else if (genre.Id == 10764)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.REALITY, Name = genre.Name });
        else if (genre.Id == 10765)
        {
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.SCIENCE_FICTION, Name = genre.Name });
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.FANTASY, Name = genre.Name });
        }
        else if (genre.Id == 9648)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.MYSTERY, Name = genre.Name });
        else if (genre.Id == 10766)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.SOAP, Name = genre.Name });
        else if (genre.Id == 10767)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.TALK, Name = genre.Name });
        else if (genre.Id == 10768)
        {
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.WAR, Name = genre.Name });
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.POLITICS, Name = genre.Name });
        }
        else if (genre.Id == 37)
          seriesGenres.Add(new GenreInfo { Id = SeriesGenre.WESTERN, Name = genre.Name });
      }
      return seriesGenres;
    }

    #endregion

    #region FanArt

    public override bool GetFanArt<T>(T infoObject, string language, string fanartMediaType, out ApiWrapperImageCollection<ImageItem> images)
    {
      language = language ?? PreferredLanguage;

      ImageCollection imgs = null;
      images = new ApiWrapperImageCollection<ImageItem>();

      if (fanartMediaType == FanArtMediaTypes.MovieCollection)
      {
        MovieInfo movie = infoObject as MovieInfo;
        MovieCollectionInfo collection = infoObject as MovieCollectionInfo;
        if (collection == null && movie != null)
        {
          collection = movie.CloneBasicInstance<MovieCollectionInfo>();
        }
        if (collection != null && collection.MovieDbId > 0)
        {
          // Download all image information, filter later!
          imgs = _movieDbHandler.GetMovieCollectionImages(collection.MovieDbId, null);
        }
      }
      else if (fanartMediaType == FanArtMediaTypes.Movie)
      {
        MovieInfo movie = infoObject as MovieInfo;
        if (movie != null && movie.MovieDbId > 0)
        {
          // Download all image information, filter later!
          imgs = _movieDbHandler.GetMovieImages(movie.MovieDbId, null);
        }
      }
      else if (fanartMediaType == FanArtMediaTypes.Series)
      {
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
        if (series != null && series.MovieDbId > 0)
        {
          // Download all image information, filter later!
          imgs = _movieDbHandler.GetSeriesImages(series.MovieDbId, null);
        }
      }
      else if (fanartMediaType == FanArtMediaTypes.SeriesSeason)
      {
        EpisodeInfo episode = infoObject as EpisodeInfo;
        SeasonInfo season = infoObject as SeasonInfo;
        if (season == null && episode != null)
        {
          season = episode.CloneBasicInstance<SeasonInfo>();
        }
        if (season != null && season.SeriesMovieDbId > 0 && season.SeasonNumber.HasValue)
        {
          // Download all image information, filter later!
          imgs = _movieDbHandler.GetSeriesSeasonImages(season.SeriesMovieDbId, season.SeasonNumber.Value, null);
        }
      }
      else if (fanartMediaType == FanArtMediaTypes.Episode)
      {
        EpisodeInfo episode = infoObject as EpisodeInfo;
        if (episode != null && episode.SeriesMovieDbId > 0 && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
        {
          // Download all image information, filter later!
          imgs = _movieDbHandler.GetSeriesEpisodeImages(episode.SeriesMovieDbId, episode.SeasonNumber.Value, episode.EpisodeNumbers[0], null);
        }
      }
      else if (fanartMediaType == FanArtMediaTypes.Actor || fanartMediaType == FanArtMediaTypes.Director || fanartMediaType == FanArtMediaTypes.Writer)
      {
        PersonInfo person = infoObject as PersonInfo;
        if (person != null && person.MovieDbId > 0)
        {
          // Download all image information, filter later!
          imgs = _movieDbHandler.GetPersonImages(person.MovieDbId, null);
        }
      }
      else
      {
        return true;
      }

      if (imgs != null)
      {
        if (imgs.Id > 0) images.Id = imgs.Id.ToString();
        if (imgs.Backdrops != null) images.Backdrops.AddRange(imgs.Backdrops);
        if (imgs.Covers != null) images.Covers.AddRange(imgs.Covers);
        if (imgs.Posters != null) images.Posters.AddRange(imgs.Posters);
        if (imgs.Profiles != null) images.Thumbnails.AddRange(imgs.Profiles);
        if (imgs.Stills != null) images.Thumbnails.AddRange(imgs.Stills);
        return true;
      }
      return false;
    }

    public override bool DownloadFanArt(string id, ImageItem image, string folderPath)
    {
      return _movieDbHandler.DownloadImage(id, image, folderPath);
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
            changedItems.Add(change.Id);
          while (page < changes.TotalPages)
          {
            page++;
            changes = _movieDbHandler.GetMovieChanges(page, lastRefresh);
            foreach (Change change in changes.Changes)
              changedItems.Add(change.Id);
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
            changedItems.Add(change.Id);
          while (page < changes.TotalPages)
          {
            page++;
            changes = _movieDbHandler.GetPersonChanges(page, lastRefresh);
            foreach (Change change in changes.Changes)
              changedItems.Add(change.Id);
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
            changedItems.Add(change.Id);
          while (page < changes.TotalPages)
          {
            page++;
            changes = _movieDbHandler.GetSeriesChanges(page, lastRefresh);
            foreach (Change change in changes.Changes)
              changedItems.Add(change.Id);
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
