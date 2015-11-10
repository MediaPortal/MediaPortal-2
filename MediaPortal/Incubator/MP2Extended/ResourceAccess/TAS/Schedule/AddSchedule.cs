using System;
using HttpServer;
using HttpServer.Exceptions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule
{
  internal class AddSchedule : IRequestMicroModuleHandler
  {
    // TODO: ScheduleType is not supported by MP2 SlimTv
    public dynamic Process(IHttpRequest request)
    {
      HttpParam httpParam = request.Param;
      string channelId = httpParam["channelId"].Value;
      string title = httpParam["title"].Value;
      string startTime = httpParam["startTime"].Value;
      string endTime = httpParam["endTime"].Value;
      string scheduleType = httpParam["scheduleType"].Value;

      if (channelId == null)
        throw new BadRequestException("AddSchedule: channelId is null");
      if (title == null)
        throw new BadRequestException("AddSchedule: title is null");
      if (startTime == null)
        throw new BadRequestException("AddSchedule: startTime is null");
      if (endTime == null)
        throw new BadRequestException("AddSchedule: endTime is null");
      if (scheduleType == null)
        throw new BadRequestException("AddSchedule: scheduleType is null");

      int channelIdInt;
      if (!int.TryParse(channelId, out channelIdInt))
        throw new BadRequestException(string.Format("AddSchedule: Couldn't parse channelId to int: {0}", channelId));
      DateTime startDateTime;
      if (!DateTime.TryParse(startTime, out startDateTime))
        throw new BadRequestException(string.Format("AddSchedule: Couldn't parse startTime to DateTime: {0}", startTime));
      DateTime endDateTime;
      if (!DateTime.TryParse(endTime, out endDateTime))
        throw new BadRequestException(string.Format("AddSchedule: Couldn't parse endTime to DateTime: {0}", endTime));

      WebScheduleType webScheduleType = (WebScheduleType)JsonConvert.DeserializeObject(scheduleType, typeof(WebScheduleType));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("AddSchedule: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IChannel channel;
      if (!channelAndGroupInfo.GetChannel(channelIdInt, out channel))
        throw new BadRequestException(string.Format("AddSchedule: Couldn't get channel with Id: {0}", channelIdInt));
      
      ISchedule schedule;
      bool result = scheduleControl.CreateScheduleByTime(channel, startDateTime, endDateTime, out schedule);


      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}