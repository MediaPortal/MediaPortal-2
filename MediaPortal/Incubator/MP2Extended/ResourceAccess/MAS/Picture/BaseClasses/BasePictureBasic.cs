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

      WebPictureBasic webPictureBasic = new WebPictureBasic();

      //webPictureBasic.Categories = imageAspects.GetAttributeValue(ImageAspect);
      //webPictureBasic.DateTaken = imageAspects.GetAttributeValue(ImageAspect.);
      webPictureBasic.Type = WebMediaType.Picture;
      //webPictureBasic.Artwork;
      webPictureBasic.DateAdded = (DateTime)item.Aspects[ImporterAspect.ASPECT_ID].GetAttributeValue(ImporterAspect.ATTR_DATEADDED);
      webPictureBasic.Id = item.MediaItemId.ToString();
      webPictureBasic.PID = 0;
      //webPictureBasic.Path;
      webPictureBasic.Title = (string)item.Aspects[MediaAspect.ASPECT_ID].GetAttributeValue(MediaAspect.ATTR_TITLE);

      return webPictureBasic;
    }
  }
}
