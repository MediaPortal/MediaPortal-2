using System;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG
{
  internal class SearchProgramsDetailed : BaseProgramDetailed, IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string searchTerm = httpParam["searchTerm"].Value;

      if (searchTerm == null)
        throw new BadRequestException("SearchProgramsDetailed: searchTerm is null");



      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("SearchProgramsDetailed: ITvProvider not found");

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;

      IProgram program;
      if (!programInfo.)
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