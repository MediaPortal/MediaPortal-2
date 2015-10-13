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
      SingleMediaItemAspect imageAspect = MediaItemAspect.GetAspect(item.Aspects, ImageAspect.Metadata);

      WebPictureBasic webPictureBasic = new WebPictureBasic();

      //webPictureBasic.Categories = imageAspects.GetAttributeValue(ImageAspect);
      //webPictureBasic.DateTaken = imageAspects.GetAttributeValue(ImageAspect.);
      webPictureBasic.Type = WebMediaType.Picture;
      //webPictureBasic.Artwork;
      webPictureBasic.DateAdded = (DateTime)imageAspect.GetAttributeValue(ImporterAspect.ATTR_DATEADDED);
      webPictureBasic.Id = item.MediaItemId.ToString();
      webPictureBasic.PID = 0;
      //webPictureBasic.Path;
      string title;
      MediaItemAspect.TryGetAttribute(item.Aspects, MediaAspect.ATTR_TITLE, out title);
      webPictureBasic.Title = title;

      return webPictureBasic;
    }
  }
}
