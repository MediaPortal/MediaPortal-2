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
using MediaPortal.Common.FanArt;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data;
using MediaPortal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediaPortal.Extensions.OnlineLibraries.Wrappers
{
  class TvMazeWrapper : ApiWrapper<TvMazeImageCollection, string>
  {
    protected TvMazeApiV1 _tvMazeHandler;
    protected TimeSpan _cacheTimeout = TimeSpan.FromHours(12);

    /// <summary>
    /// Initializes the library. Needs to be called at first.
    /// </summary>
    /// <returns></returns>
    public bool Init(string cachePath)
    {
      _tvMazeHandler = new TvMazeApiV1(cachePath);
      SetDefaultLanguage(TvMazeApiV1.DefaultLanguage);
      SetCachePath(cachePath);
      return true;
    }

    #region Search

    public override async Task<List<EpisodeInfo>> SearchSeriesEpisodeAsync(EpisodeInfo episodeSearch, string language)
    {
      SeriesInfo seriesSearch = episodeSearch.CloneBasicInstance<SeriesInfo>();
      if (episodeSearch.SeriesTvMazeId <= 0)
      {
        if (!await SearchSeriesUniqueAndUpdateAsync(seriesSearch, language).ConfigureAwait(false))
          return null;
        episodeSearch.CopyIdsFrom(seriesSearch);
      }

      List<EpisodeInfo> episodes = null;
      if (episodeSearch.SeriesTvMazeId > 0 && episodeSearch.SeasonNumber.HasValue)
      {
        TvMazeSeries seriesDetail = await _tvMazeHandler.GetSeriesAsync(episodeSearch.SeriesTvMazeId, false).ConfigureAwait(false);
        List<TvMazeEpisode> seasonEpisodes = null;
        if(seriesDetail.Embedded != null && seriesDetail.Embedded.Episodes != null)
          seasonEpisodes = seriesDetail.Embedded.Episodes.Where(s => s.SeasonNumber == episodeSearch.SeasonNumber.Value).ToList();

        foreach (TvMazeEpisode episode in seasonEpisodes)
        {
          if (episodeSearch.EpisodeNumbers.Contains(episode.EpisodeNumber) || episodeSearch.EpisodeNumbers.Count == 0)
          {
            if (episodes == null)
              episodes = new List<EpisodeInfo>();

            EpisodeInfo info = new EpisodeInfo()
            {
              TvMazeId = episode.Id,
              SeriesName = seriesSearch.SeriesName,
              SeasonNumber = episode.SeasonNumber,
              EpisodeName = new SimpleTitle(episode.Name, true),
            };
            info.EpisodeNumbers.Add(episode.EpisodeNumber);
            info.CopyIdsFrom(seriesSearch);
            episodes.Add(info);
          }
        }
      }

      if (episodes == null)
      {
        episodes = new List<EpisodeInfo>();
        EpisodeInfo info = new EpisodeInfo()
        {
          SeriesName = seriesSearch.SeriesName,
          SeasonNumber = episodeSearch.SeasonNumber,
          EpisodeName = episodeSearch.EpisodeName,
        };
        info.CopyIdsFrom(seriesSearch);
        info.EpisodeNumbers = info.EpisodeNumbers.Union(episodeSearch.EpisodeNumbers).ToList();
        episodes.Add(info);
      }

      return episodes;
    }

    public override async Task<List<SeriesInfo>> SearchSeriesAsync(SeriesInfo seriesSearch, string language)
    {
      List<TvMazeSeries> foundSeries = await _tvMazeHandler.SearchSeriesAsync(seriesSearch.SeriesName.Text).ConfigureAwait(false);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.OriginalName))
        foundSeries = await _tvMazeHandler.SearchSeriesAsync(seriesSearch.OriginalName).ConfigureAwait(false);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = await _tvMazeHandler.SearchSeriesAsync(seriesSearch.AlternateName).ConfigureAwait(false);
      if (foundSeries == null) return null;
      return foundSeries.Select(s => new SeriesInfo()
      {
        TvMazeId = s.Id,
        ImdbId = s.Externals.ImDbId,
        TvdbId = s.Externals.TvDbId ?? 0,
        TvRageId = s.Externals.TvRageId ?? 0,
        SeriesName = new SimpleTitle(s.Name, true),
        FirstAired = s.Premiered,
      }).ToList();
    }

    public override async Task<List<PersonInfo>> SearchPersonAsync(PersonInfo personSearch, string language)
    {
      List<TvMazePerson> foundPersons = await _tvMazeHandler.SearchPersonAsync(personSearch.Name).ConfigureAwait(false);
      if (foundPersons == null) return null;
      return foundPersons.Select(p => new PersonInfo()
      {
        TvMazeId = p.Id,
        Name = p.Name,
      }).ToList();
    }

    #endregion

    #region Update

    public override async Task<bool> UpdateFromOnlineSeriesAsync(SeriesInfo series, string language, bool cacheOnly)
    {
      try
      {
        TvMazeSeries seriesDetail = null;
        if (series.TvMazeId > 0)
          seriesDetail = await _tvMazeHandler.GetSeriesAsync(series.TvMazeId, cacheOnly).ConfigureAwait(false);
        if (seriesDetail == null && !string.IsNullOrEmpty(series.ImdbId))
          seriesDetail = await _tvMazeHandler.GetSeriesByImDbAsync(series.ImdbId, cacheOnly).ConfigureAwait(false);
        if (seriesDetail == null && series.TvdbId > 0)
          seriesDetail = await _tvMazeHandler.GetSeriesByTvDbAsync(series.TvdbId, cacheOnly).ConfigureAwait(false);
        if (seriesDetail == null) return false;

        series.TvMazeId = seriesDetail.Id;
        series.TvdbId = seriesDetail.Externals.TvDbId ?? 0;
        series.TvRageId = seriesDetail.Externals.TvRageId ?? 0;
        series.ImdbId = seriesDetail.Externals.ImDbId;

        series.SeriesName = new SimpleTitle(seriesDetail.Name, true);
        series.FirstAired = seriesDetail.Premiered;
        series.Description = new SimpleTitle(seriesDetail.Summary, true);
        series.Rating = new SimpleRating(seriesDetail.Rating != null && seriesDetail.Rating.Rating.HasValue ? seriesDetail.Rating.Rating.Value : 0);
        series.Genres = seriesDetail.Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList();
        series.Networks = ConvertToCompanies(seriesDetail.Network ?? seriesDetail.WebNetwork, CompanyAspect.COMPANY_TV_NETWORK);
        if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
        {
          series.IsEnded = true;
        }
        if (seriesDetail.Embedded != null)
        {
          if (seriesDetail.Embedded.Cast != null)
          {
            series.Actors = ConvertToPersons(seriesDetail.Embedded.Cast, PersonAspect.OCCUPATION_ACTOR, seriesDetail.Name);
            series.Characters = ConvertToCharacters(seriesDetail.Embedded.Cast, seriesDetail.Name);
          }
          if (seriesDetail.Embedded.Episodes != null)
          {
            foreach (TvMazeEpisode episodeDetail in seriesDetail.Embedded.Episodes)
            {
              SeasonInfo seasonInfo = new SeasonInfo()
              {
                SeriesTvMazeId = seriesDetail.Id,
                SeriesImdbId = seriesDetail.Externals.ImDbId,
                SeriesTvdbId = seriesDetail.Externals.TvDbId ?? 0,
                SeriesTvRageId = seriesDetail.Externals.TvRageId ?? 0,
                SeriesName = new SimpleTitle(seriesDetail.Name, true),
                SeasonNumber = episodeDetail.SeasonNumber,
                FirstAired = episodeDetail.AirDate,
                TotalEpisodes = seriesDetail.Embedded.Episodes.FindAll(e => e.SeasonNumber == episodeDetail.SeasonNumber).Count
              };
              if (!series.Seasons.Contains(seasonInfo))
                series.Seasons.Add(seasonInfo);

              EpisodeInfo info = new EpisodeInfo()
              {
                TvMazeId = episodeDetail.Id,

                SeriesTvMazeId = seriesDetail.Id,
                SeriesImdbId = seriesDetail.Externals.ImDbId,
                SeriesTvdbId = seriesDetail.Externals.TvDbId ?? 0,
                SeriesTvRageId = seriesDetail.Externals.TvRageId ?? 0,
                SeriesName = new SimpleTitle(seriesDetail.Name, true),
                SeriesFirstAired = seriesDetail.Premiered,

                SeasonNumber = episodeDetail.SeasonNumber,
                EpisodeNumbers = new List<int>(new int[] { episodeDetail.EpisodeNumber }),
                FirstAired = episodeDetail.AirDate,
                EpisodeName = new SimpleTitle(episodeDetail.Name, true),
                Summary = new SimpleTitle(episodeDetail.Summary, true),
                Genres = seriesDetail.Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList(),
              };

              info.Actors = series.Actors.ToList();
              info.Characters = series.Characters.ToList();

              series.Episodes.Add(info);
            }

            series.TotalSeasons = series.Seasons.Count;
            series.TotalEpisodes = series.Episodes.Count;

            TvMazeEpisode nextEpisode = seriesDetail.Embedded.Episodes.Where(e => e.AirDate > DateTime.Now).FirstOrDefault();
            if (nextEpisode != null)
            {
              series.NextEpisodeName = new SimpleTitle(nextEpisode.Name, true);
              series.NextEpisodeAirDate = nextEpisode.AirStamp;
              series.NextEpisodeSeasonNumber = nextEpisode.SeasonNumber;
              series.NextEpisodeNumber = nextEpisode.EpisodeNumber;
            }
          }
        }

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Exception while processing series {0}", ex, series.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesSeasonAsync(SeasonInfo season, string language, bool cacheOnly)
    {
      try
      {
        TvMazeSeries seriesDetail = null;
        TvMazeSeason seasonDetail = null;
        if (season.SeriesTvMazeId > 0)
          seriesDetail = await _tvMazeHandler.GetSeriesAsync(season.SeriesTvMazeId, cacheOnly).ConfigureAwait(false);
        if (seriesDetail == null) return false;
        if (season.SeriesTvMazeId > 0 && season.SeasonNumber.HasValue)
        {
          List<TvMazeSeason> seasons = await _tvMazeHandler.GetSeriesSeasonsAsync(season.SeriesTvMazeId, cacheOnly).ConfigureAwait(false);
          if (seasons != null)
            seasonDetail = seasons.Where(s => s.SeasonNumber == season.SeasonNumber).FirstOrDefault();
        }
        if (seasonDetail == null) return false;

        season.TvMazeId = seasonDetail.Id;

        season.SeriesTvMazeId = seriesDetail.Id;
        season.SeriesImdbId = seriesDetail.Externals.ImDbId;
        season.SeriesTvdbId = seriesDetail.Externals.TvDbId ?? 0;
        season.SeriesTvRageId = seriesDetail.Externals.TvRageId ?? 0;

        season.SeriesName = new SimpleTitle(seriesDetail.Name, true);
        season.FirstAired = seasonDetail.PremiereDate;
        season.SeasonNumber = seasonDetail.SeasonNumber;
        season.TotalEpisodes = seasonDetail.EpisodeCount ?? 0;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Exception while processing season {0}", ex, season.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesEpisodeAsync(EpisodeInfo episode, string language, bool cacheOnly)
    {
      try
      {
        List<EpisodeInfo> episodeDetails = new List<EpisodeInfo>();
        TvMazeEpisode episodeDetail = null;
        TvMazeSeries seriesDetail = null;

        if (episode.SeriesTvMazeId > 0 && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
        {
          seriesDetail = await _tvMazeHandler.GetSeriesAsync(episode.SeriesTvMazeId, cacheOnly).ConfigureAwait(false);
          if (seriesDetail == null && !string.IsNullOrEmpty(episode.SeriesImdbId))
            seriesDetail = await _tvMazeHandler.GetSeriesByImDbAsync(episode.SeriesImdbId, cacheOnly).ConfigureAwait(false);
          if (seriesDetail == null && episode.SeriesTvdbId > 0)
            seriesDetail = await _tvMazeHandler.GetSeriesByTvDbAsync(episode.SeriesTvdbId, cacheOnly).ConfigureAwait(false);
          if (seriesDetail == null) return false;

          foreach (int episodeNumber in episode.EpisodeNumbers)
          {
            episodeDetail = await _tvMazeHandler.GetSeriesEpisodeAsync(episode.SeriesTvMazeId, episode.SeasonNumber.Value, episodeNumber, cacheOnly).ConfigureAwait(false);
            if (episodeDetail == null) continue;
            if (episodeDetail.EpisodeNumber <= 0) continue;

            EpisodeInfo info = new EpisodeInfo()
            {
              TvMazeId = episodeDetail.Id,

              SeriesTvMazeId = seriesDetail.Id,
              SeriesImdbId = seriesDetail.Externals.ImDbId,
              SeriesTvdbId = seriesDetail.Externals.TvDbId ?? 0,
              SeriesTvRageId = seriesDetail.Externals.TvRageId ?? 0,
              SeriesName = new SimpleTitle(seriesDetail.Name, true),
              SeriesFirstAired = seriesDetail.Premiered,

              SeasonNumber = episodeDetail.SeasonNumber,
              EpisodeNumbers = new List<int>(new int[] { episodeDetail.EpisodeNumber }),
              FirstAired = episodeDetail.AirDate,
              EpisodeName = new SimpleTitle(episodeDetail.Name, true),
              Summary = new SimpleTitle(episodeDetail.Summary, true),
              Genres = seriesDetail.Genres.Where(s => !string.IsNullOrEmpty(s?.Trim())).Select(s => new GenreInfo { Name = s.Trim() }).ToList(),
            };

            if (seriesDetail.Embedded != null && seriesDetail.Embedded.Cast != null)
            {
              info.Actors = ConvertToPersons(seriesDetail.Embedded.Cast, PersonAspect.OCCUPATION_ACTOR, seriesDetail.Name);
              info.Characters = ConvertToCharacters(seriesDetail.Embedded.Cast, seriesDetail.Name);
            }

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
        ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Exception while processing episode {0}", ex, episode.ToString());
        return false;
      }
    }

    public override async Task<bool> UpdateFromOnlineSeriesPersonAsync(SeriesInfo seriesInfo, PersonInfo person, string language, bool cacheOnly)
    {
      try
      {
        TvMazePerson personDetail = null;
        if (person.TvMazeId > 0)
          personDetail = await _tvMazeHandler.GetPersonAsync(person.TvMazeId, cacheOnly).ConfigureAwait(false);
        if (personDetail == null) return false;

        person.TvMazeId = personDetail.Id;
        person.Name = personDetail.Name;

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Exception while processing person {0}", ex, person.ToString());
        return false;
      }
    }

    public override Task<bool> UpdateFromOnlineSeriesEpisodePersonAsync(EpisodeInfo episodeInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesPersonAsync(episodeInfo.CloneBasicInstance<SeriesInfo>(), person, language, cacheOnly);
    }

    public override async Task<bool> UpdateFromOnlineSeriesCharacterAsync(SeriesInfo seriesInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      try
      {
        TvMazeSeries seriesDetail = null;
        if (seriesInfo.TvMazeId > 0)
          seriesDetail = await _tvMazeHandler.GetSeriesAsync(seriesInfo.TvMazeId, cacheOnly).ConfigureAwait(false);
        if (seriesDetail == null) return false;
        if (seriesDetail.Embedded != null)
        {
          if (seriesDetail.Embedded.Cast != null)
          {
            List<CharacterInfo> characters = ConvertToCharacters(seriesDetail.Embedded.Cast, seriesDetail.Name);
            int index = characters.IndexOf(character);
            if (index >= 0)
            {
              character.ActorTvMazeId = characters[index].ActorTvMazeId;
              character.ActorName = characters[index].ActorName;
              character.Name = characters[index].Name;
              character.Order = characters[index].Order;
              character.ParentMediaName = seriesDetail.Name;

              return true;
            }
          }
        }

        return false;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Debug("TvMazeWrapper: Exception while processing character {0}", ex, character.ToString());
        return false;
      }
    }

    public override Task<bool> UpdateFromOnlineSeriesEpisodeCharacterAsync(EpisodeInfo episodeInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesCharacterAsync(episodeInfo.CloneBasicInstance<SeriesInfo>(), character, language, cacheOnly);
    }

    #endregion

    #region Convert

    private List<PersonInfo> ConvertToPersons(List<TvMazeCast> cast, string occupation, string series)
    {
      if (cast == null || cast.Count == 0)
        return new List<PersonInfo>();

      List<PersonInfo> retValue = new List<PersonInfo>();
      foreach (TvMazeCast person in cast)
        retValue.Add(new PersonInfo() { TvMazeId = person.Person.Id, Name = person.Person.Name, Occupation = occupation, ParentMediaName = series });
      return retValue;
    }

    private List<CharacterInfo> ConvertToCharacters(List<TvMazeCast> characters, string series)
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
          Name = person.Character.Name,
          ParentMediaName = series
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

    #region FanArt

    public override Task<ApiWrapperImageCollection<TvMazeImageCollection>> GetFanArtAsync<T>(T infoObject, string language, string fanartMediaType)
    {
      if (fanartMediaType == FanArtMediaTypes.Series)
        return GetSeriesFanArtAsync(infoObject.AsSeries());
      if (fanartMediaType == FanArtMediaTypes.Episode)
        return GetEpisodeFanArtAsync(infoObject as EpisodeInfo);
      if (fanartMediaType == FanArtMediaTypes.Actor)
        return GetActorFanArtAsync(infoObject as PersonInfo);
      if (fanartMediaType == FanArtMediaTypes.Character)
        return GetCharactorFanArtAsync(infoObject as CharacterInfo);
      return Task.FromResult<ApiWrapperImageCollection<TvMazeImageCollection>>(null);
    }

    public override Task<bool> DownloadFanArtAsync(string id, TvMazeImageCollection image, string folderPath)
    {
      int intId;
      if (!int.TryParse(id, out intId))
        return Task.FromResult(false);
      return _tvMazeHandler.DownloadImageAsync(intId, image, folderPath);
    }

    protected async Task<ApiWrapperImageCollection<TvMazeImageCollection>> GetSeriesFanArtAsync(SeriesInfo series)
    {
      if (series == null || series.TvMazeId < 1)
        return null;
      TvMazeSeries seriesDetail = await _tvMazeHandler.GetSeriesAsync(series.TvMazeId, false).ConfigureAwait(false);
      if (seriesDetail == null)
        return null;
      ApiWrapperImageCollection<TvMazeImageCollection> images = new ApiWrapperImageCollection<TvMazeImageCollection>();
      images.Id = series.TvMazeId.ToString();
      images.Posters.Add(seriesDetail.Images);
      return images;
    }

    protected async Task<ApiWrapperImageCollection<TvMazeImageCollection>> GetEpisodeFanArtAsync(EpisodeInfo episode)
    {
      if (episode == null || episode.SeriesTvMazeId < 1 || !episode.SeasonNumber.HasValue || episode.EpisodeNumbers.Count == 0)
        return null;
      TvMazeEpisode episodeDetail = await _tvMazeHandler.GetSeriesEpisodeAsync(episode.SeriesTvMazeId, episode.SeasonNumber.Value, episode.FirstEpisodeNumber, false).ConfigureAwait(false);
      if (episodeDetail == null)
        return null;
      ApiWrapperImageCollection<TvMazeImageCollection> images = new ApiWrapperImageCollection<TvMazeImageCollection>();
      images.Id = episode.SeriesTvMazeId.ToString();
      images.Thumbnails.Add(episodeDetail.Images);
      return images;
    }

    protected async Task<ApiWrapperImageCollection<TvMazeImageCollection>> GetActorFanArtAsync(PersonInfo person)
    {
      if (person == null || person.TvMazeId < 1)
        return null;
      TvMazePerson personDetail = await _tvMazeHandler.GetPersonAsync(person.TvMazeId, false).ConfigureAwait(false);
      if (personDetail == null)
        return null;
      ApiWrapperImageCollection<TvMazeImageCollection> images = new ApiWrapperImageCollection<TvMazeImageCollection>();
      images.Id = person.TvMazeId.ToString();
      images.Thumbnails.Add(personDetail.Images);
      return images;
    }

    protected async Task<ApiWrapperImageCollection<TvMazeImageCollection>> GetCharactorFanArtAsync(CharacterInfo character)
    {
      if (character == null || character.TvMazeId < 1)
        return null;
      TvMazePerson personDetail = await _tvMazeHandler.GetCharacterAsync(character.TvMazeId, false).ConfigureAwait(false);
      if (personDetail == null)
        return null;
      ApiWrapperImageCollection<TvMazeImageCollection> images = new ApiWrapperImageCollection<TvMazeImageCollection>();
      images.Id = character.TvMazeId.ToString();
      images.Thumbnails.Add(personDetail.Images);
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
        if (DateTime.Now - lastRefresh <= _cacheTimeout)
          return false;

        DateTime startTime = DateTime.Now;
        List<int> changedItems = new List<int>();
        Dictionary<int, DateTime> seriesChangeDates = _tvMazeHandler.GetSeriesChangeDatesAsync().Result;
        foreach (var change in seriesChangeDates)
        {
          if(change.Value > lastRefresh)
            changedItems.Add(change.Key);
        }
        foreach (int seriesId in changedItems)
          _tvMazeHandler.DeleteSeriesCacheAsync(seriesId).Wait();
        FireCacheUpdateFinished(startTime, DateTime.Now, UpdateType.Person, changedItems.Select(i => i.ToString()).ToList());

        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("TvMazeWrapper: Error updating cache", ex);
        return false;
      }
    }

    #endregion
  }
}
