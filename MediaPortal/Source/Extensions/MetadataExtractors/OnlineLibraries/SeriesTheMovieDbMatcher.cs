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
using MediaPortal.Utilities;
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

    /// <summary>
    /// Tries to lookup the series from TheMovieDB and updates the given <paramref name="episodeInfo"/> with the online information.
    /// </summary>
    /// <param name="episodeInfo">Episode to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateEpisode(EpisodeInfo episodeInfo)
    {
      string preferredLookupLanguage = FindBestMatchingLanguage(episodeInfo);
      Series seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (/* Best way is to get details by an unique IMDB id */
        MatchByTmdbId(episodeInfo, out seriesDetail) ||
        TryMatch(episodeInfo.Series, preferredLookupLanguage, false, out seriesDetail)
        )
      {
        int movieDbId = 0;
        if (seriesDetail != null)
        {
          movieDbId = seriesDetail.Id;

          MetadataUpdater.SetOrUpdateId(ref episodeInfo.MovieDbId, seriesDetail.Id);
          if (seriesDetail.ExternalId.TvDbId.HasValue)
            MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvdbId, seriesDetail.ExternalId.TvDbId.Value);
          if (!string.IsNullOrEmpty(seriesDetail.ExternalId.ImDbId))
            MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, seriesDetail.ExternalId.ImDbId);

          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Series, seriesDetail.Name, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, seriesDetail.Genres.Select(g => g.Name).ToList(), true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Networks, ConvertToCompanys(seriesDetail.Networks, CompanyType.TVNetwork), true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.ProductionCompanys, ConvertToCompanys(seriesDetail.ProductionCompanies, CompanyType.ProductionStudio), true);

          if(seriesDetail.ContentRatingResults.Results.Count > 0)
          {
            var cert = seriesDetail.ContentRatingResults.Results.Where(c => c.CountryId == "US").First();
            if(cert != null)
              MetadataUpdater.SetOrUpdateString(ref episodeInfo.Certification, cert.ContentRating, true);
          }

          MovieCasts movieCasts;
          if (_movieDb.GetSeriesCast(movieDbId, out movieCasts))
          {
            MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(movieCasts.Cast, PersonOccupation.Actor), true);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(episodeInfo.MovieDbId, episodeInfo.Series, movieCasts.Cast), true);
          }

          // Also try to fill episode title from series details (most file names don't contain episode name).
          if (!TryMatchEpisode(episodeInfo, seriesDetail))
            return false;
        }

        if (movieDbId > 0)
          ScheduleDownload(movieDbId.ToString());
        return true;
      }
      return false;
    }

    public bool UpdateSeries(SeriesInfo seriesInfo)
    {
      Series seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seriesInfo.MovieDbId > 0 && _movieDb.GetSeries(seriesInfo.MovieDbId, out seriesDetail))
      {
        if (seriesDetail.ExternalId.TvDbId.HasValue)
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.TvdbId, seriesDetail.ExternalId.TvDbId.Value);
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

        MetadataUpdater.SetOrUpdateList(seriesInfo.Genres, seriesDetail.Genres.Select(g => g.Name).ToList(), true);
        MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanys(seriesDetail.Networks, CompanyType.TVNetwork), true);
        MetadataUpdater.SetOrUpdateList(seriesInfo.ProductionCompanys, ConvertToCompanys(seriesDetail.ProductionCompanies, CompanyType.ProductionStudio), true);

        MovieCasts movieCasts;
        if (_movieDb.GetSeriesCast(seriesInfo.MovieDbId, out movieCasts))
        {
          MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(movieCasts.Cast, PersonOccupation.Actor), true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Directors, ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Director").ToList(), PersonOccupation.Director), true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Writers, ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Writer").ToList(), PersonOccupation.Writer), true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesInfo.MovieDbId, seriesInfo.Series, movieCasts.Cast), true);
        }

        SeriesSeason season = seriesDetail.Seasons.Where(s => s.AirDate < DateTime.Now).Last();
        if (season != null)
        {
          Season currentSeason;
          if (_movieDb.GetSeriesSeason(seriesDetail.Id, season.SeasonNumber, out currentSeason) == false)
            return false;

          SeasonEpisode nextEpisode = currentSeason.Episodes.Where(e => e.AirDate > DateTime.Now).First();
          if(nextEpisode == null) //Try next season
          {
            if (_movieDb.GetSeriesSeason(seriesDetail.Id, season.SeasonNumber, out currentSeason) == false)
              return false;

            nextEpisode = currentSeason.Episodes.Where(e => e.AirDate > DateTime.Now).First();
          }
          if (nextEpisode != null)
          {
            MetadataUpdater.SetOrUpdateString(ref seriesInfo.NextEpisodeName, nextEpisode.Name, false);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeAirDate, nextEpisode.AirDate);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeSeasonNumber, nextEpisode.SeasonNumber);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeNumber, nextEpisode.EpisodeNumber);
          }
        }

        return true;
      }
      return false;
    }

    public bool UpdateSeason(SeasonInfo seasonInfo)
    {
      Series seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seasonInfo.MovieDbId > 0 && _movieDb.GetSeries(seasonInfo.MovieDbId, out seriesDetail))
      {
        if (seriesDetail.ExternalId.TvDbId.HasValue)
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvdbId, seriesDetail.ExternalId.TvDbId.Value);
        if (!string.IsNullOrEmpty(seriesDetail.ExternalId.ImDbId))
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.ImdbId, seriesDetail.ExternalId.ImDbId);

        MetadataUpdater.SetOrUpdateString(ref seasonInfo.Series, seriesDetail.Name, false);

        Season seasonDetail;
        if (_movieDb.GetSeriesSeason(seasonInfo.MovieDbId, seasonInfo.SeasonNumber.Value, out seasonDetail))
        {
          MetadataUpdater.SetOrUpdateValue(ref seasonInfo.FirstAired, seasonDetail.AirDate);
          MetadataUpdater.SetOrUpdateString(ref seasonInfo.Description, seasonDetail.Overview, false);
        }

        return true;
      }
      return false;
    }

    public bool UpdateEpisodePersons(EpisodeInfo episodeInfo, List<PersonInfo> persons, PersonOccupation occupation)
    {
      Series seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (episodeInfo.MovieDbId > 0 && _movieDb.GetSeries(episodeInfo.MovieDbId, out seriesDetail))
      {
        if (!TryMatchEpisode(episodeInfo, seriesDetail))
          return false;

        if (occupation == PersonOccupation.Actor)
        {
          MovieCasts movieCasts;
          if (_movieDb.GetSeriesCast(episodeInfo.MovieDbId, out movieCasts))
          {
            MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(movieCasts.Cast, PersonOccupation.Actor), false);
          }
          MetadataUpdater.SetOrUpdateList(persons, episodeInfo.Actors, false);
        }
        if (occupation == PersonOccupation.Director)
          MetadataUpdater.SetOrUpdateList(persons, episodeInfo.Directors, false);
        if (occupation == PersonOccupation.Writer)
          MetadataUpdater.SetOrUpdateList(persons, episodeInfo.Writers, false);

        return true;
      }
      return false;
    }

    public bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo, List<CharacterInfo> characters)
    {
      Series seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (episodeInfo.MovieDbId > 0 && _movieDb.GetSeries(episodeInfo.MovieDbId, out seriesDetail))
      {
        if (!TryMatchEpisode(episodeInfo, seriesDetail))
          return false;

        MovieCasts movieCasts;
        if (_movieDb.GetSeriesCast(episodeInfo.MovieDbId, out movieCasts))
        {
          MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(episodeInfo.MovieDbId, episodeInfo.Series, movieCasts.Cast), true);
        }
        MetadataUpdater.SetOrUpdateList(characters, episodeInfo.Characters, false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesPersons(SeriesInfo seriesInfo, List<PersonInfo> persons, PersonOccupation occupation)
    {
      // Try online lookup
      if (!Init())
        return false;

      MovieCasts movieCasts;
      if (seriesInfo.MovieDbId > 0 && _movieDb.GetSeriesCast(seriesInfo.MovieDbId, out movieCasts))
      {
        if (occupation == PersonOccupation.Actor)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(movieCasts.Cast, PersonOccupation.Actor), false);
        if (occupation == PersonOccupation.Director)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Director").ToList(), PersonOccupation.Director), false);
        if (occupation == PersonOccupation.Writer)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(movieCasts.Crew.Where(p => p.Job == "Writer").ToList(), PersonOccupation.Writer), false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesCharacters(SeriesInfo seriesInfo, List<CharacterInfo> characters)
    {
      Series seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      MovieCasts movieCasts;
      if (seriesInfo.MovieDbId > 0 && _movieDb.GetSeriesCast(seriesInfo.MovieDbId, out movieCasts))
      {
        MetadataUpdater.SetOrUpdateList(characters, ConvertToCharacters(seriesInfo.MovieDbId, seriesInfo.Series, movieCasts.Cast), false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesCompanys(SeriesInfo seriesInfo, List<CompanyInfo> companys, CompanyType type)
    {
      Series seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seriesInfo.MovieDbId > 0 && _movieDb.GetSeries(seriesInfo.MovieDbId, out seriesDetail))
      {
        if (type == CompanyType.TVNetwork)
          MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanys(seriesDetail.Networks, CompanyType.TVNetwork), false);
        if (type == CompanyType.ProductionStudio)
          MetadataUpdater.SetOrUpdateList(seriesInfo.ProductionCompanys, ConvertToCompanys(seriesDetail.ProductionCompanies, CompanyType.ProductionStudio), false);

        return true;
      }
      return false;
    }

    protected bool TryMatchEpisode(EpisodeInfo episodeInfo, Series seriesDetail)
    {
      Season season = null;
      List<SeasonEpisode> episodes = null;
      if (episodeInfo.SeasonNumber.HasValue)
      {
        if (_movieDb.GetSeriesSeason(seriesDetail.Id, episodeInfo.SeasonNumber.Value, out season) == false)
          return false;

        SeasonEpisode episode;
        episodes = season.Episodes.FindAll(e => e.Name == episodeInfo.Episode);
        // In few cases there can be multiple episodes with same name. In this case we cannot know which one is right
        // and keep the current episode details.
        // Use this way only for single episodes.
        if (episodeInfo.EpisodeNumbers.Count == 1 && episodes.Count == 1)
        {
          episode = episodes[0];
          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.Name, false);
          SetEpisodeDetails(episodeInfo, seriesDetail, episode);
          return true;
        }

        episodes = season.Episodes.Where(e => episodeInfo.EpisodeNumbers.Contains(e.EpisodeNumber) && e.SeasonNumber == episodeInfo.SeasonNumber).ToList();
        if (episodes.Count == 0)
          return false;

        // Single episode entry
        if (episodes.Count == 1)
        {
          episode = episodes[0];
          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.Name, false);
          SetEpisodeDetails(episodeInfo, seriesDetail, episode);
          return true;
        }

        // Multiple episodes
        SetMultiEpisodeDetailsl(episodeInfo, seriesDetail, episodes);
        return true;
      }
      return false;
    }

    private void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, Series seriesDetail, List<SeasonEpisode> episodes)
    {
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episodes.First().SeasonNumber);
      MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodes.Select(x => x.EpisodeNumber).ToList(), true);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episodes.First().AirDate);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.TotalRating, episodes.Sum(e => e.Rating.HasValue ? e.Rating.Value : 0) / episodes.Count); // Average rating
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.RatingCount, episodes.Sum(e => e.RatingCount.HasValue ? e.RatingCount.Value : 0));

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.Name).ToArray()), false);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Overview)).ToArray()), false);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episodes.SelectMany(e => e.GuestStars).ToList(), PersonOccupation.Actor), true);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episodes.SelectMany(e => e.Crew.Where(p => p.Job == "Director")).ToList(), PersonOccupation.Director), true);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episodes.SelectMany(e => e.Crew.Where(p => p.Job == "Writer")).ToList(), PersonOccupation.Writer), true);
    }

    private void SetEpisodeDetails(EpisodeInfo episodeInfo, Series seriesDetail, SeasonEpisode episode)
    {
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episode.SeasonNumber);
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episode.AirDate);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episode.Overview, false);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.TotalRating, seriesDetail.Rating.HasValue ? seriesDetail.Rating.Value : 0);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.RatingCount, seriesDetail.RatingCount.HasValue ? seriesDetail.RatingCount.Value : 0);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episode.GuestStars, PersonOccupation.Actor), true);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episode.Crew.Where(p => p.Job == "Director").ToList(), PersonOccupation.Director), true);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episode.Crew.Where(p => p.Job == "Writer").ToList(), PersonOccupation.Writer), true);
    }

    private List<PersonInfo> ConvertToPersons(List<CrewItem> crew, PersonOccupation occupation)
    {
      if (crew == null || crew.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (CrewItem person in crew)
      {
        Person personDetail;
        if (_movieDb.GetPerson(person.PersonId, out personDetail))
        {
          retValue.Add(new PersonInfo()
          {
            MovieDbId = person.PersonId,
            Name = person.Name,
            Biography = personDetail.Biography,
            DateOfBirth = personDetail.DateOfBirth,
            DateOfDeath = personDetail.DateOfDeath,
            Orign = personDetail.PlaceOfBirth,
            ImdbId = personDetail.ExternalId.ImDbId,
            TvdbId = personDetail.ExternalId.TvDbId.HasValue ? personDetail.ExternalId.TvDbId.Value : 0,
            Occupation = occupation
          });
        }
      }
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<CastItem> cast, PersonOccupation occupation)
    {
      if (cast == null || cast.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (CastItem person in cast)
      {
        Person personDetail;
        if (_movieDb.GetPerson(person.PersonId, out personDetail))
        {
          retValue.Add(new PersonInfo()
          {
            MovieDbId = person.PersonId,
            Name = person.Name,
            Biography = personDetail.Biography,
            DateOfBirth = personDetail.DateOfBirth,
            DateOfDeath = personDetail.DateOfDeath,
            Orign = personDetail.PlaceOfBirth,
            ImdbId = personDetail.ExternalId.ImDbId,
            TvdbId = personDetail.ExternalId.TvDbId.HasValue ? personDetail.ExternalId.TvDbId.Value : 0,
            Occupation = occupation
          });
        }
      }
      return retValue;
    }

    private List<CharacterInfo> ConvertToCharacters(int seriesId, string seriesTitle, List<CastItem> characters)
    {
      if (characters == null || characters.Count == 0)
        return new List<CharacterInfo>();

      List<CharacterInfo> retValue = new List<CharacterInfo>();
      foreach (CastItem person in characters)
        retValue.Add(new CharacterInfo()
        {
          MediaIsMovie = false,
          MediaMovieDbId = seriesId,
          MediaTitle = seriesTitle,
          ActorMovieDbId = person.PersonId,
          ActorName = person.Name,
          Name = person.Character
        });
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanys(List<ProductionCompany> companys, CompanyType type)
    {
      if (companys == null || companys.Count == 0)
        return new List<CompanyInfo>();

      List<CompanyInfo> retValue = new List<CompanyInfo>();
      foreach (ProductionCompany company in companys)
      {
        Company companyDetail;
        if (_movieDb.GetCompany(company.Id, out companyDetail))
        {
          retValue.Add(new CompanyInfo()
          {
            MovieDbId = company.Id,
            Name = company.Name,
            Description = companyDetail.Description,
            Type = type
          });
        }
      }
      return retValue;
    }

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

    private bool MatchByTmdbId(EpisodeInfo episodeInfo, out Series seriesDetails)
    {
      if (episodeInfo.MovieDbId > 0 && _movieDb.GetSeries(episodeInfo.MovieDbId, out seriesDetails))
      {
        SaveMatchToPersistentCache(seriesDetails, seriesDetails.Name);
        return true;
      }
      seriesDetails = null;
      return false;
    }

    protected bool TryMatch(string seriesName, string language, bool cacheOnly, out Series seriesDetail)
    {
      seriesDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(seriesName, out seriesDetail))
          return true;

        // Load cache or create new list
        List<SeriesMatch> matches = _storage.GetMatches();

        // Init empty
        seriesDetail = null;

        // Use cached values before doing online query
        SeriesMatch match = matches.Find(m => 
          string.Equals(m.ItemName, seriesName, StringComparison.OrdinalIgnoreCase) || 
          string.Equals(m.TvDBName, seriesName, StringComparison.OrdinalIgnoreCase));
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesName, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        int tmDb = 0;
        if (!string.IsNullOrEmpty(match.Id) && int.TryParse(match.Id, out tmDb))
        {
          // If this is a known movie, only return the movie details.
          if (match != null)
            return _movieDb.GetSeries(tmDb, out seriesDetail);
        }

        if (cacheOnly)
          return false;

        List<SeriesSearchResult> series;
        if (_movieDb.SearchSeriesUnique(seriesName, 0, language, out series))
        {
          SeriesSearchResult seriesResult = series[0];
          ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: Found unique online match for \"{0}\": \"{1}\"", seriesName, seriesResult.Name);
          if (_movieDb.GetSeries(series[0].Id, out seriesDetail))
          {
            SaveMatchToPersistentCache(seriesDetail, seriesName);
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher: No unique match found for \"{0}\"", seriesName);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new SeriesMatch { ItemName = seriesName, TvDBName = seriesName });
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
          _memoryCache.TryAdd(seriesName, seriesDetail);
      }
    }

    private void SaveMatchToPersistentCache(Series seriesDetails, string seriesName)
    {
      var onlineMatch = new SeriesMatch
      {
        Id = seriesDetails.Id.ToString(),
        ItemName = seriesName,
        TvDBName = seriesDetails.Name
      };
      _storage.TryAddMatch(onlineMatch);
    }

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

    protected override void DownloadFanArt(string movieDbId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Started for ID {0}", movieDbId);

        if (!Init())
          return;

        int tmDb = 0;
        if (!int.TryParse(movieDbId, out tmDb))
          return;

        Series series;
        if (!_movieDb.GetSeries(tmDb, out series))
          return;

        ImageCollection imageCollection;
        if (!_movieDb.GetSeriesFanArt(tmDb, out imageCollection))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Begin saving banners for ID {0}", movieDbId);
        SaveBanners(imageCollection.Backdrops, "Backdrops");
        SaveBanners(imageCollection.Posters, "Posters");

        //Save season banners
        foreach (int season in series.Seasons.Select(b => b.SeasonNumber).Distinct().ToList())
        {
          if (_movieDb.GetSeriesSeasonFanArt(tmDb, season, out imageCollection))
          {
            SaveBanners(imageCollection.Posters, string.Format(@"Posters\Season {0}", season));
          }
        }

        //Save person banners
        MovieCasts seriesCasts;
        if (_movieDb.GetSeriesCast(tmDb, out seriesCasts))
        {
          foreach (CastItem actor in seriesCasts.Cast)
          {
            if (_movieDb.GetPersonFanArt(actor.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, "Thumbnails");
            }
          }
          foreach (CrewItem crew in seriesCasts.Crew.Where(p => p.Job == "Director").ToList())
          {
            if (_movieDb.GetPersonFanArt(crew.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, "Thumbnails");
            }
          }
          foreach (CrewItem crew in seriesCasts.Crew.Where(p => p.Job == "Author").ToList())
          {
            if (_movieDb.GetPersonFanArt(crew.PersonId, out imageCollection))
            {
              SaveBanners(imageCollection.Profiles, "Thumbnails");
            }
          }
        }

        //Save company banners
        Company company;
        foreach (ProductionCompany proCompany in series.ProductionCompanies)
        {
          if (_movieDb.GetCompany(proCompany.Id, out company))
          {
            ImageItem image = new ImageItem();
            image.Id = company.Id;
            image.FilePath = company.LogoPath;
            SaveBanners(new ImageItem[] { image } , "Logos");
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
  }
}
