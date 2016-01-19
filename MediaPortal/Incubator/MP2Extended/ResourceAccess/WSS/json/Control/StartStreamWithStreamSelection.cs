using System;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using System.Net;
using System.Linq;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using System.Net.Sockets;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Utilities.Network;
using System.Collections.Generic;
using HttpServer.Sessions;
using MediaPortal.Plugins.MP2Extended.Utils;
using MediaPortal.Plugins.Transcoding.Service;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "profileName", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "startPosition", Type = typeof(long), Nullable = false)]
  [ApiFunctionParam(Name = "audioId", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "subtitleId", Type = typeof(string), Nullable = true)]
  internal class StartStreamWithStreamSelection : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      
      string identifier = httpParam["identifier"].Value;
      string profileName = httpParam["profileName"].Value;
      string startPosition = httpParam["startPosition"].Value;
      string audioId = httpParam["audioId"].Value;
      string subtitleId = httpParam["subtitleId"].Value;

      if (identifier == null)
        throw new BadRequestException("StartStreamWithStreamSelection: identifier is null");
      if (profileName == null)
        throw new BadRequestException("StartStreamWithStreamSelection: profileName is null");
      if (startPosition == null)
        throw new BadRequestException("StartStreamWithStreamSelection: startPosition is null");

      long startPositionLong;
      if (!long.TryParse(startPosition, out startPositionLong))
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: Couldn't parse startPosition '{0}' to long", startPosition));

      int audioTrack = -1;
      if (audioId != null && !int.TryParse(audioId, out audioTrack))
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: Couldn't parse audioId '{0}' to int", audioId));

      int subtitleTrack = -1;
      if (subtitleId != null && !int.TryParse(subtitleId, out subtitleTrack))
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: Couldn't parse subtitleId '{0}' to int", subtitleId));

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
      streamItem.StartPosition = startPositionLong;

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
        if (audioTrack >= 0)
          ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceAudioStreamIndex = audioTrack;
        if (subtitleTrack >= 0)
          ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceSubtitleStreamIndex = subtitleTrack;
        else
          ((VideoTranscoding)streamItem.TranscoderObject.TranscodingParameter).SourceSubtitleStreamIndex = MediaConverter.NO_SUBTITLE;
      }

      StreamControl.StartStreaming(identifier, startPositionLong);

      string filePostFix = "&file=media.ts";
      if (profile.MediaTranscoding != null && profile.MediaTranscoding.Video != null)
      {
        foreach (var target in profile.MediaTranscoding.Video)
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
