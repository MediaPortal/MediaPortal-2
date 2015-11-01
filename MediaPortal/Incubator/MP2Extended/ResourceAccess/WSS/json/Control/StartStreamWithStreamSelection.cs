using System;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using System.Net;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Services.ResourceAccess.Settings;
using System.Net.Sockets;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Network;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  internal class StartStreamWithStreamSelection : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
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

      if (!ProfileManager.Profiles.ContainsKey(profileName))
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: unknown profile: {0}", profileName));

      if (!StreamControl.ValidateIdentifie(identifier))
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: unknown identifier: {0}", identifier));

      EndPointProfile profile = ProfileManager.Profiles[profileName];

      StreamItem streamItem = StreamControl.GetStreamItem(identifier);
      streamItem.Profile = profile;
      streamItem.StartPosition = startPositionLong;
      streamItem.AudioStream = audioTrack;
      streamItem.SubtitleStream = subtitleTrack;

      string filePostFix = "&file=media.ts";
      if (profile.MediaTranscoding != null && profile.MediaTranscoding.Video != null)
      {
        foreach (var target in profile.MediaTranscoding.Video)
        {
          if (target.Target.VideoContainerType == Transcoding.Service.VideoContainer.Hls)
          {
            filePostFix = "&file=playlist.m3u8"; //Must be added for some clients to work
            break;
          }
        }
      }

      // Add the stream to the stream controler
      StreamControl.AddStreamItem(identifier, streamItem);

      // TODO: Return the proper URL
      return new WebStringResult { Result = GetBaseStreamURL() + "/MPExtended/StreamingService/stream/RetrieveStream?identifier=" + identifier + filePostFix };
    }

    private static IPAddress GetLocalIp()
    {
      bool useIPv4 = true;
      bool useIPv6 = false;
      ServerSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<ServerSettings>();
      if (settings.UseIPv4) useIPv4 = true;
      if (settings.UseIPv6) useIPv6 = true;

      var host = Dns.GetHostEntry(Dns.GetHostName());
      IPAddress ip6 = null;
      foreach (var ip in host.AddressList)
      {
        if (IPAddress.IsLoopback(ip) == true)
        {
          continue;
        }
        if (useIPv4)
        {
          if (ip.AddressFamily == AddressFamily.InterNetwork)
          {
            return ip;
          }
        }
        if (useIPv6)
        {
          if (ip.AddressFamily == AddressFamily.InterNetworkV6)
          {
            ip6 = ip;
          }
        }
      }
      if (ip6 != null)
      {
        return ip6;
      }
      return null;
    }

    private static string GetBaseStreamURL()
    {
      var rs = ServiceRegistration.Get<IResourceServer>();
      return "http://" + NetworkUtils.IPAddrToString(GetLocalIp()) + ":" + rs.GetPortForIP(GetLocalIp());
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
