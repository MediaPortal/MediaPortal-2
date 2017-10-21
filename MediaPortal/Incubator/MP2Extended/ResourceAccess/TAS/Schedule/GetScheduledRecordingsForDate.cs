using System;
using System.Collections.Generic;
using System.Linq;
using HttpServer;
using HttpServer.Sessions;
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
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "")]
  [ApiFunctionParam(Name = "date", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetScheduledRecordingsForDate : BaseScheduledRecording
  {
    public IList<WebScheduledRecording> Process(DateTime date, WebSortField? sort, WebSortOrder? order, string filter = null)
    {
       if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetScheduledRecordingsForDate: ITvProvider not found");

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IList<ISchedule> schedules;
      scheduleControl.GetSchedules(out schedules);

      List<WebScheduledRecording> output = schedules.Select(schedule => ScheduledRecording(schedule)).Where(x => x.StartTime.Date == date.Date).ToList();

      // sort and filter
      if (sort != null && order != null)
      {
        output = output.Filter(filter).SortScheduledRecordingList(sort, order).ToList();
      }
      else
        output = output.Filter(filter).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}