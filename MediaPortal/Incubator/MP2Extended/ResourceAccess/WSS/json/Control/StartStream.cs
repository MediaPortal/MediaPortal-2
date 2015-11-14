using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Utilities.Network;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "profileName", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "startPosition", Type = typeof(long), Nullable = false)]
  internal class StartStream : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
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
      streamItem.StartPosition = startPositionLong;

      string filePostFix = "&file=media.ts";
      if (profile.MediaTranscoding != null && profile.MediaTranscoding.Video != null)
      {
        foreach (var target in profile.MediaTranscoding.Video)
        {
          if (target.Target.VideoContainerType == Transcoding.Service.VideoContainer.Hls)
          {
            filePostFix = "&file=playlist.m3u8"; //Must be added for some clients to work (Android mostly)
            break;
          }
        }
      }

      // Add the stream to the stream controler
      StreamControl.AddStreamItem(identifier, streamItem);

      string url = GetBaseStreamUrl.GetBaseStreamURL() + "/MPExtended/StreamingService/stream/RetrieveStream?identifier=" + identifier + filePostFix;
      return new WebStringResult { Result = url };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
