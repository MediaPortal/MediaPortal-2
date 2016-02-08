using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(Guid), Nullable = false)]
  internal class GetPictureDetailedById
  {
    public WebPictureDetailed Process(Guid id)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);

      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);

      if (item == null)
        throw new BadRequestException(string.Format("No Image found with id: {0}", id));

        MediaItemAspect imageAspects = item[ImageAspect.Metadata];

        WebPictureDetailed webPictureDetailed = new WebPictureDetailed();

      //webPictureBasic.Categories = imageAspects.GetAttributeValue(ImageAspect);
      //webPictureBasic.DateTaken = imageAspects.GetAttributeValue(ImageAspect.);
      webPictureDetailed.Type = WebMediaType.Picture;
      //webPictureBasic.Artwork;
      webPictureDetailed.DateAdded = (DateTime)item[ImporterAspect.Metadata].GetAttributeValue(ImporterAspect.ATTR_DATEADDED);
      webPictureDetailed.Id = item.MediaItemId.ToString();
      webPictureDetailed.PID = 0;
      //webPictureBasic.Path;
      webPictureDetailed.Title = (string)item[MediaAspect.Metadata].GetAttributeValue(MediaAspect.ATTR_TITLE);
      //webPictureDetailed.Rating = imageAspects.GetAttributeValue(ImageAspect.);
      //webPictureDetailed.Author = imageAspects.GetAttributeValue(ImageAspect.);
      //webPictureDetailed.Dpi = imageAspects.GetAttributeValue(ImageAspect.);
      webPictureDetailed.Width = (string)(imageAspects.GetAttributeValue(ImageAspect.ATTR_WIDTH) ?? string.Empty);
      webPictureDetailed.Height = (string)(imageAspects.GetAttributeValue(ImageAspect.ATTR_HEIGHT) ?? string.Empty);
      //webPictureDetailed.Mpixel = imageAspects.GetAttributeValue(ImageAspect.);
      //webPictureDetailed.Copyright;
      webPictureDetailed.CameraModel = (string)(imageAspects.GetAttributeValue(ImageAspect.ATTR_MODEL) ?? string.Empty);
      webPictureDetailed.CameraManufacturer = (string)(imageAspects.GetAttributeValue(ImageAspect.ATTR_MAKE) ?? string.Empty);
      //webPictureDetailed.Comment;
      //webPictureDetailed.Subject;

      return webPictureDetailed;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}