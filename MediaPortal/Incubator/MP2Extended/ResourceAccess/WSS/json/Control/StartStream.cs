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
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.Transcoding.Service.Objects;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "profileName", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "startPosition", Type = typeof(long), Nullable = false)]
  internal class StartStream : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      
      string identifier = httpParam["identifier"].Value;
      string profileName = httpParam["profileName"].Value;
      string startPosition = httpParam["startPosition"].Value;

      if (identifier == null)
        throw new BadRequestException("InitStream: identifier is null");
      if (profileName == null)
        throw new BadRequestException("InitStream: profileName is null");
      if (startPosition == null)
        throw new BadRequestException("InitStream: startPosition is null");

      long startPositionLong;
      if (!long.TryParse(startPosition, out startPositionLong))
        throw new BadRequestException(string.Format("InitStream: Couldn't parse startPosition '{0}' to long", startPosition));

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
        throw new BadRequestException(string.Format("StartStream: unknown profile: {0}", profileName));

      if (!StreamControl.ValidateIdentifie(identifier))
        throw new BadRequestException(string.Format("StartStream: unknown identifier: {0}", identifier));


      StreamItem streamItem = StreamControl.GetStreamItem(identifier);
      streamItem.Profile = profile;
      
      if (streamItem.RequestedMediaItem is LiveTvMediaItem)
      {
        LiveTvMediaItem tvStream = (LiveTvMediaItem)streamItem.RequestedMediaItem;
        DateTime tuningStart = (DateTime)tvStream.AdditionalProperties[LiveTvMediaItem.TUNING_TIME];
        startPositionLong = Convert.ToInt64((DateTime.Now - tuningStart).TotalSeconds);
      }
      streamItem.StartPosition = startPositionLong;

      EndPointSettings endPointSettings = ProfileManager.GetEndPointSettings(profile.ID);
      streamItem.TranscoderObject = new ProfileMediaItem(identifier, streamItem.RequestedMediaItem, endPointSettings, streamItem.IsLive);
      if ((streamItem.TranscoderObject.TranscodingParameter is VideoTranscoding))
      {
        ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).HlsBaseUrl = string.Format("RetrieveStream?identifier={0}&hls=", identifier);
        ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceSubtitleStreamIndex = MediaConverter.NO_SUBTITLE;
      }

      StreamControl.StartStreaming(identifier, startPositionLong);
   
      string filePostFix = "&file=media.ts";
      if (profile.MediaTranscoding != null && profile.MediaTranscoding.VideoTargets != null)
      {
        foreach (var target in profile.MediaTranscoding.VideoTargets)
        {
          if (target.Target.VideoContainerType == Transcoding.Service.VideoContainer.Hls)
          {
            filePostFix = "&file=manifest.m3u8"; //Must be added for some clients to work (Android mostly)
            break;
          }
        }
      }

      string url = GetBaseStreamUrl.GetBaseStreamURL() + "/MPExtended/StreamingService/stream/RetrieveStream?identifier=" + identifier + filePostFix;
      return new WebStringResult { Result = url };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
