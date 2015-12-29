using System;
using System.Collections.Generic;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Extensions.MetadataExtractors.Aspects;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Recording
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "id", Type = typeof(int), Nullable = false)]
  internal class GetRecordingById : BaseRecordingBasic, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string id = httpParam["id"].Value;
      if (id == null)
        throw new BadRequestException("GetRecordingById: id is null");
      
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetRecordingById: ITvProvider not found");

      ISet<Guid> necessaryMIATypes = new HashSet<Guid>();
      necessaryMIATypes.Add(MediaAspect.ASPECT_ID);
      necessaryMIATypes.Add(ProviderResourceAspect.ASPECT_ID);
      necessaryMIATypes.Add(ImporterAspect.ASPECT_ID);
      necessaryMIATypes.Add(VideoAspect.ASPECT_ID);
      necessaryMIATypes.Add(RecordingAspect.ASPECT_ID);

      MediaItem item = GetMediaItems.GetMediaItemById(id, necessaryMIATypes);


      WebRecordingBasic webRecordingBasic = RecordingBasic(item);



      return webRecordingBasic;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}