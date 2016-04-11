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
using MediaPortal.Utilities;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

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

    /// <summary>
    /// Tries to lookup the series from TheTvDB and return the found ID.
    /// </summary>
    /// <param name="seriesName">Series name to check</param>
    /// <param name="tvDbId">Return the TvDB ID of series</param>
    /// <returns><c>true</c> if successful</returns>
    public bool TryGetTvDbId(string seriesName, out int tvDbId)
    {
      return TryGetId(seriesName, out tvDbId);
    }

    /// <summary>
    /// Tries to lookup the series from TheTvDB and updates the given <paramref name="episodeInfo"/> with the online information (Series and Episode names).
    /// </summary>
    /// <param name="episodeInfo">Series to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateEpisode(EpisodeInfo episodeInfo)
    {
      TvdbSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (TryMatch(episodeInfo, false, out seriesDetail))
      {
        int tvDbId = 0;
        if (seriesDetail != null)
        {
          tvDbId = seriesDetail.Id;

          MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvdbId, seriesDetail.Id);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, seriesDetail.ImdbId);

          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Series, seriesDetail.SeriesName, false);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, seriesDetail.Genre, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Networks, ConvertToCompanys(seriesDetail.NetworkID, seriesDetail.Network, CompanyType.TVNetwork), false);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(seriesDetail.TvdbActors, PersonOccupation.Actor), true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.SeriesName, seriesDetail.TvdbActors), true);
          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Certification, seriesDetail.ContentRating, true);

          // Also try to fill episode title from series details (most file names don't contain episode name).
          if (!TryMatchEpisode(episodeInfo, seriesDetail))
            return false;
        }

        if (tvDbId > 0)
          ScheduleDownload(tvDbId.ToString());
        return true;
      }
      return false;
    }

    public bool UpdateSeries(SeriesInfo seriesInfo)
    {
      TvdbSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seriesInfo.TvdbId > 0 && _tv.GetSeries(seriesInfo.TvdbId, false, true, out seriesDetail))
      {
        MetadataUpdater.SetOrUpdateId(ref seriesInfo.ImdbId, seriesDetail.ImdbId);

        MetadataUpdater.SetOrUpdateString(ref seriesInfo.Series, seriesDetail.SeriesName, false);
        MetadataUpdater.SetOrUpdateString(ref seriesInfo.Description, seriesDetail.Overview, false);
        MetadataUpdater.SetOrUpdateValue(ref seriesInfo.FirstAired, seriesDetail.FirstAired);
        if(seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.IsEnded, true);
        }
        MetadataUpdater.SetOrUpdateString(ref seriesInfo.Certification, seriesDetail.ContentRating, true);
        MetadataUpdater.SetOrUpdateList(seriesInfo.Genres, seriesDetail.Genre, true);
        MetadataUpdater.SetOrUpdateValue(ref seriesInfo.TotalRating, seriesDetail.Rating); // Average rating
        MetadataUpdater.SetOrUpdateValue(ref seriesInfo.RatingCount, seriesDetail.RatingCount > 0 ? seriesDetail.RatingCount : 0);

        MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanys(seriesDetail.NetworkID, seriesDetail.Network, CompanyType.TVNetwork), false);
        MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(seriesDetail.TvdbActors, PersonOccupation.Actor), true);
        MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.SeriesName, seriesDetail.TvdbActors), true);

        return true;
      }
      return false;
    }

    public bool UpdateSeason(SeasonInfo seasonInfo)
    {
      TvdbSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seasonInfo.TvdbId > 0 && _tv.GetSeries(seasonInfo.TvdbId, false, false, out seriesDetail))
      {
        MetadataUpdater.SetOrUpdateId(ref seasonInfo.ImdbId, seriesDetail.ImdbId);

        MetadataUpdater.SetOrUpdateString(ref seasonInfo.Series, seriesDetail.SeriesName, false);
        MetadataUpdater.SetOrUpdateString(ref seasonInfo.Description, seriesDetail.Overview, false);

        List<TvdbEpisode> episodes = seriesDetail.GetEpisodes(seasonInfo.SeasonNumber.Value);
        MetadataUpdater.SetOrUpdateValue(ref seasonInfo.FirstAired, episodes.OrderBy(e => e.FirstAired).First().FirstAired);

        return true;
      }
      return false;
    }

    public bool UpdateEpisodePersons(EpisodeInfo episodeInfo, List<PersonInfo> persons, PersonOccupation occupation)
    {
      TvdbSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (occupation != PersonOccupation.Actor)
        return false;

      if (episodeInfo.TvdbId > 0 && _tv.GetSeries(episodeInfo.TvdbId, false, true, out seriesDetail))
      {
        if(occupation == PersonOccupation.Actor)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(seriesDetail.TvdbActors, PersonOccupation.Actor), false);

        return true;
      }
      return false;
    }

    public bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo, List<CharacterInfo> characters)
    {
      TvdbSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (episodeInfo.TvdbId > 0 && _tv.GetSeries(episodeInfo.TvdbId, false, true, out seriesDetail))
      {
        MetadataUpdater.SetOrUpdateList(characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.SeriesName, seriesDetail.TvdbActors), false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesPersons(SeriesInfo seriesInfo, List<PersonInfo> persons, PersonOccupation occupation)
    {
      TvdbSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (occupation != PersonOccupation.Actor)
        return false;

      if (seriesInfo.TvdbId > 0 && _tv.GetSeries(seriesInfo.TvdbId, false, true, out seriesDetail))
      {
        if (occupation == PersonOccupation.Actor)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(seriesDetail.TvdbActors, PersonOccupation.Actor), false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesCharacters(SeriesInfo seriesInfo, List<CharacterInfo> characters)
    {
      TvdbSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seriesInfo.TvdbId > 0 && _tv.GetSeries(seriesInfo.TvdbId, false, true, out seriesDetail))
      {
        MetadataUpdater.SetOrUpdateList(characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.SeriesName, seriesDetail.TvdbActors), false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesCompanys(SeriesInfo seriesInfo, List<CompanyInfo> companys, CompanyType type)
    {
      TvdbSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (type != CompanyType.TVNetwork)
        return false;

      if (seriesInfo.TvdbId > 0 && _tv.GetSeries(seriesInfo.TvdbId, false, true, out seriesDetail))
      {
        if (type == CompanyType.TVNetwork)
          MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanys(seriesDetail.NetworkID, seriesDetail.Network, CompanyType.TVNetwork), false);

        return true;
      }
      return false;
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

    private void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, TvdbSeries seriesDetail, List<TvdbEpisode> episodes)
    {
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episodes.First().SeasonNumber);
      MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodes.Select(x => x.EpisodeNumber).ToList(), true);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episodes.First().FirstAired);
      MetadataUpdater.SetOrUpdateList(episodeInfo.DvdEpisodeNumbers, episodes.Select(x => x.DvdEpisodeNumber).ToList(), true);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.TotalRating, episodes.Sum(e => e.Rating) / episodes.Count); // Average rating
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.RatingCount, episodes.Sum(e => e.RatingCount > 0 ? e.RatingCount : 0));

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.EpisodeName).ToArray()), false);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Overview)).ToArray()), false);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episodes.SelectMany(e => e.GuestStars).ToList(), PersonOccupation.Actor), true);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episodes.SelectMany(e => e.Directors).ToList(), PersonOccupation.Director), true);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episodes.SelectMany(e => e.Writer).ToList(), PersonOccupation.Writer), true);
    }

    private void SetEpisodeDetails(EpisodeInfo episodeInfo, TvdbSeries seriesDetail, TvdbEpisode episode)
    {
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episode.SeasonNumber);
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      episodeInfo.DvdEpisodeNumbers.Clear();
      episodeInfo.DvdEpisodeNumbers.Add(episode.DvdEpisodeNumber);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episode.FirstAired);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.TotalRating, episode.Rating); // Average rating
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.RatingCount, episode.RatingCount > 0 ? episode.RatingCount : 0);

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, episode.EpisodeName, false);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episode.Overview, false);

      MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(episode.GuestStars, PersonOccupation.Actor), true);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, ConvertToPersons(episode.Directors, PersonOccupation.Director), true);
      MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, ConvertToPersons(episode.Writer, PersonOccupation.Writer), true);
    }

    private List<PersonInfo> ConvertToPersons(List<TvdbActor> actors, PersonOccupation occupation)
    {
      if (actors == null || actors.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (TvdbActor person in actors)
        retValue.Add(new PersonInfo() { TvdbId = person.Id, Name = person.Name, Occupation = occupation });
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<string> actors, PersonOccupation occupation)
    {
      if (actors == null || actors.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (string person in actors)
        retValue.Add(new PersonInfo() { Name = person, Occupation = occupation });
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanys(int companyID, string company, CompanyType type)
    {
      if (!string.IsNullOrEmpty(company))
        return new List<CompanyInfo>();

      return new List<CompanyInfo>(new CompanyInfo[]
      {
        new CompanyInfo()
        {
          TvdbId = companyID,
          Name = company,
          Type = type
        }
      });
    }

    private List<CharacterInfo> ConvertToCharacters(int seriesId, string seriesTitle, List<TvdbActor> actors)
    {
      if (actors == null || actors.Count == 0)
        return new List<CharacterInfo>();

      List<CharacterInfo> retValue = new List<CharacterInfo>();
      foreach (TvdbActor person in actors)
        retValue.Add(new CharacterInfo()
        {
          MediaIsMovie = false,
          MediaTvdbId = seriesId,
          MediaTitle = seriesTitle,
          ActorTvdbId = person.Id,
          ActorName = person.Name,
          Name = person.Role
        });
      return retValue;
    }

    protected bool TryGetId(string seriesName, out int tvDbId)
    {
      tvDbId = 0;
      // Prefer memory cache
      TvdbSeries seriesDetail;
      CheckCacheAndRefresh();
      if (_memoryCache.TryGetValue(seriesName, out seriesDetail))
      {
        tvDbId = seriesDetail.Id;
        return true;
      }

      // Load cache or create new list
      List<SeriesMatch> matches;
      lock (_syncObj)
        matches = Settings.Load<List<SeriesMatch>>(MatchesSettingsFile) ?? new List<SeriesMatch>();

      // Use cached values before doing online query
      SeriesMatch match = matches.Find(m => m.ItemName == seriesName || m.TvDBName == seriesName);
      if (match != null && !string.IsNullOrEmpty(match.Id))
      {
        tvDbId = Convert.ToInt32(match.Id);
        return true;
      }
      return false;
    }

    protected bool TryMatch(EpisodeInfo episodeInfo, bool cacheOnly, out TvdbSeries seriesDetail)
    {
      // If series has an TVDBID, prefer it over imdb or name lookup.
      if (episodeInfo.TvdbId != 0 && TryMatch(episodeInfo.Series, false, cacheOnly, out seriesDetail, episodeInfo.TvdbId))
        return true;

      // If series has an IMDBID, prefer it over name lookup.
      string imdbId = episodeInfo.ImdbId;
      if (!string.IsNullOrWhiteSpace(imdbId) && TryMatch(imdbId, true, cacheOnly, out seriesDetail))
        return true;

      // Perform name lookup.
      return TryMatch(episodeInfo.Series, false, cacheOnly, out seriesDetail);
    }

    protected bool TryMatch(string seriesNameOrImdbId, bool isImdbId, bool cacheOnly, out TvdbSeries seriesDetail, int tvdbid = 0)
    {
      seriesDetail = null;
      try
      {
        // Prefer memory cache
        CheckCacheAndRefresh();
        if (_memoryCache.TryGetValue(seriesNameOrImdbId, out seriesDetail))
        {
          if (tvdbid == 0 || seriesDetail.Id == tvdbid)
            return true;
        }

        // Load cache or create new list
        List<SeriesMatch> matches;
        lock (_syncObj)
          matches = Settings.Load<List<SeriesMatch>>(MatchesSettingsFile) ?? new List<SeriesMatch>();

        // Init empty
        seriesDetail = null;

        // Use cached values before doing online query
        SeriesMatch match = matches.Find(m =>
          (
          string.Equals(m.ItemName, seriesNameOrImdbId, StringComparison.OrdinalIgnoreCase) ||
          string.Equals(m.TvDBName, seriesNameOrImdbId, StringComparison.OrdinalIgnoreCase)
          ) && (tvdbid == 0 || m.Id == tvdbid.ToString()));

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesNameOrImdbId, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        int tvDb = 0;
        if (!string.IsNullOrEmpty(match.Id))
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
        bool foundResult = false;
        if (tvdbid != 0)
        {
          foundResult = _tv.GetSeries(tvdbid, true, true, out seriesDetail);
        }
        else
          if (isImdbId)
          {
            // If we got an IMDBID, use it to lookup by key directly
            _tv.GetSeries(seriesNameOrImdbId, out matchedSeries);
          }
          else
          {
            // Otherwise we try to find unique series by name
            List<TvdbSearchResult> series;
            if (_tv.SearchSeriesUnique(seriesNameOrImdbId, out series))
              matchedSeries = series[0];
          }

        if (matchedSeries != null)
        {
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Found unique online match for \"{0}\": \"{1}\" [Lang: {2}]", seriesNameOrImdbId, matchedSeries.SeriesName, matchedSeries.Language);
          foundResult = _tv.GetSeries(matchedSeries.Id, true, true, out seriesDetail);
        }
        if (foundResult)
        {
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Loaded details for \"{0}\"", seriesDetail.SeriesName);
          // Add this match to cache
          SeriesMatch onlineMatch = new SeriesMatch
              {
                ItemName = seriesNameOrImdbId,
                Id = seriesDetail.Id.ToString(),
                TvDBName = seriesDetail.SeriesName
              };

          // Save cache
          _storage.TryAddMatch(onlineMatch);
          return true;
        }

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: No unique match found for \"{0}\"", seriesNameOrImdbId);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new SeriesMatch { ItemName = seriesNameOrImdbId });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher: Exception while processing series {0}", ex, seriesNameOrImdbId);
        return false;
      }
      finally
      {
        if (seriesDetail != null)
          _memoryCache.TryAdd(seriesNameOrImdbId, seriesDetail);
      }
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

    protected override void DownloadFanArt(string tvDbId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Started for ID {0}", tvDbId);

        if (!Init())
          return;

        int tvDb = 0;
        if (!int.TryParse(tvDbId, out tvDb))
          return;

        TvdbSeries seriesDetail;
        if (!_tv.GetSeriesFanArt(tvDb, out seriesDetail))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving banners for ID {0}", tvDbId);
        TvdbLanguage language = _tv.PreferredLanguage;
        SaveBanners(seriesDetail.SeriesBanners, language);

        // Save Season Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving season banners for ID {0}", tvDbId);
        // Build a key from Season number and banner type (season or seasonwide), so each combination is handled separately.
        var seasonLookup = seriesDetail.SeasonBanners.ToLookup(s => string.Format("{0}_{1}", s.Season, s.BannerType), v => v);
        foreach (IGrouping<string, TvdbSeasonBanner> tvdbSeasonBanners in seasonLookup)
          SaveBanners(seasonLookup[tvdbSeasonBanners.Key], language);

        // Save Posters
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving posters for ID {0}", tvDbId);
        SaveBanners(seriesDetail.PosterBanners, language);

        // Save FanArt
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving fanarts for ID {0}", tvDbId);
        SaveBanners(seriesDetail.FanartBanners, language);

        // Save Actors
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Begin saving actors for ID {0}", tvDbId);
        SaveBanners(seriesDetail.TvdbActors.Select(a => a.ActorImage).ToList(), language);

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvDbMatcher Download: Finished ID {0}", tvDbId);

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
        if (tvdbBanner.Language != language)
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
  }
}
