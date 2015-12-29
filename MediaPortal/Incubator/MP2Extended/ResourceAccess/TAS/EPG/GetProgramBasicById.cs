using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "programId", Type = typeof(int), Nullable = false)]
  internal class GetProgramBasicById : BaseProgramBasic, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string programId = httpParam["programId"].Value;

      if (programId == null)
        throw new BadRequestException("GetProgramBasicById: channelId is null");

      int programIdInt;
      if (!int.TryParse(programId, out programIdInt))
        throw new BadRequestException(string.Format("GetProgramBasicById: Couldn't parse programId to int: {0}", programId));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetCurrentProgramOnChannel: ITvProvider not found");

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IProgram program;
      if (!programInfo.GetProgram(programIdInt, out program))
        Logger.Warn("GetProgramBasicById: Couldn't get Now/Next Info for channel with Id: {0}", programIdInt);

      WebProgramBasic webProgramBasic = ProgramBasic(program);


      return webProgramBasic;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}