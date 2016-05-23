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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Libraries.MovieDbV3.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TheMovieDB;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class SeriesTheMovieDbMatcher : BaseMatcher<SeriesMatch, string>
  {
    #region Static instance

    public static SeriesTheMovieDbMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesTheMovieDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TheMovieDB\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "SeriesMatches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, Series> _memoryCache = new ConcurrentDictionary<string, Series>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized TheMovieDbWrapper.
    /// </summary>
    private TheMovieDbWrapper _movieDb;

    #endregion

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the series from TheMovieDB and updates the given <paramref name="episodeInfo"/> with the online information.
    /// </summary>
    /// <param name="episodeInfo">Episode to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateEpisode(EpisodeInfo episodeInfo)
    {
      try
      {
        Series seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (TryMatch(episodeInfo, out seriesDetail))
        {
          string movieDbId = "";

          if (seriesDetail != null)
          {
            movieDbId = seriesDetail.Id.ToString();

            MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesMovieDbId, seriesDetail.Id);
            if (seriesDetail.ExternalId.TvDbId.HasValue)
              MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvdbId, seriesDetail.ExternalId.TvDbId.Value);
            if (seriesDetail.ExternalId.TvRageId.HasValue)
              MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvRageId, seriesDetail.ExternalId.TvRageId.Value);
            if (!string.IsNullOrEmpty(seriesDetail.ExternalId.ImDbId))
              MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesImdbId, seriesDetail.ExternalId.ImDbId);

            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Series, seriesDetail.Name, true);
            MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, seriesDetail.FirstAirDate);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, seriesDetail.Genres.Select(g => g.Name).ToList(), true, false);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Networks, ConvertToCompanies(seriesDetail.Networks, CompanyAspect.COMPANY_TV_NETWORK), true, false);
            MetadataUpdater.SetOrUpdateList(episodeInfo.ProductionCompanies, ConvertToCompanies(seriesDetail.ProductionCompanies, CompanyAspect.COMPANY_PRODUCTION), true, false);

            if (seriesDetail.ContentRatingResults.Results.Count > 0)
            {
              var cert = seriesDetail.ContentRatingResults.Results.Where(c => c.CountryId == "US").First();
              if (cert != null)
                MetadataUpdater.SetOrUpdateString(ref episodeInfo.Certification, cert.ContentRating, true);
            }

            MovieCasts movieCasts;
            if (_movieDb.GetSeriesCast(seriesDetail.Id, out movieCasts))
            {
              MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(movieCasts.Cast, PersonAspect.OCCUPATION_ACTOR), true, false);
              MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(episodeInfo.SeriesMovieDbId, episodeInfo.Series, movieCasts.Cast), true, false);
            }

            // Also try to fill episode title from series details (most file names don't contain episode name).
            if (!TryMatchEpisode(episodeInfo, seriesDetail))
              return false;

            if (episodeInfo.SeasonNumber.HasValue && episodeInfo.EpisodeNumbers.Count > 0)
              movieDbId += "|" + episodeInfo.SeasonNumber.Value + "|" + episodeInfo.EpisodeNumbers[0];

            if (episodeInfo.Thumbnail == null)
            {
              List<string> thumbs = GetFanArtFiles(episodeInfo, FanArtScope.Episode, FanArtType.Thumbnails);
              if (thumbs.Count > 0)
                episodeInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }

          if (movieDbId.Length > 0)
            ScheduleDownload(movieDbId);
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing episode {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeries(SeriesInfo seriesInfo)
    {
      try
      {
        Series seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (seriesInfo.MovieDbId > 0 && _movieDb.GetSeries(seriesInfo.MovieDbId, out seriesDetail))
        {
          if (seriesDetail.ExternalId.TvDbId.HasValue)
            MetadataUpdater.SetOrUpdateId(ref seriesInfo.TvdbId, seriesDetail.ExternalId.TvDbId.Value);
          if (seriesDetail.ExternalId.TvRageId.HasValue)
            MetadataUpdater.SetOrUpdateId(ref seriesInfo.TvRageId, seriesDetail.ExternalId.TvRageId.Value);
          if (!string.IsNullOrEmpty(seriesDetail.ExternalId.ImDbId))
            MetadataUpdater.SetOrUpdateId(ref seriesInfo.ImdbId, seriesDetail.ExternalId.ImDbId);

          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Series, seriesDetail.Name, true);
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.OriginalName, seriesDetail.OriginalName, true);
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Description, seriesDetail.Overview, false);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.FirstAired, seriesDetail.FirstAirDate);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.Popularity, seriesDetail.Popularity.HasValue ? seriesDetail.Popularity.Value : 0);
          if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
          {
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.IsEnded, true);
          }

          if (seriesDetail.ContentRatingResults.Results.Count > 0)
          {
            var cert = seriesDetail.ContentRatingResults.Results.Where(c => c.CountryId == "US").First();
            if (cert != null)
              MetadataUpdater.SetOrUpdateString(ref seriesInfo.Certification, cert.ContentRating, true);
          }

          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.TotalRating, seriesDetail.Rating.HasValue ? seriesDetail.Rating.Value : 0);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.RatingCount, seriesDetail.RatingCount.HasValue ? seriesDetail.RatingCount.Value : 0);

          MetadataUpdater.SetOrUpdateRatings(ref seriesInfo.TotalRating, ref seriesInfo.RatingCount, seriesDetail.Rating, seriesDetail.RatingCount);

          MetadataUpdater.SetOrUpdateList(seriesInfo.Genres, seriesDetail.Genres.Select(g => g.Name).ToList(), true, false);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanies(seriesDetail.Networks, CompanyAspect.COMPANY_TV_NETWORK), true, false);
          MetadataUpdater.SetOrUpdateList(seriesInfo.ProductionCompanies, ConvertToCompanies(seriesDetail.ProductionCompanies, CompanyAspect.COMPANY_PRODUCTION), true, false);

          MovieCasts movieCasts;
          if (_movieDb.GetSeriesCast(seriesInfo.MovieDbId, out movieCasts))
          {
            MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(movieCasts.Cast, PersonAspect.OCCUPATION_ACTOR), true, false);
            MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesInfo.MovieDbId, seriesInfo.Series, movieCasts.Cast), true, false);
          }

          SeriesSeason season = seriesDetail.Seasons.Where(s => s.AirDate < DateTime.Now).LastOrDefault();
          if (season != null)
          {
            Season currentSeason;
            if (_movieDb.GetSeriesSeason(seriesDetail.Id, season.SeasonNumber, out currentSeason) == false)
              return false;

            SeasonEpisode nextEpisode = currentSeason.Episodes.Where(e => e.AirDate > DateTime.Now).FirstOrDefault();
            if (nextEpisode == null) //Try next season
            {
              if (_movieDb.GetSeriesSeason(seriesDetail.Id, season.SeasonNumber, out currentSeason) == false)
                return false;

              nextEpisode = currentSeason.Episodes.Where(e => e.AirDate > DateTime.Now).FirstOrDefault();
            }
            if (nextEpisode != null)
            {
              MetadataUpdater.SetOrUpdateString(ref seriesInfo.NextEpisodeName, nextEpisode.Name, false);
              MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeAirDate, nextEpisode.AirDate);
              MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeSeasonNumber, nextEpisode.SeasonNumber);
              MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeNumber, nextEpisode.EpisodeNumber);
            }
          }

          if (seriesInfo.Thumbnail == null)
          {
            List<string> thumbs = GetFanArtFiles(seriesInfo, FanArtScope.Series, FanArtType.Posters);
            if (thumbs.Count > 0)
              seriesInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing series {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeason(SeasonInfo seasonInfo)
    {
      try
      {
        Series seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (seasonInfo.SeriesMovieDbId > 0 && _movieDb.GetSeries(seasonInfo.SeriesMovieDbId, out seriesDetail))
        {
          if (seriesDetail.ExternalId.TvDbId.HasValue)
            MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesTvdbId, seriesDetail.ExternalId.TvDbId.Value);
          if (seriesDetail.ExternalId.TvRageId.HasValue)
            MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesTvRageId, seriesDetail.ExternalId.TvRageId.Value);
          if (!string.IsNullOrEmpty(seriesDetail.ExternalId.ImDbId))
            MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesImdbId, seriesDetail.ExternalId.ImDbId);

          MetadataUpdater.SetOrUpdateString(ref seasonInfo.Series, seriesDetail.Name, false);

          Season seasonDetail;
          if (_movieDb.GetSeriesSeason(seasonInfo.SeriesMovieDbId, seasonInfo.SeasonNumber.Value, out seasonDetail))
          {
            MetadataUpdater.SetOrUpdateId(ref seasonInfo.MovieDbId, seasonDetail.SeasonId);
            if (seasonDetail.ExternalId.TvDbId.HasValue)
              MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvdbId, seasonDetail.ExternalId.TvDbId.Value);
            if (seasonDetail.ExternalId.TvRageId.HasValue)
              MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvRageId, seasonDetail.ExternalId.TvRageId.Value);
            if (!string.IsNullOrEmpty(seasonDetail.ExternalId.ImDbId))
              MetadataUpdater.SetOrUpdateId(ref seasonInfo.ImdbId, seasonDetail.ExternalId.ImDbId);

            MetadataUpdater.SetOrUpdateValue(ref seasonInfo.FirstAired, seasonDetail.AirDate);
            MetadataUpdater.SetOrUpdateString(ref seasonInfo.Description, seasonDetail.Overview, false);
          }

          if (seasonInfo.Thumbnail == null && seasonInfo.SeasonNumber.HasValue)
          {
            List<string> thumbs = GetFanArtFiles(seasonInfo, FanArtScope.Season, FanArtType.Posters);
            if (thumbs.Count > 0)
              seasonInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing season {0}", ex, seasonInfo.ToString());
        return false;
      }
    }

    public bool UpdateEpisodePersons(EpisodeInfo episodeInfo, string occupation)
    {
      try
      {
        Series seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (episodeInfo.SeriesMovieDbId > 0 && _movieDb.GetSeries(episodeInfo.SeriesMovieDbId, out seriesDetail))
        {
          if (!TryMatchEpisode(episodeInfo, seriesDetail))
            return false;

          if (occupation == PersonAspect.OCCUPATION_ACTOR)
          {
            MovieCasts movieCasts;
            if (_movieDb.GetSeriesCast(episodeInfo.SeriesMovieDbId, out movieCasts))
            {
              MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(movieCasts.Cast, occupation), false, false);
              string preferredLookupLanguage = FindBestMatchingLanguage(episodeInfo);
              foreach (PersonInfo person in episodeInfo.Actors) UpdatePerson(preferredLookupLanguage, person);

              return true;
            }
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing persons {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo)
    {
      try
      {
        Series seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (episodeInfo.SeriesMovieDbId > 0 && _movieDb.GetSeries(episodeInfo.SeriesMovieDbId, out seriesDetail))
        {
          if (!TryMatchEpisode(episodeInfo, seriesDetail))
            return false;

          MovieCasts movieCasts;
          if (_movieDb.GetSeriesCast(episodeInfo.SeriesMovieDbId, out movieCasts))
          {
            MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(episodeInfo.SeriesMovieDbId, episodeInfo.Series, movieCasts.Cast), false, false);
            return true;
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing characters {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesPersons(SeriesInfo seriesInfo, string occupation)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        MovieCasts movieCasts;
        if (seriesInfo.MovieDbId > 0 && _movieDb.GetSeriesCast(seriesInfo.MovieDbId, out movieCasts))
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
          {
            MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(movieCasts.Cast, occupation), false, false);
            foreach (PersonInfo person in seriesInfo.Actors) UpdatePerson(null, person);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing series persons {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesCharacters(SeriesInfo seriesInfo)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        MovieCasts movieCasts;
        if (seriesInfo.MovieDbId > 0 && _movieDb.GetSeriesCast(seriesInfo.MovieDbId, out movieCasts))
        {
          MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesInfo.MovieDbId, seriesInfo.Series, movieCasts.Cast), false, false);

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing series characters {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string type)
    {
      try
      {
        Series seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (seriesInfo.MovieDbId > 0 && _movieDb.GetSeries(seriesInfo.MovieDbId, out seriesDetail))
        {
          if (type == CompanyAspect.COMPANY_TV_NETWORK)
          {
            MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanies(seriesDetail.Networks, type), false, false);
            foreach (CompanyInfo company in seriesInfo.Networks) UpdateCompany(null, company, true);
            return true;
          }
          else if (type == CompanyAspect.COMPANY_PRODUCTION)
          {
            MetadataUpdater.SetOrUpdateList(seriesInfo.ProductionCompanies, ConvertToCompanies(seriesDetail.ProductionCompanies, type), false, false);
            foreach (CompanyInfo company in seriesInfo.ProductionCompanies) UpdateCompany(null, company, false);
            return true;
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing series companies {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    private void UpdatePerson(string preferredLookupLanguage, PersonInfo person)
    {
      if (person.MovieDbId <= 0)
      {
        List<IdResult> results = null;
        if (person.TvRageId > 0)
          results = _movieDb.FindPersonByTvRageId(person.TvRageId);
        else if (!string.IsNullOrEmpty(person.ImdbId))
          results = _movieDb.FindPersonByImdbId(person.ImdbId);

        if (results != null && results.Count == 1)
        {
          person.MovieDbId = results[0].Id;
        }
        else
        {
          List<PersonSearchResult> personsFound;
          if (_movieDb.SearchPersonUnique(person.Name, preferredLookupLanguage, out personsFound))
            person.MovieDbId = personsFound[0].Id;
        }
      }
      if (person.MovieDbId > 0)
      {
        Person personDetail;
        if (_movieDb.GetPerson(person.MovieDbId, out personDetail))
        {
          person.Name = personDetail.Name;
          person.Biography = personDetail.Biography;
          person.DateOfBirth = personDetail.DateOfBirth;
          person.DateOfDeath = personDetail.DateOfDeath;
          person.Orign = personDetail.PlaceOfBirth;
          person.ImdbId = personDetail.ExternalId.ImDbId ?? person.ImdbId;
          person.TvdbId = personDetail.ExternalId.TvDbId.HasValue ? personDetail.ExternalId.TvDbId.Value : 0;
          person.TvRageId = personDetail.ExternalId.TvRageId.HasValue ? personDetail.ExternalId.TvRageId.Value : 0;
        }

        if (person.Thumbnail == null)
        {
          List<string> thumbs = new List<string>();
          if (person.Occupation == PersonAspect.OCCUPATION_ACTOR)
            thumbs = GetFanArtFiles(person, FanArtScope.Actor, FanArtType.Thumbnails);
          else if (person.Occupation == PersonAspect.OCCUPATION_DIRECTOR)
            thumbs = GetFanArtFiles(person, FanArtScope.Director, FanArtType.Thumbnails);
          else if (person.Occupation == PersonAspect.OCCUPATION_WRITER)
            thumbs = GetFanArtFiles(person, FanArtScope.Writer, FanArtType.Thumbnails);
          if (thumbs.Count > 0)
            person.Thumbnail = File.ReadAllBytes(thumbs[0]);
        }
      }
    }

    private void UpdateCompany(string preferredLookupLanguage, CompanyInfo company, bool isNetwork)
    {
      if (company.MovieDbId <= 0)
      {
        if (!isNetwork)
        {
          List<CompanySearchResult> companiesFound;
          if (_movieDb.SearchCompanyUnique(company.Name, preferredLookupLanguage, out companiesFound))
            company.MovieDbId = companiesFound[0].Id;
        }
      }
      if (company.MovieDbId > 0)
      {
        if (isNetwork)
        {
          Network networkDetail;
          if (_movieDb.GetNetwork(company.MovieDbId, out networkDetail))
          {
            company.Name = networkDetail.Name;
          }
        }
        else
        {
          Company companyDetail;
          if (_movieDb.GetCompany(company.MovieDbId, out companyDetail))
          {
            company.Name = companyDetail.Name;
            company.Description = companyDetail.Description;
          }

          if (company.Thumbnail == null)
          {
            List<string> thumbs = new List<string>();
            if (company.Type == CompanyAspect.COMPANY_PRODUCTION)
              thumbs = GetFanArtFiles(company, FanArtScope.Company, FanArtType.Logos);
            if (thumbs.Count > 0)
              company.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }
        }
      }
    }

    #endregion

    #region Metadata update helpers

    protected bool TryMatchEpisode(EpisodeInfo episodeInfo, Series seriesDetail)
    {
      Season season = null;
      List<SeasonEpisode> seasonEpisodes = null;
      if (episodeInfo.SeasonNumber.HasValue)
      {
        if (_movieDb.GetSeriesSeason(seriesDetail.Id, episodeInfo.SeasonNumber.Value, out season) == false)
          return false;

        Episode episode;
        seasonEpisodes = season.Episodes.FindAll(e => e.Name == episodeInfo.Episode);
        // In few cases there can be multiple episodes with same name. In this case we cannot know which one is right
        // and keep the current episode details.
        // Use this way only for single episodes.
        if (episodeInfo.EpisodeNumbers.Count == 1 && seasonEpisodes.Count == 1)
        {
          if (_movieDb.GetSeriesEpisode(seriesDetail.Id, episodeInfo.SeasonNumber.Value, seasonEpisodes[0].EpisodeNumber, out episode) == false)
            return false;
          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.Name, false);
          SetEpisodeDetails(episodeInfo, seriesDetail, episode);
          return true;
        }

        seasonEpisodes = season.Episodes.Where(e => episodeInfo.EpisodeNumbers.Contains(e.EpisodeNumber) && e.SeasonNumber == episodeInfo.SeasonNumber).ToList();
        if (seasonEpisodes.Count == 0)
          return false;

        // Single episode entry
        if (seasonEpisodes.Count == 1)
        {
          if (_movieDb.GetSeriesEpisode(seriesDetail.Id, episodeInfo.SeasonNumber.Value, seasonEpisodes[0].EpisodeNumber, out episode) == false)
            return false;
          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.Name, false);
          SetEpisodeDetails(episodeInfo, seriesDetail, episode);
          return true;
        }

        List<Episode> episodes = new List<Episode>();
        foreach(SeasonEpisode seasonEpisode in seasonEpisodes)
        {
          Episode newEpisode = new Episode();
          if (_movieDb.GetSeriesEpisode(seriesDetail.Id, episodeInfo.SeasonNumber.Value, seasonEpisode.EpisodeNumber, out newEpisode) == false)
            return false;

          episodes.Add(newEpisode);
        }

        // Multiple episodes
        SetMultiEpisodeDetailsl(episodeInfo, seriesDetail, episodes);

        return true;
      }
      return false;
    }

    private void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, Series seriesDetail, List<Episode> episodes)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.MovieDbId, episodes.First().Id);
      if (!string.IsNullOrEmpty(episodes.First().ExternalId.ImDbId)) MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, episodes.First().ExternalId.ImDbId);
      if (episodes.First().ExternalId.TvDbId.HasValue) MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvdbId, episodes.First().ExternalId.TvDbId.Value);
      if (episodes.First().ExternalId.TvRageId.HasValue) MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvRageId, episodes.First().ExternalId.TvRageId.Value);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episodes.First().SeasonNumber);
      MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodes.Select(x => x.EpisodeNumber).ToList(), true, false);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episodes.First().AirDate);

      MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount,
        episodes.Sum(e => e.Rating.HasValue ? e.Rating.Value : 0) / episodes.Count, episodes.Sum(e => e.RatingCount.HasValue ? e.RatingCount.Value : 0)); // Average rating

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.Name).ToArray()), false);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Overview)).ToArray()), false);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episodes.SelectMany(e => e.GuestStars).ToList(), PersonAspect.OCCUPATION_ACTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episodes.SelectMany(e => e.Crew.Where(p => p.Job == "Director")).ToList(), PersonAspect.OCCUPATION_DIRECTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episodes.SelectMany(e => e.Crew.Where(p => p.Job == "Writer")).ToList(), PersonAspect.OCCUPATION_WRITER), true, false);
    }

    private void SetEpisodeDetails(EpisodeInfo episodeInfo, Series seriesDetail, Episode episode)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.MovieDbId, episode.Id);
      if (!string.IsNullOrEmpty(episode.ExternalId.ImDbId)) MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, episode.ExternalId.ImDbId);
      if (episode.ExternalId.TvDbId.HasValue) MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvdbId, episode.ExternalId.TvDbId.Value);
      if (episode.ExternalId.TvRageId.HasValue) MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvRageId, episode.ExternalId.TvRageId.Value);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episode.SeasonNumber);
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episode.AirDate);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episode.Overview, false);

      MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount, seriesDetail.Rating, seriesDetail.RatingCount);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episode.GuestStars, PersonAspect.OCCUPATION_ACTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episode.Crew.Where(p => p.Job == "Director").ToList(), PersonAspect.OCCUPATION_DIRECTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episode.Crew.Where(p => p.Job == "Writer").ToList(), PersonAspect.OCCUPATION_WRITER), true, false);
    }

    private List<PersonInfo> ConvertToPersons(List<CrewItem> crew, string occupation)
    {
      if (crew == null || crew.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (CrewItem person in crew)
      {
        retValue.Add(new PersonInfo()
        {
          MovieDbId = person.PersonId,
          Name = person.Name,
          Occupation = occupation,
          Order = sortOrder++
        });
      }
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<CastItem> cast, string occupation)
    {
      if (cast == null || cast.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (CastItem person in cast)
      {
        retValue.Add(new PersonInfo()
        {
          MovieDbId = person.PersonId,
          Name = person.Name,
          Occupation = occupation,
          Order = sortOrder++
        });
      }
      return retValue;
    }

    private List<CharacterInfo> ConvertToCharacters(int seriesId, string seriesTitle, List<CastItem> characters)
    {
      if (characters == null || characters.Count == 0)
        return new List<CharacterInfo>();

      int sortOrder = 0;
      List<CharacterInfo> retValue = new List<CharacterInfo>();
      foreach (CastItem person in characters)
        retValue.Add(new CharacterInfo()
        {
          ActorMovieDbId = person.PersonId,
          ActorName = person.Name,
          Name = person.Character,
          Order = sortOrder++
        });
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanies(List<ProductionCompany> companies, string type)
    {
      if (companies == null || companies.Count == 0)
        return new List<CompanyInfo>();

      int sortOrder = 0;
      List<CompanyInfo> retValue = new List<CompanyInfo>();
      foreach (ProductionCompany company in companies)
      {
        retValue.Add(new CompanyInfo()
        {
          MovieDbId = company.Id,
          Name = company.Name,
          Type = type,
          Order = sortOrder++
        });
      }
      return retValue;
    }

    #endregion

    #region Online matching

    private static string FindBestMatchingLanguage(EpisodeInfo episodeInfo)
    {
      CultureInfo mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
      if (episodeInfo.Languages.Count == 0 || episodeInfo.Languages.Contains(mpLocal.TwoLetterISOLanguageName))
        return mpLocal.TwoLetterISOLanguageName;

      // If there is only one language available, use this one.
      if (episodeInfo.Languages.Count == 1)
        return episodeInfo.Languages[0];

      // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
      // By returning null we allow fallback to the default language of the online source (en).
      return null;
    }

    private bool TryMatch(EpisodeInfo episodeInfo, out Series seriesDetails)
    {
      if (episodeInfo.SeriesMovieDbId > 0 && _movieDb.GetSeries(episodeInfo.SeriesMovieDbId, out seriesDetails))
      {
        SeriesInfo searchSeries = new SeriesInfo()
        {
          Series = seriesDetails.Name,
          FirstAired = seriesDetails.FirstAirDate.HasValue ? seriesDetails.FirstAirDate : default(DateTime?)
        };
        StoreSeriesMatch(seriesDetails, searchSeries.ToString());
        return true;
      }
      seriesDetails = null;
      string preferredLookupLanguage = FindBestMatchingLanguage(episodeInfo);

      return TryMatch(episodeInfo.Series, episodeInfo.SeriesFirstAired.HasValue ? episodeInfo.SeriesFirstAired.Value.Year : 0, 
        preferredLookupLanguage, false, out seriesDetails);
    }

    protected bool TryMatch(string seriesName, int year, string language, bool cacheOnly, out Series seriesDetail)
    {
      SeriesInfo searchSeries = new SeriesInfo()
      {
        Series = seriesName,
        FirstAired = year > 0 ? new DateTime(year, 1, 1) : default(DateTime?)
      };
      seriesDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(searchSeries.ToString(), out seriesDetail))
          return true;

        // Load cache or create new list
        List<SeriesMatch> matches = _storage.GetMatches();

        // Init empty
        seriesDetail = null;

        // Use cached values before doing online query
        SeriesMatch match = matches.Find(m =>
          string.Equals(m.ItemName, searchSeries.ToString(), StringComparison.OrdinalIgnoreCase) ||
          string.Equals(m.OnlineName, searchSeries.ToString(), StringComparison.OrdinalIgnoreCase));
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesName, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        int tmDb = 0;
        if (match != null && !string.IsNullOrEmpty(match.Id) && int.TryParse(match.Id, out tmDb))
        {
          // If this is a known movie, only return the movie details.
          return _movieDb.GetSeries(tmDb, out seriesDetail);
        }

        if (cacheOnly)
          return false;

        List<SeriesSearchResult> series;
        if (_movieDb.SearchSeriesUnique(seriesName, year, language, out series))
        {
          SeriesSearchResult seriesResult = series[0];
          ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Found unique online match for \"{0}\": \"{1}\"", seriesName, seriesResult.Name);
          if (_movieDb.GetSeries(series[0].Id, out seriesDetail))
          {
            StoreSeriesMatch(seriesDetail, searchSeries.ToString());
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: No unique match found for \"{0}\"", seriesName);
        // Also save "non matches" to avoid retrying
        StoreSeriesMatch(null, searchSeries.ToString());
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception while processing series {0}", ex, seriesName);
        return false;
      }
      finally
      {
        if (seriesDetail != null)
          _memoryCache.TryAdd(searchSeries.ToString(), seriesDetail);
      }
    }

    private void StoreSeriesMatch(Series series, string seriesName)
    {
      if (series == null)
      {
        _storage.TryAddMatch(new SeriesMatch()
        {
          ItemName = seriesName
        });
        return;
      }
      SeriesInfo seriesMatch = new SeriesInfo()
      {
        Series = series.Name,
        FirstAired = series.FirstAirDate.HasValue ? series.FirstAirDate.Value : default(DateTime?)
      };
      var onlineMatch = new SeriesMatch
      {
        Id = series.Id.ToString(),
        ItemName = seriesName,
        OnlineName = seriesMatch.ToString()
      };
      _storage.TryAddMatch(onlineMatch);
    }

    #endregion

    #region Caching

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= MAX_MEMCACHE_DURATION)
        return;
      _memoryCache.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      // TODO: when updating movie information is implemented, start here a job to do it
    }

    #endregion

    public override bool Init()
    {
      if (!base.Init())
        return false;

      if (_movieDb != null)
        return true;

      _movieDb = new TheMovieDbWrapper();
      // Try to lookup online content in the configured language
      CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
      _movieDb.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
      return _movieDb.Init(CACHE_PATH);
    }

    #region FanArt

    public List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      List<string> fanartFiles = new List<string>();
      string path = null;
      if (scope == FanArtScope.Series)
      {
        SeriesInfo series = infoObject as SeriesInfo;
        if (series != null && series.MovieDbId > 0)
        {
          path = Path.Combine(CACHE_PATH, series.MovieDbId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtScope.Season)
      {
        SeasonInfo season = infoObject as SeasonInfo;
        if (season != null && season.SeriesMovieDbId > 0 && season.SeasonNumber.HasValue)
        {
          path = Path.Combine(CACHE_PATH, season.SeriesMovieDbId.ToString(), string.Format(@"{0} {1}\{2}\", scope, season.SeasonNumber, type));
        }
      }
      else if (scope == FanArtScope.Episode)
      {
        EpisodeInfo episode = infoObject as EpisodeInfo;
        if (episode != null && episode.MovieDbId > 0)
        {
          path = Path.Combine(CACHE_PATH, episode.MovieDbId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtScope.Actor || scope == FanArtScope.Director || scope == FanArtScope.Writer)
      {
        PersonInfo person = infoObject as PersonInfo;
        if (person != null && person.MovieDbId > 0)
        {
          path = Path.Combine(CACHE_PATH, person.MovieDbId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtScope.Company)
      {
        CompanyInfo company = infoObject as CompanyInfo;
        if (company != null && company.MovieDbId > 0)
        {
          path = Path.Combine(CACHE_PATH, company.MovieDbId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      if (Directory.Exists(path))
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
      return fanartFiles;
    }

    protected override void DownloadFanArt(string movieDbId)
    {
      try
      {
        if (string.IsNullOrEmpty(movieDbId))
          return;

        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Started for ID {0}", movieDbId);

        if (!Init())
          return;

        string[] ids;
        if (movieDbId.Contains("|"))
          ids = movieDbId.Split('|');
        else
          ids = new string[] { movieDbId };

        int seriesId = 0;
        if (!int.TryParse(ids[0], out seriesId))
          return;

        if (seriesId <= 0)
          return;

        Series seriesDetail;
        if (!_movieDb.GetSeries(seriesId, out seriesDetail))
          return;

        ImageCollection imageCollection;
        if (!_movieDb.GetSeriesFanArt(seriesId, out imageCollection))
          return;

        int episodeSeasonId = 0;
        int episodeId = 0;
        ImageCollection episodeImageCollection = null;
        if (ids.Length > 1)
        {
          if (int.TryParse(ids[1], out episodeSeasonId) && int.TryParse(ids[2], out episodeId))
          {
            _movieDb.GetSeriesEpisodeFanArt(seriesId, episodeSeasonId, episodeId, out episodeImageCollection);
          }
        }

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Begin saving banners for ID {0}", movieDbId);
        SaveBanners(imageCollection.Backdrops, string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.Backdrops));
        SaveBanners(imageCollection.Posters, string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.Posters));

        //Save season banners
        foreach (int season in seriesDetail.Seasons.Select(b => b.SeasonNumber).Distinct().ToList())
        {
          if (_movieDb.GetSeriesSeasonFanArt(seriesId, season, out imageCollection))
          {
            SaveBanners(imageCollection.Posters, string.Format(@"{0} {1}\{2}", FanArtScope.Season, season, FanArtType.Posters));
          }
        }

        if (episodeImageCollection != null)
        {
          // Save Episode Banners
          ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Begin saving episode banners for ID {0}", episodeImageCollection.Id);
          SaveBanners(episodeImageCollection.Stills, string.Format(@"{0}\{1}", FanArtScope.Episode, FanArtType.Thumbnails));
        }

        //Save person banners
        MovieCasts seriesCasts;
        if (_movieDb.GetSeriesCast(seriesId, out seriesCasts))
        {
          foreach (CastItem actor in seriesCasts.Cast)
          {
            if (_movieDb.GetPersonFanArt(actor.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, string.Format(@"{0}\{1}", FanArtScope.Actor, FanArtType.Thumbnails));
            }
          }
          foreach (CrewItem crew in seriesCasts.Crew.Where(p => p.Job == "Director").ToList())
          {
            if (_movieDb.GetPersonFanArt(crew.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, string.Format(@"{0}\{1}", FanArtScope.Director, FanArtType.Thumbnails));
            }
          }
          foreach (CrewItem crew in seriesCasts.Crew.Where(p => p.Job == "Author").ToList())
          {
            if (_movieDb.GetPersonFanArt(crew.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, string.Format(@"{0}\{1}", FanArtScope.Writer, FanArtType.Thumbnails));
            }
          }
        }

        //Save company banners
        Company company;
        foreach (ProductionCompany proCompany in seriesDetail.ProductionCompanies)
        {
          if (_movieDb.GetCompany(proCompany.Id, out company))
          {
            ImageItem image = new ImageItem();
            image.Id = company.Id;
            image.FilePath = company.LogoPath;
            SaveBanners(new ImageItem[] { image } , string.Format(@"{0}\{1}", FanArtScope.Company, FanArtType.Logos));
          }
        }
        
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Finished saving banners for ID {0}", movieDbId);

        // Remember we are finished
        FinishDownloadFanArt(movieDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Exception downloading FanArt for ID {0}", ex, movieDbId);
      }
    }

    private int SaveBanners(IEnumerable<ImageItem> banners, string category)
    {
      if (banners == null)
        return 0;

      int idx = 0;
      foreach (ImageItem banner in banners.Where(b => b.Language == null || b.Language == _movieDb.PreferredLanguage))
      {
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_movieDb.DownloadImage(banner, category))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Saved {0} {1}", idx, category);
      return idx;
    }

    #endregion
  }
}
