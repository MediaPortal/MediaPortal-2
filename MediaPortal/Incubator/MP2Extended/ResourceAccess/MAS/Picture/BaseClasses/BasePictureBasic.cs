using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture.BaseClasses
{
  class BasePictureBasic
  {
    internal WebPictureBasic PictureBasic(MediaItem item)
    {
      MediaItemAspect imageAspects = item.Aspects[ImageAspect.ASPECT_ID];

      WebPictureBasic webPictureBasic = new WebPictureBasic
      {
        Type = WebMediaType.Picture,
        DateAdded = (DateTime)item.Aspects[ImporterAspect.ASPECT_ID].GetAttributeValue(ImporterAspect.ATTR_DATEADDED),
        Id = item.MediaItemId.ToString(),
        PID = 0,
        Title = (string)item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_TITLE),
        DateTaken = (DateTime)item.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_RECORDINGTIME]
      };

      //webPictureBasic.Categories = imageAspects.GetAttributeValue(ImageAspect);
      //webPictureBasic.DateTaken = imageAspects.GetAttributeValue(ImageAspect.);
      //webPictureBasic.Artwork;
      //webPictureBasic.Path;

      return webPictureBasic;
    }
  }
}
