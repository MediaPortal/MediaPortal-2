using System.Net;
using System.Net.Sockets;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.Transcoding.Service;
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
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Utils;
using Microsoft.AspNet.Http;

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
      streamItem.AudioStream = audioId;
      streamItem.SubtitleStream = subtitleId;

      string filePostFix = "&file=media.ts";
      if (profile.MediaTranscoding != null && profile.MediaTranscoding.Video != null)
      {
        foreach (var target in profile.MediaTranscoding.Video)
        {
          if (target.Target.VideoContainerType == VideoContainer.Hls)
          {
            filePostFix = "&file=playlist.m3u8"; //Must be added for some clients to work (Android mostly)
            break;
          }
        }
      }

      // Add the stream to the stream controler
      StreamControl.AddStreamItem(identifier, streamItem);
      
      string url = GetBaseStreamUrl.GetBaseStreamURL(httpContext) + "/MPExtended/StreamingService/stream/RetrieveStream?identifier=" + identifier + filePostFix;
      return new WebStringResult { Result = url };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
