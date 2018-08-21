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
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.BaseClasses;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  // TODO: implement offset
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetImage : BaseSendData
  {
    public async Task<bool> ProcessAsync(IOwinContext context, WebMediaType type, string id)
    {
      if (id == null)
        throw new BadRequestException("GetImage: id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImageAspect.ASPECT_ID);
      MediaItem item = MediaLibraryAccess.GetMediaItemById(context, id, necessaryMIATypes, null);

      var resourcePathStr = item.PrimaryResources[item.ActiveResourceLocatorIndex].GetAttributeValue(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH);
      var resourcePath = ResourcePath.Deserialize(resourcePathStr.ToString());

      var ra = GetResourceAccessor(resourcePath);
      IFileSystemResourceAccessor fsra = ra as IFileSystemResourceAccessor;
      if (fsra == null)
        throw new InternalServerException("GetImage: failed to create IFileSystemResourceAccessor");

      using (var resourceStream = fsra.OpenRead())
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
      return true;
    }

    internal Task ProcessAsync(object owinHttpRequestContext, WebMediaType type, string id)
    {
      throw new NotImplementedException();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
