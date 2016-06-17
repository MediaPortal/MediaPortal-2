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
using System.Globalization;
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Matches;
using System.Collections.Generic;
using System.Reflection;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.OnlineLibraries.Wrappers;
using MediaPortal.Extensions.UserServices.FanArtService.Interfaces;

namespace MediaPortal.Extensions.OnlineLibraries.Matchers
{
  public abstract class SeriesMatcher<TImg, TLang> : BaseMatcher<SeriesMatch, string>
  {
    #region Init

    public SeriesMatcher(string cachePath, TimeSpan maxCacheDuration)
    {
      _cachePath = cachePath;
      _matchesSettingsFile = Path.Combine(cachePath, "SeriesMatches.xml");
      _maxCacheDuration = maxCacheDuration;
    }

    private new bool Init()
    {
      if (!base.Init())
        return false;

      if (_wrapper != null)
        return true;

      return InitWrapper();
    }

    public abstract bool InitWrapper();

    #endregion

    #region Constants

    protected override string MatchesSettingsFile
    {
      get { return _matchesSettingsFile; }
    }

    #endregion

    #region Fields

    private DateTime _memoryCacheInvalidated = DateTime.MinValue;
    private ConcurrentDictionary<string, SeriesInfo> _memoryCache = new ConcurrentDictionary<string, SeriesInfo>(StringComparer.OrdinalIgnoreCase);
    private ConcurrentDictionary<string, EpisodeInfo> _memoryCacheEpisode = new ConcurrentDictionary<string, EpisodeInfo>(StringComparer.OrdinalIgnoreCase);
    private string _cachePath;
    private string _matchesSettingsFile;
    private TimeSpan _maxCacheDuration;

    protected ApiWrapper<TImg, TLang> _wrapper = null;
    protected bool UseSeasonIdForFanArt { get; set; }
    protected bool UseEpisodeIdForFanArt { get; set; }

    #endregion

    #region Metadata updaters

