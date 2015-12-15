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
  [ApiFunctionParam(Name = "scheduleId", Type = typeof(int), Nullable = false)]
  internal class DeleteSchedule : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string scheduleId = httpParam["scheduleId"].Value;

      if (scheduleId == null)
        throw new BadRequestException("DeleteSchedule: scheduleId is null");

      int scheduleIdInt;
      if (!int.TryParse(scheduleId, out scheduleIdInt))
        throw new BadRequestException(string.Format("DeleteSchedule: Couldn't parse scheduleId to int: {0}", scheduleId));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("DeleteSchedule: ITvProvider not found");

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IList<ISchedule> schedules;
      scheduleControl.GetSchedules(out schedules);

      bool result = scheduleControl.RemoveSchedule(schedules.Single(x => x.ScheduleId == scheduleIdInt));


      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}