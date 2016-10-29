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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace MediaPortal.Extensions.MetadataExtractors.SeriesMetadataExtractor
{
  public abstract class ISeriesRelationshipExtractor
  {
    protected static MemoryCache _actorMemoryCache = null;
    protected static MemoryCache _characterMemoryCache = null;
    protected static MemoryCache _writerMemoryCache = null;
    protected static MemoryCache _directorMemoryCache = null;
    protected static MemoryCache _seriesMemoryCache = null;
    protected static MemoryCache _seasonMemoryCache = null;
    protected static MemoryCache _studioMemoryCache = null;
    protected static MemoryCache _networkMemoryCache = null;
    protected static CacheItemPolicy _cachePolicy = null;

    protected MemoryCache _checkMemoryCache = null;

    static ISeriesRelationshipExtractor()
    {
      _actorMemoryCache = new MemoryCache("SeriesActorItemCache");
      _characterMemoryCache = new MemoryCache("SeriesCharacterItemCache");
      _writerMemoryCache = new MemoryCache("SeriesWriterItemCache");
      _directorMemoryCache = new MemoryCache("SeriesDirectorItemCache");
      _seriesMemoryCache = new MemoryCache("SeriesItemCache");
      _seasonMemoryCache = new MemoryCache("SeriesSeasonItemCache");
      _studioMemoryCache = new MemoryCache("SeriesStudioItemCache");
      _networkMemoryCache = new MemoryCache("SeriesNetworkItemCache");

      _cachePolicy = new CacheItemPolicy();
      _cachePolicy.SlidingExpiration = TimeSpan.FromHours(SeriesMetadataExtractor.MINIMUM_HOUR_AGE_BEFORE_UPDATE);
    }

    public virtual void ClearCache()
    {
      if (_actorMemoryCache != null)
        _actorMemoryCache.Trim(100);
      if (_characterMemoryCache != null)
        _characterMemoryCache.Trim(100);
      if (_writerMemoryCache != null)
        _writerMemoryCache.Trim(100);
      if (_directorMemoryCache != null)
        _directorMemoryCache.Trim(100);
      if (_seriesMemoryCache != null)
        _seriesMemoryCache.Trim(100);
      if (_seasonMemoryCache != null)
        _seasonMemoryCache.Trim(100);
      if (_studioMemoryCache != null)
        _studioMemoryCache.Trim(100);
      if (_networkMemoryCache != null)
        _networkMemoryCache.Trim(100);

      if (_checkMemoryCache != null)
        _checkMemoryCache.Trim(100);
    }

    protected virtual bool AddToActorCache(Guid mediaItemId, PersonInfo mediaItemInfo)
    {
      if (_actorMemoryCache == null)
        return false;

      return _actorMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromActorCache(PersonInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_actorMemoryCache == null)
        return false;

      List<string> items = _actorMemoryCache.Where(mi => mediaItemInfo.Equals((PersonInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _actorMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToCharacterCache(Guid mediaItemId, CharacterInfo mediaItemInfo)
    {
      if (_characterMemoryCache == null)
        return false;

      return _characterMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromCharacterCache(CharacterInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_characterMemoryCache == null)
        return false;

      List<string> items = _characterMemoryCache.Where(mi => mediaItemInfo.Equals((CharacterInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _characterMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToWriterCache(Guid mediaItemId, PersonInfo mediaItemInfo)
    {
      if (_writerMemoryCache == null)
        return false;

      return _writerMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromWriterCache(PersonInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_writerMemoryCache == null)
        return false;

      List<string> items = _writerMemoryCache.Where(mi => mediaItemInfo.Equals((PersonInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _writerMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToDirectorCache(Guid mediaItemId, PersonInfo mediaItemInfo)
    {
      if (_directorMemoryCache == null)
        return false;

      return _directorMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromDirectorCache(PersonInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_directorMemoryCache == null)
        return false;

      List<string> items = _directorMemoryCache.Where(mi => mediaItemInfo.Equals((PersonInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _directorMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToSeriesCache(Guid mediaItemId, SeriesInfo mediaItemInfo)
    {
      if (_seriesMemoryCache == null)
        return false;

      return _seriesMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromSeriesCache(SeriesInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_seriesMemoryCache == null)
        return false;

      List<string> items = _seriesMemoryCache.Where(mi => mediaItemInfo.Equals((SeriesInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _seriesMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual SeriesInfo GetFromSeriesCache(Guid mediaItemId)
    {
      if (_seriesMemoryCache == null)
        return null;

      return (SeriesInfo)_seriesMemoryCache[mediaItemId.ToString()];
    }

    protected virtual bool AddToSeasonCache(Guid mediaItemId, SeasonInfo mediaItemInfo)
    {
      if (_seasonMemoryCache == null)
        return false;

      return _seasonMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromSeasonCache(SeasonInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_seasonMemoryCache == null)
        return false;

      List<string> items = _seasonMemoryCache.Where(mi => mediaItemInfo.Equals((SeasonInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _seasonMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual SeasonInfo GetFromSeasonCache(Guid mediaItemId)
    {
      if (_seasonMemoryCache == null)
        return null;

      return (SeasonInfo)_seasonMemoryCache[mediaItemId.ToString()];
    }

    protected virtual bool AddToStudioCache(Guid mediaItemId, CompanyInfo mediaItemInfo)
    {
      if (_studioMemoryCache == null)
        return false;

      return _studioMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromStudioCache(CompanyInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_studioMemoryCache == null)
        return false;

      List<string> items = _studioMemoryCache.Where(mi => mediaItemInfo.Equals((CompanyInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _studioMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToNetworkCache(Guid mediaItemId, CompanyInfo mediaItemInfo)
    {
      if (_networkMemoryCache == null)
        return false;

      return _networkMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromNetworkCache(CompanyInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_networkMemoryCache == null)
        return false;

      List<string> items = _networkMemoryCache.Where(mi => mediaItemInfo.Equals((CompanyInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _networkMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual bool AddToCheckCache<T>(T mediaItemInfo)
    {
      if (_checkMemoryCache == null)
        _checkMemoryCache = new MemoryCache(GetType().ToString() + "CheckCache");

      List<string> items = _checkMemoryCache.Where(mi => mediaItemInfo.Equals((T)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
        return _checkMemoryCache.Add(items[0], mediaItemInfo, _cachePolicy);

      return _checkMemoryCache.Add(mediaItemInfo.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool CheckCacheContains<T>(T mediaItemInfo)
    {
      if (_checkMemoryCache == null)
        _checkMemoryCache = new MemoryCache(GetType().ToString() + "CheckCache");

      List<string> items = _checkMemoryCache.Where(mi => mediaItemInfo.Equals((T)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
        return _checkMemoryCache.Contains(items[0]);

      return false;
    }

    public static IFilter GetSeriesSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> seriesFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(SeriesAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_SERIES)
          {
            if (seriesFilters.Count == 0)
            {
              seriesFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              seriesFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, seriesFilters[0],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, seriesFilters.ToArray());
    }

    public static IFilter GetSeasonSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> seasonFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(SeasonAspect.ASPECT_ID))
        return null;

      int seasonFilter = -1;
      int seriesFilter = -1;
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        //Season filter
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_SEASON)
          {
            if (seasonFilter < 0)
            {
              seasonFilter = seasonFilters.Count;
              seasonFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              seasonFilters[seasonFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, seasonFilters[seasonFilter],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }

        //Series filter
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_SERIES)
          {
            if (seriesFilter < 0)
            {
              seriesFilter = seasonFilters.Count;
              seasonFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              seasonFilters[seriesFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, seasonFilters[seriesFilter],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
        if (seriesFilter >= 0)
        {
          SingleMediaItemAspect seasonAspect;
          if (MediaItemAspect.TryGetAspect(extractedAspects, SeasonAspect.Metadata, out seasonAspect))
          {
            int? seasonNumber = seasonAspect.GetAttributeValue<int?>(SeasonAspect.ATTR_SEASON);
            if (seasonNumber.HasValue)
            {
              seasonFilters[seriesFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, seasonFilters[seriesFilter],
                new RelationalFilter(SeasonAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNumber.Value));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, seasonFilters.ToArray());
    }

    public static IFilter GetEpisodeSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> episodeFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(EpisodeAspect.ASPECT_ID))
        return null;

      int episodeFilter = -1;
      int seriesFilter = -1;
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        //Episode filter
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_EPISODE)
          {
            if (episodeFilter < 0)
            {
              episodeFilter = episodeFilters.Count;
              episodeFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              episodeFilters[episodeFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, episodeFilters[episodeFilter],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }

        //Series filter
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_SERIES)
          {
            if (seriesFilter < 0)
            {
              seriesFilter = episodeFilters.Count;
              episodeFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              episodeFilters[seriesFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, episodeFilters[seriesFilter],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
        if (seriesFilter >= 0)
        {
          SingleMediaItemAspect episodeAspect;
          if (MediaItemAspect.TryGetAspect(extractedAspects, EpisodeAspect.Metadata, out episodeAspect))
          {
            int? seasonNumber = episodeAspect.GetAttributeValue<int?>(EpisodeAspect.ATTR_SEASON);
            IEnumerable collection = episodeAspect.GetCollectionAttribute(EpisodeAspect.ATTR_EPISODE);
            List<int> episodeNumbers = new List<int>();
            if (collection != null)
              episodeNumbers.AddRange(collection.Cast<object>().Select(s => Convert.ToInt32(s)));
            if (seasonNumber.HasValue)
            {
              episodeFilters[seriesFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, episodeFilters[seriesFilter],
                new RelationalFilter(EpisodeAspect.ATTR_SEASON, RelationalOperator.EQ, seasonNumber.Value));
            }
            if (episodeNumbers.Count > 0)
            {
              episodeFilters[seriesFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.And, episodeFilters[seriesFilter],
                new RelationalFilter(EpisodeAspect.ATTR_EPISODE, RelationalOperator.EQ, episodeNumbers[0]));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, episodeFilters.ToArray());
    }

    public static IFilter GetCharacterSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> characterFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(CharacterAspect.ASPECT_ID))
        return null;

      int characterFilter = -1;
      int personFilter = -1;
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        //Character filter
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_CHARACTER)
          {
            if (characterFilter < 0)
            {
              characterFilter = characterFilters.Count;
              characterFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              characterFilters[characterFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, characterFilters[characterFilter],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }

        //Person filter
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_PERSON)
          {
            if (personFilter < 0)
            {
              personFilter = characterFilters.Count;
              characterFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              characterFilters[personFilter] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, characterFilters[personFilter],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, characterFilters.ToArray());
    }

    public static IFilter GetPersonSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> personFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(PersonAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_PERSON)
          {
            if (personFilters.Count == 0)
            {
              personFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              personFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, personFilters[0],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, personFilters.ToArray());
    }

    public static IFilter GetCompanySearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> companyFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(CompanyAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_COMPANY)
          {
            if (companyFilters.Count == 0)
            {
              companyFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              companyFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, companyFilters[0],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, companyFilters.ToArray());
    }

    public static IFilter GetTvNetworkSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> tvNetworkFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(CompanyAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_NETWORK)
          {
            if (tvNetworkFilters.Count == 0)
            {
              tvNetworkFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              tvNetworkFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, tvNetworkFilters[0],
              new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
          }
        }
      }
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, tvNetworkFilters.ToArray());
    }
  }
}
