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
using System.IO;
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.PathManager;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data;
using MediaPortal.Extensions.OnlineLibraries.OmDB;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class SeriesOmDbMatcher : BaseMatcher<SeriesMatch, string>
  {
    #region Static instance

    public static SeriesOmDbMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesOmDbMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\OmDB\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "SeriesMatches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, OmDBSeries> _memoryCache = new ConcurrentDictionary<string, OmDBSeries>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized OmDbWrapper.
    /// </summary>
    private OmDbWrapper _omDb;

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
        OmDBSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (TryMatch(episodeInfo, out seriesDetail))
        {
          if (seriesDetail != null)
          {
            MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesImdbId, seriesDetail.ImdbID);

            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Series, seriesDetail.Title, true);
            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Certification, seriesDetail.Rated, true);

            // Also try to fill episode title from series details (most file names don't contain episode name).
            if (!TryMatchEpisode(episodeInfo, seriesDetail))
              return false;
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesOmDbMatcher: Exception while processing episode {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeries(SeriesInfo seriesInfo)
    {
      try
      {
        OmDBSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (!string.IsNullOrEmpty(seriesInfo.ImdbId) && _omDb.GetSeries(seriesInfo.ImdbId, out seriesDetail))
        {
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Series, seriesDetail.Title, true);
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Description, seriesDetail.Plot, true);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.FirstAired, seriesDetail.Released);
          if (seriesDetail.EndYear.HasValue)
          {
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.IsEnded, true);
          }
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Certification, seriesDetail.Rated, true);

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
            MetadataUpdater.SetOrUpdateList(seriesInfo.Awards, awards, true, true);
          }

          if (seriesDetail.ImdbRating.HasValue)
          {
            MetadataUpdater.SetOrUpdateRatings(ref seriesInfo.TotalRating, ref seriesInfo.RatingCount, seriesDetail.ImdbRating, seriesDetail.ImdbVotes);
          }
          else if (seriesDetail.TomatoRating.HasValue)
          {
            MetadataUpdater.SetOrUpdateRatings(ref seriesInfo.TotalRating, ref seriesInfo.RatingCount, seriesDetail.TomatoRating, seriesDetail.TomatoTotalReviews);
          }
          else if (seriesDetail.TomatoUserRating.HasValue)
          {
            MetadataUpdater.SetOrUpdateRatings(ref seriesInfo.TotalRating, ref seriesInfo.RatingCount, seriesDetail.TomatoUserRating, seriesDetail.TomatoUserTotalReviews);
          }
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.Score, seriesDetail.Metascore.HasValue ? seriesDetail.Metascore.Value : 0);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Genres, seriesDetail.Genres, true, true);

          //Only use these if absolutely necessary because there is no way to ID them
          if (seriesDetail.Actors == null || seriesDetail.Actors.Count == 0)
            MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(seriesDetail.Actors, PersonAspect.OCCUPATION_ACTOR), true, true);

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesOmDbMatcher: Exception while processing series {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeason(SeasonInfo seasonInfo)
    {
      try
      {
        OmDBSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (!string.IsNullOrEmpty(seasonInfo.SeriesImdbId) && _omDb.GetSeries(seasonInfo.SeriesImdbId, out seriesDetail))
        {
          MetadataUpdater.SetOrUpdateString(ref seasonInfo.Series, seriesDetail.Title, false);
          MetadataUpdater.SetOrUpdateString(ref seasonInfo.Description, seriesDetail.Plot, true);

          OmDBSeason seasonDetail;
          if (_omDb.GetSeriesSeason(seasonInfo.SeriesImdbId, seasonInfo.SeasonNumber.Value, out seasonDetail))
          {
            MetadataUpdater.SetOrUpdateValue(ref seasonInfo.FirstAired, seasonDetail.Episodes.OrderBy(e => e.Released).First().Released);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesOmDbMatcher: Exception while processing season {0}", ex, seasonInfo.ToString());
        return false;
      }
    }

    protected bool TryMatchEpisode(EpisodeInfo episodeInfo, OmDBSeries seriesDetail)
    {
      OmDBSeason season = null;
      List<OmDBSeasonEpisode> episodes = null;
      if (episodeInfo.SeasonNumber.HasValue)
      {
        if (_omDb.GetSeriesSeason(seriesDetail.ImdbID, episodeInfo.SeasonNumber.Value, out season) == false)
          return false;

        OmDbEpisode episode;
        episodes = season.Episodes.FindAll(e => e.Title == episodeInfo.Episode);
        // In few cases there can be multiple episodes with same name. In this case we cannot know which one is right
        // and keep the current episode details.
        // Use this way only for single episodes.
        if (episodeInfo.EpisodeNumbers.Count == 1 && episodes.Count == 1)
        {
          if (_omDb.GetSeriesEpisode(seriesDetail.ImdbID, episodeInfo.SeasonNumber.Value, episodes[0].EpisodeNumber, out episode))
          {
            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.Title, true);
            SetEpisodeDetails(episodeInfo, seriesDetail, episode);
            return true;
          }
          return false;
        }

        episodes = season.Episodes.Where(e => episodeInfo.EpisodeNumbers.Contains(e.EpisodeNumber)).ToList();
        if (episodes.Count == 0)
          return false;

        // Single episode entry
        if (episodes.Count == 1)
        {
          if (_omDb.GetSeriesEpisode(seriesDetail.ImdbID, episodeInfo.SeasonNumber.Value, episodes[0].EpisodeNumber, out episode))
          {
            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.Title, true);
            SetEpisodeDetails(episodeInfo, seriesDetail, episode);
            return true;
          }
          return false;
        }

        List<OmDbEpisode> fullEpisodes = new List<OmDbEpisode>();
        foreach(OmDBSeasonEpisode ep in episodes)
        {
          if (!_omDb.GetSeriesEpisode(seriesDetail.ImdbID, episodeInfo.SeasonNumber.Value, ep.EpisodeNumber, out episode))
          {
            return false;
          }
          fullEpisodes.Add(episode);
        }
        // Multiple episodes
        SetMultiEpisodeDetailsl(episodeInfo, seriesDetail, fullEpisodes);
        return true;
      }
      return false;
    }

    private void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, OmDBSeries seriesDetail, List<OmDbEpisode> episodes)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, episodes.First().ImdbID);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episodes.First().SeasonNumber);
      MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodes.Select(x => x.EpisodeNumber).ToList(), true, true);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episodes.First().Released);

      if (episodes.First().ImdbRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount,
          episodes.Sum(e => e.ImdbRating.HasValue ? e.ImdbRating.Value : 0) / episodes.Count, 
          episodes.Sum(e => e.ImdbVotes.HasValue ? e.ImdbVotes.Value : 0)); // Average rating
      }
      if (episodes.First().TomatoRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount,
          episodes.Sum(e => e.TomatoRating.HasValue ? e.TomatoRating.Value : 0) / episodes.Count, 
          episodes.Sum(e => e.TomatoTotalReviews.HasValue ? e.TomatoTotalReviews.Value : 0)); // Average rating
      }
      if (episodes.First().TomatoUserRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount,
          episodes.Sum(e => e.TomatoUserRating.HasValue ? e.TomatoUserRating.Value : 0) / episodes.Count,
          episodes.Sum(e => e.TomatoUserTotalReviews.HasValue ? e.TomatoUserTotalReviews.Value : 0)); // Average rating
      }

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.Title).ToArray()), true);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Plot)).ToArray()), true);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, episodes.SelectMany(e => e.Genres).Distinct().ToList(), true, true);

      //Only use these if absolutely necessary because there is no way to ID them
      if (episodeInfo.Actors == null || episodeInfo.Actors.Count == 0)
        MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episodes.SelectMany(e => e.Actors).Distinct().ToList(), PersonAspect.OCCUPATION_ACTOR), false, true);
      if (episodeInfo.Directors == null || episodeInfo.Directors.Count == 0)
        MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episodes.SelectMany(e => e.Directors).Distinct().ToList(), PersonAspect.OCCUPATION_DIRECTOR), false, true);
      if (episodeInfo.Writers == null || episodeInfo.Writers.Count == 0)
        MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episodes.SelectMany(e => e.Writers).Distinct().ToList(), PersonAspect.OCCUPATION_WRITER), false, true);
    }

    private void SetEpisodeDetails(EpisodeInfo episodeInfo, OmDBSeries seriesDetail, OmDbEpisode episode)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, episode.ImdbID);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episode.SeasonNumber);
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episode.Released);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episode.Plot, true);

      if (seriesDetail.ImdbRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount, seriesDetail.ImdbVotes, seriesDetail.ImdbVotes);
      }
      if (seriesDetail.TomatoRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount, seriesDetail.TomatoRating, seriesDetail.TomatoTotalReviews);
      }
      if (seriesDetail.TomatoUserRating.HasValue)
      {
        MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount, seriesDetail.TomatoUserRating, seriesDetail.TomatoUserTotalReviews);
      }

      MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, seriesDetail.Genres, true, true);

      //Only use these if absolutely necessary because there is no way to ID them
      if (episodeInfo.Actors == null || episodeInfo.Actors.Count == 0)
        MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(seriesDetail.Actors, PersonAspect.OCCUPATION_ARTIST), false, true);
      if (episodeInfo.Directors == null || episodeInfo.Directors.Count == 0)
        MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(seriesDetail.Writers, PersonAspect.OCCUPATION_DIRECTOR), false, true);
      if (episodeInfo.Writers == null || episodeInfo.Writers.Count == 0)
        MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(seriesDetail.Directors, PersonAspect.OCCUPATION_WRITER), false, true);
    }

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

    private bool TryMatch(EpisodeInfo episodeInfo, out OmDBSeries seriesDetails)
    {
      if (!string.IsNullOrEmpty(episodeInfo.SeriesImdbId) && _omDb.GetSeries(episodeInfo.SeriesImdbId, out seriesDetails))
      {
        SaveMatchToPersistentCache(seriesDetails, seriesDetails.Title);
        return true;
      }
      seriesDetails = null;
      return TryMatch(episodeInfo.Series, false, out seriesDetails);
    }

    protected bool TryMatch(string seriesName, bool cacheOnly, out OmDBSeries seriesDetail)
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
        ServiceRegistration.Get<ILogger>().Debug("SeriesOmDbMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesName, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        // If this is a known movie, only return the movie details.
        if (match != null)
          return !string.IsNullOrEmpty(match.Id) && _omDb.GetSeries(match.Id, out seriesDetail);

        if (cacheOnly)
          return false;

        List<OmDbSearchItem> series;
        if (_omDb.SearchSeriesUnique(seriesName, 0, out series))
        {
          OmDbSearchItem seriesResult = series[0];
          ServiceRegistration.Get<ILogger>().Debug("SeriesOmDbMatcher: Found unique online match for \"{0}\": \"{1}\"", seriesName, seriesResult.Title);
          if (_omDb.GetSeries(series[0].ImdbID, out seriesDetail))
          {
            SaveMatchToPersistentCache(seriesDetail, seriesName);
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("SeriesOmDbMatcher: No unique match found for \"{0}\"", seriesName);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new SeriesMatch { ItemName = seriesName });
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

    private void SaveMatchToPersistentCache(OmDBSeries seriesDetails, string seriesName)
    {
      var onlineMatch = new SeriesMatch
      {
        Id = seriesDetails.ImdbID,
        ItemName = seriesName,
        TvDBName = seriesDetails.Title
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

      if (_omDb != null)
        return true;

      _omDb = new OmDbWrapper();
      return _omDb.Init(CACHE_PATH);
    }

    protected override void DownloadFanArt(string id)
    {
    }
  }
}
