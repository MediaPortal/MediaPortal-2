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

using MediaPortal.Common.MediaManagement;
using MediaPortal.Utilities.Cache;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Common.Services.MediaManagement.ImportDataflowBlocks
{
  /// <summary>
  /// Implementation of <see cref="IRelationshipCache"/> backed by an LRU cache that prunes
  /// cached items when the cache reaches a certain size.
  /// </summary>
  public class LRURelationshipCache : IRelationshipCache
  {
    #region Inner classes
    
    protected class CacheItem
    {
      protected Guid _id;
      protected IDictionary<Guid, IList<MediaItemAspect>> _aspects;

      public CacheItem(Guid id, IDictionary<Guid, IList<MediaItemAspect>> aspects)
      {
        _id = id;
        _aspects = aspects;
      }

      public Guid Id => _id;
      public IDictionary<Guid, IList<MediaItemAspect>> Aspects => _aspects;
    }

    #endregion

    #region Consts

    //The maximum number of items to cache. This value is a tradeoff between performance and
    //memory usage, we need quite a large value as there can sometimes be nearly a hundred extracted
    //relations from a single media item.
    protected const int CACHE_SIZE = 1000;

    #endregion

    #region Protected fields

    //Currently cached items
    protected ILRUCache<string, IList<CacheItem>> _cache;

    //All ids that have ever been cached
    protected HashSet<Guid> _cacheHistory;

    #endregion

    #region Constructor

    public LRURelationshipCache()
    {
      _cache = new LargeLRUCache<string, IList<CacheItem>>(CACHE_SIZE);
      _cacheHistory = new HashSet<Guid>();
    }

    #endregion

    #region IRelationshipCache implementation

    public bool HasItemEverBeenCached(Guid mediaItemId)
    {
      return _cacheHistory.Contains(mediaItemId);
    }

    public bool TryAddItem(MediaItem item, IRelationshipRoleExtractor itemMatcher)
    {
      ICollection<string> identifiers = itemMatcher.GetExternalIdentifiers(item.Aspects);

      //We can only cache using external identifiers
      if (identifiers == null || identifiers.Count == 0)
        return false;

      //Lazily created below
      CacheItem cacheItem = null;

      foreach (string identifier in identifiers)
      {
        IList<CacheItem> cacheList;
        if (!_cache.TryGetValue(identifier, out cacheList))
        {
          //Identifier has never been cached, add it
          cacheList = new List<CacheItem>();
          _cache.Add(identifier, cacheList);
        }
        else if (cacheList.Any(c => c.Id == item.MediaItemId))
        {
          //Already cached under this identifier
          continue;
        }

        //Create the cache item if not already created.
        //Only the minimum number of aspects required by the itemMatcher
        //will be cached to avoid caching large thumbnails, etc
        if (cacheItem == null)
          cacheItem = CreateCacheItem(item, itemMatcher);

        cacheList.Add(cacheItem);
      }

      if (cacheItem == null)
        return false;

      //Item was added, add it to the cache history
      _cacheHistory.Add(item.MediaItemId);
      return true;
    }

    public bool TryGetItemId(IDictionary<Guid, IList<MediaItemAspect>> aspects, IRelationshipRoleExtractor itemMatcher, out Guid mediaItemId)
    {
      mediaItemId = Guid.Empty;
      ICollection<string> identifiers = itemMatcher.GetExternalIdentifiers(aspects);

      //We can only match using external identifiers
      if (identifiers == null || identifiers.Count == 0)
        return false;

      //All items that have been checked under the identifiers but didn't match
      ICollection<Guid> checkedIds = new HashSet<Guid>();

      foreach (string identifier in identifiers)
      {
        IList<CacheItem> cacheList;
        //Is the identifier in the cache?
        if (!_cache.TryGetValue(identifier, out cacheList))
          continue;

        //Multiple media items can be cached under the same external identifier, we need
        //to delegate the actual matching to the itemMatcher
        foreach (CacheItem item in cacheList)
        {
          //The same media items can appear under each identifier, don't bother checking
          //any items that we've already checked.
          if (checkedIds.Contains(item.Id))
            continue;

          if (itemMatcher.TryMatch(aspects, item.Aspects))
          {
            mediaItemId = item.Id;
            return true;
          }
          //Store this unmatching id so it's not checked again
          checkedIds.Add(item.Id);
        }
      }
      return false;
    }

    #endregion

    #region Protected methods

    protected CacheItem CreateCacheItem(MediaItem item, IRelationshipRoleExtractor itemMatcher)
    {
      IDictionary<Guid, IList<MediaItemAspect>> aspects = new Dictionary<Guid, IList<MediaItemAspect>>();
      IList<MediaItemAspect> aspect;
      //Only cache the aspects needed by the item matcher, to avoid cacheing unnecessary
      //heavy aspects like thumbnails
      foreach (Guid aspectId in itemMatcher.MatchAspects)
        if (item.Aspects.TryGetValue(aspectId, out aspect))
          aspects.Add(aspectId, aspect);
      return new CacheItem(item.MediaItemId, aspects);
    }

    #endregion
  }
}