    /// <summary>
    /// Tries to lookup the Episode online and downloads images.
    /// </summary>
    /// <param name="episodeInfo">Episode to check</param>
    /// <returns><c>true</c> if successful</returns>
    public virtual bool FindAndUpdateEpisode(EpisodeInfo episodeInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        EpisodeInfo episodeMatch = null;
        SeriesInfo seriesMatch = null;
        SeriesInfo episodeSeries = episodeInfo.CloneBasicSeries();
        string seriesId = null;
        string episodeId = null;
        string altEpisodeId = null;
        bool matchFound = false;
        TLang language = FindBestMatchingLanguage(episodeInfo);

        if (GetSeriesId(episodeSeries, out seriesId))
        {
          // Prefer memory cache
          CheckCacheAndRefresh();
          if (_memoryCache.TryGetValue(seriesId, out seriesMatch))
            matchFound = true;
        }

        if (seriesId != null && episodeInfo.SeasonNumber.HasValue && episodeInfo.EpisodeNumbers.Count > 0)
        {
          altEpisodeId = seriesId + "|" + episodeInfo.SeasonNumber.Value + "|" + episodeInfo.EpisodeNumbers[0];
        }
        if (GetSeriesEpisodeId(episodeInfo, out episodeId))
        {
          // Prefer memory cache
          CheckCacheAndRefresh();
          if (_memoryCacheEpisode.TryGetValue(episodeId, out episodeMatch))
            matchFound = true;
          else if (_memoryCacheEpisode.TryGetValue(altEpisodeId, out episodeMatch))
            matchFound = true;
        }

        if(!matchFound)
        {
          // Load cache or create new list
          List<SeriesMatch> matches = _storage.GetMatches();

          // Use cached values before doing online query
          SeriesMatch match = matches.Find(m =>
            (string.Equals(m.ItemName, episodeSeries.SeriesName.ToString(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(m.OnlineName, episodeSeries.SeriesName.ToString(), StringComparison.OrdinalIgnoreCase)) &&
            (episodeSeries.FirstAired.HasValue && m.Year == episodeSeries.FirstAired.Value.Year || !episodeSeries.FirstAired.HasValue || m.Year == 0));
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Try to lookup series \"{0}\" from cache: {1}", episodeSeries, match != null && !string.IsNullOrEmpty(match.Id));

          episodeMatch = CloneProperties(episodeInfo);
          if (match != null && match.Id != null)
          {
            if (SetSeriesId(episodeMatch, match.Id))
            {
              //If Id was found in cache the online movie info is probably also in the cache
              if (_wrapper.UpdateFromOnlineSeriesEpisode(episodeMatch, language, true))
                matchFound = true;
            }
          }

          if (!matchFound && !forceQuickMode)
          {
            //Try to update movie information from online source if online Ids are present
            if (!_wrapper.UpdateFromOnlineSeriesEpisode(episodeMatch, language, false))
            {
              //Search for the movie online and update the Ids if a match is found
              if(_wrapper.SearchSeriesEpisodeUniqueAndUpdate(episodeMatch, language))
              {
                //Ids were updated now try to update movie information from online source
                if (_wrapper.UpdateFromOnlineSeriesEpisode(episodeMatch, language, false))
                  matchFound = true;
              }
            }
          }
        }

        //Always save match even if none to avoid retries
        SeriesInfo cloneBasicSeries = episodeMatch != null ? episodeMatch.CloneBasicSeries() : null;
        StoreSeriesMatch(episodeSeries, cloneBasicSeries);

        if (matchFound && episodeMatch != null)
        {
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.ImdbId, episodeMatch.ImdbId);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.MovieDbId, episodeMatch.MovieDbId);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvdbId, episodeMatch.TvdbId);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvMazeId, episodeMatch.TvMazeId);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.TvRageId, episodeMatch.TvRageId);

          MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesImdbId, episodeMatch.SeriesImdbId);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesMovieDbId, episodeMatch.SeriesMovieDbId);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvdbId, episodeMatch.SeriesTvdbId);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvMazeId, episodeMatch.SeriesTvMazeId);
          MetadataUpdater.SetOrUpdateId(ref episodeInfo.SeriesTvRageId, episodeMatch.SeriesTvRageId);

          MetadataUpdater.SetOrUpdateString(ref episodeInfo.EpisodeName, episodeMatch.EpisodeName);
          MetadataUpdater.SetOrUpdateString(ref episodeInfo.Summary, episodeMatch.Summary);
          MetadataUpdater.SetOrUpdateString(ref episodeInfo.SeriesName, episodeMatch.SeriesName);

          MetadataUpdater.SetOrUpdateValue(ref episodeInfo.FirstAired, episodeMatch.FirstAired);
          MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeasonNumber, episodeMatch.SeasonNumber);
          MetadataUpdater.SetOrUpdateValue(ref episodeInfo.SeriesFirstAired, episodeMatch.SeriesFirstAired);

          MetadataUpdater.SetOrUpdateRatings(ref episodeInfo.TotalRating, ref episodeInfo.RatingCount, episodeMatch.TotalRating, episodeMatch.RatingCount);

          MetadataUpdater.SetOrUpdateList(episodeInfo.EpisodeNumbers, episodeMatch.EpisodeNumbers, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.DvdEpisodeNumbers, episodeMatch.DvdEpisodeNumbers, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, episodeMatch.Actors, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, episodeMatch.Characters, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, episodeMatch.Directors, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Genres, episodeMatch.Genres, true);
          MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, episodeMatch.Writers, true);

          MetadataUpdater.SetOrUpdateValue(ref episodeInfo.Thumbnail, episodeMatch.Thumbnail);
          if (episodeInfo.Thumbnail == null)
          {
            List<string> thumbs = GetFanArtFiles(episodeInfo, FanArtMediaTypes.Episode, FanArtTypes.Thumbnail);
            if (thumbs.Count > 0)
              episodeInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }

          if (GetSeriesId(episodeInfo.CloneBasicSeries(), out seriesId))
          {
            _memoryCache.TryAdd(seriesId, episodeInfo.CloneBasicSeries());

            if (GetSeriesEpisodeId(episodeInfo, out episodeId))
            {
              _memoryCacheEpisode.TryAdd(episodeId, episodeInfo);

              seriesId += "|" + episodeId;
            }
            else
            {
              if (episodeInfo.SeasonNumber.HasValue && episodeInfo.EpisodeNumbers.Count > 0)
              {
                seriesId += "|" + episodeInfo.SeasonNumber.Value + "|" + episodeInfo.EpisodeNumbers[0];

                _memoryCacheEpisode.TryAdd(seriesId, episodeInfo);
              }
            }
            ScheduleDownload(seriesId);
          }

          return true;
        }

        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing episode {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateSeries(SeriesInfo seriesInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(seriesInfo);
        bool updated = false;
        SeriesInfo seriesMatch = CloneProperties(seriesInfo);
        //Try updating from cache
        if (!_wrapper.UpdateFromOnlineSeries(seriesMatch, language, true))
        {
          if (!forceQuickMode)
          {
            //Try to update series information from online source if online Ids are present
            if (!_wrapper.UpdateFromOnlineSeries(seriesMatch, language, false))
            {
              //Search for the series online and update the Ids if a match is found
              if (_wrapper.SearchSeriesUniqueAndUpdate(seriesMatch, language))
              {
                //Ids were updated now try to fetch the online series info
                if (_wrapper.UpdateFromOnlineSeries(seriesMatch, language, false))
                  updated = true;
              }
            }
          }
        }
        else
        {
          updated = true;
        }

        if (updated)
        {
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.TvdbId, seriesMatch.TvdbId);
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.ImdbId, seriesMatch.ImdbId);
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.MovieDbId, seriesMatch.MovieDbId);
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.TvMazeId, seriesMatch.TvMazeId);
          MetadataUpdater.SetOrUpdateId(ref seriesInfo.TvRageId, seriesMatch.TvRageId);

          MetadataUpdater.SetOrUpdateString(ref seriesInfo.SeriesName, seriesMatch.SeriesName);
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.OriginalName, seriesMatch.OriginalName);
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Description, seriesMatch.Description);
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.Certification, seriesMatch.Certification);
          MetadataUpdater.SetOrUpdateString(ref seriesInfo.NextEpisodeName, seriesMatch.NextEpisodeName);

          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.FirstAired, seriesMatch.FirstAired);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.Popularity, seriesMatch.Popularity);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.IsEnded, seriesMatch.IsEnded);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeAirDate, seriesMatch.NextEpisodeAirDate);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeNumber, seriesMatch.NextEpisodeNumber);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.NextEpisodeSeasonNumber, seriesMatch.NextEpisodeSeasonNumber);
          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.Score, seriesMatch.Score);

          MetadataUpdater.SetOrUpdateRatings(ref seriesInfo.TotalRating, ref seriesInfo.RatingCount, seriesMatch.TotalRating, seriesMatch.RatingCount);

          MetadataUpdater.SetOrUpdateList(seriesInfo.Genres, seriesMatch.Genres, true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Awards, seriesMatch.Awards, true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, seriesMatch.Networks, true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.ProductionCompanies, seriesMatch.ProductionCompanies, true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, seriesMatch.Actors, true);
          MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, seriesMatch.Characters, true);

          MetadataUpdater.SetOrUpdateValue(ref seriesInfo.Thumbnail, seriesMatch.Thumbnail);
        }

        if (seriesInfo.Thumbnail == null)
        {
          List<string> thumbs = GetFanArtFiles(seriesInfo, FanArtMediaTypes.Series, FanArtTypes.Poster);
          if (thumbs.Count > 0)
            seriesInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing series {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateSeason(SeasonInfo seasonInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = default(TLang);
        bool updated = false;
        SeasonInfo seasonMatch = CloneProperties(seasonInfo);
        //Try updating from cache
        if (!_wrapper.UpdateFromOnlineSeriesSeason(seasonMatch, language, true))
        {
          if (!forceQuickMode)
          {
            //Try to update season information from online source
            if (_wrapper.UpdateFromOnlineSeriesSeason(seasonMatch, language, false))
              updated = true;
          }
        }
        else
        {
          updated = true;
        }

        if (updated)
        {
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvdbId, seasonMatch.TvdbId);
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.ImdbId, seasonMatch.ImdbId);
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.MovieDbId, seasonMatch.MovieDbId);
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvMazeId, seasonMatch.TvMazeId);
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.TvRageId, seasonMatch.TvRageId);

          MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesImdbId, seasonMatch.SeriesImdbId);
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesMovieDbId, seasonMatch.SeriesMovieDbId);
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesTvdbId, seasonMatch.SeriesTvdbId);
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesTvMazeId, seasonMatch.SeriesTvMazeId);
          MetadataUpdater.SetOrUpdateId(ref seasonInfo.SeriesTvRageId, seasonMatch.SeriesTvRageId);

          MetadataUpdater.SetOrUpdateString(ref seasonInfo.SeriesName, seasonMatch.SeriesName);
          MetadataUpdater.SetOrUpdateString(ref seasonInfo.Description, seasonMatch.Description);

          MetadataUpdater.SetOrUpdateValue(ref seasonInfo.FirstAired, seasonMatch.FirstAired);
          MetadataUpdater.SetOrUpdateValue(ref seasonInfo.SeasonNumber, seasonMatch.SeasonNumber);

          MetadataUpdater.SetOrUpdateValue(ref seasonInfo.Thumbnail, seasonMatch.Thumbnail);
        }
 
        if (seasonInfo.Thumbnail == null)
        {
          List<string> thumbs = GetFanArtFiles(seasonInfo, FanArtMediaTypes.SeriesSeason, FanArtTypes.Poster);
          if (thumbs.Count > 0)
            seasonInfo.Thumbnail = File.ReadAllBytes(thumbs[0]);
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing season {0}", ex, seasonInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateSeriesPersons(SeriesInfo seriesInfo, string occupation, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(seriesInfo);
        bool updated = false;
        SeriesInfo seriesMatch = CloneProperties(seriesInfo);
        List<PersonInfo> persons = new List<PersonInfo>();
        if (occupation == PersonAspect.OCCUPATION_ACTOR)
          persons = seriesMatch.Actors;
        foreach (PersonInfo person in persons)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineSeriesPerson(person, language, true))
          {
            if (!forceQuickMode)
            {
              //Try to update movie information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineSeriesPerson(person, language, false))
              {
                //Search for the movie online and update the Ids if a match is found
                if (_wrapper.SearchPersonUniqueAndUpdate(person, language))
                {
                  //Ids were updated now try to fetch the online movie info
                  if (_wrapper.UpdateFromOnlineSeriesPerson(person, language, false))
                    updated = true;
                }
              }
            }
          }
          else
          {
            updated = true;
          }
        }

        if (updated)
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
            MetadataUpdater.SetOrUpdateList(seriesInfo.Actors, seriesMatch.Actors, false);
        }

        List<string> thumbs = new List<string>();
        if (occupation == PersonAspect.OCCUPATION_ACTOR)
        {
          foreach (PersonInfo person in seriesInfo.Actors)
          {
            if (person.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(person, FanArtMediaTypes.Actor, FanArtTypes.Thumbnail);
              if (thumbs.Count > 0)
                person.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing persons {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateSeriesCharacters(SeriesInfo seriesInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(seriesInfo);
        bool updated = false;
        SeriesInfo seriesMatch = CloneProperties(seriesInfo);
        foreach (CharacterInfo character in seriesMatch.Characters)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineMovieCharacter(character, language, true))
          {
            if (!forceQuickMode)
            {
              //Try to update movie information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineSeriesCharacter(character, language, false))
              {
                //Search for the movie online and update the Ids if a match is found
                if (_wrapper.SearchCharacterUniqueAndUpdate(character, language))
                {
                  //Ids were updated now try to fetch the online movie info
                  if (_wrapper.UpdateFromOnlineSeriesCharacter(character, language, false))
                    updated = true;
                }
              }
            }
          }
          else
          {
            updated = true;
          }
        }

        if (updated)
          MetadataUpdater.SetOrUpdateList(seriesInfo.Characters, seriesMatch.Characters, false);

        List<string> thumbs = new List<string>();
        foreach (CharacterInfo character in seriesInfo.Characters)
        {
          if (character.Thumbnail == null)
          {
            thumbs = GetFanArtFiles(character, FanArtMediaTypes.Character, FanArtTypes.Thumbnail);
            if (thumbs.Count > 0)
              character.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing characters {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateSeriesCompanies(SeriesInfo seriesInfo, string companyType, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(seriesInfo);
        bool updated = false;
        SeriesInfo seriesMatch = CloneProperties(seriesInfo);
        List<CompanyInfo> companies = new List<CompanyInfo>();
        if (companyType == CompanyAspect.COMPANY_PRODUCTION)
          companies = seriesMatch.ProductionCompanies;
        else if (companyType == CompanyAspect.COMPANY_TV_NETWORK)
          companies = seriesMatch.Networks;
        foreach (CompanyInfo company in companies)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineSeriesCompany(company, language, true))
          {
            if (!forceQuickMode)
            {
              //Try to update company information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineSeriesCompany(company, language, false))
              {
                //Search for the company online and update the Ids if a match is found
                if (_wrapper.SearchCompanyUniqueAndUpdate(company, language))
                {
                  //Ids were updated now try to fetch the online company info
                  if (_wrapper.UpdateFromOnlineSeriesCompany(company, language, false))
                    updated = true;
                }
              }
            }
          }
          else
          {
            updated = true;
          }
        }

        if (updated)
        {
          if (companyType == CompanyAspect.COMPANY_PRODUCTION)
            MetadataUpdater.SetOrUpdateList(seriesInfo.ProductionCompanies, seriesMatch.ProductionCompanies, false);
          else if (companyType == CompanyAspect.COMPANY_TV_NETWORK)
            MetadataUpdater.SetOrUpdateList(seriesInfo.Networks, seriesMatch.Networks, false);
        }

        List<string> thumbs = new List<string>();
        if (companyType == CompanyAspect.COMPANY_PRODUCTION)
        {
          foreach (CompanyInfo company in seriesInfo.ProductionCompanies)
          {
            if (company.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(company, FanArtMediaTypes.Company, FanArtTypes.Logo);
              if (thumbs.Count > 0)
                company.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }
        else if (companyType == CompanyAspect.COMPANY_TV_NETWORK)
        {
          foreach (CompanyInfo company in seriesInfo.Networks)
          {
            if (company.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(company, FanArtMediaTypes.Company, FanArtTypes.Logo);
              if (thumbs.Count > 0)
                company.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing companies {0}", ex, seriesInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateEpisodePersons(EpisodeInfo episodeInfo, string occupation, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(episodeInfo);
        bool updated = false;
        EpisodeInfo episodeMatch = CloneProperties(episodeInfo);
        List<PersonInfo> persons = new List<PersonInfo>();
        if (occupation == PersonAspect.OCCUPATION_ACTOR)
          persons = episodeMatch.Actors;
        else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
          persons = episodeMatch.Directors;
        else if (occupation == PersonAspect.OCCUPATION_WRITER)
          persons = episodeMatch.Writers;
        foreach (PersonInfo person in persons)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineSeriesPerson(person, language, true))
          {
            if (!forceQuickMode)
            {
              //Try to update person information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineSeriesPerson(person, language, false))
              {
                //Search for the person online and update the Ids if a match is found
                if (_wrapper.SearchPersonUniqueAndUpdate(person, language))
                {
                  //Ids were updated now try to fetch the online person info
                  if (_wrapper.UpdateFromOnlineSeriesPerson(person, language, false))
                    updated = true;
                }
              }
            }
          }
          else
          {
            updated = true;
          }
        }

        if (updated)
        {
          if (occupation == PersonAspect.OCCUPATION_ACTOR)
            MetadataUpdater.SetOrUpdateList(episodeInfo.Actors, episodeMatch.Actors, false);
          else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
            MetadataUpdater.SetOrUpdateList(episodeInfo.Directors, episodeMatch.Directors, false);
          else if (occupation == PersonAspect.OCCUPATION_WRITER)
            MetadataUpdater.SetOrUpdateList(episodeInfo.Writers, episodeMatch.Writers, false);
        }

        List<string> thumbs = new List<string>();
        if (occupation == PersonAspect.OCCUPATION_ACTOR)
        {
          foreach (PersonInfo person in episodeInfo.Actors)
          {
            if (person.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(person, FanArtMediaTypes.Actor, FanArtTypes.Thumbnail);
              if (thumbs.Count > 0)
                person.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_DIRECTOR)
        {
          foreach (PersonInfo person in episodeInfo.Directors)
          {
            if (person.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(person, FanArtMediaTypes.Director, FanArtTypes.Thumbnail);
              if (thumbs.Count > 0)
                person.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }
        else if (occupation == PersonAspect.OCCUPATION_WRITER)
        {
          foreach (PersonInfo person in episodeInfo.Writers)
          {
            if (person.Thumbnail == null)
            {
              thumbs = GetFanArtFiles(person, FanArtMediaTypes.Writer, FanArtTypes.Thumbnail);
              if (thumbs.Count > 0)
                person.Thumbnail = File.ReadAllBytes(thumbs[0]);
            }
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing persons {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    public virtual bool UpdateEpisodeCharacters(EpisodeInfo episodeInfo, bool forceQuickMode)
    {
      try
      {
        // Try online lookup
        if (!Init())
          return false;

        TLang language = FindBestMatchingLanguage(episodeInfo);
        bool updated = false;
        EpisodeInfo episodeMatch = CloneProperties(episodeInfo);
        foreach (CharacterInfo character in episodeMatch.Characters)
        {
          //Try updating from cache
          if (!_wrapper.UpdateFromOnlineSeriesCharacter(character, language, true))
          {
            if (!forceQuickMode)
            {
              //Try to update character information from online source if online Ids are present
              if (!_wrapper.UpdateFromOnlineSeriesCharacter(character, language, false))
              {
                //Search for the character online and update the Ids if a match is found
                if (_wrapper.SearchCharacterUniqueAndUpdate(character, language))
                {
                  //Ids were updated now try to fetch the online character info
                  if (_wrapper.UpdateFromOnlineSeriesCharacter(character, language, false))
                    updated = true;
                }
              }
            }
          }
          else
          {
            updated = true;
          }
        }

        if (updated)
          MetadataUpdater.SetOrUpdateList(episodeInfo.Characters, episodeMatch.Characters, false);

        List<string> thumbs = new List<string>();
        foreach (CharacterInfo character in episodeInfo.Characters)
        {
          if (character.Thumbnail == null)
          {
            thumbs = GetFanArtFiles(character, FanArtMediaTypes.Character, FanArtTypes.Thumbnail);
            if (thumbs.Count > 0)
              character.Thumbnail = File.ReadAllBytes(thumbs[0]);
          }
        }

        return updated;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception while processing characters {0}", ex, episodeInfo.ToString());
        return false;
      }
    }

    #endregion

    #region Metadata update helpers

    private T CloneProperties<T>(T obj)
    {
      if (obj == null)
        return default(T);
      Type type = obj.GetType();

      if (type.IsValueType || type == typeof(string))
      {
        return obj;
      }
      else if (type.IsArray)
      {
        Type elementType = obj.GetType().GetElementType();
        var array = obj as Array;
        Array arrayCopy = Array.CreateInstance(elementType, array.Length);
        for (int i = 0; i < array.Length; i++)
        {
          arrayCopy.SetValue(CloneProperties(array.GetValue(i)), i);
        }
        return (T)Convert.ChangeType(arrayCopy, obj.GetType());
      }
      else if (type.IsClass)
      {
        T newInstance = (T)Activator.CreateInstance(obj.GetType());
        FieldInfo[] fields = type.GetFields(BindingFlags.Public |
                    BindingFlags.NonPublic | BindingFlags.Instance);
        foreach (FieldInfo field in fields)
        {
          object fieldValue = field.GetValue(obj);
          if (fieldValue == null)
            continue;
          field.SetValue(newInstance, CloneProperties(fieldValue));
        }
        return newInstance;
      }
      return default(T);
    }

    private void StoreSeriesMatch(SeriesInfo seriesSearch, SeriesInfo seriesMatch)
    {
      if (seriesMatch == null)
      {
        _storage.TryAddMatch(new SeriesMatch
        {
          ItemName = seriesSearch.ToString()
        });
        return;
      }

      string idValue = null;
      if (GetSeriesId(seriesSearch, out idValue))
      {
        var onlineMatch = new SeriesMatch
        {
          Id = idValue,
          ItemName = seriesSearch.SeriesName.ToString(),
          OnlineName = seriesMatch.SeriesName.ToString(),
          Year = seriesSearch.FirstAired.HasValue ? seriesSearch.FirstAired.Value.Year :
            seriesMatch.FirstAired.HasValue ? seriesMatch.FirstAired.Value.Year : 0
        };
        _storage.TryAddMatch(onlineMatch);
      }
    }

    protected virtual TLang FindBestMatchingLanguage(SeriesInfo seriesInfo)
    {
      if (typeof(TLang) == typeof(string))
      {
        CultureInfo mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture;
        // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
        if (seriesInfo.Languages.Count == 0 || seriesInfo.Languages.Contains(mpLocal.TwoLetterISOLanguageName))
          return (TLang)Convert.ChangeType(mpLocal.TwoLetterISOLanguageName, typeof(TLang));

        // If there is only one language available, use this one.
        if (seriesInfo.Languages.Count == 1)
          return (TLang)Convert.ChangeType(seriesInfo.Languages[0], typeof(TLang));
      }
      // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
      // By returning null we allow fallback to the default language of the online source (en).
      return default(TLang);
    }

    protected virtual TLang FindBestMatchingLanguage(EpisodeInfo episodeInfo)
    {
      if (typeof(TLang) == typeof(string))
      {
        CultureInfo mpLocal = ServiceRegistration.Get<ILocalization>().CurrentCulture;
        // If we don't have movie languages available, or the MP2 setting language is available, prefer it.
        if (episodeInfo.Languages.Count == 0 || episodeInfo.Languages.Contains(mpLocal.TwoLetterISOLanguageName))
          return (TLang)Convert.ChangeType(mpLocal.TwoLetterISOLanguageName, typeof(TLang));

        // If there is only one language available, use this one.
        if (episodeInfo.Languages.Count == 1)
          return (TLang)Convert.ChangeType(episodeInfo.Languages[0], typeof(TLang));
      }
      // If there are multiple languages, that are different to MP2 setting, we cannot guess which one is the "best".
      // By returning null we allow fallback to the default language of the online source (en).
      return default(TLang);
    }

    #endregion

    #region Ids

    protected abstract bool GetSeriesId(SeriesInfo series, out string id);

    protected abstract bool SetSeriesId(SeriesInfo series, string id);

    protected abstract bool SetSeriesId(EpisodeInfo episode, string id);

    protected virtual bool GetSeriesSeasonId(SeasonInfo season, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool GetSeriesEpisodeId(EpisodeInfo episode, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool GetPersonId(PersonInfo person, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool GetCharacterId(CharacterInfo character, out string id)
    {
      id = null;
      return false;
    }

    protected virtual bool GetCompanyId(CompanyInfo company, out string id)
    {
      id = null;
      return false;
    }

    #endregion

    #region Caching

    /// <summary>
    /// Check if the memory cache should be cleared and starts an online update of (file-) cached series information.
    /// </summary>
    private void CheckCacheAndRefresh()
    {
      if (DateTime.Now - _memoryCacheInvalidated <= _maxCacheDuration)
        return;
      _memoryCache.Clear();
      _memoryCacheEpisode.Clear();
      _memoryCacheInvalidated = DateTime.Now;

      RefreshCache();
    }

    protected virtual void RefreshCache()
    {
      // TODO: when updating movie information is implemented, start here a job to do it
    }

    #endregion

    #region FanArt

    public virtual List<string> GetFanArtFiles<T>(T infoObject, string scope, string type)
    {
      List<string> fanartFiles = new List<string>();
      string path = null;
      string id;
      if (scope == FanArtMediaTypes.Series)
      {
        SeriesInfo series = infoObject as SeriesInfo;
        if (series != null && GetSeriesId(series, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.SeriesSeason)
      {
        SeasonInfo season = infoObject as SeasonInfo;
        if (season != null && UseSeasonIdForFanArt && GetSeriesSeasonId(season, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
        if (season != null && !UseSeasonIdForFanArt && GetSeriesId(season.CloneBasicSeries(), out id) && season.SeasonNumber.HasValue)
        {
          path = Path.Combine(_cachePath, id, string.Format(@"S{0:00} {1}\{2}\", season.SeasonNumber, scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.Episode)
      {
        EpisodeInfo episode = infoObject as EpisodeInfo;
        if (episode != null && UseEpisodeIdForFanArt && GetSeriesEpisodeId(episode, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
        if (episode != null && !UseEpisodeIdForFanArt && GetSeriesId(episode.CloneBasicSeries(), out id) && episode.SeasonNumber.HasValue &&
          episode.EpisodeNumbers.Count > 0)
        {
          path = Path.Combine(_cachePath, id, string.Format(@"S{0:00}E{1:00} {2}\{3}\", episode.SeasonNumber.Value, episode.EpisodeNumbers[0], scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.Actor || scope == FanArtMediaTypes.Director || scope == FanArtMediaTypes.Writer)
      {
        PersonInfo person = infoObject as PersonInfo;
        if (person != null && GetPersonId(person, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.Character)
      {
        CharacterInfo character = infoObject as CharacterInfo;
        if (character != null && GetCharacterId(character, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      else if (scope == FanArtMediaTypes.Company)
      {
        CompanyInfo company = infoObject as CompanyInfo;
        if (company != null && GetCompanyId(company, out id))
        {
          path = Path.Combine(_cachePath, id, string.Format(@"{0}\{1}\", scope, type));
        }
      }
      if (Directory.Exists(path))
      {
        fanartFiles.AddRange(Directory.GetFiles(path, "*.jpg"));
        while (fanartFiles.Count > MAX_FANART_IMAGES)
        {
          fanartFiles.RemoveAt(fanartFiles.Count - 1);
        }
      }
      return fanartFiles;
    }

    protected override void DownloadFanArt(string downloadId)
    {
      try
      {
        if (string.IsNullOrEmpty(downloadId))
          return;

        string seriesId = null;
        string episodeId = null;
        int seasonNo = 0;
        int episodeNo = 0;

        string[] ids = null;
        if (downloadId.Contains("|"))
          ids = downloadId.Split('|');

        seriesId = downloadId;
        if (ids != null && ids.Length > 2)
        {
          seriesId = ids[0];
          int.TryParse(ids[1], out seasonNo);
          int.TryParse(ids[2], out episodeNo);
          episodeId = downloadId;
        }
        else if (ids != null && ids.Length > 1)
        {
          seriesId = ids[0];
          episodeId = ids[1];
        }

        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Started for ID {0}", downloadId);

        SeriesInfo seriesInfo;
        if (!_memoryCache.TryGetValue(seriesId, out seriesInfo))
          return;

        EpisodeInfo episodeInfo = null;
        if (episodeId != null)
          _memoryCacheEpisode.TryGetValue(episodeId, out episodeInfo);

        if (!Init())
          return;

        TLang language = FindBestMatchingLanguage(seriesInfo);
        ApiWrapperImageCollection<TImg> images;
        string scope = FanArtMediaTypes.Movie;
        if (_wrapper.GetFanArt(seriesInfo, language, scope, out images) == false)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Failed getting images for movie ID {0}", downloadId);

          return;
        }

        if(images != null)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading movie images for ID {0}", downloadId);

          SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
          SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
          SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
          SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
          SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
          SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
          SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
          SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
        }

        if (seasonNo > 0)
        {
          scope = FanArtMediaTypes.SeriesSeason;
          SeasonInfo seasonInfo = new SeasonInfo();
          seasonInfo.SeasonNumber = seasonNo;
          seasonInfo.CopyIdsFrom(seriesInfo);
          if (_wrapper.GetFanArt(seasonInfo, language, scope, out images) == false)
          {
            ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Failed getting season images for ID {0}", downloadId);

            return;
          }

          if (images != null)
          {
            ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading season images for ID {0}", downloadId);

            if (UseSeasonIdForFanArt)
            {
              SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
              SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
              SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
              SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
              SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
              SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
              SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
              SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
            }
            else
            {
              SaveSeriesSeasonFanArtImages(seriesId, seasonNo, images.Backdrops, scope, FanArtTypes.FanArt);
              SaveSeriesSeasonFanArtImages(seriesId, seasonNo, images.Posters, scope, FanArtTypes.Poster);
              SaveSeriesSeasonFanArtImages(seriesId, seasonNo, images.Banners, scope, FanArtTypes.Banner);
              SaveSeriesSeasonFanArtImages(seriesId, seasonNo, images.ClearArt, scope, FanArtTypes.ClearArt);
              SaveSeriesSeasonFanArtImages(seriesId, seasonNo, images.Covers, scope, FanArtTypes.Cover);
              SaveSeriesSeasonFanArtImages(seriesId, seasonNo, images.DiscArt, scope, FanArtTypes.DiscArt);
              SaveSeriesSeasonFanArtImages(seriesId, seasonNo, images.Logos, scope, FanArtTypes.Logo);
              SaveSeriesSeasonFanArtImages(seriesId, seasonNo, images.Thumbnails, scope, FanArtTypes.Thumbnail);
            }
          }
        }

        if (episodeInfo == null && seasonNo > 0 && episodeNo > 0)
        {
          episodeInfo = new EpisodeInfo();
          episodeInfo.SeasonNumber = seasonNo;
          episodeInfo.EpisodeNumbers.Add(episodeNo);
          episodeInfo.CopyIdsFrom(seriesInfo);
        }

        if (episodeInfo != null)
        {
          scope = FanArtMediaTypes.Episode;
          if (_wrapper.GetFanArt(episodeInfo, language, scope, out images) == false)
          {
            ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Failed getting episode images for ID {0}", downloadId);

            return;
          }

          if (images != null)
          {
            ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading episode images for ID {0}", downloadId);

            if (UseEpisodeIdForFanArt)
            {
              SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
              SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
              SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
              SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
              SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
              SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
              SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
              SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
            }
            else
            {
              SaveSeriesEpisodeFanArtImages(seriesId, seasonNo, episodeNo, images.Backdrops, scope, FanArtTypes.FanArt);
              SaveSeriesEpisodeFanArtImages(seriesId, seasonNo, episodeNo, images.Posters, scope, FanArtTypes.Poster);
              SaveSeriesEpisodeFanArtImages(seriesId, seasonNo, episodeNo, images.Banners, scope, FanArtTypes.Banner);
              SaveSeriesEpisodeFanArtImages(seriesId, seasonNo, episodeNo, images.ClearArt, scope, FanArtTypes.ClearArt);
              SaveSeriesEpisodeFanArtImages(seriesId, seasonNo, episodeNo, images.Covers, scope, FanArtTypes.Cover);
              SaveSeriesEpisodeFanArtImages(seriesId, seasonNo, episodeNo, images.DiscArt, scope, FanArtTypes.DiscArt);
              SaveSeriesEpisodeFanArtImages(seriesId, seasonNo, episodeNo, images.Logos, scope, FanArtTypes.Logo);
              SaveSeriesEpisodeFanArtImages(seriesId, seasonNo, episodeNo, images.Thumbnails, scope, FanArtTypes.Thumbnail);
            }
          }
        }

        scope = FanArtMediaTypes.Actor;
        List<PersonInfo> persons = seriesInfo.Actors;
        if(episodeInfo != null)
        {
          foreach (PersonInfo person in episodeInfo.Actors)
            if (!persons.Contains(person)) persons.Add(person);
        }
        if(persons != null && persons.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading actors images for ID {0}", downloadId);
          foreach (PersonInfo person in persons)
          {
            if (_wrapper.GetFanArt(person, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Director;
        persons = null;
        if (episodeInfo != null)
        {
          persons = episodeInfo.Directors;
        }
        if (persons != null && persons.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading director images for ID {0}", downloadId);
          foreach (PersonInfo person in persons)
          {
            if (_wrapper.GetFanArt(person, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Writer;
        persons = null;
        if (episodeInfo != null)
        {
          persons = episodeInfo.Writers;
        }
        if (persons != null && persons.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading writer images for ID {0}", downloadId);
          foreach (PersonInfo person in persons)
          {
            if (_wrapper.GetFanArt(person, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Character;
        List<CharacterInfo> characters = seriesInfo.Characters;
        if (episodeInfo != null)
        {
          foreach (CharacterInfo character in episodeInfo.Characters)
            if (!characters.Contains(character)) characters.Add(character);
        }
        if (characters != null && characters.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading character images for ID {0}", downloadId);
          foreach (CharacterInfo character in characters)
          {
            if (_wrapper.GetFanArt(character, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Company;
        List<CompanyInfo> companies = seriesInfo.ProductionCompanies;
        if (companies != null && companies.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading company images for ID {0}", downloadId);
          foreach (CompanyInfo company in companies)
          {
            if (_wrapper.GetFanArt(company, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        scope = FanArtMediaTypes.Network;
        List<CompanyInfo> networks = seriesInfo.Networks;
        if (companies != null && companies.Count > 0)
        {
          ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Downloading network images for ID {0}", downloadId);
          foreach (CompanyInfo company in companies)
          {
            if (_wrapper.GetFanArt(company, language, scope, out images) == false)
            {
              if (images != null)
              {
                SaveFanArtImages(images.Id, images.Backdrops, scope, FanArtTypes.FanArt);
                SaveFanArtImages(images.Id, images.Posters, scope, FanArtTypes.Poster);
                SaveFanArtImages(images.Id, images.Banners, scope, FanArtTypes.Banner);
                SaveFanArtImages(images.Id, images.ClearArt, scope, FanArtTypes.ClearArt);
                SaveFanArtImages(images.Id, images.Covers, scope, FanArtTypes.Cover);
                SaveFanArtImages(images.Id, images.DiscArt, scope, FanArtTypes.DiscArt);
                SaveFanArtImages(images.Id, images.Logos, scope, FanArtTypes.Logo);
                SaveFanArtImages(images.Id, images.Thumbnails, scope, FanArtTypes.Thumbnail);
              }
            }
          }
        }

        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Finished saving images for ID {0}", downloadId);

        // Remember we are finished
        FinishDownloadFanArt(downloadId);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + " Download: Exception downloading images for ID {0}", ex, downloadId);
      }
    }

    protected virtual bool VerifyFanArtImage(TImg image)
    {
      return image != null;
    }

    protected virtual int SaveFanArtImages(string id, IEnumerable<TImg> images, string scope, string type)
    {
      if (images == null)
        return 0;

      int idx = 0;
      foreach (TImg img in images)
      {
        if (!VerifyFanArtImage(img))
          continue;
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_wrapper.DownloadFanArt(id, img, scope, type))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + @" Download: Saved {0} {1}\{2}", idx, scope, type);
      return idx;
    }

    protected virtual int SaveSeriesSeasonFanArtImages(string id, int seasonNo, IEnumerable<TImg> images, string scope, string type)
    {
      if (images == null)
        return 0;

      int idx = 0;
      foreach (TImg img in images)
      {
        if (!VerifyFanArtImage(img))
          continue;
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_wrapper.DownloadSeriesSeasonFanArt(id, seasonNo, img, scope, type))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + @" Download: Saved {0} Season {1}: {2}\{3}", idx, seasonNo, scope, type);
      return idx;
    }

    protected virtual int SaveSeriesEpisodeFanArtImages(string id, int seasonNo, int episodeNo, IEnumerable<TImg> images, string scope, string type)
    {
      if (images == null)
        return 0;

      int idx = 0;
      foreach (TImg img in images)
      {
        if (!VerifyFanArtImage(img))
          continue;
        if (idx >= MAX_FANART_IMAGES)
          break;
        if (_wrapper.DownloadSeriesEpisodeFanArt(id, seasonNo, episodeNo, img, scope, type))
          idx++;
      }
      ServiceRegistration.Get<ILogger>().Debug(GetType().Name + @" Download: Saved {0} S{1}E{2}: {3}\{4}", idx, seasonNo, episodeNo, scope, type);
      return idx;
    }

    #endregion
  }
}
