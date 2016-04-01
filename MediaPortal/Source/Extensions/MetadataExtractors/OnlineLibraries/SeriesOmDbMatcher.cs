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
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Utilities;
using MediaPortal.Extensions.OnlineLibraries.Libraries.OmDbV1.Data;
using MediaPortal.Extensions.OnlineLibraries.OmDB;

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
    protected ConcurrentDictionary<string, Series> _memoryCache = new ConcurrentDictionary<string, Series>(StringComparer.OrdinalIgnoreCase);

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
    public bool FindAndUpdateSeries(EpisodeInfo episodeInfo)
    {
      Series seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (/* Best way is to get details by an unique IMDB id */
        MatchByTmdbId(episodeInfo, out seriesDetail) ||
        TryMatch(episodeInfo.Series, false, out seriesDetail)
        )
      {
        string imdbId;
        if (seriesDetail != null)
        {
          imdbId = seriesDetail.ImdbID;

          episodeInfo.Series = seriesDetail.Title;
          episodeInfo.ImdbId = seriesDetail.ImdbID;

          // Also try to fill episode title from series details (most file names don't contain episode name).
          if (!TryMatchEpisode(episodeInfo, seriesDetail))
            return false;
        }

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
        if (_omDb.GetSeriesSeason(seriesDetail.ImdbID, episodeInfo.SeasonNumber.Value, out season) == false)
          return false;

        Episode episode;
        episodes = season.Episodes.FindAll(e => e.Title == episodeInfo.Episode);
        // In few cases there can be multiple episodes with same name. In this case we cannot know which one is right
        // and keep the current episode details.
        // Use this way only for single episodes.
        if (episodeInfo.EpisodeNumbers.Count == 1 && episodes.Count == 1)
        {
          if (_omDb.GetSeriesEpisode(seriesDetail.ImdbID, episodeInfo.SeasonNumber.Value, episodes[0].EpisodeNumber, out episode))
          {
            episodeInfo.Episode = episode.Title;
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
            episodeInfo.Episode = episode.Title;
            SetEpisodeDetails(episodeInfo, seriesDetail, episode);
            return true;
          }
          return false;
        }

        List<Episode> fullEpisodes = new List<Episode>();
        foreach(SeasonEpisode ep in episodes)
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

    private static void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, Series seriesDetail, List<Episode> episodes)
    {
      episodeInfo.SeasonNumber = episodeInfo.SeasonNumber;
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.ToList().AddRange(episodes.Select(x => x.EpisodeNumber));
      episodeInfo.FirstAired = episodes.First().Released;

      if(episodes.First().ImdbRating.HasValue)
        episodeInfo.TotalRating = episodes.Sum(e => e.ImdbRating.HasValue ? e.ImdbRating.Value : 0) / episodes.Count; // Average rating
      if (episodes.First().TomatoRating.HasValue)
        episodeInfo.TotalRating = episodes.Sum(e => e.TomatoRating.HasValue ? e.TomatoRating.Value : 0) / episodes.Count; // Average rating
      if (episodes.First().TomatoUserRating.HasValue)
        episodeInfo.TotalRating = episodes.Sum(e => e.TomatoUserRating.HasValue ? e.TomatoUserRating.Value : 0) / episodes.Count; // Average rating

      episodeInfo.Episode = string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.Title).ToArray());
      episodeInfo.Summary = string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Plot)).ToArray());

      //Only use these if absolutely necessary because there is no way to ID them
      if (episodeInfo.Actors.Count == 0)
      {
        episodeInfo.Actors.Clear();
        var actors = episodes.SelectMany(e => e.Actors).Distinct().ToList();
        if (actors.Count > 0)
          CollectionUtils.AddAll(episodeInfo.Actors, actors);
      }
      if (episodeInfo.Directors.Count == 0)
      {
        episodeInfo.Directors.Clear();
        var directors = episodes.SelectMany(e => e.Directors).Distinct().ToList();
        if (directors.Count > 0)
          CollectionUtils.AddAll(episodeInfo.Directors, directors);
      }
      if (episodeInfo.Writers.Count == 0)
      {
        episodeInfo.Writers.Clear();
        var writers = episodes.SelectMany(e => e.Writers).Distinct().ToList();
        if (writers.Count > 0)
          CollectionUtils.AddAll(episodeInfo.Writers, writers);
      }
      episodeInfo.Genres.Clear();
      var genres = episodes.SelectMany(e => e.Genres).Distinct().ToList();
      if (genres.Count > 0)
        CollectionUtils.AddAll(episodeInfo.Genres, genres);
    }

    private static void SetEpisodeDetails(EpisodeInfo episodeInfo, Series seriesDetail, Episode episode)
    {
      episodeInfo.SeasonNumber = episode.SeasonNumber;
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      episodeInfo.FirstAired = episode.Released;
      episodeInfo.Summary = episode.Plot;

      if (seriesDetail.ImdbRating.HasValue)
      {
        episodeInfo.TotalRating = seriesDetail.ImdbRating.Value;
        if (seriesDetail.ImdbVotes.HasValue)
          episodeInfo.RatingCount = seriesDetail.ImdbVotes.Value;
      }
      else if (seriesDetail.TomatoRating.HasValue)
      {
        episodeInfo.TotalRating = seriesDetail.TomatoRating.Value;
        if (seriesDetail.TomatoTotalReviews.HasValue)
          episodeInfo.RatingCount = seriesDetail.TomatoTotalReviews.Value;
      }
      else if (seriesDetail.TomatoUserRating.HasValue)
      {
        episodeInfo.TotalRating = seriesDetail.TomatoUserRating.Value;
        if (seriesDetail.TomatoUserTotalReviews.HasValue)
          episodeInfo.RatingCount = seriesDetail.TomatoUserTotalReviews.Value;
      }

      //Only use these if absolutely necessary because there is no way to ID them
      if (episodeInfo.Actors.Count == 0) CollectionUtils.AddAll(episodeInfo.Actors, seriesDetail.Actors);
      if (episodeInfo.Writers.Count == 0) CollectionUtils.AddAll(episodeInfo.Writers, seriesDetail.Writers);
      if (episodeInfo.Directors.Count == 0) CollectionUtils.AddAll(episodeInfo.Directors, seriesDetail.Directors);

      if (seriesDetail.Genres.Count > 0)
      {
        episodeInfo.Genres.Clear();
        CollectionUtils.AddAll(episodeInfo.Genres, seriesDetail.Genres);
      }
    }

    private bool MatchByTmdbId(EpisodeInfo episodeInfo, out Series seriesDetails)
    {
      if (episodeInfo.MovieDbId > 0 && _omDb.GetSeries(episodeInfo.ImdbId, out seriesDetails))
      {
        SaveMatchToPersistentCache(seriesDetails, seriesDetails.Title);
        return true;
      }
      seriesDetails = null;
      return false;
    }

    protected bool TryMatch(string seriesName, bool cacheOnly, out Series seriesDetail)
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

        List<SearchItem> series;
        if (_omDb.SearchSeriesUnique(seriesName, 0, out series))
        {
          SearchItem seriesResult = series[0];
          ServiceRegistration.Get<ILogger>().Debug("SeriesOmDbMatcher: Found unique online match for \"{0}\": \"{1}\"", seriesName, seriesResult.Title);
          if (_omDb.GetSeries(series[0].ImdbID, out seriesDetail))
          {
            SaveMatchToPersistentCache(seriesDetail, seriesName);
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("SeriesOmDbMatcher: No unique match found for \"{0}\"", seriesName);
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
