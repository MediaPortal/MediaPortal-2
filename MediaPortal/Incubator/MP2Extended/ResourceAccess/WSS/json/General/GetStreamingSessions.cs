using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.WSS.General;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.General
{
  // Todo: Add the missing information
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetStreamingSessions : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);

      return StreamControl.GetStreamItems().Select(streamItem => new WebStreamingSession
      {
        ClientDescription = streamItem.Value.ClientDescription, 
        Profile = streamItem.Value.Profile.Name, 
        Identifier = streamItem.Key, 
        StartPosition = streamItem.Value.StartPosition, 
        TranscodingInfo = new WebTranscodingInfo(), // TODO: We don't have these information, yet
        StartTime = streamItem.Value.StartTime, 
        SourceId = streamItem.Value.ItemId.ToString(),
        ClientIPAddress = streamItem.Value.ClientIp,
        DisplayName = (string)GetMediaItems.GetMediaItemById(streamItem.Value.ItemId, necessaryMIATypes)[MediaAspect.Metadata][MediaAspect.ATTR_TITLE],
      }).ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}