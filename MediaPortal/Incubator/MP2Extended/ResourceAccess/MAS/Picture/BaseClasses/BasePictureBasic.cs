using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MediaPortal.Utilities;
using MP2Extended.Extensions;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture.BaseClasses
{
  class BasePictureBasic
  {
    internal WebPictureBasic PictureBasic(MediaItem item)
    {
      MediaItemAspect imageAspects = item.GetAspect(ImageAspect.Metadata);
      ResourcePath path = ResourcePath.Deserialize(item.PrimaryProviderResourcePath());

      WebPictureBasic webPictureBasic = new WebPictureBasic
      {
        Type = WebMediaType.Picture,
        DateAdded = (DateTime)item.GetAspect(ImporterAspect.Metadata).GetAttributeValue(ImporterAspect.ATTR_DATEADDED),
        Id = item.MediaItemId.ToString(),
        PID = 0,
        Title = (string)item.GetAspect(MediaAspect.Metadata).GetAttributeValue(MediaAspect.ATTR_TITLE),
        DateTaken = (DateTime)item.GetAspect(MediaAspect.Metadata)[MediaAspect.ATTR_RECORDINGTIME],
        Path = new List<string> { (path != null && path.PathSegments.Count > 0) ? StringUtils.RemovePrefixIfPresent(path.LastPathSegment.Path, "/") : string.Empty },
      };

      //webPictureBasic.Categories = imageAspects.GetAttributeValue(ImageAspect);
      //webPictureBasic.Artwork;

      return webPictureBasic;
    }
  }
}
