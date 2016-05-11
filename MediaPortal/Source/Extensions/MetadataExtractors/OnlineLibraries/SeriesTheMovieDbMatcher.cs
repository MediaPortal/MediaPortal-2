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
          int movieDbId = 0;
          if (seriesDetail != null)
          {
            movieDbId = seriesDetail.Id;

            MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesMovieDbId, seriesDetail.Id);
            if (seriesDetail.ExternalId.TvDbId.HasValue)
              MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvdbId, seriesDetail.ExternalId.TvDbId.Value);
            if (seriesDetail.ExternalId.TvRageId.HasValue)
              MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvRageId, seriesDetail.ExternalId.TvRageId.Value);
            if (!string.IsNullOrEmpty(seriesDetail.ExternalId.ImDbId))
              MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesImdbId, seriesDetail.ExternalId.ImDbId);

            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Series, seriesDetail.Name, true);
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
            if (_movieDb.GetSeriesCast(movieDbId, out movieCasts))
            {
              MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(movieCasts.Cast, PersonAspect.OCCUPATION_ACTOR), true, false);
              MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(episodeInfo.SeriesMovieDbId, episodeInfo.Series, movieCasts.Cast), true, false);
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

          ImageCollection imageCollection;
          if (seriesInfo.Thumbnail == null &&
            _movieDb.GetSeriesFanArt(seriesInfo.MovieDbId, out imageCollection))
          {
            seriesInfo.Thumbnail = GetImage(imageCollection.Posters, "Posters");
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

          ImageCollection imageCollection;
          if (seasonInfo.Thumbnail == null && seasonInfo.SeasonNumber.HasValue &&
            _movieDb.GetSeriesSeasonFanArt(seasonInfo.SeriesMovieDbId, seasonInfo.SeasonNumber.Value, out imageCollection))
          {
            seasonInfo.Thumbnail = GetImage(imageCollection.Posters, string.Format(@"Posters\Season {0}", seasonInfo.SeasonNumber.Value));
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

        ImageCollection imageCollection;
        if (person.Thumbnail == null && _movieDb.GetPersonFanArt(person.MovieDbId, out imageCollection))
        {
          person.Thumbnail = GetImage(imageCollection.Profiles, "Thumbnails");
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
            ImageItem image = new ImageItem();
            image.Id = company.MovieDbId;
            image.FilePath = companyDetail.LogoPath;
            company.Thumbnail = GetImage(new ImageItem[] { image }, "Logos");
          }
        }
      }
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

    private byte[] GetImage(IEnumerable<ImageItem> images, string category)
    {
      if (images == null)
        return null;

      foreach (ImageItem image in images.Where(b => b.Language == null || b.Language == _movieDb.PreferredLanguage))
      {
        if (_movieDb.DownloadImage(image, category))
        {
          return _movieDb.GetImage(image, category);
        }
      }
      return null;
    }

    private void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, Series seriesDetail, List<SeasonEpisode> episodes)
    {
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

      //Thumbnail
      ImageCollection imageCollection;
      if (episodeInfo.Thumbnail == null && episodeInfo.SeasonNumber.HasValue && episodeInfo.EpisodeNumbers.Count > 0 &&
        _movieDb.GetSeriesEpsiodeFanArt(episodeInfo.SeriesMovieDbId, episodeInfo.SeasonNumber.Value, episodeInfo.EpisodeNumbers[0], out imageCollection))
      {
        episodeInfo.Thumbnail = GetImage(imageCollection.Stills, string.Format(@"Thumbnails\Season {0}\Episode {1}",
          episodeInfo.SeasonNumber.Value, episodeInfo.EpisodeNumbers[0]));
      }
    }

    private void SetEpisodeDetails(EpisodeInfo episodeInfo, Series seriesDetail, SeasonEpisode episode)
    {
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episode.SeasonNumber);
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episode.AirDate);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episode.Overview, false);

      MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount, seriesDetail.Rating, seriesDetail.RatingCount);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episode.GuestStars, PersonAspect.OCCUPATION_ACTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episode.Crew.Where(p => p.Job == "Director").ToList(), PersonAspect.OCCUPATION_DIRECTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episode.Crew.Where(p => p.Job == "Writer").ToList(), PersonAspect.OCCUPATION_WRITER), true, false);

      //Thumbnail
      ImageCollection imageCollection;
      if (episodeInfo.Thumbnail == null && episodeInfo.SeasonNumber.HasValue && episodeInfo.EpisodeNumbers.Count > 0 &&
        _movieDb.GetSeriesEpsiodeFanArt(episodeInfo.SeriesMovieDbId, episodeInfo.SeasonNumber.Value, episodeInfo.EpisodeNumbers[0], out imageCollection))
      {
        episodeInfo.Thumbnail = GetImage(imageCollection.Stills, string.Format(@"Thumbnails\Season {0}\Episode {1}",
          episodeInfo.SeasonNumber.Value, episodeInfo.EpisodeNumbers[0]));
      }
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
        SaveMatchToPersistentCache(seriesDetails, seriesDetails.Name);
        return true;
      }
      seriesDetails = null;
      string preferredLookupLanguage = FindBestMatchingLanguage(episodeInfo);
      return TryMatch(episodeInfo.Series, preferredLookupLanguage, false, out seriesDetails);
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
        if (match != null && !string.IsNullOrEmpty(match.Id) && int.TryParse(match.Id, out tmDb))
        {
          // If this is a known movie, only return the movie details.
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
        if (string.IsNullOrEmpty(movieDbId))
          return;

        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Started for ID {0}", movieDbId);

        if (!Init())
          return;

        int tmDb = 0;
        if (!int.TryParse(movieDbId, out tmDb))
          return;

        if (tmDb <= 0)
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
