using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
