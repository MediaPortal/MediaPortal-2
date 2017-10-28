using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.Exceptions;
using MediaPortal.Plugins.MP2Extended.Extensions;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule.BaseClasses;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  internal class GetScheduledRecordingsForToday : BaseScheduledRecording
  {
    public IList<WebScheduledRecording> Process(WebSortField? sort, WebSortOrder? order, string filter = null)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetScheduledRecordingsForToday: ITvProvider not found");

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IList<ISchedule> schedules;
      scheduleControl.GetSchedules(out schedules);

      var output = schedules.Select(schedule => ScheduledRecording(schedule)).Where(x => x.StartTime.Date == DateTime.Now.Date)
        .Filter(filter);

      // sort and filter
      if (sort != null && order != null)
        output = output.SortScheduledRecordingList(sort, order);

      return output.ToList();
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}
