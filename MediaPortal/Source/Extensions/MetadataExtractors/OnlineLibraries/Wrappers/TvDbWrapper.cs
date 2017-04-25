#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using MediaPortal.Common;
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.Common;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Cache;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvdbLib.Data.Banner;
using MediaPortal.Extensions.OnlineLibraries.Matchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class TvDbWrapper : ApiWrapper<TvdbBanner, TvdbLanguage>
  {
    protected TvdbHandler _tvdbHandler;
    private IdMapper _seriesToActorMap;

    /// <summary>
    /// Sets the preferred language in short format like: en, de, ...
    /// </summary>
    /// <param name="langShort">Short language</param>
    public void SetPreferredLanguage(string langShort)
    {
      TvdbLanguage language = _tvdbHandler.Languages.Find(l => l.Abbriviation == langShort);
      if (language != null)
        SetPreferredLanguage(language);
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath, bool useHttps)
    {
      ICacheProvider cacheProvider = new XmlCacheProvider(cachePath);
      _tvdbHandler = new TvdbHandler("9628A4332A8F3487", useHttps, cacheProvider);
      _tvdbHandler.InitCache();
      if (!_tvdbHandler.IsLanguagesCached)
        _tvdbHandler.ReloadLanguages();
      _tvdbHandler.UpdateFinished += TvdbHandlerOnUpdateFinished;
      _tvdbHandler.UpdateProgressed += TvdbHandlerOnUpdateProgressed;
      SetDefaultLanguage(TvdbLanguage.DefaultLanguage);
      SetCachePath(cachePath);

      _seriesToActorMap = new IdMapper(Path.Combine(cachePath, "SeriesToActorMap.xml"));
      return true;
    }

    private void TvdbHandlerOnUpdateFinished(TvdbHandler.UpdateFinishedEventArgs args)
    {
      FireCacheUpdateFinished(args.UpdateStarted, args.UpdateFinished, UpdateType.Series, args.UpdatedSeries.Select(i => i.ToString()).ToList());
      FireCacheUpdateFinished(args.UpdateStarted, args.UpdateFinished, UpdateType.Episode, args.UpdatedEpisodes.Select(i => i.ToString()).ToList());

      ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: Finished updating cache from {0} to {1}", args.UpdateStarted, args.UpdateFinished);
      ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: Updated {0} Series, {1} Episodes, {2} Banners.", args.UpdatedSeries.Count, args.UpdatedEpisodes.Count, args.UpdatedBanners.Count);
    }

    private void TvdbHandlerOnUpdateProgressed(TvdbHandler.UpdateProgressEventArgs args)
    {
      ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: ... {0} {2}. Total: {3}", args.CurrentUpdateStage, args.CurrentStageProgress, args.CurrentUpdateDescription, args.OverallProgress);
    }

    #region Search

    public override bool SearchSeriesEpisode(EpisodeInfo episodeSearch, TvdbLanguage language, out List<EpisodeInfo> episodes)
    {
      language = language ?? PreferredLanguage;

      episodes = null;
      SeriesInfo seriesSearch = episodeSearch.CloneBasicInstance<SeriesInfo>();
      if (episodeSearch.SeriesTvdbId <= 0)
      {
        if (!SearchSeriesUniqueAndUpdate(seriesSearch, language))
          return false;
        episodeSearch.CopyIdsFrom(seriesSearch);
      }

      if (episodeSearch.SeriesTvdbId > 0 && episodeSearch.SeasonNumber.HasValue)
      {
        TvdbSeries seriesDetail = _tvdbHandler.GetSeries(episodeSearch.SeriesTvdbId, language, true, false, false);

        foreach (TvdbEpisode episode in seriesDetail.Episodes.OrderByDescending(e => e.Id))
        {
          if ((episodeSearch.EpisodeNumbers.Contains(episode.EpisodeNumber) || episodeSearch.EpisodeNumbers.Count == 0) &&
            (episodeSearch.SeasonNumber == episode.SeasonNumber || episodeSearch.SeasonNumber.HasValue == false))
          {
            if (episodes == null)
              episodes = new List<EpisodeInfo>();

            EpisodeInfo info = new EpisodeInfo
            {
              TvdbId = episode.Id,
              SeriesName = new SimpleTitle(seriesDetail.SeriesName, false),
              SeasonNumber = episode.SeasonNumber,
              EpisodeName = new SimpleTitle(episode.EpisodeName, false),
            };
            info.EpisodeNumbers.Add(episode.EpisodeNumber);
            info.CopyIdsFrom(seriesSearch);
            info.Languages.Add(episode.Language.Abbriviation);
            if (!episodes.Contains(info))
              episodes.Add(info);
          }
        }
        if (episodes != null)
          episodes.Sort();
      }

      if (episodes == null)
      {
        episodes = new List<EpisodeInfo>();
        EpisodeInfo info = new EpisodeInfo
        {
          SeriesName = seriesSearch.SeriesName,
          SeasonNumber = episodeSearch.SeasonNumber,
          EpisodeName = episodeSearch.EpisodeName,
        };
        info.CopyIdsFrom(seriesSearch);
        info.EpisodeNumbers.AddRange(episodeSearch.EpisodeNumbers);
        info.Languages = seriesSearch.Languages;
        episodes.Add(info);
        return true;
      }

      return episodes != null;
    }

    public override bool SearchSeries(SeriesInfo seriesSearch, TvdbLanguage language, out List<SeriesInfo> series)
    {
      language = language ?? PreferredLanguage;

      series = null;
      List<TvdbSearchResult> foundSeries = _tvdbHandler.SearchSeries(seriesSearch.SeriesName.Text, language);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = _tvdbHandler.SearchSeries(seriesSearch.AlternateName, language);
      if (foundSeries == null) return false;
      series = new List<SeriesInfo>();
      foreach (TvdbSearchResult found in foundSeries)
      {
        bool addSeries = true;
        if (seriesSearch.SearchSeason.HasValue)
        {
          addSeries = false;
          TvdbSeries seriesDetail = _tvdbHandler.GetSeries(found.Id, language, true, false, false);
          if (seriesDetail.Episodes.Where(e => e.SeasonNumber == seriesSearch.SearchSeason).Count() > 0)
          {
            if (seriesSearch.SearchEpisode.HasValue)
            {
              if (seriesDetail.Episodes.Where(e => e.SeasonNumber == seriesSearch.SearchSeason &&
                e.EpisodeNumber == seriesSearch.SearchEpisode.Value).Count() > 0)
              {
                addSeries = true;
              }
            }
            else
            {
              addSeries = true;
            }
          }
        }
        if(addSeries)
        {
          series.Add(
              new SeriesInfo
              {
                TvdbId = found.Id,
                ImdbId = found.ImdbId,
                SeriesName = new SimpleTitle(found.SeriesName, false),
                FirstAired = found.FirstAired,
                Languages = new List<string>(new string[] { found.Language.Abbriviation })
              });
        }
      }
      return series.Count > 0;
    }

    #endregion

    #region Update

    public override bool UpdateFromOnlineSeries(SeriesInfo series, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (series.TvdbId > 0)
          seriesDetail = _tvdbHandler.GetSeries(series.TvdbId, language, true, true, false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(series.ImdbId))
        {
          TvdbSearchResult foundSeries = _tvdbHandler.GetSeriesByRemoteId(ExternalId.ImdbId, series.ImdbId);
          if (foundSeries != null)
          {
            seriesDetail = _tvdbHandler.GetSeries(foundSeries.Id, language, true, true, false);
          }
        }
        if (seriesDetail == null) return false;

        series.TvdbId = seriesDetail.Id;
        series.ImdbId = seriesDetail.ImdbId;

        series.SeriesName = new SimpleTitle(seriesDetail.SeriesName, false);
        series.FirstAired = seriesDetail.FirstAired;
        series.Description = new SimpleTitle(seriesDetail.Overview, false);
        series.Certification = seriesDetail.ContentRating;
        series.Rating = new SimpleRating(seriesDetail.Rating, seriesDetail.RatingCount);
        series.Genres = seriesDetail.Genre.Select(s => new GenreInfo { Name = s }).ToList();
        series.Networks = ConvertToCompanies(seriesDetail.NetworkID, seriesDetail.Network, CompanyAspect.COMPANY_TV_NETWORK);
        if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
          series.IsEnded = true;
        }

        series.Actors = ConvertToPersons(seriesDetail.TvdbActors, PersonAspect.OCCUPATION_ACTOR, null, seriesDetail.SeriesName);
        series.Characters = ConvertToCharacters(seriesDetail.TvdbActors, null, seriesDetail.SeriesName);

        foreach (TvdbActor actor in seriesDetail.TvdbActors)
        {
          _seriesToActorMap.StoreMappedId(actor.Id.ToString(), seriesDetail.Id.ToString());
        }

        foreach (TvdbEpisode episodeDetail in seriesDetail.Episodes.OrderByDescending(e => e.Id))
        {
          SeasonInfo seasonInfo = new SeasonInfo()
          {
            TvdbId = episodeDetail.SeasonId,

            SeriesTvdbId = seriesDetail.Id,
            SeriesImdbId = seriesDetail.ImdbId,
            SeriesName = new SimpleTitle(seriesDetail.SeriesName, false),
            SeasonNumber = episodeDetail.SeasonNumber,
          };
          if (!series.Seasons.Contains(seasonInfo))
            series.Seasons.Add(seasonInfo);

          EpisodeInfo episodeInfo = new EpisodeInfo()
          {
            TvdbId = episodeDetail.Id,

            SeriesTvdbId = seriesDetail.Id,
            SeriesImdbId = seriesDetail.ImdbId,
            SeriesName = new SimpleTitle(seriesDetail.SeriesName, false),
            SeriesFirstAired = seriesDetail.FirstAired,

            ImdbId = episodeDetail.ImdbId,
            SeasonNumber = episodeDetail.SeasonNumber,
            EpisodeNumbers = new List<int>(new int[] { episodeDetail.EpisodeNumber }),
            FirstAired = episodeDetail.FirstAired,
            EpisodeName = new SimpleTitle(episodeDetail.EpisodeName, false),
            Summary = new SimpleTitle(episodeDetail.Overview, false),
            Genres = seriesDetail.Genre.Select(s => new GenreInfo { Name = s }).ToList(),
            Rating = new SimpleRating(episodeDetail.Rating, episodeDetail.RatingCount),
          };

          if (episodeDetail.DvdEpisodeNumber > 0)
            episodeInfo.DvdEpisodeNumbers = new List<double>(new double[] { episodeDetail.DvdEpisodeNumber });

          episodeInfo.Actors = ConvertToPersons(seriesDetail.TvdbActors, PersonAspect.OCCUPATION_ACTOR, episodeDetail.EpisodeName, seriesDetail.SeriesName);
          //info.Actors.AddRange(ConvertToPersons(episodeDetail.GuestStars, PersonAspect.OCCUPATION_ACTOR, info.Actors.Count));
          episodeInfo.Characters = ConvertToCharacters(seriesDetail.TvdbActors, episodeDetail.EpisodeName, seriesDetail.SeriesName);
          episodeInfo.Directors = ConvertToPersons(episodeDetail.Directors, PersonAspect.OCCUPATION_DIRECTOR, 0, episodeDetail.EpisodeName, seriesDetail.SeriesName);
          episodeInfo.Writers = ConvertToPersons(episodeDetail.Writer, PersonAspect.OCCUPATION_WRITER, 0, episodeDetail.EpisodeName, seriesDetail.SeriesName);
          episodeInfo.Languages.Add(episodeDetail.Language.Abbriviation);

          if (!series.Episodes.Contains(episodeInfo))
            series.Episodes.Add(episodeInfo);
        }
        series.Episodes.Sort();
        series.TotalEpisodes = series.Episodes.Count;

        for (int index = 0; index < series.Seasons.Count; index++)
        {
          series.Seasons[index].FirstAired = series.Episodes.Find(e => e.SeasonNumber == series.Seasons[index].SeasonNumber).FirstAired;
          series.Seasons[index].TotalEpisodes = series.Episodes.FindAll(e => e.SeasonNumber == series.Seasons[index].SeasonNumber).Count;
        }
        series.Seasons.Sort();
        series.TotalSeasons = series.Seasons.Count;

        TvdbEpisode nextEpisode = seriesDetail.Episodes.Where(e => e.FirstAired > DateTime.Now).OrderBy(e => e.FirstAired)
          .ThenByDescending(p => p.Id).FirstOrDefault();
        if (nextEpisode != null)
        {
          series.NextEpisodeName = new SimpleTitle(nextEpisode.EpisodeName, false);
          series.NextEpisodeAirDate = nextEpisode.FirstAired;
          series.NextEpisodeSeasonNumber = nextEpisode.SeasonNumber;
          series.NextEpisodeNumber = nextEpisode.EpisodeNumber;
        }

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: Exception while processing series {0}", ex, series.ToString());
        return false;
      }
    }

    public override bool UpdateFromOnlineSeriesSeason(SeasonInfo season, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (season.SeriesTvdbId > 0)
          seriesDetail = _tvdbHandler.GetSeries(season.SeriesTvdbId, language, true, false, false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(season.SeriesImdbId))
        {
          TvdbSearchResult foundSeries = _tvdbHandler.GetSeriesByRemoteId(ExternalId.ImdbId, season.SeriesImdbId);
          if (foundSeries != null)
          {
            seriesDetail = _tvdbHandler.GetSeries(foundSeries.Id, language, true, false, false);
          }
        }
        if (seriesDetail == null) return false;
        if (!season.SeasonNumber.HasValue)
          return false;
        var episode = seriesDetail.Episodes.Where(e => e.SeasonNumber == season.SeasonNumber).ToList().FirstOrDefault();
        if (episode == null)
          return false;

        season.TvdbId = episode.SeasonId;
        season.SeriesTvdbId = seriesDetail.Id;
        season.SeriesImdbId = seriesDetail.ImdbId;
        season.FirstAired = episode.FirstAired;
        season.SeriesName = new SimpleTitle(seriesDetail.SeriesName, false);
        season.SeasonNumber = season.SeasonNumber.Value;
        season.Description = new SimpleTitle(seriesDetail.Overview, false);
        season.TotalEpisodes = seriesDetail.Episodes.FindAll(e => e.SeasonNumber == season.SeasonNumber).Count;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: Exception while processing season {0}", ex, season.ToString());
        return false;
      }
    }

    public override bool UpdateFromOnlineSeriesEpisode(EpisodeInfo episode, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        List<EpisodeInfo> episodeDetails = new List<EpisodeInfo>();
        TvdbSeries seriesDetail = null;
        TvdbEpisode episodeDetail = null;

        if (episode.SeriesTvdbId > 0 && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
        {
          seriesDetail = _tvdbHandler.GetSeries(episode.SeriesTvdbId, language, true, true, false);
          if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(episode.SeriesImdbId))
          {
            TvdbSearchResult foundSeries = _tvdbHandler.GetSeriesByRemoteId(ExternalId.ImdbId, episode.SeriesImdbId);
            if (foundSeries != null)
            {
              seriesDetail = _tvdbHandler.GetSeries(foundSeries.Id, language, true, true, false);
            }
          }
          if (seriesDetail == null) return false;

          foreach (int episodeNumber in episode.EpisodeNumbers)
          {
            episodeDetail = seriesDetail.Episodes.Where(e => e.EpisodeNumber == episodeNumber &&
            e.SeasonNumber == episode.SeasonNumber.Value).OrderByDescending(e => e.Id).FirstOrDefault();
            if (episodeDetail == null) continue;

            EpisodeInfo info = new EpisodeInfo()
            {
              TvdbId = episodeDetail.Id,

              SeriesTvdbId = seriesDetail.Id,
              SeriesImdbId = seriesDetail.ImdbId,
              SeriesName = new SimpleTitle(seriesDetail.SeriesName, false),
              SeriesFirstAired = seriesDetail.FirstAired,

              ImdbId = episodeDetail.ImdbId,
              SeasonNumber = episodeDetail.SeasonNumber,
              EpisodeNumbers = new List<int>(new int[] { episodeDetail.EpisodeNumber }),
              FirstAired = episodeDetail.FirstAired,
              EpisodeName = new SimpleTitle(episodeDetail.EpisodeName, false),
              Summary = new SimpleTitle(episodeDetail.Overview, false),
              Genres = seriesDetail.Genre.Select(s => new GenreInfo { Name = s }).ToList(),
              Rating = new SimpleRating(episodeDetail.Rating, episodeDetail.RatingCount),
            };

            if (episodeDetail.DvdEpisodeNumber > 0)
              info.DvdEpisodeNumbers = new List<double>(new double[] { episodeDetail.DvdEpisodeNumber });

            info.Actors = ConvertToPersons(seriesDetail.TvdbActors, PersonAspect.OCCUPATION_ACTOR, episodeDetail.EpisodeName, seriesDetail.SeriesName);
            //info.Actors.AddRange(ConvertToPersons(episodeDetail.GuestStars, PersonAspect.OCCUPATION_ACTOR, info.Actors.Count));
            info.Characters = ConvertToCharacters(seriesDetail.TvdbActors, episodeDetail.EpisodeName, seriesDetail.SeriesName);
            info.Directors = ConvertToPersons(episodeDetail.Directors, PersonAspect.OCCUPATION_DIRECTOR, 0, episodeDetail.EpisodeName, seriesDetail.SeriesName);
            info.Writers = ConvertToPersons(episodeDetail.Writer, PersonAspect.OCCUPATION_WRITER, 0, episodeDetail.EpisodeName, seriesDetail.SeriesName);
            info.Languages.Add(episodeDetail.Language.Abbriviation);

            episodeDetails.Add(info);
          }
        }

        if (episodeDetails.Count > 1)
        {
          SetMultiEpisodeDetails(episode, episodeDetails);
          return true;
        }
        else if (episodeDetails.Count > 0)
        {
          SetEpisodeDetails(episode, episodeDetails[0]);
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: Exception while processing episode {0}", ex, episode.ToString());
        return false;
      }
    }

    public override bool UpdateFromOnlineSeriesCharacter(SeriesInfo seriesInfo, CharacterInfo character, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (seriesInfo.TvdbId > 0)
          seriesDetail = _tvdbHandler.GetSeries(seriesInfo.TvdbId, language, true, false, false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(seriesInfo.ImdbId))
        {
          TvdbSearchResult foundSeries = _tvdbHandler.GetSeriesByRemoteId(ExternalId.ImdbId, seriesInfo.ImdbId);
          if (foundSeries != null)
          {
            seriesDetail = _tvdbHandler.GetSeries(foundSeries.Id, language, true, false, false);
          }
        }
        if (seriesDetail == null) return false;

        List<CharacterInfo> characters = ConvertToCharacters(seriesDetail.TvdbActors, null, seriesDetail.SeriesName);
        int index = characters.IndexOf(character);
        if (index >= 0)
        {
          character.ActorTvdbId = characters[index].ActorTvdbId;
          character.ActorName = characters[index].ActorName;
          character.Name = characters[index].Name;
          character.Order = characters[index].Order;
          character.ParentMediaName = seriesDetail.SeriesName;

          return true;
        }

        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: Exception while processing character {0}", ex, character.ToString());
        return false;
      }
    }

    public override bool UpdateFromOnlineSeriesEpisodeCharacter(EpisodeInfo episodeInfo, CharacterInfo character, TvdbLanguage language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesCharacter(episodeInfo.CloneBasicInstance<SeriesInfo>(), character, language, cacheOnly);
    }

    public override bool UpdateFromOnlineSeriesPerson(SeriesInfo seriesInfo, PersonInfo person, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        if (person.Occupation != PersonAspect.OCCUPATION_ACTOR)
          return false;

        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (seriesInfo.TvdbId > 0)
          seriesDetail = _tvdbHandler.GetSeries(seriesInfo.TvdbId, language, true, false, false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(seriesInfo.ImdbId))
        {
          TvdbSearchResult foundSeries = _tvdbHandler.GetSeriesByRemoteId(ExternalId.ImdbId, seriesInfo.ImdbId);
          if (foundSeries != null)
          {
            seriesDetail = _tvdbHandler.GetSeries(foundSeries.Id, language, true, false, false);
          }
        }
        if (seriesDetail == null) return false;

        List<PersonInfo> actors = ConvertToPersons(seriesDetail.TvdbActors, PersonAspect.OCCUPATION_ACTOR, null, seriesDetail.SeriesName);
        int index = actors.IndexOf(person);
        if (index >= 0)
        {
          person.TvdbId = actors[index].TvdbId;
          person.Name = actors[index].Name;
          person.Occupation = actors[index].Occupation;
          person.Order = actors[index].Order;
          person.ParentMediaName = seriesDetail.SeriesName;

          return true;
        }

        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: Exception while processing person {0}", ex, person.ToString());
        return false;
      }
    }

    public override bool UpdateFromOnlineSeriesEpisodePerson(EpisodeInfo episodeInfo, PersonInfo person, TvdbLanguage language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesPerson(episodeInfo.CloneBasicInstance<SeriesInfo>(), person, language, cacheOnly);
    }

    public override bool UpdateFromOnlineSeriesCompany(SeriesInfo seriesInfo, CompanyInfo company, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        if (company.Type != CompanyAspect.COMPANY_TV_NETWORK)
          return false;

        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (seriesInfo.TvdbId > 0)
          seriesDetail = _tvdbHandler.GetSeries(seriesInfo.TvdbId, language, true, false, false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(seriesInfo.ImdbId))
        {
          TvdbSearchResult foundSeries = _tvdbHandler.GetSeriesByRemoteId(ExternalId.ImdbId, seriesInfo.ImdbId);
          if (foundSeries != null)
          {
            seriesDetail = _tvdbHandler.GetSeries(foundSeries.Id, language, true, false, false);
          }
        }
        if (seriesDetail == null) return false;

        List<CompanyInfo> companies = ConvertToCompanies(seriesDetail.NetworkID, seriesDetail.Network, CompanyAspect.COMPANY_TV_NETWORK);
        int index = companies.IndexOf(company);
        if (index >= 0)
        {
          company.TvdbId = companies[index].TvdbId;
          company.Name = companies[index].Name;
          company.Type = companies[index].Type;
          company.Order = companies[index].Order;

          return true;
        }

        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvDbWrapper: Exception while processing company {0}", ex, company.ToString());
        return false;
      }
    }

    #endregion

    #region Convert

    private List<PersonInfo> ConvertToPersons(List<TvdbActor> actors, string occupation, string episode, string series)
    {
      if (actors == null || actors.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = 0;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (TvdbActor person in actors)
        retValue.Add(new PersonInfo() { TvdbId = person.Id, Name = person.Name, Occupation = occupation, Order = sortOrder++, MediaName = episode, ParentMediaName = series });
      return retValue;
    }

    private List<PersonInfo> ConvertToPersons(List<string> actors, string occupation, int offset, string episode, string series)
    {
      if (actors == null || actors.Count == 0)
        return new List<PersonInfo>();

      int sortOrder = offset;
      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (string person in actors)
        retValue.Add(new PersonInfo() { Name = person, Occupation = occupation, Order = sortOrder++, MediaName = episode, ParentMediaName = series });
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

    private List<CharacterInfo> ConvertToCharacters(List<TvdbActor> actors, string episode, string series)
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
          Order = sortOrder++,
          MediaName = episode,
          ParentMediaName = series
        });
      return retValue;
    }

    #endregion

    #region FanArt

    public override bool GetFanArt<T>(T infoObject, TvdbLanguage language, string fanartMediaType, out ApiWrapperImageCollection<TvdbBanner> images)
    {
      images = new ApiWrapperImageCollection<TvdbBanner>();

      try
      {
        try
        {
          TvdbSeries seriesDetail = null;
          language = language ?? PreferredLanguage;

          if (fanartMediaType == FanArtMediaTypes.Series)
          {
            EpisodeInfo episode = infoObject as EpisodeInfo;
            SeasonInfo season = infoObject as SeasonInfo;
            SeriesInfo series = infoObject as SeriesInfo;
            if (series == null && season != null)
            {
              series = season.CloneBasicInstance<SeriesInfo>();
            }
            if (series == null && episode != null)
            {
              series = episode.CloneBasicInstance<SeriesInfo>();
            }
            if (series != null && series.TvdbId > 0)
            {
              seriesDetail = _tvdbHandler.GetSeries(series.TvdbId, language, false, true, true);

              if (seriesDetail != null)
              {
                images.Id = series.TvdbId.ToString();
                images.Posters.AddRange(seriesDetail.PosterBanners.OrderBy(b => b.Language != language));
                images.Banners.AddRange(seriesDetail.SeriesBanners.OrderBy(b => b.Language != language));
                images.Backdrops.AddRange(seriesDetail.FanartBanners.OrderBy(b => b.Language != language));
                return true;
              }
            }
          }
          else if (fanartMediaType == FanArtMediaTypes.SeriesSeason)
          {
            EpisodeInfo episode = infoObject as EpisodeInfo;
            SeasonInfo season = infoObject as SeasonInfo;
            if (season == null && episode != null)
            {
              season = episode.CloneBasicInstance<SeasonInfo>();
            }
            if (season != null && season.SeriesTvdbId > 0 && season.SeasonNumber.HasValue)
            {
              seriesDetail = _tvdbHandler.GetSeries(season.SeriesTvdbId, language, false, false, true);

              if (seriesDetail != null)
              {
                images.Id = season.TvdbId.ToString();

                var seasonLookup = seriesDetail.SeasonBanners.Where(s => s.Season == season.SeasonNumber).ToLookup(s => string.Format("{0}_{1}", s.Season, s.BannerType), v => v);
                foreach (IGrouping<string, TvdbSeasonBanner> tvdbSeasonBanners in seasonLookup)
                {
                  images.Banners.AddRange(seasonLookup[tvdbSeasonBanners.Key].OrderBy(b => b.Language != language));
                }
                return true;
              }
            }
          }
          else if (fanartMediaType == FanArtMediaTypes.Episode)
          {
            EpisodeInfo episode = infoObject as EpisodeInfo;
            if (episode != null && episode.SeriesTvdbId > 0 && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
            {
              seriesDetail = _tvdbHandler.GetSeries(episode.SeriesTvdbId, language, true, false, true);

              if (seriesDetail != null)
              {
                images.Id = episode.TvdbId.ToString();

                TvdbEpisode episodeDetail = seriesDetail.Episodes.Find(e => e.SeasonNumber == episode.SeasonNumber.Value && e.EpisodeNumber == episode.EpisodeNumbers[0]);
                if (episodeDetail != null)
                  images.Thumbnails.AddRange(new TvdbBanner[] { episodeDetail.Banner });
                return true;
              }
            }
          }
          else if (fanartMediaType == FanArtMediaTypes.Actor)
          {
            PersonInfo person = infoObject as PersonInfo;
            string seriesId = null;
            _seriesToActorMap.GetMappedId(person.TvdbId.ToString(), out seriesId);
            int seriesTvdbId = 0;
            if (int.TryParse(seriesId, out seriesTvdbId))
            {
              seriesDetail = _tvdbHandler.GetSeries(seriesTvdbId, language, false, true, true);
              if (seriesDetail != null)
              {
                foreach (TvdbActor actor in seriesDetail.TvdbActors)
                {
                  if (actor.Id == person.TvdbId)
                  {
                    images.Id = actor.Id.ToString();
                    images.Thumbnails.AddRange(new TvdbBanner[] { actor.ActorImage });
                    return true;
                  }
                }
              }
            }
          }
          else
          {
            return true;
          }
        }
        catch (Exception ex)
        {
          ServiceRegistration.Get<ILogger>().Error("TvDbWrapper: Error getting fan art for scope {0}", ex, fanartMediaType);
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception downloading images", ex);
      }
      return false;
    }

    public override bool DownloadFanArt(string id, TvdbBanner image, string folderPath)
    {
      image.CachePath = folderPath;
      image.LoadBanner();
      return image.UnloadBanner(true);
    }

    #endregion

    #region Cache

    /// <summary>
    /// Updates the local available information with updated ones from online source.
    /// </summary>
    /// <returns></returns>
    public override bool RefreshCache(DateTime lastRefresh)
    {
      try
      {
        return _tvdbHandler.UpdateAllSeries(true);
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("TvDbWrapper: Error updating cache", ex);
        return false;
      }
    }

    #endregion
  }
}
