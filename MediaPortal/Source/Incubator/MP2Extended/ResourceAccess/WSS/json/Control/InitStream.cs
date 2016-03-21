#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using Microsoft.AspNet.Http;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.Transcoding.Interfaces.Aspects;
using MediaPortal.Plugins.Transcoding.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "clientDescription", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "type", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "idleTimeout", Type = typeof(int), Nullable = true)]
  internal class InitStream
  {
    public WebBoolResult Process(HttpContext httpContext, string itemId, string clientDescription, string identifier, WebMediaType type, int? idleTimeout)
    {

      if (itemId == null)
        throw new BadRequestException("InitStream: itemId is null");
      if (clientDescription == null)
        throw new BadRequestException("InitStream: clientDescription is null");
      if (identifier == null)
        throw new BadRequestException("InitStream: identifier is null");

      StreamItem streamItem = new StreamItem
      {
        ItemType = type,
        ClientDescription = clientDescription,
        IdleTimeout = idleTimeout ?? -1,
        ClientIp = httpContext.Request.Headers["remote_addr"]
      };

      MediaItem mediaItem = null;
      if (streamItem.ItemType == WebMediaType.TV || streamItem.ItemType == WebMediaType.Radio)
      {
        int channelIdInt;
        if (!int.TryParse(itemId, out channelIdInt))
          throw new BadRequestException(string.Format("InitStream: Couldn't convert channelId to int: {0}", itemId));

        streamItem.Title = "Live TV";
        if (streamItem.ItemType == WebMediaType.Radio) streamItem.Title = "Live Radio";
        streamItem.LiveChannelId = channelIdInt;

        if(MediaAnalyzer.ParseChannelStream(channelIdInt, out mediaItem) == null)
        {
          throw new BadRequestException(string.Format("InitStream: Couldn't parse channel stream: {0}", channelIdInt));
        }
      }
      else
      {
        Guid itemGuid;
        if (!Guid.TryParse(itemId, out itemGuid))
          throw new BadRequestException(string.Format("InitStream: Couldn't parse itemId: {0}", itemId));

        ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
        necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
        necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);

        ISet<Guid> optionalMIATypes = new HashSet<Guid>();
        optionalMIATypes.Add(VideoAspect.ASPECT_ID);
        optionalMIATypes.Add(AudioAspect.ASPECT_ID);
        optionalMIATypes.Add(ImageAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemAudioAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemImageAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemVideoAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemVideoAudioAspect.ASPECT_ID);
        optionalMIATypes.Add(TranscodeItemVideoEmbeddedAspect.ASPECT_ID);

        mediaItem = GetMediaItems.GetMediaItemById(itemGuid, necessaryMIATypes, optionalMIATypes);
        if (mediaItem == null)
        {
          throw new BadRequestException(string.Format("InitStream: Couldn't init stream! No MediaItem found with id: {0}", itemId));
        }
        streamItem.Title = (string)mediaItem[MediaAspect.Metadata].GetAttributeValue(MediaAspect.ATTR_TITLE);
      }
      streamItem.RequestedMediaItem = mediaItem;

      // Add the stream to the stream controller
      StreamControl.AddStreamItem(identifier, streamItem);

      return new WebBoolResult { Result = true };
    }

    internal static IMediaAnalyzer MediaAnalyzer
    {
      get { return ServiceRegistration.Get<IMediaAnalyzer>(); }
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
