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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http.Controllers;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  internal class GetMediaItem : BaseSendData
  {
    public async Task ProcessAsync(IOwinContext context, Guid itemId)
    {
      // Grab the media item given in the request.
      try
      {
        Logger.Debug("GetMediaItem: Attempting to load mediaitem {0}", itemId.ToString());

        // Attempt to grab the media item from the database.
        ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
        necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
        necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
        necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
        var item = MediaLibraryAccess.GetMediaItemById(context, itemId, necessaryMIATypes, null);
        if (item == null)
          throw new BadRequestException(string.Format("Media item '{0}' not found.", itemId));

        // Grab the mimetype from the media item and set the Content Type header.
        // TODO: Fix
        string mimeType = item?.PrimaryResources?.FirstOrDefault()?.GetAttributeValue<string>(ProviderResourceAspect.ATTR_MIME_TYPE) ?? "video/*";
        context.Response.ContentType = mimeType;

        // Grab the resource path for the media item.
        var resourcePathStr = item.PrimaryResources[item.ActiveResourceLocatorIndex].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
        var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());

        var ra = GetResourceAccessor(resourcePath);
        IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
        if (fsra == null)
          throw new InternalServerException("GetMediaItem: failed to create IFileSystemResourceAccessor");

        using (var resourceStream = fsra.OpenRead())
        {
          // HTTP/1.1 RFC2616 section 14.25 'If-Modified-Since'
          if (!string.IsNullOrEmpty(context.Response.Headers["If-Modified-Since"]))
          {
            DateTime lastRequest = DateTime.Parse(context.Response.Headers["If-Modified-Since"]);
            if (lastRequest.CompareTo(fsra.LastChanged) <= 0)
              context.Response.StatusCode = (int)HttpStatusCode.NotModified;
          }

          // HTTP/1.1 RFC2616 section 14.29 'Last-Modified'
          context.Response.Headers["Last-Modified"] = fsra.LastChanged.ToUniversalTime().ToString("r");

          string byteRangesSpecifier = context.Request.Headers["Range"];
          IList<Range> ranges = ParseByteRanges(byteRangesSpecifier, resourceStream.Length);
          bool onlyHeaders = context.Request.Method == "HEAD" || context.Response.StatusCode == (int)HttpStatusCode.NotModified;
          if (ranges != null)
            // We only support last range
            await SendRangeAsync(context, resourceStream, ranges[ranges.Count - 1], onlyHeaders);
          else
            await SendWholeFileAsync(context, resourceStream, onlyHeaders);
        }
      }
      catch (FileNotFoundException ex)
      {
        throw new InternalServerException(string.Format("Failed to proccess media item '{0}'", itemId), ex);
      }
    }


    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
