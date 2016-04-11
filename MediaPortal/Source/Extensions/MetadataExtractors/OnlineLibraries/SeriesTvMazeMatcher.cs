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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TvMaze;
using MediaPortal.Utilities;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;

namespace MediaPortal.Extensions.OnlineLibraries
{
  public class SeriesTvMazeMatcher : BaseMatcher<SeriesMatch, string>
  {
    #region Static instance

    public static SeriesTvMazeMatcher Instance
    {
      get { return ServiceRegistration.Get<SeriesTvMazeMatcher>(); }
    }

    #endregion

    #region Constants

    public static string CACHE_PATH = ServiceRegistration.Get<IPathManager>().GetPath(@"<DATA>\TvMaze\");
    protected static string _matchesSettingsFile = Path.Combine(CACHE_PATH, "Matches.xml");
    protected static TimeSpan MAX_MEMCACHE_DURATION = TimeSpan.FromHours(12);

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    protected DateTime _memoryCacheInvalidated = DateTime.MinValue;
    protected ConcurrentDictionary<string, TvMazeSeries> _memoryCache = new ConcurrentDictionary<string, TvMazeSeries>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contains the initialized TvMazeWrapper.
    /// </summary>
    private TvMazeWrapper _tvMaze;

    #endregion

    /// <summary>
    /// Tries to lookup the series from TvMaze and updates the given <paramref name="episodeInfo"/> with the online information.
    /// </summary>
    /// <param name="episodeInfo">Episode to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateEpisode(EpisodeInfo episodeInfo)
    {
      TvMazeSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (TryMatch(episodeInfo, out seriesDetail))
      {
        int tvMazeId = 0;
        if (seriesDetail != null)
        {
          tvMazeId = seriesDetail.Id;

          MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvMazeId, seriesDetail.Id);
          if (seriesDetail.Externals.TvDbId.HasValue)
            MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvdbId, seriesDetail.Externals.TvDbId.Value);
          if (!string.IsNullOrEmpty(seriesDetail.Externals.ImDbId))
            MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, seriesDetail.Externals.ImDbId);

          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Series, seriesDetail.Name, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, seriesDetail.Genres, true);

          MetadataUpdater.SetOrUpdateList(episodeInfo.Networks, ConvertToCompanys(seriesDetail.Network ?? seriesDetail.WebNetwork, CompanyType.TVNetwork), true);

          MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(seriesDetail.Embedded.Cast, PersonOccupation.Actor), true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(episodeInfo.MovieDbId, episodeInfo.Series, seriesDetail.Embedded.Cast), true);

