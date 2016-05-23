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
using MediaPortal.Common.Threading;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TheTvDB;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System.Drawing;

namespace MediaPortal.Extensions.OnlineLibraries
{
  /// <summary>
  /// <see cref="SeriesTvDbMatcher"/> is used to look up online series information from TheTvDB.com.
  /// </summary>
  public class SeriesTvDbMatcher : BaseMatcher<SeriesMatch, string>
  {
    #region Static instance

    public static SeriesTvDbMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesTvDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TvDB\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, TvdbSeries> _memoryCache = new ConcurrentDictionary<string, TvdbSeries>(StringComparer.OrdinalIgnoreCase);
    protected bool _useUniversalLanguage = false; // Universal language often leads to unwanted cover languages (i.e. russian)

    /// <summary>
    /// Contains the initialized TvDbWrapper.
    /// </summary>
    private TvDbWrapper _tv;

    #endregion

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the series from TheTvDB and updates the given <paramref name="episodeInfo"/> with the online information (Series and Episode names).
    /// </summary>
    /// <param name="episodeInfo">Series to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateEpisode(EpisodeInfo episodeInfo)
    {
      try
      {
        TvdbSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (TryMatch(episodeInfo, false, out seriesDetail))
        {
          string tvDbId = "";
          if (seriesDetail != null)
          {
            tvDbId = seriesDetail.Id.ToString();

            MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvdbId, seriesDetail.Id);
            MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesImdbId, seriesDetail.ImdbId);

            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Series, seriesDetail.SeriesName, false);
            MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, seriesDetail.FirstAired);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, seriesDetail.Genre, true, false);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Networks, ConvertToCompanies(seriesDetail.NetworkID, seriesDetail.Network, CompanyAspect.COMPANY_TV_NETWORK), true, false);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(seriesDetail.TvdbActors, PersonAspect.OCCUPATION_ACTOR), true, false);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.SeriesName, seriesDetail.TvdbActors), true, false);
            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Certification, seriesDetail.ContentRating, true);

            // Also try to fill episode title from series details (most file names don't contain episode name).
            if (!TryMatchEpisode(episodeInfo, seriesDetail))
              return false;

            if(episodeInfo.TvdbId > 0)
              tvDbId += "|" + episodeInfo.TvdbId;

            if (episodeInfo.Thumbnail == null)
            {
              List<string> thumbs = GetFanArtFiles(episodeInfo, FanArtScope.Episode, FanArtType.Thumbnails);
              if (thumbs.Count > 0)
                episodeInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }

          if (tvDbId.Length > 0)
            ScheduleDownload(tvDbId);
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing episode {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeries(SeriesInfo seriesInfo)
    {
      try
      {
        TvdbSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (seriesInfo.TvdbId > 0 && _tv.GetSeries(seriesInfo.TvdbId, true, true, out seriesDetail))
        {
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.ImdbId, seriesDetail.ImdbId);

          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Series, seriesDetail.SeriesName, false);
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Description, seriesDetail.Overview, false);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.FirstAired, seriesDetail.FirstAired);
          if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
          {
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.IsEnded, true);
          }
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Certification, seriesDetail.ContentRating, true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Genres, seriesDetail.Genre, true, false);

          MetadataUpdater.SetOrUpdateRatings(ref seriesInfo.TotalRating, ref seriesInfo.RatingCount, seriesDetail.Rating, seriesDetail.RatingCount > 0 ? seriesDetail.RatingCount : 0);

          MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanies(seriesDetail.NetworkID, seriesDetail.Network, CompanyAspect.COMPANY_TV_NETWORK), false, false);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(seriesDetail.TvdbActors, PersonAspect.OCCUPATION_ACTOR), false, false);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.SeriesName, seriesDetail.TvdbActors), false, false);

          TvdbEpisode nextEpisode = seriesDetail.Episodes.Where(e => e.FirstAired > DateTime.Now).FirstOrDefault();
          if (nextEpisode != null)
          {
            MetadataUpdater.SetOrUpdateString(ref seriesInfo.NextEpisodeName, nextEpisode.EpisodeName, false);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeAirDate, nextEpisode.FirstAired);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeSeasonNumber, nextEpisode.SeasonNumber);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeNumber, nextEpisode.EpisodeNumber);
          }

          if (seriesInfo.Thumbnail == null)
          {
            List<string> thumbs = GetFanArtFiles(seriesInfo, FanArtScope.Series, FanArtType.Posters);
            if(thumbs.Count > 0)
              seriesInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing series {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeason(SeasonInfo seasonInfo)
    {
      try
      {
        TvdbSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (seasonInfo.SeriesTvdbId > 0 && _tv.GetSeries(seasonInfo.SeriesTvdbId, true, false, out seriesDetail))
        {
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesImdbId, seriesDetail.ImdbId);
          var episode = seriesDetail.Episodes.Where(e => e.SeasonNumber == seasonInfo.SeasonNumber).ToList().FirstOrDefault();
          if (episode != null)
          {
            MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvdbId, episode.SeasonId);
          }

          MetadataUpdater.SetOrUpdateString(ref seasonInfo.Series, seriesDetail.SeriesName, false);
          MetadataUpdater.SetOrUpdateString(ref seasonInfo.Description, seriesDetail.Overview, false);

          List<TvdbEpisode> episodes = seriesDetail.GetEpisodes(seasonInfo.SeasonNumber.Value);
          MetadataUpdater.SetOrUpdateValue(ref seasonInfo.FirstAired, episodes.OrderBy(e => e.FirstAired).First().FirstAired);

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
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing season {0}", ex, seasonInfo.ToString());
        return false;
      }
    }

    public bool UpdateEpisodePersons(EpisodeInfo episodeInfo, string occupation)
    {
      try
      {
        TvdbSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (occupation != PersonAspect.OCCUPATION_ACTOR)
          return false;

        if (episodeInfo.SeriesTvdbId > 0 && _tv.GetSeries(episodeInfo.SeriesTvdbId, false, true, out seriesDetail))
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
          {
            MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(seriesDetail.TvdbActors, occupation), false, false);

            foreach (PersonInfo person in episodeInfo.Actors)
            {
              if (person.Thumbnail == null && person.TvdbId > 0)
              {
                List<string> thumbs = GetFanArtFiles(person, FanArtScope.Actor, FanArtType.Thumbnails);
                if (thumbs.Count > 0)
                  person.Thumbnail = File.ReadAllBytes(thumbs[0]);
              }
            }
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing persons {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo)
    {
      try
      {
        TvdbSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (episodeInfo.SeriesTvdbId > 0 && _tv.GetSeries(episodeInfo.SeriesTvdbId, false, true, out seriesDetail))
        {
          MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.SeriesName, seriesDetail.TvdbActors), false, false);

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing characters {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesPersons(SeriesInfo seriesInfo, string occupation)
    {
      try
      {
        TvdbSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (occupation != PersonAspect.OCCUPATION_ACTOR)
          return false;

        if (seriesInfo.TvdbId > 0 && _tv.GetSeries(seriesInfo.TvdbId, false, true, out seriesDetail))
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
          {
            MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(seriesDetail.TvdbActors, occupation), false, false);

            foreach (PersonInfo person in seriesInfo.Actors)
            {
              if (person.Thumbnail == null && person.TvdbId > 0)
              {
                List<string> thumbs = GetFanArtFiles(person, FanArtScope.Actor, FanArtType.Thumbnails);
                if (thumbs.Count > 0)
                  person.Thumbnail = File.ReadAllBytes(thumbs[0]);
              }
            }
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing series persons {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesCharacters(SeriesInfo seriesInfo)
    {
      try
      {
        TvdbSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (seriesInfo.TvdbId > 0 && _tv.GetSeries(seriesInfo.TvdbId, false, true, out seriesDetail))
        {
          MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.SeriesName, seriesDetail.TvdbActors), false, false);

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing series characters {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string type)
    {
      try
      {
        TvdbSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (type != CompanyAspect.COMPANY_TV_NETWORK)
          return false;

        if (seriesInfo.TvdbId > 0 && _tv.GetSeries(seriesInfo.TvdbId, false, true, out seriesDetail))
        {
          if (type == CompanyAspect.COMPANY_TV_NETWORK)
            MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanies(seriesDetail.NetworkID, seriesDetail.Network, type), false, false);

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing series companies {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, TvdbSeries seriesDetail, List<TvdbEpisode> episodes)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvMazeId, episodes.First().Id);
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, episodes.First().ImdbId);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episodes.First().SeasonNumber);
      MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodes.Select(x => x.EpisodeNumber).ToList(), true, false);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episodes.First().FirstAired);
      MetadataUpdater.SetOrUpdateList(episodeInfo.DvdEpisodeNumbers, episodes.Where(x => x.DvdEpisodeNumber >= 0).Select(x => x.DvdEpisodeNumber).ToList(), true, false);

      MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount,
        episodes.Sum(e => e.Rating) / episodes.Count, episodes.Sum(e => e.RatingCount > 0 ? e.RatingCount : 0)); // Average rating

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.EpisodeName).ToArray()), false);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Overview)).ToArray()), false);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episodes.SelectMany(e => e.GuestStars).ToList(), PersonAspect.OCCUPATION_ACTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episodes.SelectMany(e => e.Directors).ToList(), PersonAspect.OCCUPATION_DIRECTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episodes.SelectMany(e => e.Writer).ToList(), PersonAspect.OCCUPATION_WRITER), true, false);
    }

    private void SetEpisodeDetails(EpisodeInfo episodeInfo, TvdbSeries seriesDetail, TvdbEpisode episode)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvdbId, episode.Id);
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, episode.ImdbId);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episode.SeasonNumber);
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      episodeInfo.DvdEpisodeNumbers.Clear();
      if (episode.DvdEpisodeNumber >= 0) episodeInfo.DvdEpisodeNumbers.Add(episode.DvdEpisodeNumber);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episode.FirstAired);

      MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount, episode.Rating, episode.RatingCount > 0 ? episode.RatingCount : 0);

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.EpisodeName, false);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episode.Overview, false);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episode.GuestStars, PersonAspect.OCCUPATION_ACTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episode.Directors, PersonAspect.OCCUPATION_DIRECTOR), true, false);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episode.Writer, PersonAspect.OCCUPATION_WRITER), true, false);
    }

    private List<PersonInfo> ConvertToPersons(List<TvdbActor> actors, string occupation)
    {
      if (actors == null || actors.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (TvdbActor person in actors)
        retValue.Add(new PersonInfo() { TvdbId = person.Id, Name = person.Name, Occupation = occupation, Order = sortOrder++ });
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<string> actors, string occupation)
    {
      if (actors == null || actors.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (string person in actors)
        retValue.Add(new PersonInfo() { Name = person, Occupation = occupation, Order = sortOrder++ });
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanies(int companyID, string company, string type)
    {
      if (string.IsNullOrEmpty(company))
        return new List<CompanyInfo>();

      int sortOrder = 0;
      return new List<CompanyInfo>(new CompanyInfo[]
      {
        new CompanyInfo()
        {
          TvdbId = companyID > 0 ? companyID : 0,
          Name = company,
          Type = type,
          Order = sortOrder++
        }
      });
    }

    private List<CharacterInfo> ConvertToCharacters(int seriesId, string seriesTitle, List<TvdbActor> actors)
    {
      if (actors == null || actors.Count == 0)
        return new List<CharacterInfo>();

      int sortOrder = 0;
      List<CharacterInfo> retValue = new List<CharacterInfo>();
      foreach (TvdbActor person in actors)
        retValue.Add(new CharacterInfo()
        {
          ActorTvdbId = person.Id,
          ActorName = person.Name,
          Name = person.Role,
          Order = sortOrder++
        });
      return retValue;
    }

    #endregion

    #region Online matching

    protected bool TryMatch(EpisodeInfo episodeInfo, bool cacheOnly, out TvdbSeries seriesDetail)
    {
      // If series has an TVDBID, prefer it over imdb or name lookup.
      if (episodeInfo.SeriesTvdbId != 0)
      {
        if(_tv.GetSeries(episodeInfo.SeriesTvdbId, true, true, out seriesDetail))
        {
          StoreSeriesMatch(seriesDetail, episodeInfo.ToString());
          return true;
        }
      }

      // If series has an IMDBID, prefer it over name lookup.
      string imdbId = episodeInfo.SeriesImdbId;
      if (!string.IsNullOrWhiteSpace(episodeInfo.SeriesImdbId))
      {
        TvdbSearchResult matchedSeries = null;
        if (_tv.GetSeries(episodeInfo.SeriesImdbId, out matchedSeries))
        {
          if (_tv.GetSeries(matchedSeries.Id, true, true, out seriesDetail))
          {
            StoreSeriesMatch(seriesDetail, episodeInfo.ToString());
            return true;
          }
        }
      }

      // Perform name lookup.
      return TryMatch(episodeInfo.Series, episodeInfo.SeriesFirstAired.HasValue ? episodeInfo.SeriesFirstAired.Value.Year : 0, cacheOnly, out seriesDetail);
    }

    protected bool TryMatch(string seriesName, int year, bool cacheOnly, out TvdbSeries seriesDetail)
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
        List<SeriesMatch> matches;
        lock (_syncObj)
          matches = _storage.GetMatches();

        // Init empty
        seriesDetail = null;

        // Use cached values before doing online query
        SeriesMatch match = matches.Find(m =>
          (string.Equals(m.ItemName, searchSeries.ToString(), StringComparison.OrdinalIgnoreCase) ||
          string.Equals(m.OnlineName, searchSeries.ToString(), StringComparison.OrdinalIgnoreCase)));
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesName, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        int tvDb = 0;
        if (match != null && !string.IsNullOrEmpty(match.Id))
        {
          if (int.TryParse(match.Id, out tvDb))
          {
            // If this is a known series, only return the series details (including episodes).
            if (match != null)
              return tvDb != 0 && _tv.GetSeries(tvDb, true, true, out seriesDetail);
          }
        }

        if (cacheOnly)
          return false;

        TvdbSearchResult matchedSeries = null;
        List<TvdbSearchResult> series;
        if (_tv.SearchSeriesUnique(seriesName, out series))
          matchedSeries = series[0];

        if (matchedSeries != null)
        {
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Found unique online match for \"{0}\": \"{1}\" [Lang: {2}]", seriesName, matchedSeries.SeriesName, matchedSeries.Language);
          if( _tv.GetSeries(matchedSeries.Id, true, true, out seriesDetail))
          {
            // Add this match to cache
            StoreSeriesMatch(seriesDetail, seriesName);
            return true;
          }
        }

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: No unique match found for \"{0}\"", seriesName);
        // Also save "non matches" to avoid retrying
        StoreSeriesMatch(null, seriesName);
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing series {0}", ex, seriesName);
        return false;
      }
      finally
      {
        if (seriesDetail != null)
          _memoryCache.TryAdd(searchSeries.ToString(), seriesDetail);
      }
    }

    protected bool TryMatchEpisode(EpisodeInfo episodeInfo, TvdbSeries seriesDetail)
    {
      // We deal with two scenarios here:
      //  - Having a real episode title, but the Season/Episode numbers might be wrong (seldom case)
      //  - Having only Season/Episode numbers and we need to fill Episode title (more common)
      TvdbEpisode episode;
      List<TvdbEpisode> episodes = seriesDetail.Episodes.FindAll(e => e.EpisodeName == episodeInfo.Episode);
      // In few cases there can be multiple episodes with same name. In this case we cannot know which one is right
      // and keep the current episode details.
      // Use this way only for single episodes.
      if (episodeInfo.EpisodeNumbers.Count == 1 && episodes.Count == 1)
      {
        episode = episodes[0];
        MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.EpisodeName, false);
        SetEpisodeDetails(episodeInfo, seriesDetail, episode);
        return true;
      }

      episodes = seriesDetail.Episodes.Where(e => episodeInfo.EpisodeNumbers.Contains(e.EpisodeNumber) && e.SeasonNumber == episodeInfo.SeasonNumber).ToList();
      if (episodes.Count == 0)
        return false;

      // Single episode entry
      if (episodes.Count == 1)
      {
        episode = episodes[0];
        MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.EpisodeName, false);
        SetEpisodeDetails(episodeInfo, seriesDetail, episode);
        return true;
      }

      // Multiple episodes
      SetMultiEpisodeDetailsl(episodeInfo, seriesDetail, episodes);

      return true;
    }

    private void StoreSeriesMatch(TvdbSeries series, string seriesName)
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
        Series = series.SeriesName,
        FirstAired = series.FirstAired
      };
      // Add this match to cache
      SeriesMatch onlineMatch = new SeriesMatch
      {
        Id = series.Id.ToString(),
        ItemName = seriesName,
        OnlineName = seriesMatch.ToString()
      };

      // Save cache
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
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>(false);
      if (threadPool != null)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Refreshing local cache");
        threadPool.Add(() =>
        {
          if (Init())
            _tv.UpdateCache();
        });
      }
    }

    #endregion

    public override bool Init()
    {
      if (!base.Init())
        return false;

      if (_tv != null)
        return true;
      try
      {
        TvDbWrapper tv = new TvDbWrapper();
        // Try to lookup online content in the configured language
        CultureInfo currentCulture = ServiceRegistration.Get<ILocalization>().CurrentCulture;
        tv.SetPreferredLanguage(currentCulture.TwoLetterISOLanguageName);
        bool res = tv.Init();
        _tv = tv;
        return res;
      }
      catch (Libraries.TvdbLib.Exceptions.TvdbNotAvailableException)
      {
        return false;
      }
    }

    #region FanArt

    public List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      List<string> fanartFiles = new List<string>();
      if (scope == FanArtScope.Series)
      {
        SeriesInfo series = infoObject as SeriesInfo;
        if (series != null && series.TvdbId > 0)
        {
          string path = Path.Combine(CACHE_PATH, series.TvdbId.ToString());
          if (Directory.Exists(path))
          {
            if (type == FanArtType.Banners)
            {
              fanartFiles.AddRange(Directory.GetFiles(path, "img_graphical_ *.jpg"));
              fanartFiles.AddRange(Directory.GetFiles(path, "img_text_*.jpg"));
            }
            else if (type == FanArtType.Posters)
            {
              fanartFiles.AddRange(Directory.GetFiles(path, "img_posters_*.jpg"));
            }
            else if (type == FanArtType.Backdrops)
            {
              fanartFiles.AddRange(Directory.GetFiles(path, "img_fan-*.jpg"));
            }
          }
        }
      }
      else if (scope == FanArtScope.Season)
      {
        SeasonInfo season = infoObject as SeasonInfo;
        if (season != null && season.SeriesTvdbId > 0 && season.SeasonNumber.HasValue)
        {
          string path = Path.Combine(CACHE_PATH, season.SeriesTvdbId.ToString());
          if (Directory.Exists(path))
          {
            if (type == FanArtType.Banners)
            {
              fanartFiles.AddRange(Directory.GetFiles(path, string.Format("img_seasonswide_{0}-{1}*.jpg", season.SeriesTvdbId, season.SeasonNumber.Value)));
            }
            else if (type == FanArtType.Posters)
            {
              fanartFiles.AddRange(Directory.GetFiles(path, string.Format("img_seasons_{0}-{1}*.jpg", season.SeriesTvdbId, season.SeasonNumber.Value)));
            }
          }
        }
      }
      else if (scope == FanArtScope.Episode)
      {
        EpisodeInfo episode = infoObject as EpisodeInfo;
        if (episode != null && episode.TvdbId > 0 && episode.SeriesTvdbId > 0)
        {
          string path = Path.Combine(CACHE_PATH, episode.SeriesTvdbId.ToString());
          if (Directory.Exists(path))
          {
            if (type == FanArtType.Thumbnails)
            {
              string file = Path.Combine(path, string.Format("img_episodes_{0}-{1}.jpg", episode.SeriesTvdbId, episode.TvdbId));
              if (File.Exists(file))
                fanartFiles.Add(file);
            }
          }
        }
      }
      else if (scope == FanArtScope.Actor)
      {
        PersonInfo person = infoObject as PersonInfo;
        if (person != null && person.TvdbId > 0)
        {
          string path = Path.Combine(CACHE_PATH, person.TvdbId.ToString());
          if (Directory.Exists(path))
          {
            if (type == FanArtType.Thumbnails)
            {
              if (Directory.Exists(path))
                fanartFiles.AddRange(Directory.GetFiles(path, string.Format(@"img_actors_{0}.jpg", person.TvdbId), SearchOption.AllDirectories));
            }
          }
        }
      }
      return fanartFiles;
    }

    protected override void DownloadFanArt(string tvDbId)
    {
      try
      {
        if (string.IsNullOrEmpty(tvDbId))
          return;

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Started for ID {0}", tvDbId);

        if (!Init())
          return;

        string[] ids;
        if (tvDbId.Contains("|"))
          ids = tvDbId.Split('|');
        else
          ids = new string[] { tvDbId };

        int tvDbSeriesId = 0;
        if (!int.TryParse(ids[0], out tvDbSeriesId))
          return;

        if (tvDbSeriesId <= 0)
          return;

        TvdbSeries seriesDetail;
        if (!_tv.GetSeriesFanArt(tvDbSeriesId, out seriesDetail))
          return;

        int tvDbEpisodeId = 0;
        TvdbEpisode episodeDetail = null;
        if (ids.Length > 1)
        {
          if (int.TryParse(ids[1], out tvDbEpisodeId))
          {
            episodeDetail = seriesDetail.GetEpisodesAbsoluteOrder().Find(e => e.Id == tvDbEpisodeId);
          }
        }

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving banners for ID {0}", tvDbSeriesId);
        TvdbLanguage language = _tv.PreferredLanguage;
        SaveBanners(seriesDetail.SeriesBanners, language);

        // Save Season Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving season banners for ID {0}", tvDbSeriesId);
        // Build a key from Season number and banner type (season or seasonwide), so each combination is handled separately.
        var seasonLookup = seriesDetail.SeasonBanners.ToLookup(s => string.Format("{0}_{1}", s.Season, s.BannerType), v => v);
        foreach (IGrouping<string, TvdbSeasonBanner> tvdbSeasonBanners in seasonLookup)
          SaveBanners(seasonLookup[tvdbSeasonBanners.Key], language);

        // Save Posters
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving posters for ID {0}", tvDbSeriesId);
        SaveBanners(seriesDetail.PosterBanners, language);

        // Save FanArt
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving fanarts for ID {0}", tvDbSeriesId);
        SaveBanners(seriesDetail.FanartBanners, language);

        if (episodeDetail != null)
        {
          // Save Episode Banners
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving episode banners for ID {0}", tvDbEpisodeId);
          SaveBanners(new TvdbBanner[] { episodeDetail.Banner }, null);
        }

        // Save Actors
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving actors for ID {0}", tvDbSeriesId);
        SaveBanners(seriesDetail.TvdbActors.Select(a => a.ActorImage).ToList(), null);

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Finished ID {0}", tvDbSeriesId);

        // Remember we are finished
        FinishDownloadFanArt(tvDbId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception downloading FanArt for ID {0}", ex, tvDbId);
      }
    }

    private int SaveBanners<TE>(IEnumerable<TE> banners, TvdbLanguage language) where TE : TvdbBanner
    {
      int idx = 0;
      foreach (TE tvdbBanner in banners)
      {
        if (tvdbBanner.Language != language && language != null)
          continue;

        if (idx++ >= MAX_FANART_IMAGES)
          break;

        if (!tvdbBanner.IsLoaded)
        {
          // We need the image only loaded once, later we will access the cache directly
          try
          {
            tvdbBanner.LoadBanner();
            tvdbBanner.UnloadBanner();
          }
          catch (Exception ex)
          {
            ServiceRegistration.Get<ILogger>().Warn("SeriesTvDbMatcher: Exception saving FanArt image", ex);
          }
        }
      }
      if (idx > 0)
        return idx;

      // Try fallback languages if no images found for preferred
      if (language != TvdbLanguage.UniversalLanguage && language != TvdbLanguage.DefaultLanguage)
      {
        if (_useUniversalLanguage)
        {
          idx = SaveBanners(banners, TvdbLanguage.UniversalLanguage);
          if (idx > 0)
            return idx;
        }

        idx = SaveBanners(banners, TvdbLanguage.DefaultLanguage);
      }
      return idx;
    }

    #endregion
  }
}
