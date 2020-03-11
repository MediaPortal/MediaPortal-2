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

using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.OnlineVideos;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images.BaseClasses;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Owin;
using System.IO;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream.Images
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Stream, Summary = "Returns the Thumbnail for Sites, GlobalSites, Categories, Subcategories and Videos. This function uses the OnlineVideos Cache, not the MP2Ext Cache.")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "mediatype", Type = typeof(WebOnlineVideosMediaType), Nullable = false)]
  internal class GetOnlineVideosArtwork : BaseGetArtwork
  {
    public static async Task ProcessAsync(IOwinContext context, WebOnlineVideosMediaType mediatype, string id)
    {
      if (id == null)
        throw new BadRequestException("GetOnlineVideosArtwork: id is null");

      Stream resourceStream = ImageFile(OnlineVideosThumbs.GetThumb(mediatype, id));
      context.Response.ContentType = "image/*";
      await SendWholeFileAsync(context, resourceStream, false);
      resourceStream.Dispose();
    }
  }
}
