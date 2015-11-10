using System;
using MediaPortal.Common;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule.BaseClasses
{
  class BaseScheduledRecording
  {
    internal WebScheduledRecording ScheduledRecording(ISchedule schedule)
    {
      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IChannel channel;
      string channelName = "";
      if (!channelAndGroupInfo.GetChannel(schedule.ChannelId, out channel))
        channelName = channel.Name;

      WebScheduledRecording webScheduledRecording = new WebScheduledRecording
        {
          Title = schedule.Name,
          ChannelId = schedule.ChannelId,
          EndTime = schedule.EndTime,
          StartTime = schedule.StartTime,
          ScheduleId = schedule.ScheduleId,
          //ProgramId = schedule,
          ChannelName = channelName
        };

      return webScheduledRecording;
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
