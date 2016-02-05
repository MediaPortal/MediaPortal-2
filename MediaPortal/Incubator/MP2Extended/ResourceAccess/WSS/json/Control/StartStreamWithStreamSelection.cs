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

using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNet.Http;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.Transcoding.Service;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Utils;

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
    public WebStringResult Process(HttpContext httpContext, string identifier, string profileName, long startPosition, int audioId = -1, int subtitleId = -1)
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

      if (!StreamControl.ValidateIdentifie(identifier))
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: unknown identifier: {0}", identifier));


      StreamItem streamItem = StreamControl.GetStreamItem(identifier);
      streamItem.Profile = profile;
      streamItem.StartPosition = startPosition;

      bool isLive = false;
      if (streamItem.ItemType == Common.WebMediaType.TV || streamItem.ItemType == Common.WebMediaType.Radio)
      {
        isLive = true;
      }
      EndPointSettings endPointSettings = ProfileManager.GetEndPointSettings(profile.ID);
      streamItem.TranscoderObject = new ProfileMediaItem(identifier, streamItem.RequestedMediaItem, endPointSettings, isLive);
      if ((streamItem.TranscoderObject.TranscodingParameter is VideoTranscoding))
      {
        ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).HlsBaseUrl = string.Format("RetrieveStream?identifier={0}&hls=", identifier);
        if (audioId >= 0)
          ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceAudioStreamIndex = audioId;
        if (subtitleId >= 0)
          ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceSubtitleStreamIndex = subtitleId;
        else
          ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceSubtitleStreamIndex = MediaConverter.NO_SUBTITLE;
      }

      StreamControl.StartStreaming(identifier, startPosition);

      string filePostFix = "&file=media.ts";
      if (profile.MediaTranscoding != null && profile.MediaTranscoding.Video != null)
      {
        foreach (var target in profile.MediaTranscoding.Video)
        {
          if (target.Target.VideoContainerType == VideoContainer.Hls)
          {
            filePostFix = "&file=manifest.m3u8"; //Must be added for some clients to work (Android mostly)
            break;
          }
        }
      }

      string url = GetBaseStreamUrl.GetBaseStreamURL(httpContext) + "/MPExtended/StreamingService/stream/RetrieveStream?identifier=" + identifier + filePostFix;
      return new WebStringResult { Result = url };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
