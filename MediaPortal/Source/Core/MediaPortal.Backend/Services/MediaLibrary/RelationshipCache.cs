using MediaPortal.Backend.MediaLibrary;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.MediaManagement.MLQueries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaPortal.Backend.Services.MediaLibrary
{
  class RelationshipCache
  {
    protected readonly object _syncOb = new object();
    protected Dictionary<string, IList<MediaItem>> _externalItemsMatch = new Dictionary<string, IList<MediaItem>>();

    public Guid MatchExternalItem(IRelationshipRoleExtractor roleExtractor, IDictionary<Guid, IList<MediaItemAspect>> extractedItem, IList<Guid> linkedRoleAspectIds, out bool fromCache)
    {
      fromCache = false;
      IList<MultipleMediaItemAspect> externalAspects;
      if (MediaItemAspect.TryGetAspects(extractedItem, ExternalIdentifierAspect.Metadata, out externalAspects))
      {
        List<IFilter> externalFilters = new List<IFilter>();
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);

          lock (_syncOb)
          {
            IList<MediaItem> cachedItems = null;
            if (_externalItemsMatch.TryGetValue(GetCacheKey(source, type, id), out cachedItems))
            {
              foreach (var item in cachedItems)
                if (roleExtractor.TryMatch(extractedItem, item.Aspects))
                {
                  fromCache = true;
                  return item.MediaItemId;
                }
            }
          }

          // Search using external identifiers
          externalFilters.Add(new BooleanCombinationFilter(BooleanOperator.And, new[]
                {
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_SOURCE, RelationalOperator.EQ, source),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_TYPE, RelationalOperator.EQ, type),
                  new RelationalFilter(ExternalIdentifierAspect.ATTR_ID, RelationalOperator.EQ, id),
                }));
        }

        IMediaLibrary ml = ServiceRegistration.Get<IMediaLibrary>();
        //Logger.Debug("Searching for external items matching {0} / {1} / {2} with [{3}]", source, type, id, string.Join(",", linkedRoleAspectIds.Select(x => GetManagedMediaItemAspectMetadata()[x].Name)));
        // Any potential linked item must contain all of LinkedRoleAspects
        IList<Guid> optionalAspectIds = ml.GetManagedMediaItemAspectMetadata().Keys.Except(linkedRoleAspectIds).ToList();
        if (optionalAspectIds.Contains(RelationshipAspect.ASPECT_ID))
        {
          //Because relationships are loaded for both parties in the relationship (one the inverse of the other) saving the aspects will cause a duplication of the relationship.
          //So don't load it to avoid duplication. Merging will still work because the existing relationship is already persisted.
          optionalAspectIds.Remove(RelationshipAspect.ASPECT_ID);
        }

        IFilter filter = BooleanCombinationFilter.CombineFilters(BooleanOperator.Or, externalFilters);
        IList<MediaItem> externalItems = ml.Search(new MediaItemQuery(linkedRoleAspectIds, optionalAspectIds, filter), false, null, true);
        foreach (var item in externalItems)
        {
          if (roleExtractor.TryMatch(extractedItem, item.Aspects))
          {
            AddToCache(item);
            return item.MediaItemId;
          }
        }
      }
      return Guid.Empty;
    }

    protected static string GetCacheKey(string source, string type, string id)
    {
      return string.Format("{0} | {1} | {2}", source, type, id);
    }

    protected void AddToCache(MediaItem item)
    {
      IList<MultipleMediaItemAspect> externalAspects;
      if (!MediaItemAspect.TryGetAspects(item.Aspects, ExternalIdentifierAspect.Metadata, out externalAspects))
        return;

      lock (_syncOb)
      {
        foreach (MultipleMediaItemAspect externalAspect in externalAspects)
        {
          string source = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_SOURCE);
          string type = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_TYPE);
          string id = externalAspect.GetAttributeValue<string>(ExternalIdentifierAspect.ATTR_ID);
          string cacheKey = GetCacheKey(source, type, id);
          IList<MediaItem> cachedItems;
          if (!_externalItemsMatch.TryGetValue(cacheKey, out cachedItems))
            _externalItemsMatch[cacheKey] = cachedItems = new List<MediaItem>();
          cachedItems.Add(item);
        }
      }
    }
  }
}
