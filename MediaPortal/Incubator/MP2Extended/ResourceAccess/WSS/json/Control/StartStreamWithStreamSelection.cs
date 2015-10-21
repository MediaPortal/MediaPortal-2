using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.MAS.General;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  // TODO: Add Stream Selection
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
      if (audioId == null)
        throw new BadRequestException("StartStreamWithStreamSelection: audioId is null");
      if (subtitleId == null)
        throw new BadRequestException("StartStreamWithStreamSelection: subtitleId is null");

      long startPositionLong;
      if (!long.TryParse(startPosition, out startPositionLong))
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: Couldn't parse startPosition '{0}' to long", startPosition));

      int audioTrack;
      if (audioId != null && !int.TryParse(audioId, out audioTrack))
        throw new BadRequestException(string.Format("StartStreamWithStreamSelection: Couldn't parse audioId '{0}' to int", audioId));

      int subtitleTrack;
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

      StreamControl.UpdateStreamItem(identifier, streamItem);

      // Add the stream to the stream controler
      StreamControl.AddStreamItem(identifier, streamItem);

      // TODO: Return the proper URL
      return new WebStringResult { Result = "http://192.168.178.26:26405/MPExtended/StreamingService/stream/RetrieveStream?identifier=" + identifier };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}