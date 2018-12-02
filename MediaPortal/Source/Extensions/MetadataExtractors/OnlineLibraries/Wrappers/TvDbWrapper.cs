#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using MediaPortal.Common.Certifications;
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
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
    public async Task SetPreferredLanguageAsync(string langShort)
    {
      TvdbLanguage language = (await _tvdbHandler.GetLanguagesAsync().ConfigureAwait(false)).Find(l => l.Abbriviation == langShort);
      if (language != null)
        SetPreferredLanguage(language);
    }

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> InitAsync(string cachePath, bool useHttps)
    {
      ICacheProvider cacheProvider = new XmlCacheProvider(cachePath);
      _tvdbHandler = new TvdbHandler("9628A4332A8F3487", useHttps, cacheProvider);
      _tvdbHandler.InitCache();
      if (!_tvdbHandler.IsLanguagesCached)
        await _tvdbHandler.ReloadLanguagesAsync().ConfigureAwait(false);
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

    public override async Task<List<EpisodeInfo>> SearchSeriesEpisodeAsync(EpisodeInfo episodeSearch, TvdbLanguage language)
    {
      language = language ?? PreferredLanguage;
      
      SeriesInfo seriesSearch = episodeSearch.CloneBasicInstance<SeriesInfo>();
      if (episodeSearch.SeriesTvdbId <= 0 && string.IsNullOrEmpty(episodeSearch.SeriesImdbId))
      {
        if (!await SearchSeriesUniqueAndUpdateAsync(seriesSearch, language).ConfigureAwait(false))
          return null;
        episodeSearch.CopyIdsFrom(seriesSearch);
      }

      List<EpisodeInfo> episodes = null;
      if ((episodeSearch.SeriesTvdbId > 0 || !string.IsNullOrEmpty(episodeSearch.SeriesImdbId)) && episodeSearch.SeasonNumber.HasValue)
      {
        int seriesId = 0;
        if (episodeSearch.SeriesTvdbId > 0)
          seriesId = episodeSearch.SeriesTvdbId;
        else if (!string.IsNullOrEmpty(episodeSearch.SeriesImdbId))
        {
          TvdbSearchResult searchResult = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, episodeSearch.SeriesImdbId);
          if (searchResult?.Id > 0)
            seriesId = searchResult.Id;
        }
        TvdbSeries seriesDetail = await _tvdbHandler.GetSeriesAsync(seriesId, language, true, false, false).ConfigureAwait(false);
        if (seriesDetail == null)
          return null;

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
        info.EpisodeNumbers = info.EpisodeNumbers.Union(episodeSearch.EpisodeNumbers).ToList();
        info.Languages = seriesSearch.Languages;
        episodes.Add(info);
      }

      return episodes;
    }

    public override async Task<List<SeriesInfo>> SearchSeriesAsync(SeriesInfo seriesSearch, TvdbLanguage language)
    {
      language = language ?? PreferredLanguage;
      
      List<TvdbSearchResult> foundSeries = await _tvdbHandler.SearchSeriesAsync(seriesSearch.SeriesName.Text, language).ConfigureAwait(false);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = await _tvdbHandler.SearchSeriesAsync(seriesSearch.AlternateName, language).ConfigureAwait(false);
      if (foundSeries == null) return null;
      List<SeriesInfo> series = new List<SeriesInfo>();
      foreach (TvdbSearchResult found in foundSeries)
      {
        bool addSeries = true;
        if (seriesSearch.SearchSeason.HasValue)
        {
          addSeries = false;
          TvdbSeries seriesDetail = await _tvdbHandler.GetSeriesAsync(found.Id, language, true, false, false).ConfigureAwait(false);
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
      return series;
    }

    #endregion

    #region Update

    public override async Task<bool> UpdateFromOnlineSeriesAsync(SeriesInfo series, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (series.TvdbId > 0)
          seriesDetail = await _tvdbHandler.GetSeriesAsync(series.TvdbId, language, true, true, false).ConfigureAwait(false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(series.ImdbId))
        {
          TvdbSearchResult foundSeries = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, series.ImdbId).ConfigureAwait(false);
          if (foundSeries != null)
          {
            seriesDetail = await _tvdbHandler.GetSeriesAsync(foundSeries.Id, language, true, true, false).ConfigureAwait(false);
          }
        }
        if (seriesDetail == null) return false;

        series.TvdbId = seriesDetail.Id;
        series.ImdbId = seriesDetail.ImdbId;

        series.SeriesName = new SimpleTitle(seriesDetail.SeriesName, false);
        series.FirstAired = seriesDetail.FirstAired;
        series.Description = new SimpleTitle(seriesDetail.Overview, false);
        series.Rating = new SimpleRating(seriesDetail.Rating, seriesDetail.RatingCount);
        series.Genres = seriesDetail.Genre.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList();
        series.Networks = ConvertToCompanies(seriesDetail.NetworkID, seriesDetail.Network, CompanyAspect.COMPANY_TV_NETWORK);
        if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
          series.IsEnded = true;
        }

        CertificationMapping certification = null;
        if (CertificationMapper.TryFindMovieCertification(seriesDetail.ContentRating, out certification))
        {
          series.Certification = certification.CertificationId;
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
            Genres = seriesDetail.Genre.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList(),
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

    public override async Task<bool> UpdateFromOnlineSeriesSeasonAsync(SeasonInfo season, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (season.SeriesTvdbId > 0)
          seriesDetail = await _tvdbHandler.GetSeriesAsync(season.SeriesTvdbId, language, true, false, false).ConfigureAwait(false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(season.SeriesImdbId))
        {
          TvdbSearchResult foundSeries = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, season.SeriesImdbId).ConfigureAwait(false);
          if (foundSeries != null)
          {
            seriesDetail = await _tvdbHandler.GetSeriesAsync(foundSeries.Id, language, true, false, false).ConfigureAwait(false);
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

    public override async Task<bool> UpdateFromOnlineSeriesEpisodeAsync(EpisodeInfo episode, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        List<EpisodeInfo> episodeDetails = new List<EpisodeInfo>();
        TvdbSeries seriesDetail = null;
        TvdbEpisode episodeDetail = null;

        if ((episode.SeriesTvdbId > 0 || !string.IsNullOrEmpty(episode.SeriesImdbId)) && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
        {
          if (episode.SeriesTvdbId > 0)
            seriesDetail = await _tvdbHandler.GetSeriesAsync(episode.SeriesTvdbId, language, true, true, true).ConfigureAwait(false);
          if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(episode.SeriesImdbId))
          {
            TvdbSearchResult foundSeries = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, episode.SeriesImdbId).ConfigureAwait(false);
            if (foundSeries != null)
            {
              seriesDetail = await _tvdbHandler.GetSeriesAsync(foundSeries.Id, language, true, true, true).ConfigureAwait(false);
            }
          }
          if (seriesDetail == null) return false;

          bool isFirstEpisode = true;
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
              Genres = seriesDetail.Genre.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList(),
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

            if (isFirstEpisode && !episode.HasThumbnail && episodeDetail.Banner != null)
              info.Thumbnail = await episodeDetail.Banner.LoadImageDataAsync().ConfigureAwait(false);
            
            episodeDetails.Add(info);
            isFirstEpisode = false;
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

    public override async Task<bool> UpdateFromOnlineSeriesCharacterAsync(SeriesInfo seriesInfo, CharacterInfo character, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (seriesInfo.TvdbId > 0)
          seriesDetail = await _tvdbHandler.GetSeriesAsync(seriesInfo.TvdbId, language, true, true, false).ConfigureAwait(false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(seriesInfo.ImdbId))
        {
          TvdbSearchResult foundSeries = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, seriesInfo.ImdbId).ConfigureAwait(false);
          if (foundSeries != null)
          {
            seriesDetail = await _tvdbHandler.GetSeriesAsync(foundSeries.Id, language, true, true, false).ConfigureAwait(false);
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

    public override Task<bool> UpdateFromOnlineSeriesEpisodeCharacterAsync(EpisodeInfo episodeInfo, CharacterInfo character, TvdbLanguage language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesCharacterAsync(episodeInfo.CloneBasicInstance<SeriesInfo>(), character, language, cacheOnly);
    }

    public override async Task<bool> UpdateFromOnlineSeriesPersonAsync(SeriesInfo seriesInfo, PersonInfo person, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        if (person.Occupation != PersonAspect.OCCUPATION_ACTOR)
          return false;

        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (seriesInfo.TvdbId > 0)
          seriesDetail = await _tvdbHandler.GetSeriesAsync(seriesInfo.TvdbId, language, true, true, false).ConfigureAwait(false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(seriesInfo.ImdbId))
        {
          TvdbSearchResult foundSeries = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, seriesInfo.ImdbId).ConfigureAwait(false);
          if (foundSeries != null)
          {
            seriesDetail = await _tvdbHandler.GetSeriesAsync(foundSeries.Id, language, true, true, false).ConfigureAwait(false);
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

    public override Task<bool> UpdateFromOnlineSeriesEpisodePersonAsync(EpisodeInfo episodeInfo, PersonInfo person, TvdbLanguage language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesPersonAsync(episodeInfo.CloneBasicInstance<SeriesInfo>(), person, language, cacheOnly);
    }

    public override async Task<bool> UpdateFromOnlineSeriesCompanyAsync(SeriesInfo seriesInfo, CompanyInfo company, TvdbLanguage language, bool cacheOnly)
    {
      try
      {
        if (company.Type != CompanyAspect.COMPANY_TV_NETWORK)
          return false;

        language = language ?? PreferredLanguage;

        TvdbSeries seriesDetail = null;
        if (seriesInfo.TvdbId > 0)
          seriesDetail = await _tvdbHandler.GetSeriesAsync(seriesInfo.TvdbId, language, true, false, false).ConfigureAwait(false);
        if (seriesDetail == null && !cacheOnly && !string.IsNullOrEmpty(seriesInfo.ImdbId))
        {
          TvdbSearchResult foundSeries = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, seriesInfo.ImdbId).ConfigureAwait(false);
          if (foundSeries != null)
          {
            seriesDetail = await _tvdbHandler.GetSeriesAsync(foundSeries.Id, language, true, false, false).ConfigureAwait(false);
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

    public override Task<ApiWrapperImageCollection<TvdbBanner>> GetFanArtAsync<T>(T infoObject, TvdbLanguage language, string fanartMediaType)
    {
      language = language ?? PreferredLanguage;
      if (fanartMediaType == FanArtMediaTypes.Series)
        return GetSeriesFanArtAsync(infoObject.AsSeries(), language);
      if (fanartMediaType == FanArtMediaTypes.SeriesSeason)
        return GetSeasonFanArtAsync(infoObject.AsSeason(), language);
      if (fanartMediaType == FanArtMediaTypes.Episode)
        return GetEpisodeFanArtAsync(infoObject as EpisodeInfo, language);
      if (fanartMediaType == FanArtMediaTypes.Actor)
        return GetActorFanArtAsync(infoObject as PersonInfo, language);
      return Task.FromResult<ApiWrapperImageCollection<TvdbBanner>>(null);
    }

    public override async Task<bool> DownloadFanArtAsync(string id, TvdbBanner image, string folderPath)
    {
      bool result = await image.LoadBannerAsync(true, folderPath).ConfigureAwait(false);
      image.UnloadBanner(true);
      return result;
    }

    protected async Task<ApiWrapperImageCollection<TvdbBanner>> GetSeriesFanArtAsync(SeriesInfo series, TvdbLanguage language)
    {
      if (series == null || series.TvdbId < 1)
        return null;
      TvdbSeries seriesDetail = await _tvdbHandler.GetSeriesAsync(series.TvdbId, language, false, false, true).ConfigureAwait(false);
      if (seriesDetail == null)
        return null;
      ApiWrapperImageCollection<TvdbBanner> images = new ApiWrapperImageCollection<TvdbBanner>();
      images.Id = series.TvdbId.ToString();
      images.Posters.AddRange(seriesDetail.PosterBanners.OrderBy(b => b.Language != language));
      images.Banners.AddRange(seriesDetail.SeriesBanners.OrderBy(b => b.Language != language));
      images.Backdrops.AddRange(seriesDetail.FanartBanners.OrderBy(b => b.Language != language));
      return images;
    }

    protected async Task<ApiWrapperImageCollection<TvdbBanner>> GetSeasonFanArtAsync(SeasonInfo season, TvdbLanguage language)
    {
      if (season == null || (season.SeriesTvdbId < 1 && string.IsNullOrEmpty(season.SeriesImdbId)) || !season.SeasonNumber.HasValue)
        return null;
      TvdbSeries seriesDetail = null;
      if (season.SeriesTvdbId > 0)
        seriesDetail = await _tvdbHandler.GetSeriesAsync(season.SeriesTvdbId, language, false, false, true).ConfigureAwait(false);
      if (seriesDetail == null && !string.IsNullOrEmpty(season.SeriesImdbId))
      {
        TvdbSearchResult foundSeries = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, season.SeriesImdbId).ConfigureAwait(false);
        if (foundSeries != null)
        {
          seriesDetail = await _tvdbHandler.GetSeriesAsync(foundSeries.Id, language, false, false, true).ConfigureAwait(false);
        }
      }
      if (seriesDetail == null)
        return null;
      ApiWrapperImageCollection<TvdbBanner> images = new ApiWrapperImageCollection<TvdbBanner>();
      images.Id = season.TvdbId.ToString();
      var seasonLookup = seriesDetail.SeasonBanners.Where(s => s.Season == season.SeasonNumber).ToLookup(s => string.Format("{0}_{1}", s.Season, s.BannerType), v => v);
      foreach (IGrouping<string, TvdbSeasonBanner> tvdbSeasonBanners in seasonLookup)
      {
        images.Banners.AddRange(seasonLookup[tvdbSeasonBanners.Key].Where(b => b.BannerPath.Contains("wide")).OrderBy(b => b.Language != language));
        images.Posters.AddRange(seasonLookup[tvdbSeasonBanners.Key].Where(b => !b.BannerPath.Contains("wide")).OrderBy(b => b.Language != language));
      }
      return images;
    }

    protected async Task<ApiWrapperImageCollection<TvdbBanner>> GetEpisodeFanArtAsync(EpisodeInfo episode, TvdbLanguage language)
    {
      if (episode == null || (episode.SeriesTvdbId < 1 && string.IsNullOrEmpty(episode.SeriesImdbId)) || !episode.SeasonNumber.HasValue || episode.EpisodeNumbers.Count == 0)
        return null;
      TvdbSeries seriesDetail = null;
      if (episode.SeriesTvdbId > 0)
        seriesDetail = await _tvdbHandler.GetSeriesAsync(episode.SeriesTvdbId, language, true, false, true).ConfigureAwait(false);
      if (seriesDetail == null && !string.IsNullOrEmpty(episode.SeriesImdbId))
      {
        TvdbSearchResult foundSeries = await _tvdbHandler.GetSeriesByRemoteIdAsync(ExternalId.ImdbId, episode.SeriesImdbId).ConfigureAwait(false);
        if (foundSeries != null)
        {
          seriesDetail = await _tvdbHandler.GetSeriesAsync(foundSeries.Id, language, true, false, true).ConfigureAwait(false);
        }
      }
      if (seriesDetail == null)
        return null;
      TvdbEpisode episodeDetail = seriesDetail.Episodes.Find(e => e.SeasonNumber == episode.SeasonNumber.Value && e.EpisodeNumber == episode.FirstEpisodeNumber);
      if (episodeDetail == null)
        return null;
      ApiWrapperImageCollection<TvdbBanner> images = new ApiWrapperImageCollection<TvdbBanner>();
      images.Id = episode.TvdbId.ToString();
      images.Thumbnails.AddRange(new TvdbBanner[] { episodeDetail.Banner });
      return images;
    }

    protected async Task<ApiWrapperImageCollection<TvdbBanner>> GetActorFanArtAsync(PersonInfo person, TvdbLanguage language)
    {
      if (person == null || person.TvdbId < 1)
        return null;
      int seriesTvdbId;
      if (!_seriesToActorMap.GetMappedId(person.TvdbId.ToString(), out string seriesId) || !int.TryParse(seriesId, out seriesTvdbId))
        return null;
      TvdbSeries seriesDetail = await _tvdbHandler.GetSeriesAsync(seriesTvdbId, language, false, true, true).ConfigureAwait(false);
      if (seriesDetail == null)
        return null;
      TvdbActor actor = seriesDetail.TvdbActors.FirstOrDefault(a => a.Id == person.TvdbId);
      if (actor == null)
        return null;
      ApiWrapperImageCollection<TvdbBanner> images = new ApiWrapperImageCollection<TvdbBanner>();
      images.Id = actor.Id.ToString();
      images.Thumbnails.AddRange(new TvdbBanner[] { actor.ActorImage });
      return images;
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
        return _tvdbHandler.UpdateAllSeriesAsync(true).Result;
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
