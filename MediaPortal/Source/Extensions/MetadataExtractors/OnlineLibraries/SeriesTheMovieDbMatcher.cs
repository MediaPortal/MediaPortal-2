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
    public bool FindAndUpdateSeries(EpisodeInfo episodeInfo)
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

          episodeInfo.Series = seriesDetail.Name;
          episodeInfo.MovieDbId = seriesDetail.Id;
          if (seriesDetail.ExternalId.TvDbId.HasValue) episodeInfo.TvdbId = seriesDetail.ExternalId.TvDbId.Value;
          if (!string.IsNullOrEmpty(seriesDetail.ExternalId.ImDbId)) episodeInfo.ImdbId = seriesDetail.ExternalId.ImDbId;

          episodeInfo.Genres.Clear();
          if (seriesDetail.Genres.Count > 0)
            CollectionUtils.AddAll(episodeInfo.Genres, seriesDetail.Genres.Select(g => g.Name).Distinct().ToList());

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
          episodeInfo.Episode = episode.Name;
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
          episodeInfo.Episode = episode.Name;
          SetEpisodeDetails(episodeInfo, seriesDetail, episode);
          return true;
        }

        // Multiple episodes
        SetMultiEpisodeDetailsl(episodeInfo, seriesDetail, episodes);
        return true;
      }
      return false;
    }

    private static void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, Series seriesDetail, List<SeasonEpisode> episodes)
    {
      episodeInfo.SeasonNumber = episodes.First().SeasonNumber;
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.ToList().AddRange(episodes.Select(x => x.EpisodeNumber));
      episodeInfo.FirstAired = episodes.First().AirDate;

      episodeInfo.TotalRating = episodes.Sum(e => e.Rating.HasValue ? e.Rating.Value : 0.0) / episodes.Count; // Average rating
      episodeInfo.Episode = string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.Name).ToArray());
      episodeInfo.Summary = string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Overview)).ToArray());

      episodeInfo.Actors.Clear();
      var guestStars = episodes.SelectMany(e => e.GuestStars.Select(p => p.Name)).Distinct().ToList();
      if (guestStars.Count > 0)
        CollectionUtils.AddAll(episodeInfo.Actors, guestStars);
      episodeInfo.Directors.Clear();
      var directors = episodes.SelectMany(e => e.Crew.Where(p => p.Job == "Director").Select(p => p.Name)).Distinct().ToList();
      if (directors.Count > 0)
        CollectionUtils.AddAll(episodeInfo.Directors, directors);
      episodeInfo.Writers.Clear();
      var writers = episodes.SelectMany(e => e.Crew.Where(p => p.Job == "Writer").Select(p => p.Name)).Distinct().ToList();
      if (writers.Count > 0)
        CollectionUtils.AddAll(episodeInfo.Writers, writers);
    }

    private static void SetEpisodeDetails(EpisodeInfo episodeInfo, Series seriesDetail, SeasonEpisode episode)
    {
      episodeInfo.SeasonNumber = episode.SeasonNumber;
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      episodeInfo.FirstAired = episode.AirDate;

      episodeInfo.TotalRating = episode.Rating.HasValue ? episode.Rating.Value : 0;
      episodeInfo.Summary = episode.Overview;

      episodeInfo.Actors.Clear();
      if (episode.GuestStars.Count > 0)
        CollectionUtils.AddAll(episodeInfo.Actors, episode.GuestStars.Select(p => p.Name).Distinct().ToList());
      episodeInfo.Directors.Clear();
      if (episode.Crew.Count > 0)
        CollectionUtils.AddAll(episodeInfo.Directors, episode.Crew.Where(p => p.Job == "Director").Select(p => p.Name).Distinct().ToList());
      episodeInfo.Writers.Clear();
      if (episode.Crew.Count > 0)
        CollectionUtils.AddAll(episodeInfo.Writers, episode.Crew.Where(p => p.Job == "Writer").Select(p => p.Name).Distinct().ToList());
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

        ImageCollection imageCollection;
        if (!_movieDb.GetSeriesFanArt(tmDb, out imageCollection))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTheMovieDbMatcher Download: Begin saving banners for ID {0}", movieDbId);
        SaveBanners(imageCollection.Backdrops, "Backdrops");
        SaveBanners(imageCollection.Covers, "Covers");
        SaveBanners(imageCollection.Posters, "Posters");
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
