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

using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1;
using MediaPortal.Extensions.OnlineLibraries.Libraries.TvMazeV1.Data;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.FanArt;

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

    public override bool SearchSeriesEpisode(EpisodeInfo episodeSearch, string language, out List<EpisodeInfo> episodes)
    {
      episodes = null;
      SeriesInfo seriesSearch = episodeSearch.CloneBasicInstance<SeriesInfo>();
      if (episodeSearch.SeriesTvMazeId <= 0)
      {
        if (!SearchSeriesUniqueAndUpdate(seriesSearch, language))
          return false;
        episodeSearch.CopyIdsFrom(seriesSearch);
      }

      if (episodeSearch.SeriesTvMazeId > 0 && episodeSearch.SeasonNumber.HasValue)
      {
        TvMazeSeries seriesDetail = _tvMazeHandler.GetSeries(episodeSearch.SeriesTvMazeId, false);
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
        info.EpisodeNumbers.AddRange(episodeSearch.EpisodeNumbers);
        episodes.Add(info);
        return true;
      }

      return episodes != null;
    }

    public override bool SearchSeries(SeriesInfo seriesSearch, string language, out List<SeriesInfo> series)
    {
      series = null;
      List<TvMazeSeries> foundSeries = _tvMazeHandler.SearchSeries(seriesSearch.SeriesName.Text);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.OriginalName))
        foundSeries = _tvMazeHandler.SearchSeries(seriesSearch.OriginalName);
      if (foundSeries == null && !string.IsNullOrEmpty(seriesSearch.AlternateName))
        foundSeries = _tvMazeHandler.SearchSeries(seriesSearch.AlternateName);
      if (foundSeries == null) return false;
      series = foundSeries.Select(s => new SeriesInfo()
      {
        TvMazeId = s.Id,
        ImdbId = s.Externals.ImDbId,
        TvdbId = s.Externals.TvDbId ?? 0,
        TvRageId = s.Externals.TvRageId ?? 0,
        SeriesName = new SimpleTitle(s.Name, true),
        FirstAired = s.Premiered,
      }).ToList();
      return series.Count > 0;
    }

    public override bool SearchPerson(PersonInfo personSearch, string language, out List<PersonInfo> persons)
    {
      persons = null;
      List<TvMazePerson> foundPersons = _tvMazeHandler.SearchPerson(personSearch.Name);
      if (foundPersons == null) return false;
      persons = foundPersons.Select(p => new PersonInfo()
      {
        TvMazeId = p.Id,
        Name = p.Name,
      }).ToList();
      return persons.Count > 0;
    }

    #endregion

    #region Update

    public override bool UpdateFromOnlineSeries(SeriesInfo series, string language, bool cacheOnly)
    {
      TvMazeSeries seriesDetail = null;
      if (series.TvMazeId > 0)
        seriesDetail = _tvMazeHandler.GetSeries(series.TvMazeId, cacheOnly);
      if (seriesDetail == null && !string.IsNullOrEmpty(series.ImdbId))
        seriesDetail = _tvMazeHandler.GetSeriesByImDb(series.ImdbId, cacheOnly);
      if (seriesDetail == null && series.TvdbId > 0)
        seriesDetail = _tvMazeHandler.GetSeriesByTvDb(series.TvdbId, cacheOnly);
      if (seriesDetail == null) return false;

      series.TvMazeId = seriesDetail.Id;
      series.TvdbId = seriesDetail.Externals.TvDbId ?? 0;
      series.TvRageId = seriesDetail.Externals.TvRageId ?? 0;
      series.ImdbId = seriesDetail.Externals.ImDbId;

      series.SeriesName = new SimpleTitle(seriesDetail.Name, true);
      series.FirstAired = seriesDetail.Premiered;
      series.Description = new SimpleTitle(seriesDetail.Summary, true);
      series.Rating = new SimpleRating(seriesDetail.Rating != null && seriesDetail.Rating.Rating.HasValue ? seriesDetail.Rating.Rating.Value : 0);
      series.Genres = seriesDetail.Genres.Select(s => new GenreInfo { Name = s }).ToList();
      series.Networks = ConvertToCompanies(seriesDetail.Network ?? seriesDetail.WebNetwork, CompanyAspect.COMPANY_TV_NETWORK);
      if (seriesDetail.Status.IndexOf("Ended", StringComparison.InvariantCultureIgnoreCase) >= 0)
      {
        series.IsEnded = true;
      }
      if(seriesDetail.Embedded != null)
      {
        if (seriesDetail.Embedded.Cast != null)
        {
          series.Actors = ConvertToPersons(seriesDetail.Embedded.Cast, PersonAspect.OCCUPATION_ACTOR, seriesDetail.Name);
          series.Characters = ConvertToCharacters(seriesDetail.Embedded.Cast, seriesDetail.Name);
        }
        if(seriesDetail.Embedded.Episodes != null)
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
              Genres = seriesDetail.Genres.Select(s => new GenreInfo { Name = s }).ToList(),
            };

            info.Actors = series.Actors;
            info.Characters = series.Characters;

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

    public override bool UpdateFromOnlineSeriesSeason(SeasonInfo season, string language, bool cacheOnly)
    {
      TvMazeSeries seriesDetail = null;
      TvMazeSeason seasonDetail = null;
      if (season.SeriesTvMazeId > 0)
        seriesDetail = _tvMazeHandler.GetSeries(season.SeriesTvMazeId, cacheOnly);
      if (seriesDetail == null) return false;
      if (season.SeriesTvMazeId > 0 && season.SeasonNumber.HasValue)
      {
        List<TvMazeSeason> seasons = _tvMazeHandler.GetSeriesSeasons(season.SeriesTvMazeId, cacheOnly);
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

    public override bool UpdateFromOnlineSeriesEpisode(EpisodeInfo episode, string language, bool cacheOnly)
    {
      List<EpisodeInfo> episodeDetails = new List<EpisodeInfo>();
      TvMazeEpisode episodeDetail = null;
      TvMazeSeries seriesDetail = null;
      
      if (episode.SeriesTvMazeId > 0 && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
      {
        seriesDetail = _tvMazeHandler.GetSeries(episode.SeriesTvMazeId, cacheOnly);
        if (seriesDetail == null && !string.IsNullOrEmpty(episode.SeriesImdbId))
          seriesDetail = _tvMazeHandler.GetSeriesByImDb(episode.SeriesImdbId, cacheOnly);
        if (seriesDetail == null && episode.SeriesTvdbId > 0)
          seriesDetail = _tvMazeHandler.GetSeriesByTvDb(episode.SeriesTvdbId, cacheOnly);
        if (seriesDetail == null) return false;

        foreach (int episodeNumber in episode.EpisodeNumbers)
        {
          episodeDetail = _tvMazeHandler.GetSeriesEpisode(episode.SeriesTvMazeId, episode.SeasonNumber.Value, episodeNumber, cacheOnly);
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
            Genres = seriesDetail.Genres.Select(s => new GenreInfo { Name = s }).ToList(),
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

    public override bool UpdateFromOnlineSeriesPerson(SeriesInfo seriesInfo, PersonInfo person, string language, bool cacheOnly)
    {
      TvMazePerson personDetail = null;
      if (person.TvMazeId > 0)
        personDetail = _tvMazeHandler.GetPerson(person.TvMazeId, cacheOnly);
      if (personDetail == null) return false;

      person.TvMazeId = personDetail.Id;
      person.Name = personDetail.Name;

      return true;
    }

    public override bool UpdateFromOnlineSeriesEpisodePerson(EpisodeInfo episodeInfo, PersonInfo person, string language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesPerson(episodeInfo.CloneBasicInstance<SeriesInfo>(), person, language, cacheOnly);
    }

    public override bool UpdateFromOnlineSeriesCharacter(SeriesInfo seriesInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      TvMazeSeries seriesDetail = null;
      if (seriesInfo.TvMazeId > 0)
        seriesDetail = _tvMazeHandler.GetSeries(seriesInfo.TvMazeId, cacheOnly);
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

    public override bool UpdateFromOnlineSeriesEpisodeCharacter(EpisodeInfo episodeInfo, CharacterInfo character, string language, bool cacheOnly)
    {
      return UpdateFromOnlineSeriesCharacter(episodeInfo.CloneBasicInstance<SeriesInfo>(), character, language, cacheOnly);
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

    public override bool GetFanArt<T>(T infoObject, string language, string fanartMediaType, out ApiWrapperImageCollection<TvMazeImageCollection> images)
    {
      images = new ApiWrapperImageCollection<TvMazeImageCollection>();

      try
      {
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
          if (series != null && series.TvMazeId > 0)
          {
            // Download all image information, filter later!
            TvMazeSeries seriesDetail = _tvMazeHandler.GetSeries(series.TvMazeId, false);
            if (seriesDetail != null)
            {
              images.Id = series.TvMazeId.ToString();
              images.Posters.Add(seriesDetail.Images);
              return true;
            }
          }
        }
        else if (fanartMediaType == FanArtMediaTypes.Episode)
        {
          EpisodeInfo episode = infoObject as EpisodeInfo;
          if (episode != null && episode.SeriesTvMazeId > 0 && episode.SeasonNumber.HasValue && episode.EpisodeNumbers.Count > 0)
          {
            // Download all image information, filter later!
            TvMazeEpisode episodeDetail = _tvMazeHandler.GetSeriesEpisode(episode.SeriesTvMazeId, episode.SeasonNumber.Value, episode.EpisodeNumbers[0], false);
            if (episodeDetail != null)
            {
              images.Id = episode.SeriesTvMazeId.ToString();
              images.Thumbnails.Add(episodeDetail.Images);
              return true;
            }
          }
        }
        else if (fanartMediaType == FanArtMediaTypes.Actor)
        {
          PersonInfo person = infoObject as PersonInfo;
          if (person != null && person.TvMazeId > 0)
          {
            // Download all image information, filter later!
            TvMazePerson personDetail = _tvMazeHandler.GetPerson(person.TvMazeId, false);
            if (personDetail != null)
            {
              images.Id = person.TvMazeId.ToString();
              images.Thumbnails.Add(personDetail.Images);
              return true;
            }
          }
        }
        else if (fanartMediaType == FanArtMediaTypes.Character)
        {
          CharacterInfo character = infoObject as CharacterInfo;
          if (character != null && character.TvMazeId > 0)
          {
            // Download all image information, filter later!
            TvMazePerson personDetail = _tvMazeHandler.GetCharacter(character.TvMazeId, false);
            if (personDetail != null)
            {
              images.Id = character.TvMazeId.ToString();
              images.Thumbnails.Add(personDetail.Images);
              return true;
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
        ServiceRegistration.Get<ILogger>().Debug(GetType().Name + ": Exception downloading images", ex);
      }
      return false;
    }

    public override bool DownloadFanArt(string id, TvMazeImageCollection image, string folderPath)
    {
      int ID;
      if (int.TryParse(id, out ID))
      {
        return _tvMazeHandler.DownloadImage(ID, image, folderPath);
      }
      return false;
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
        Dictionary<int, DateTime> seriesChangeDates = _tvMazeHandler.GetSeriesChangeDates();
        foreach (var change in seriesChangeDates)
        {
          if(change.Value > lastRefresh)
            changedItems.Add(change.Key);
        }
        foreach (int seriesId in changedItems)
          _tvMazeHandler.DeleteSeriesCache(seriesId);
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
