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
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using MediaPortal.Extensions.OnlineLibraries.TvMaze;
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

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the series from TvMaze and updates the given <paramref name="episodeInfo"/> with the online information.
    /// </summary>
    /// <param name="episodeInfo">Episode to check</param>
    /// <returns><c>true</c> if successful</returns>
    public bool FindAndUpdateEpisode(EpisodeInfo episodeInfo)
    {
      try
      {
        TvMazeSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (TryMatch(episodeInfo, out seriesDetail))
        {
          string tvMazeId = "";

          if (seriesDetail != null)
          {
            tvMazeId = seriesDetail.Id.ToString();

            MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvMazeId, seriesDetail.Id);
            if (seriesDetail.Externals.TvDbId.HasValue)
              MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvdbId, seriesDetail.Externals.TvDbId.Value);
            if (!string.IsNullOrEmpty(seriesDetail.Externals.ImDbId))
              MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesImdbId, seriesDetail.Externals.ImDbId);

            MetadataUpdater.SetOrUpdateString(ref episodeInfo.Series, seriesDetail.Name, true);
            MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, seriesDetail.Premiered);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, seriesDetail.Genres, true, true);

            MetadataUpdater.SetOrUpdateList(episodeInfo.Networks, ConvertToCompanies(seriesDetail.Network ?? seriesDetail.WebNetwork, CompanyAspect.COMPANY_TV_NETWORK), true, true);

            MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(seriesDetail.Embedded.Cast, PersonAspect.OCCUPATION_ACTOR), true, true);
            MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(episodeInfo.SeriesMovieDbId, episodeInfo.Series, seriesDetail.Embedded.Cast), true, true);

            // Also try to fill episode title from series details (most file names don't contain episode name).
            if (!TryMatchEpisode(episodeInfo, seriesDetail))
              return false;

            if (episodeInfo.TvdbId > 0)
              tvMazeId += "|" + episodeInfo.TvMazeId;

            if (episodeInfo.Thumbnail == null)
            {
              List<string> thumbs = GetFanArtFiles(episodeInfo, FanArtScope.Episode, FanArtType.Thumbnails);
              if (thumbs.Count > 0)
                episodeInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }

          if (tvMazeId.Length > 0)
            ScheduleDownload(tvMazeId);
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing episode {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeries(SeriesInfo seriesInfo)
    {
      try
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
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Description, seriesDetail.Summary, true);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.FirstAired, seriesDetail.Premiered);
          if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
          {
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.IsEnded, true);
          }

          MetadataUpdater.SetOrUpdateRatings(ref seriesInfo.TotalRating, ref seriesInfo.RatingCount, seriesDetail.Rating != null ? seriesDetail.Rating.Rating : 0, null);

          MetadataUpdater.SetOrUpdateList(seriesInfo.Genres, seriesDetail.Genres.ToList(), true, true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanies(seriesDetail.Network ?? seriesDetail.WebNetwork, CompanyAspect.COMPANY_TV_NETWORK), true, true);

          MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(seriesDetail.Embedded.Cast, PersonAspect.OCCUPATION_ACTOR), true, true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesInfo.TvMazeId, seriesInfo.Series, seriesDetail.Embedded.Cast), true, true);

          TvMazeEpisode nextEpisode = seriesDetail.Embedded.Episodes.Where(e => e.AirDate > DateTime.Now).FirstOrDefault();
          if (nextEpisode != null)
          {
            MetadataUpdater.SetOrUpdateString(ref seriesInfo.NextEpisodeName, nextEpisode.Name, true);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeAirDate, nextEpisode.AirStamp);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeSeasonNumber, nextEpisode.SeasonNumber);
            MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeNumber, nextEpisode.EpisodeNumber);
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
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing series {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeason(SeasonInfo seasonInfo)
    {
      try
      {
        TvMazeSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (seasonInfo.SeriesTvMazeId > 0 && _tvMaze.GetSeries(seasonInfo.SeriesTvMazeId, out seriesDetail))
        {
          if (seriesDetail.Externals.TvDbId.HasValue)
            MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesTvdbId, seriesDetail.Externals.TvDbId.Value);
          if (!string.IsNullOrEmpty(seriesDetail.Externals.ImDbId))
            MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesImdbId, seriesDetail.Externals.ImDbId);

          MetadataUpdater.SetOrUpdateString(ref seasonInfo.Series, seriesDetail.Name, false);

          TvMazeSeason seasonDetail;
          if (_tvMaze.GetSeriesSeason(seasonInfo.SeriesMovieDbId, seasonInfo.SeasonNumber.Value, out seasonDetail))
          {
            MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvMazeId, seasonDetail.Id);

            MetadataUpdater.SetOrUpdateValue(ref seasonInfo.FirstAired, seasonDetail.PremiereDate);
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing season {0}", ex, seasonInfo.ToString());
        return false;
      }
    }

    public bool UpdateEpisodePersons(EpisodeInfo episodeInfo, string occupation)
    {
      try
      {
        TvMazeSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (occupation != PersonAspect.OCCUPATION_ACTOR)
          return false;

        if (episodeInfo.SeriesTvMazeId > 0 && _tvMaze.GetSeries(episodeInfo.SeriesTvMazeId, out seriesDetail))
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
          {
            MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, ConvertToPersons(seriesDetail.Embedded.Cast, occupation), false, true);

            foreach (PersonInfo person in episodeInfo.Actors)
            {
              TvMazeCast cast = seriesDetail.Embedded.Cast.Find(c => c.Person.Id == person.TvMazeId);
              if (person.Thumbnail == null && person.TvMazeId > 0)
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
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing persons {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo)
    {
      try
      {
        TvMazeSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (episodeInfo.SeriesTvMazeId > 0 && _tvMaze.GetSeries(episodeInfo.SeriesTvMazeId, out seriesDetail))
        {
          MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.Name, seriesDetail.Embedded.Cast), false, true);

          foreach (CharacterInfo character in episodeInfo.Characters)
          {
            TvMazeCast cast = seriesDetail.Embedded.Cast.Find(c => c.Character.Id == character.TvMazeId);
            if (character.Thumbnail == null && character.TvMazeId > 0)
            {
              List<string> thumbs = GetFanArtFiles(character, FanArtScope.Character, FanArtType.Thumbnails);
              if (thumbs.Count > 0)
                character.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing characters {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesPersons(SeriesInfo seriesInfo, string occupation)
    {
      try
      {
        TvMazeSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (occupation != PersonAspect.OCCUPATION_ACTOR)
          return false;

        if (seriesInfo.TvMazeId > 0 && _tvMaze.GetSeries(seriesInfo.TvMazeId, out seriesDetail))
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
          {
            MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, ConvertToPersons(seriesDetail.Embedded.Cast, occupation), false, true);

            foreach (PersonInfo person in seriesInfo.Actors)
            {
              TvMazeCast cast = seriesDetail.Embedded.Cast.Find(c => c.Person.Id == person.TvMazeId);
              if (person.Thumbnail == null && person.TvMazeId > 0)
              {
                List<string> thumbs = GetFanArtFiles(person, FanArtScope.Actor, FanArtType.Thumbnails);
                if (thumbs.Count > 0)
                  person.Thumbnail = File.ReadAllBytes(thumbs[0]);
              }
            }

            return true;
          }
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing series persons {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesCharacters(SeriesInfo seriesInfo)
    {
      try
      {
        TvMazeSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (seriesInfo.TvMazeId > 0 && _tvMaze.GetSeries(seriesInfo.TvMazeId, out seriesDetail))
        {
          MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, ConvertToCharacters(seriesDetail.Id, seriesDetail.Name, seriesDetail.Embedded.Cast), false, true);

          foreach (CharacterInfo character in seriesInfo.Characters)
          {
            TvMazeCast cast = seriesDetail.Embedded.Cast.Find(c => c.Character.Id == character.TvMazeId);
            if (character.Thumbnail == null && character.TvMazeId > 0)
            {
              List<string> thumbs = GetFanArtFiles(character, FanArtScope.Character, FanArtType.Thumbnails);
              if (thumbs.Count > 0)
                character.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing series characters {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string type)
    {
      try
      {
        TvMazeSeries seriesDetail;

        // Try online lookup
        if (!Init())
          return false;

        if (type != CompanyAspect.COMPANY_TV_NETWORK)
          return false;

        if (seriesInfo.TvMazeId > 0 && _tvMaze.GetSeries(seriesInfo.TvMazeId, out seriesDetail))
        {
          if (type == CompanyAspect.COMPANY_TV_NETWORK)
            MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, ConvertToCompanies(seriesDetail.Network ?? seriesDetail.WebNetwork, type), false, true);

          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Exception while processing series companies {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private void SetMultiEpisodeDetailsl(EpisodeInfo episodeInfo, TvMazeSeries seriesDetail, List<TvMazeEpisode> episodes)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvMazeId, episodes.First().Id);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episodes.First().SeasonNumber);
      MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodes.Select(x => x.EpisodeNumber).ToList(), true, true);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episodes.First().AirStamp);

      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Episode, string.Join("; ", episodes.OrderBy(e => e.EpisodeNumber).Select(e => e.Name).ToArray()), false);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, string.Join("\r\n\r\n", episodes.OrderBy(e => e.EpisodeNumber).
        Select(e => string.Format("{0,02}) {1}", e.EpisodeNumber, e.Summary)).ToArray()), false);
    }

    private void SetEpisodeDetails(EpisodeInfo episodeInfo, TvMazeSeries seriesDetail, TvMazeEpisode episode)
    {
      MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvMazeId, episode.Id);

      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episode.SeasonNumber);
      episodeInfo.EpisodeNumbers.Clear();
      episodeInfo.EpisodeNumbers.Add(episode.EpisodeNumber);
      MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episode.AirStamp);
      MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episode.Summary, false);
    }

    private List<PersonInfo> ConvertToPersons(List<TvMazeCast> cast, string occupation)
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
          ActorTvMazeId = person.Person.Id,
          ActorName = person.Person.Name,
          TvMazeId = person.Character.Id,
          Name = person.Character.Name
        });
      return retValue;
    }

    private List<CompanyInfo> ConvertToCompanies(TvMazeNetwork company, string type)
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

    #endregion

    #region Online matching

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

    private bool TryMatch(EpisodeInfo episodeInfo, out TvMazeSeries seriesDetails)
    {
      seriesDetails = null;
      if (episodeInfo.SeriesTvMazeId > 0 && _tvMaze.GetSeries(episodeInfo.SeriesTvMazeId, out seriesDetails))
      {
        StoreSeriesMatch(seriesDetails, episodeInfo.Series);
        return true;
      }
      if (episodeInfo.SeriesTvdbId > 0 && _tvMaze.GetSeriesByTvDbId(episodeInfo.SeriesTvdbId, out seriesDetails))
      {
        StoreSeriesMatch(seriesDetails, episodeInfo.Series);
        return true;
      }
      if (!string.IsNullOrEmpty(episodeInfo.SeriesImdbId) && _tvMaze.GetSeriesByImDbId(episodeInfo.SeriesImdbId, out seriesDetails))
      {
        StoreSeriesMatch(seriesDetails, episodeInfo.Series);
        return true;
      }
      return TryMatch(episodeInfo.Series, episodeInfo.SeriesFirstAired.HasValue ? episodeInfo.SeriesFirstAired.Value.Year : 0, false, out seriesDetails);
    }

    protected bool TryMatch(string seriesName, int year, bool cacheOnly, out TvMazeSeries seriesDetail)
    {
      seriesDetail = null;
      SeriesInfo searchSeries = new SeriesInfo()
      {
        Series = seriesName,
        FirstAired = year > 0 ? new DateTime(year, 1, 1) : default(DateTime?),
      };
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
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: Try to lookup series \"{0}\" from cache: {1}", seriesName, match != null && !string.IsNullOrEmpty(match.Id));

        // Try online lookup
        if (!Init())
          return false;

        int tvMazeId = 0;
        if (match != null && !string.IsNullOrEmpty(match.Id) && int.TryParse(match.Id, out tvMazeId))
        {
          // If this is a known movie, only return the movie details.
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
            StoreSeriesMatch(seriesDetail, searchSeries.ToString());
            return true;
          }
        }
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher: No unique match found for \"{0}\"", seriesName);
        // Also save "non matches" to avoid retrying
        StoreSeriesMatch(null, searchSeries.ToString());
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
          _memoryCache.TryAdd(searchSeries.ToString(), seriesDetail);
      }
    }

    private void StoreSeriesMatch(TvMazeSeries series, string seriesName)
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
        FirstAired = series.Premiered.HasValue ? series.Premiered.Value : default(DateTime?)
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

      if (_tvMaze != null)
        return true;

      _tvMaze = new TvMazeWrapper();
      return _tvMaze.Init(CACHE_PATH);
    }

    #region FanaArt

    public List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      List<string> fanartFiles = new List<string>();
      string path = null;
      if (scope == FanArtScope.Series)
      {
        SeriesInfo series = infoObject as SeriesInfo;
        if (series != null && series.TvMazeId > 0)
        {
          path = Path.Combine(CACHE_PATH, series.TvMazeId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtScope.Episode)
      {
        EpisodeInfo episode = infoObject as EpisodeInfo;
        if (episode != null && episode.TvMazeId > 0)
        {
          path = Path.Combine(CACHE_PATH, episode.TvMazeId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtScope.Actor)
      {
        PersonInfo person = infoObject as PersonInfo;
        if (person != null && person.TvMazeId > 0)
        {
          path = Path.Combine(CACHE_PATH, person.TvMazeId.ToString(), string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtScope.Character)
      {
        CharacterInfo character = infoObject as CharacterInfo;
        if (character != null && character.TvMazeId > 0)
        {
          path = Path.Combine(CACHE_PATH, character.TvMazeId.ToString(), string.Format(@"{0}\{1}\*.*", scope, type));
        }
      }
      if (Directory.Exists(path))
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
      return fanartFiles;
    }

    protected override void DownloadFanArt(string tvMazeId)
    {
      try
      {
        if (string.IsNullOrEmpty(tvMazeId))
          return;

        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher Download: Started for ID {0}", tvMazeId);

        if (!Init())
          return;

        string[] ids;
        if (tvMazeId.Contains("|"))
          ids = tvMazeId.Split('|');
        else
          ids = new string[] { tvMazeId };

        int tvMazeSeriesId = 0;
        if (!int.TryParse(ids[0], out tvMazeSeriesId))
          return;

        if (tvMazeSeriesId <= 0)
          return;

        TvMazeSeries seriesDetail;
        if (!_tvMaze.GetSeries(tvMazeSeriesId, out seriesDetail))
          return;

        int tvMazeEpisodeId = 0;
        TvMazeEpisode episodeDetail = null;
        if (ids.Length > 1)
        {
          if (int.TryParse(ids[1], out tvMazeEpisodeId))
          {
            if (seriesDetail.Embedded != null && seriesDetail.Embedded.Episodes != null)
            {
              episodeDetail = seriesDetail.Embedded.Episodes.Find(e => e.Id == tvMazeEpisodeId);
            }
          }
        }

        // Save Banners
        ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher Download: Begin saving banners for ID {0}", tvMazeId);
        _tvMaze.DownloadImage(seriesDetail.Id, seriesDetail.Images, string.Format(@"{0}\{1}", FanArtScope.Series, FanArtType.Posters));

        //Save person banners
        foreach (TvMazeCast cast in seriesDetail.Embedded.Cast)
        {
          _tvMaze.DownloadImage(cast.Person.Id, cast.Person.Images, string.Format(@"{0}\{1}", FanArtScope.Actor, FanArtType.Thumbnails));
          _tvMaze.DownloadImage(cast.Character.Id, cast.Character.Images, string.Format(@"{0}\{1}", FanArtScope.Character, FanArtType.Thumbnails));
        }

        if (episodeDetail != null)
        {
          // Save Episode Banners
          ServiceRegistration.Get<ILogger>().Debug("SeriesTvMazeMatcher Download: Begin saving episode banners for ID {0}", tvMazeEpisodeId);
          _tvMaze.DownloadImage(episodeDetail.Id, episodeDetail.Images, string.Format(@"{0}\{1}", FanArtScope.Episode, FanArtType.Thumbnails));
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

    #endregion
  }
}
