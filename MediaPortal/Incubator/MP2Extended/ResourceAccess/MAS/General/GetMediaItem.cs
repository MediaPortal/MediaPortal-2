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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MP2Extended.Extensions;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(string), Nullable = false)]
  internal class GetMediaItem
  {
    public static Task<WebMediaItem> ProcessAsync(IOwinContext context, string id)
    {
      if (id == null)
        throw new BadRequestException("GetMediaItem: id is null");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(EpisodeAspect.ASPECT_ID);
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      MediaItem item = MediaLibraryAccess.GetMediaItemById(context, Guid.Parse(id), necessaryMIATypes, optionalMIATypes);
      if (item == null)
        throw new NotFoundException(String.Format("GetMediaItem: No MediaItem found with id: {0}", id));

      WebMediaItem webMediaItem = new WebMediaItem();
      webMediaItem.Id = item.MediaItemId.ToString();
      webMediaItem.Artwork = ResourceAccessUtils.GetWebArtwork(item);
      webMediaItem.DateAdded = item.GetAspect(ImporterAspect.Metadata).GetAttributeValue<DateTime>(ImporterAspect.ATTR_DATEADDED);
      webMediaItem.Path = ResourceAccessUtils.GetPaths(item);
      webMediaItem.Type = ResourceAccessUtils.GetWebMediaType(item);
      webMediaItem.Title = item.GetAspect(MediaAspect.Metadata).GetAttributeValue<string>(MediaAspect.ATTR_TITLE);

      return Task.FromResult(webMediaItem);
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
