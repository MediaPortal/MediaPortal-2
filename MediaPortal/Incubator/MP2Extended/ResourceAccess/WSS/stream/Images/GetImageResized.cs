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
using System.IO;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Cache;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using MediaPortal.Common.FanArt;
using System.Web;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using Microsoft.Owin;
using System.Net;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "maxWidth", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "maxHeight", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "borders", Type = typeof(string), Nullable = true)]
  internal class GetImageResized : BaseSendData
  {
    public async Task ProcessAsync(IOwinContext context, WebMediaType type, string id, int maxWidth, int maxHeight, string borders = null)
    {
      if (id == null)
        throw new BadRequestException("GetImageResized: id is null");
      if (maxWidth == 0)
        throw new BadRequestException("GetImageResized: maxWidth is null");
      if (maxHeight == 0)
        throw new BadRequestException("GetImageResized: maxHeight is null");

      Guid idGuid;
      if (!Guid.TryParse(id, out idGuid))
        throw new BadRequestException(String.Format("GetImageResized: Couldn't parse if '{0}' to Guid", id));

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);
      MediaItem item = MediaLibraryAccess.GetMediaItemById(context, idGuid, necessaryMIATypes, null);

      var resourcePathStr = item.PrimaryResources[item.ActiveResourceLocatorIndex].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());

      var ra = GetResourceAccessor(resourcePath);
      IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
        throw new InternalServerException("GetImage: failed to create IFileSystemResourceAccessor");

      // Resize
      ImageCache.CacheIdentifier identifier = ImageCache.GetIdentifier(idGuid, false, maxWidth, maxHeight, borders, 0, FanArtTypes.Undefined, FanArtMediaTypes.Image);
      byte[] resizedImage;

      if (ImageCache.TryGetImageFromCache(context, identifier, out resizedImage))
      {
        Logger.Info("GetImageResized: Got image from cache");
      }
      else
      {
        using (var resourceStream = fsra.OpenRead())
        {
          byte[] buffer = new byte[resourceStream.Length];
          resourceStream.Read(buffer, 0, Convert.ToInt32(resourceStream.Length));
          resizedImage = Plugins.MP2Extended.WSS.Images.ResizeImage(buffer, maxWidth, maxHeight, borders);
        }

        // Add to cache
        if (ImageCache.AddImageToCache(context, resizedImage, identifier))
          Logger.Info("GetImageResized: Added image to cache");
      }

      using (var resourceStream = new MemoryStream(resizedImage))
      {
        // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
        if (!string.IsNullOrEmpty(context.Request.Headers["If-Modified-Since"]))
        {
          DateTime lastRequest = DateTime.Parse(context.Request.Headers["If-Modified-Since"]);
          if (lastRequest.CompareTo(fsra.LastChanged) <= 0)
            context.Response.StatusCode = (int)HttpStatusCode.NotModified;
        }

        // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
        context.Response.Headers["Last-Modified"] = fsra.LastChanged.ToUniversalTime().ToString("r");

        string byteRangesSpecifier = context.Request.Headers["Range"];
        IList<Range> ranges = ParseByteRanges(byteRangesSpecifier, resourceStream.Length);
        bool onlyHeaders = context.Request.Method == "HEAD" || context.Response.StatusCode == (int)HttpStatusCode.NotModified;
        if (ranges != null && ranges.Count > 0)
          // We only support last range
          await SendRangeAsync(context, resourceStream, ranges[ranges.Count - 1], onlyHeaders);
        else
          await SendWholeFileAsync(context, resourceStream, onlyHeaders);
      }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
