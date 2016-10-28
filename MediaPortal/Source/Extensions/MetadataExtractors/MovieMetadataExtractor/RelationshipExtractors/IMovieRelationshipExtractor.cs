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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  public abstract class IMovieRelationshipExtractor
  {
    protected static MemoryCache _actorMemoryCache = null;
    protected static MemoryCache _characterMemoryCache = null;
    protected static MemoryCache _writerMemoryCache = null;
    protected static MemoryCache _directorMemoryCache = null;
    protected static MemoryCache _collectionMemoryCache = null;
    protected static MemoryCache _studioMemoryCache = null;
    protected static CacheItemPolicy _cachePolicy = null;

    protected MemoryCache _checkMemoryCache = null;

    static IMovieRelationshipExtractor()
    {
      _actorMemoryCache = new MemoryCache("MovieActorItemCache");
      _characterMemoryCache = new MemoryCache("MovieCharacterItemCache");
      _writerMemoryCache = new MemoryCache("MovieWriterItemCache");
      _directorMemoryCache = new MemoryCache("MovieDirectorItemCache");
      _collectionMemoryCache = new MemoryCache("MovieCollectionItemCache");
      _studioMemoryCache = new MemoryCache("MovieStudioItemCache");

      _cachePolicy = new CacheItemPolicy();
      _cachePolicy.SlidingExpiration = TimeSpan.FromHours(MovieMetadataExtractor.MINIMUM_HOUR_AGE_BEFORE_UPDATE);
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
      if (_collectionMemoryCache != null)
        _collectionMemoryCache.Trim(100);
      if (_studioMemoryCache != null)
        _studioMemoryCache.Trim(100);

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

    protected virtual bool AddToCollectionCache(Guid mediaItemId, MovieCollectionInfo mediaItemInfo)
    {
      if (_collectionMemoryCache == null)
        return false;

      return _collectionMemoryCache.Add(mediaItemId.ToString(), mediaItemInfo, _cachePolicy);
    }

    protected virtual bool TryGetIdFromCollectionCache(MovieCollectionInfo mediaItemInfo, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      if (_collectionMemoryCache == null)
        return false;

      List<string> items = _collectionMemoryCache.Where(mi => mediaItemInfo.Equals((MovieCollectionInfo)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
      {
        mediaItemId = Guid.Parse(items[0]);
        return _collectionMemoryCache.Contains(items[0]);
      }
      return false;
    }

    protected virtual MovieCollectionInfo GetFromCollectionCache(Guid mediaItemId)
    {
      if (_collectionMemoryCache == null)
        return null;

      return (MovieCollectionInfo)_collectionMemoryCache[mediaItemId.ToString()];
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

    protected virtual bool AddToCheckCache<T>(T mediaItemInfo)
    {
      if (_checkMemoryCache == null)
        _checkMemoryCache = new MemoryCache(GetType().ToString() + "CheckCache");

      List<string> items = _checkMemoryCache.Where(mi => mediaItemInfo.Equals((T)mi.Value)).Select(mi => mi.Key).ToList();
      if (items.Count > 0)
        return _checkMemoryCache.Add(items[0], mediaItemInfo, _cachePolicy);

      return _checkMemoryCache.Add(mediaItemInfo.ToString(), mediaItemInfo, _cachePolicy);
    }

    public static IFilter GetMovieSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> movieFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(MovieAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_MOVIE)
          {
            if (movieFilters.Count == 0)
            {
              movieFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              movieFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, movieFilters[0],
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
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, movieFilters.ToArray());
    }

    public static IFilter GetMovieCollectionSearchFilter(IDictionary<Guid, IList<MediaItemAspect>> extractedAspects)
    {
      List<IFilter> movieCollectionFilters = new List<IFilter>();
      if (!extractedAspects.ContainsKey(MovieCollectionAspect.ASPECT_ID))
        return null;

      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedAspects, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          if (type == ExternalIdentifierAspect.TYPE_COLLECTION)
          {
            if (movieCollectionFilters.Count == 0)
            {
              movieCollectionFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
              {
                new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
              }));
            }
            else
            {
              movieCollectionFilters[0] = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, movieCollectionFilters[0],
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
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, movieCollectionFilters.ToArray());
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
      return BooleanCombinationFilter.CombineFilters(BooleanOperator.Or,companyFilters.ToArray());
    }
  }
}
