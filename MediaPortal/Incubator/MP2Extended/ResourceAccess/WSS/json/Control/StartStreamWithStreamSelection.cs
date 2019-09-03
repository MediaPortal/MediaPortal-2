﻿#region Copyright (C) 2007-2012 Team MediaPortal

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

using System.Linq;
using System.Collections.Generic;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using System.Web;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Extensions.TranscodingService.Interfaces;
using System.Threading.Tasks;
using Microsoft.Owin;
using System;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "profileName", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "startPosition", Type = typeof(long), Nullable = false)]
  [ApiFunctionParam(Name = "audioId", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "subtitleId", Type = typeof(string), Nullable = true)]
  internal class StartStreamWithStreamSelection
  {
    public static async Task<WebStringResult> ProcessAsync(IOwinContext context, string identifier, string profileName, long startPosition, int audioId = -1, int subtitleId = -1)
    {
      if (identifier == null)
        throw new BadRequestException("StartStreamWithStreamSelection: identifier is null");
      if (profileName == null)
        throw new BadRequestException("StartStreamWithStreamSelection: profileName is null");

      EndPointProfile profile = null;
      List<EndPointProfile> namedProfiles = ProfileManager.Profiles.Where(x => x.Value.Name == profileName).Select(namedProfile => namedProfile.Value).ToList();
      if (namedProfiles.Count > 0)
      {
        profile = namedProfiles[0];
      }
      else if (ProfileManager.Profiles.ContainsKey(profileName))
      {
        profile = ProfileManager.Profiles[profileName];
      }
      if (profile == null)
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: unknown profile: {0}", profileName));

      StreamItem streamItem = await StreamControl.GetStreamItemAsync(identifier);
      if (streamItem == null)
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: unknown identifier: {0}", identifier));

      streamItem.Profile = profile;
      streamItem.StartPosition = startPosition;
      if (streamItem.RequestedMediaItem is LiveTvMediaItem)
      {
        streamItem.StartPosition = 0;
      }

      Guid? userId = ResourceAccessUtils.GetUser(context);
      streamItem.TranscoderObject = new ProfileMediaItem(identifier, streamItem.RequestedMediaItem, profile, streamItem.IsLive);
      await streamItem.TranscoderObject.Initialize(userId, audioId >= 0 ? audioId : (int?)null, subtitleId);
      if ((streamItem.TranscoderObject.TranscodingParameter is VideoTranscoding vt))
      {
        vt.HlsBaseUrl = string.Format("RetrieveStream?identifier={0}&hls=", identifier);
      }

      await StreamControl.StartStreamingAsync(identifier, startPosition);

      string filePostFix = "&file=media.ts";
      if (profile.MediaTranscoding != null && profile.MediaTranscoding.VideoTargets != null)
      {
        foreach (var target in profile.MediaTranscoding.VideoTargets)
        {
          if (target.Target.VideoContainerType == VideoContainer.Hls)
          {
            filePostFix = "&file=manifest.m3u8"; //Must be added for some clients to work (Android mostly)
            break;
          }
        }
      }

      string url = GetBaseStreamUrl.GetBaseStreamURL(context) + "/MPExtended/StreamingService/stream/RetrieveStream?identifier=" + identifier + filePostFix;
      return new WebStringResult { Result = url };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
