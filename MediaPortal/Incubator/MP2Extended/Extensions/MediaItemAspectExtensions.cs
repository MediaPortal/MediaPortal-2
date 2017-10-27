using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MP2Extended.Extensions
{
  public static class MediaItemAspectExtensions
  {
    public static MediaItemAspect GetAspect(this MediaItem mediaItem, MediaItemAspectMetadata aspectMetadata)
    {
      return mediaItem[aspectMetadata.AspectId][0];
    }

    public static MultipleMediaItemAspect PrimaryProviderResourceAspect(this MediaItem mediaItem)
    {
      return mediaItem.PrimaryResources[mediaItem.ActiveResourceLocatorIndex];
    }

    public static string PrimaryProviderResourcePath(this MediaItem mediaItem)
    {
      return mediaItem.PrimaryProviderResourceAspect().GetAttributeValue<string>(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
    }
  }

  public static class RelationshipExtensionMethods
  {
    public static Guid? GetLinkedIdOrDefault(this MediaItem mediaItem, Guid role, Guid linkedRole)
    {
      IList<MediaItemAspect> relationshipAspects;
      if (!mediaItem.Aspects.TryGetValue(RelationshipAspect.ASPECT_ID, out relationshipAspects))
        return null;
      return relationshipAspects.GetLinkedIdOrDefault(role, linkedRole);
    }

    public static Guid? GetLinkedIdOrDefault(this IEnumerable<MediaItemAspect> relationshipAspects, Guid role, Guid linkedRole)
    {
      var linkedIds = relationshipAspects.GetLinkedIds(role, linkedRole);
      if (linkedIds.Any())
        return linkedIds.First();
      return null;
    }

    public static IEnumerable<Guid> GetLinkedIds(this MediaItem mediaItem, Guid role, Guid linkedRole)
    {
      IList<MediaItemAspect> relationshipAspects;
      if (!mediaItem.Aspects.TryGetValue(RelationshipAspect.ASPECT_ID, out relationshipAspects))
        return new List<Guid>();
      return relationshipAspects.GetLinkedIds(role, linkedRole);
    }

    public static IEnumerable<Guid> GetLinkedIds(this IEnumerable<MediaItemAspect> relationshipAspects, Guid role, Guid linkedRole)
    {
      return relationshipAspects.Where(r =>
      r.GetAttributeValue<Guid>(RelationshipAspect.ATTR_ROLE) == role && r.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ROLE) == linkedRole)
        .Select(r => r.GetAttributeValue<Guid>(RelationshipAspect.ATTR_LINKED_ID));
    }
  }
}
