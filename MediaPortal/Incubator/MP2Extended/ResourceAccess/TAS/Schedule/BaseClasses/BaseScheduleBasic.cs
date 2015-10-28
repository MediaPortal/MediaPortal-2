using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule.BaseClasses
{
  class BaseScheduleBasic
  {
    internal WebScheduleBasic ScheduleBasic(ISchedule schedule)
    {
       WebScheduleBasic webScheduleBasic = new WebScheduleBasic
        {
          Title = schedule.Name,
          Id = schedule.ScheduleId,
          ChannelId = schedule.ChannelId,
          EndTime = schedule.EndTime,
          StartTime = schedule.StartTime,
          PostRecordInterval = Convert.ToInt32(schedule.PostRecordInterval.TotalMinutes),
          PreRecordInterval = Convert.ToInt32(schedule.PostRecordInterval.TotalMinutes),
          ScheduleType = ConvertTo<WebScheduleType>(schedule.RecordingType),
          Priority = (int)schedule.Priority,
          KeepMethod = ConvertTo<WebScheduleKeepMethod>(schedule.KeepMethod),
        };
      if (schedule.ParentScheduleId != null)
        webScheduleBasic.ParentScheduleId = schedule.ParentScheduleId.Value;
      if (schedule.KeepDate != null)
        webScheduleBasic.KeepDate = schedule.KeepDate.Value;

      return webScheduleBasic;
    }

    public static T ConvertTo<T>(object value)
      where T : struct,IConvertible
    {
      var sourceType = value.GetType();
      if (!sourceType.IsEnum)
        throw new ArgumentException("Source type is not enum");
      if (!typeof(T).IsEnum)
        throw new ArgumentException("Destination type is not enum");
      return (T)Enum.Parse(typeof(T), value.ToString());
    }
  }
}
