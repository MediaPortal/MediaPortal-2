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
  [ApiFunctionParam(Name = "start", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "end", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "sort", Type = typeof(WebSortField), Nullable = true)]
  [ApiFunctionParam(Name = "order", Type = typeof(WebSortOrder), Nullable = true)]
  [ApiFunctionParam(Name = "filter", Type = typeof(string), Nullable = true)]
  internal class GetSchedulesByRange : BaseScheduleBasic
  {
    public IList<WebScheduleBasic> Process(int start, int end, string filter, WebSortField? sort, WebSortOrder? order)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("GetSchedulesByRange: ITvProvider not found");

      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IList<ISchedule> schedules;
      scheduleControl.GetSchedules(out schedules);

      List<WebScheduleBasic> output = schedules.Select(schedule => ScheduleBasic(schedule)).ToList();

      // sort and filter
      if (sort != null && order != null)
      {
        output = output.Filter(filter).SortScheduleList(sort, order).ToList();
      }
      else
        output = output.Filter(filter).ToList();

      // Get Range
      output = output.TakeRange(start, end).ToList();

      return output;
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}