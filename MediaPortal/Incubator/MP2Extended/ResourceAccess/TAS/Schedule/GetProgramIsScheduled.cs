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
  internal class GetProgramIsScheduled : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string programId = httpParam["programId"].Value;

      if (programId == null)
        throw new BadRequestException("GetProgramIsScheduled: programId is null");

      int programIdInt;
      if (!int.TryParse(programId, out programIdInt))
        throw new BadRequestException(string.Format("GetProgramIsScheduled: Couldn't parse programId to int: {0}", programId));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetProgramIsScheduled: ITvProvider not found");

      IProgramInfo programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfo;
      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IProgram program;
      if (!programInfo.GetProgram(programIdInt, out program))
        throw new BadRequestException(string.Format("GetProgramIsScheduled: Program with ID={0} not found", programIdInt));

      RecordingStatus recordingStatus;
      scheduleControl.GetRecordingStatus(program, out recordingStatus);

      bool result = recordingStatus == RecordingStatus.Recording || recordingStatus == RecordingStatus.RuleScheduled
        || recordingStatus == RecordingStatus.Scheduled || recordingStatus == RecordingStatus.SeriesScheduled;


      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}