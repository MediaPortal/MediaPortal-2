#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Extensions.TranscodingService.Interfaces;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "profileName", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "startPosition", Type = typeof(long), Nullable = false)]
  internal class StartStream
  {
    public static async Task<WebStringResult> ProcessAsync(IOwinContext context, string identifier, string profileName, long startPosition)
    {
      if (identifier == null)
        throw new BadRequestException("InitStream: identifier is null");
      if (profileName == null)
        throw new BadRequestException("InitStream: profileName is null");

      StreamItem streamItem = await StreamControl.GetStreamItemAsync(identifier);
      if (streamItem == null)
        throw new BadRequestException(string.Format("StartStream: Unknown identifier: {0}", identifier));

      // Prefer getting profile by name
      EndPointProfile profile = ProfileManager.Profiles.Values.FirstOrDefault(p => p.Name == profileName);
      // If no ptofile with the specified name, see if there's one with a matching id
      if (profile == null && !ProfileManager.Profiles.TryGetValue(profileName, out profile))
        throw new BadRequestException(string.Format("StartStream: Unknown profile: {0}", profileName));

      streamItem.Profile = profile;
      // Seeking is not supported in live streams
      streamItem.StartPosition = streamItem.RequestedMediaItem is LiveTvMediaItem ? 0 : startPosition;

      Guid? userId = ResourceAccessUtils.GetUser(context);
      streamItem.TranscoderObject = new ProfileMediaItem(identifier, profile, streamItem.IsLive);
      await streamItem.TranscoderObject.Initialize(userId, streamItem.RequestedMediaItem, null);
      if (streamItem.TranscoderObject.TranscodingParameter is VideoTranscoding vt)
        vt.HlsBaseUrl = string.Format("RetrieveStream?identifier={0}&hls=", identifier);

      await StreamControl.StartStreamingAsync(identifier, startPosition);

      string filePostFix = "&file=media.ts";
      if (profile.MediaTranscoding?.VideoTargets?.Any(t => t.Target.VideoContainerType == VideoContainer.Hls) ?? false)
        filePostFix = "&file=manifest.m3u8"; //Must be added for some clients to work (Android mostly)

      string url = GetBaseStreamUrl.GetBaseStreamURL(context) + "/MPExtended/StreamingService/stream/RetrieveStream?identifier=" + identifier + filePostFix;
      return new WebStringResult { Result = url };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
