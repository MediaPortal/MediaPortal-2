using System;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "programId", Type = typeof(int), Nullable = false)]
  internal class GetProgramDetailedById : BaseProgramDetailed, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string programId = httpParam["programId"].Value;

      if (programId == null)
        throw new BadRequestException("GetProgramDetailedById: channelId is null");

      int programIdInt;
      if (!int.TryParse(programId, out programIdInt))
        throw new BadRequestException(string.Format("GetProgramDetailedById: Couldn't parse programId to int: {0}", programId));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetProgramDetailedById: ITvProvider not found");

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IProgram program;
      if (!programInfo.GetProgram(programIdInt, out program))
        Logger.Warn("GetProgramDetailedById: Couldn't get Now/Next Info for channel with Id: {0}", programIdInt);

      WebProgramDetailed webProgramDetailed = ProgramDetailed(program);


      return webProgramDetailed;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}