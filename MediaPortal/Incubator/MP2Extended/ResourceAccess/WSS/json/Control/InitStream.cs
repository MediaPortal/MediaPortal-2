using System;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Timeshiftings;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.Transcoding.Aspects;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.Transcoding.Service;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.Control
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "clientDescription", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "identifier", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "idleTimeout", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "type", Type = typeof(int), Nullable = true)]
  internal class InitStream : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string itemId = httpParam["itemId"].Value;
      string clientDescription = httpParam["clientDescription"].Value;
      string identifier = httpParam["identifier"].Value;
      string idleTimeout = httpParam["idleTimeout"].Value;
      string itemTypeInt = httpParam["type"].Value;

      if (itemId == null)
        throw new BadRequestException("InitStream: itemId is null");
      if (clientDescription == null)
        throw new BadRequestException("InitStream: clientDescription is null");
      if (identifier == null)
        throw new BadRequestException("InitStream: identifier is null");

      WebMediaType itemType = WebMediaType.Movie;
      int parsedType = 0;
      if (int.TryParse(itemTypeInt, out parsedType))
        itemType = (WebMediaType)parsedType;

      StreamItem streamItem = new StreamItem
      {
        ItemType = itemType,
        ClientDescription = clientDescription,
        ClientIp = request.Headers["remote_addr"] ?? string.Empty
      };

      MediaItem mediaItem = null;
      if (itemType == WebMediaType.TV || itemType == WebMediaType.Radio)
      {
        if (!ServiceRegistration.IsRegistered<ITvProvider>())
          throw new BadRequestException("InitStream: ITvProvider not found");

        IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;

        int channelIdInt;
        if (!int.TryParse(itemId, out channelIdInt))
          throw new BadRequestException(string.Format("InitStream: Couldn't convert channelId to int: {0}", itemId));

        IChannel channel;
        if (!channelAndGroupInfo.GetChannel(channelIdInt, out channel))
          throw new BadRequestException(string.Format("InitStream: Couldn't get channel with Id: {0}", channelIdInt));

        ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;

        if (!timeshiftControl.StartTimeshift(identifier, SlotControl.GetSlotIndex(identifier), channel, out mediaItem))
          throw new BadRequestException("InitStream: Couldn't start timeshifting");    
   
        streamItem.Title = "Live TV";
        if (itemType == WebMediaType.Radio) streamItem.Title = "Live Radio";
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

        mediaItem = GetMediaItems.GetMediaItemById(itemGuid, necessaryMIATypes, optionalMIATypes);
        if (mediaItem == null)
        {
          throw new BadRequestException(string.Format("InitStream: Couldn't init stream! No Mediaitem found with id: {0}", itemId));
        }
        streamItem.Title = (string)mediaItem.Aspects[MediaAspect.ASPECT_ID][MediaAspect.ATTR_TITLE];
      }
      streamItem.RequestedMediaItem = mediaItem;

      int idleTimeoutInt = -1;
      if (idleTimeout != null)
        int.TryParse(idleTimeout, out idleTimeoutInt);
      streamItem.IdleTimeout = idleTimeoutInt;

      // Add the stream to the stream controler
      StreamControl.AddStreamItem(identifier, streamItem);

      return new WebBoolResult { Result = true};
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
