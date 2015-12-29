using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.WSS.General;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.json.General
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "itemId", Type = typeof(string), Nullable = false)]
  internal class GetItemSupportStatus : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string itemId = httpParam["itemId"].Value;

      if (itemId == null)
        throw new BadRequestException("GetItemSupportStatus: itemId is null");


      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);

      ISet<Guid> optionalMIATypes = new HashSet<Guid>();
      optionalMIATypes.Add(VideoAspect.ASPECT_ID);
      optionalMIATypes.Add(MovieAspect.ASPECT_ID);
      optionalMIATypes.Add(SeriesAspect.ASPECT_ID);
      optionalMIATypes.Add(AudioAspect.ASPECT_ID);
      optionalMIATypes.Add(ImageAspect.ASPECT_ID);

      MediaItem item = GetMediaItems.GetMediaItemById(itemId, necessaryMIATypes, optionalMIATypes);

      bool result = item != null;

      WebItemSupportStatus webItemSupportStatus = new WebItemSupportStatus
      {
        Supported = result,
        Reason = ""
      };


      return webItemSupportStatus;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}