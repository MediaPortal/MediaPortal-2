using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebPictureDetailed>), Summary = "")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetPicturesDetailed
  {
    public IList<WebPictureDetailed> Process(string filter, WebSortField? sort, WebSortOrder? order)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);

      IList<MediaItem> items = GetMediaItems.GetMediaItemsByAspect(necessaryMIATypes);

      if (items.Count == 0)
        throw new BadRequestException("No Images found");

      var output = new List<WebPictureDetailed>();

      foreach (var item in items)
      {
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

        output.Add(webPictureDetailed);
      }

      // sort and filter
      if (sort != null && order != null)
      {
        output = output.AsQueryable().Filter(filter).SortMediaItemList(sort, order).ToList();
      }
      else
        output = output.Filter(filter).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}