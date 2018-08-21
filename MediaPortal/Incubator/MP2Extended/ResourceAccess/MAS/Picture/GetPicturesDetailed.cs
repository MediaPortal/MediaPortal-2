#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.MAS.Picture;
using MP2Extended.Extensions;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.Picture
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, ReturnType = typeof(List<WebPictureDetailed>), Summary = "")]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetPicturesDetailed
  {
    public Task<IList<WebPictureDetailed>> ProcessAsync(IOwinContext context, string filter, WebSortField? sort, WebSortOrder? order)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);

      IList<MediaItem> items = MediaLibraryAccess.GetMediaItemsByAspect(context, necessaryMIATypes, null);

      if (items.Count == 0)
        throw new BadRequestException("No Images found");

      var output = new List<WebPictureDetailed>();

      foreach (var item in items)
      {
        MediaItemAspect imageAspects = item.GetAspect(ImageAspect.Metadata);

        WebPictureDetailed webPictureDetailed = new WebPictureDetailed();

        //webPictureBasic.Categories = imageAspects.GetAttributeValue(ImageAspect);
        //webPictureBasic.DateTaken = imageAspects.GetAttributeValue(ImageAspect.);
        webPictureDetailed.Type = WebMediaType.Picture;
        //webPictureBasic.Artwork;
        webPictureDetailed.DateAdded = (DateTime)item.GetAspect(ImporterAspect.Metadata).GetAttributeValue(ImporterAspect.ATTR_DATEADDED);
        webPictureDetailed.Id = item.MediaItemId.ToString();
        webPictureDetailed.PID = 0;
        //webPictureBasic.Path;
        webPictureDetailed.Title = (string)item.GetAspect(MediaAspect.Metadata).GetAttributeValue(MediaAspect.ATTR_TITLE);
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

      return System.Threading.Tasks.Task.FromResult<IList<WebPictureDetailed>>(output);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