          // Also try to fill episode title from series details (most file names don't contain episode name).
          if (!TryMatchEpisode(episodeInfo, seriesDetail))
            return false;
        }

        if (tvMazeId > 0)
          ScheduleDownload(tvMazeId.ToString());
        return true;
      }
      return false;
    }

    public bool UpdateSeries(SeriesInfo seriesInfo)
    {
      TvMazeSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seriesInfo.TvMazeId > 0 && _tvMaze.GetSeries(seriesInfo.TvMazeId, out seriesDetail))
      {
        if (seriesDetail.Externals.TvDbId.HasValue)
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.TvdbId, seriesDetail.Externals.TvDbId.Value);
        if (!string.IsNullOrEmpty(seriesDetail.Externals.ImDbId))
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.ImdbId, seriesDetail.Externals.ImDbId);

        MetadataUpdater.SetOrUpdateString(ref seriesInfo.Series, seriesDetail.Name, true);
        MetadataUpdater.SetOrUpdateString(ref seriesInfo.Description, seriesDetail.Summary, false);
        MetadataUpdater.SetOrUpdateValue(ref seriesInfo.FirstAired, seriesDetail.Premiered);
        if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.IsEnded, true);
        }

        MetadataUpdater.SetOrUpdateValue(ref seriesInfo.TotalRating, seriesDetail.Rating != null ? seriesDetail.Rating.Rating : 0);

        MetadataUpdater.SetOrUpdateList(seriesInfo.Genres, seriesDetail.Genres.ToList(), true);
        MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanys(seriesDetail.Network ?? seriesDetail.WebNetwork, CompanyType.TVNetwork), true);

        MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(seriesDetail.Embedded.Cast, PersonOccupation.Actor), true);
        MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesInfo.TvMazeId, seriesInfo.Series, seriesDetail.Embedded.Cast), true);

        return true;
      }
      return false;
    }

    public bool UpdateSeason(SeasonInfo seasonInfo)
    {
      TvMazeSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seasonInfo.TvMazeId > 0 && _tvMaze.GetSeries(seasonInfo.TvMazeId, out seriesDetail))
      {
        if (seriesDetail.Externals.TvDbId.HasValue)
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvdbId, seriesDetail.Externals.TvDbId.Value);
        if (!string.IsNullOrEmpty(seriesDetail.Externals.ImDbId))
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.ImdbId, seriesDetail.Externals.ImDbId);

        MetadataUpdater.SetOrUpdateString(ref seasonInfo.Series, seriesDetail.Name, false);

        TvMazeSeason seasonDetail;
        if (_tvMaze.GetSeriesSeason(seasonInfo.MovieDbId, seasonInfo.SeasonNumber.Value, out seasonDetail))
        {
          MetadataUpdater.SetOrUpdateValue(ref seasonInfo.FirstAired, seasonDetail.PremiereDate);
        }

        return true;
      }
      return false;
    }

    public bool UpdateEpisodePersons(EpisodeInfo episodeInfo, List<PersonInfo> persons, PersonOccupation occupation)
    {
      TvMazeSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (occupation != PersonOccupation.Actor)
        return false;

      if (episodeInfo.TvMazeId > 0 && _tvMaze.GetSeries(episodeInfo.TvMazeId, out seriesDetail))
      {
        if (occupation == PersonOccupation.Actor)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(seriesDetail.Embedded.Cast, PersonOccupation.Actor), false);

        return true;
      }
      return false;
    }

    public bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo, List<CharacterInfo> characters)
    {
      TvMazeSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (episodeInfo.TvMazeId > 0 && _tvMaze.GetSeries(episodeInfo.TvMazeId, out seriesDetail))
      {
        MetadataUpdater.SetOrUpdateList(characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.Name, seriesDetail.Embedded.Cast), false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesPersons(SeriesInfo seriesInfo, List<PersonInfo> persons, PersonOccupation occupation)
    {
      TvMazeSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (occupation != PersonOccupation.Actor)
        return false;

      if (seriesInfo.TvMazeId > 0 && _tvMaze.GetSeries(seriesInfo.TvMazeId, out seriesDetail))
      {
        if (occupation == PersonOccupation.Actor)
          MetadataUpdater.SetOrUpdateList(persons, ConvertToPersons(seriesDetail.Embedded.Cast, PersonOccupation.Actor), false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesCharacters(SeriesInfo seriesInfo, List<CharacterInfo> characters)
    {
      TvMazeSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (seriesInfo.TvMazeId > 0 && _tvMaze.GetSeries(seriesInfo.TvMazeId, out seriesDetail))
      {
        MetadataUpdater.SetOrUpdateList(characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.Name, seriesDetail.Embedded.Cast), false);

        return true;
      }
      return false;
    }

    public bool UpdateSeriesCompanys(SeriesInfo seriesInfo, List<CompanyInfo> companys, CompanyType type)
    {
      TvMazeSeries seriesDetail;

      // Try online lookup
      if (!Init())
        return false;

      if (type != CompanyType.TVNetwork)
        return false;

      if (seriesInfo.TvMazeId > 0 && _tvMaze.GetSeries(seriesInfo.TvMazeId, out seriesDetail))
      {
        if (type == CompanyType.TVNetwork)
          MetadataUpdater.SetOrUpdateList(companys, ConvertToCompanys(seriesDetail.Network ?? seriesDetail.WebNetwork, CompanyType.TVNetwork), false);

        return true;
      }
      return false;
    }

    protected bool TryMatchEpisode(EpisodeInfo episodeInfo, TvMazeSeries seriesDetail)
    {
      TvMazeEpisode episode = null;
      List<TvMazeEpisode> episodes = null;
      if (seriesDetail.Embedded != null && seriesDetail.Embedded.Episodes != null)
      {
        episodes = seriesDetail.Embedded.Episodes.FindAll(e => e.Name == episodeInfo.Episode);
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

        episodes = seriesDetail.Embedded.Episodes.Where(e => episodeInfo.EpisodeNumbers.Contains(e.EpisodeNumber) && e.SeasonNumber == episodeInfo.SeasonNumber).ToList();
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

    private void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, TvMazeSeries seriesDetail, List<TvMazeEpisode> episodes)
    {
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episodes.First().SeasonNumber);
      MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodes.Select(x => x.EpisodeNumber).ToList(), true);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episodes.First().AirDate);

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.Name).ToArray()), false);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Summary)).ToArray()), false);
    }

    private void SetEpisodeDetails(EpisodeInfo episodeInfo, TvMazeSeries seriesDetail, TvMazeEpisode episode)
    {
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episode.SeasonNumber);
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episode.AirDate);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episode.Summary, false);
    }

    private List<PersonInfo> ConvertToPersons(List<TvMazeCast> cast, PersonOccupation occupation)
    {
      if (cast == null || cast.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (TvMazeCast person in cast)
        retValue.Add(new PersonInfo() { TvMazeId = person.Person.Id, Name = person.Person.Name, Occupation = occupation });
      return retValue;
    }

    private List<CharacterInfo> ConvertToCharacters(int seriesId, string seriesTitle, List<TvMazeCast> characters)
    {
      if (characters == null || characters.Count == 0)
        return new List<CharacterInfo>();

      List<CharacterInfo> retValue = new List<CharacterInfo>();
      foreach (TvMazeCast person in characters)
        retValue.Add(new CharacterInfo()
        {
          MediaIsMovie = false,
          MediaMovieDbId = seriesId,
          MediaTitle = seriesTitle,
          ActorMovieDbId = person.Person.Id,
          ActorName = person.Person.Name,
          TvMazeId = person.Character.Id,
          Name = person.Character.Name
        });
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanys(TvMazeNetwork company, CompanyType type)
    {
      if (company == null)
        return new List<CompanyInfo>();

      return new List<CompanyInfo>(
        new CompanyInfo[]
        {
          new CompanyInfo()
          {
             TvMazeId = company.Id,
             Name = company.Name,
             Type = type
          }
      });
    }

    private bool TryMatch(EpisodeInfo episodeInfo, out TvMazeSeries seriesDetails)
    {
      seriesDetails = null;
      if (episodeInfo.TvMazeId > 0 && _tvMaze.GetSeries(episodeInfo.TvMazeId, out seriesDetails))
      {
        SaveMatchToPersistentCache(seriesDetails, seriesDetails.Name);
        return true;
      }
      if (episodeInfo.TvdbId > 0 && _tvMaze.GetSeriesByTvDbId(episodeInfo.TvdbId, out seriesDetails))
      {
        SaveMatchToPersistentCache(seriesDetails, seriesDetails.Name);
        return true;
      }
      if (!string.IsNullOrEmpty(episodeInfo.ImdbId) && _tvMaze.GetSeriesByImDbId(episodeInfo.ImdbId, out seriesDetails))
      {
        SaveMatchToPersistentCache(seriesDetails, seriesDetails.Name);
        return true;
      }
      return TryMatch(episodeInfo.Series, episodeInfo.FirstAired.HasValue ? episodeInfo.FirstAired.Value.Year : 0, false, out seriesDetails);
    }

    protected bool TryMatch(string seriesName, int year, bool cacheOnly, out TvMazeSeries seriesDetail)
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
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesName, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        int tvMazeId = 0;
        if (!string.IsNullOrEmpty(match.Id) && int.TryParse(match.Id, out tvMazeId))
        {
          // If this is a known movie, only return the movie details.
          if (match != null)
            return _tvMaze.GetSeries(tvMazeId, out seriesDetail);
        }

        if (cacheOnly)
          return false;

        List<TvMazeSeries> series;
        if (_tvMaze.SearchSeriesUnique(seriesName, year, out series))
        {
          TvMazeSeries seriesResult = series[0];
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Found unique online match for \"{0}\": \"{1}\"", seriesName, seriesResult.Name);
          if (_tvMaze.GetSeries(series[0].Id, out seriesDetail))
          {
            SaveMatchToPersistentCache(seriesDetail, seriesName);
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: No unique match found for \"{0}\"", seriesName);
        // Also save "non matches" to avoid retrying
        _storage.TryAddMatch(new SeriesMatch { ItemName = seriesName, TvDBName = seriesName });
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing series {0}", ex, seriesName);
        return false;
      }
      finally
      {
        if (seriesDetail != null)
          _memoryCache.TryAdd(seriesName, seriesDetail);
      }
    }

    private void SaveMatchToPersistentCache(TvMazeSeries seriesDetails, string seriesName)
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

      if (_tvMaze != null)
        return true;

      _tvMaze = new TvMazeWrapper();
      return _tvMaze.Init(CACHE_PATH);
    }

    protected override void DownloadFanArt(string tvMazeId)
    {
      try
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher Download: Started for ID {0}", tvMazeId);

        if (!Init())
          return;

        int id = 0;
        if (!int.TryParse(tvMazeId, out id))
          return;

        TvMazeSeries series;
        if (!_tvMaze.GetSeries(id, out series))
          return;

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher Download: Begin saving banners for ID {0}", tvMazeId);
        _tvMaze.DownloadImage(series.Id, series.Images, "Posters");

        //Save person banners
        foreach (TvMazeCast cast in series.Embedded.Cast)
        {
          _tvMaze.DownloadImage(cast.Person.Id, cast.Person.Images, "Thumbnails");
          _tvMaze.DownloadImage(cast.Character.Id, cast.Character.Images, "Character");
        }
        
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher Download: Finished saving banners for ID {0}", tvMazeId);

        // Remember we are finished
        FinishDownloadFanArt(tvMazeId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception downloading FanArt for ID {0}", ex, tvMazeId);
      }
    }
  }
}
