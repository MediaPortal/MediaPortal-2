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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.Helpers;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Extensions.MetadataExtractors.MovieMetadataExtractor
{
  public abstract class IMovieRelationshipExtractor
  {
    protected static RelationshipCache _relationshipCache = null;
    protected HashSet<BaseInfo> _checkCache = new HashSet<BaseInfo>();

    static IMovieRelationshipExtractor()
    {
      _relationshipCache = new RelationshipCache("MovieRelationshipCache", TimeSpan.FromHours(MovieMetadataExtractor.MINIMUM_HOUR_AGE_BEFORE_UPDATE));
    }

    public virtual void ClearCache()
    {
      _relationshipCache.Clear();
      _checkCache.Clear();
    }

    protected virtual bool AddToCache<T>(Guid mediaItemId, T mediaItemInfo, bool createCopy = true) where T : BaseInfo
    {
      return _relationshipCache.AddToCache(mediaItemId, mediaItemInfo, createCopy);
    }

    protected virtual bool TryGetIdFromCache(BaseInfo mediaItemInfo, out Guid mediaItemId)
    {
      return _relationshipCache.TryGetIdFromCache(mediaItemInfo, out mediaItemId);
    }

    protected virtual bool TryGetInfoFromCache<T>(T mediaItemInfo, out T cachedInfo, out Guid mediaItemId) where T : BaseInfo
    {
      return _relationshipCache.TryGetInfoFromCache(mediaItemInfo, out cachedInfo, out mediaItemId);
    }

    protected virtual bool AddToCheckCache<T>(T mediaItemInfo) where T : BaseInfo
    {
      T cacheItem = mediaItemInfo.CloneBasicInstance<T>();
      return cacheItem != null && _checkCache.Add(cacheItem);
    }

    protected virtual bool CheckCacheContains(BaseInfo mediaItemInfo)
    {
      return _checkCache.Contains(mediaItemInfo);
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
