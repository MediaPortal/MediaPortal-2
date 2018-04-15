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

using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.Caching;

namespace MediaPortal.Common.MediaManagement.Helpers
{
  /// <summary>
  /// A cache backed by a <see cref="MemoryCache"/> that caches <see cref="BaseInfo"/> items based on their <see cref="ExternalIdentifierAspect"/>s.
  /// The <see cref="BaseInfo"/> items are used as a key in a dictionary so users of this class should ensure that <see cref="object.GetHashCode"/> is implemeted correctly
  /// in derived <see cref="BaseInfo"/> classes.
  /// </summary>
  public class RelationshipCache : IDisposable
  {
    protected class CachedRelationship
    {
      public Guid MediaItemId { get; set; }
      public BaseInfo MediaItemInfo { get; set; }
    }

    protected MemoryCache _cache;
    protected CacheItemPolicy _cachePolicy;

    public RelationshipCache(string name, TimeSpan slidingExpiration, NameValueCollection config = null)
    {
      _cache = new MemoryCache(name, config);
      _cachePolicy = new CacheItemPolicy
      {
        SlidingExpiration = slidingExpiration,
        //RemovedCallback = (e) =>
        //{
        //  ServiceRegistration.Get<ILogger>().Debug("{0}: Removed '{1}' from cache, reason: {2}", _cache.Name, e.CacheItem.Key, e.RemovedReason);
        //}
      };
    }

    public bool AddToCache<T>(Guid mediaItemId, T mediaItemInfo, bool createCopy = true) where T : BaseInfo
    {
      if (_cache == null)
        return false;

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      mediaItemInfo.SetMetadata(aspects);

      IList<MultipleMediaItemAspect> externalAspects;
      if (!MediaItemAspect.TryGetAspects(aspects, ExternalIdentifierAspect.Metadata, out externalAspects))
        return false;

      if (createCopy)
        mediaItemInfo = mediaItemInfo.CloneBasicInstance<T>();

      bool result = false;
      CachedRelationship entry = new CachedRelationship { MediaItemId = mediaItemId, MediaItemInfo = mediaItemInfo };
      foreach (MediaItemAspect externalAspect in externalAspects)
        result |= AddToCache(externalAspect, entry);
      return result;
    }

    protected bool AddToCache(MediaItemAspect externalAspect, CachedRelationship entry)
    {
      string key = CreateCacheKey(externalAspect);
      var newEntries = new Lazy<ConcurrentDictionary<BaseInfo, CachedRelationship>>();
      var entries = _cache.AddOrGetExisting(key, newEntries, _cachePolicy) as Lazy<ConcurrentDictionary<BaseInfo, CachedRelationship>> ?? newEntries;
      if (entries.Value.TryAdd(entry.MediaItemInfo, entry))
      {
        //ServiceRegistration.Get<ILogger>().Debug("{0}: Added {1} {2} to cache with Id {3} and Key {4}", _cache.Name, entry.MediaItemInfo.GetType().Name, entry.MediaItemInfo, entry.MediaItemId, key);
        //ServiceRegistration.Get<ILogger>().Debug("{0}: Current entries for external aspect: {1}", _cache.Name, entries.Value.Count);
        return true;
      }
      return false;
    }

    public bool TryGetIdFromCache(BaseInfo mediaItemInfo, out Guid mediaItemId)
    {
      CachedRelationship relationship;
      if (TryGetFromCache(mediaItemInfo, out relationship))
      {
        mediaItemId = relationship.MediaItemId;
        return true;
      }
      mediaItemId = Guid.Empty;
      return false;
    }

    public bool TryGetInfoFromCache<T>(T mediaItemInfo, out T cachedInfo, out Guid mediaItemId) where T : BaseInfo
    {
      CachedRelationship relationship;
      if (TryGetFromCache(mediaItemInfo, out relationship))
      {
        cachedInfo = relationship.MediaItemInfo as T;
        if (cachedInfo != null)
        {
          mediaItemId = relationship.MediaItemId;
          return true;
        }
      }
      cachedInfo = null;
      mediaItemId = Guid.Empty;
      return false;
    }

    protected bool TryGetFromCache(BaseInfo mediaItemInfo, out CachedRelationship cachedEntry)
    {
      cachedEntry = null;
      if (_cache == null)
        return false;

      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      mediaItemInfo.SetMetadata(aspects);

      IList<MultipleMediaItemAspect> externalAspects;
      if (!MediaItemAspect.TryGetAspects(aspects, ExternalIdentifierAspect.Metadata, out externalAspects))
        return false;

      foreach (MediaItemAspect externalAspect in externalAspects)
        if (TryGetFromCache(externalAspect, mediaItemInfo, out cachedEntry))
          return true;

      //ServiceRegistration.Get<ILogger>().Debug("{0}: Cache miss {1} {2}", _cache.Name, mediaItemInfo.GetType().Name, mediaItemInfo);
      return false;
    }

    protected bool TryGetFromCache(MediaItemAspect externalAspect, BaseInfo mediaItemInfo, out CachedRelationship cachedEntry)
    {
      string key = CreateCacheKey(externalAspect);
      var entries = _cache.Get(key) as Lazy<ConcurrentDictionary<BaseInfo, CachedRelationship>>;
      if (entries != null && entries.Value.TryGetValue(mediaItemInfo, out cachedEntry))
      {
        //ServiceRegistration.Get<ILogger>().Debug("{0}: Got {1} {2} from cache with Id {3} and Key {4}", _cache.Name, cachedEntry.MediaItemInfo.GetType().Name, cachedEntry.MediaItemInfo, cachedEntry.MediaItemId, key);
        return true;
      }
      cachedEntry = null;
      return false;
    }

    protected static string CreateCacheKey(MediaItemAspect externalAspect)
    {
      string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
      string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
      string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
      return string.Format("Source={0}; Type={1}; Id={2}", source, type, id);
    }

    public void Clear()
    {
      if (_cache != null)
        _cache.Trim(100);
    }

    public void Dispose()
    {
      if (_cache != null)
      {
        _cache.Dispose();
        _cache = null;
      }
    }
  }
}
