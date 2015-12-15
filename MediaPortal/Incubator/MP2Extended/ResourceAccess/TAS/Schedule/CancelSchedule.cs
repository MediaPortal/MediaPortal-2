using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "programId", Type = typeof(int), Nullable = false)]
  internal class CancelSchedule : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string programId = httpParam["programId"].Value;

      if (programId == null)
        throw new BadRequestException("CancelSchedule: programId is null");

      int programIdInt;
      if (!int.TryParse(programId, out programIdInt))
        throw new BadRequestException(string.Format("CancelSchedule: Couldn't parse programId to int: {0}", programId));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("CancelSchedule: ITvProvider not found");

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;
      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      bool result = false;

      IProgram program;
      if (programInfo.GetProgram(programIdInt, out program))
        result = scheduleControl.RemoveScheduleForProgram(program, ScheduleRecordingType.Once);  // TODO: not sure if ScheduleRecordingType is right


      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}