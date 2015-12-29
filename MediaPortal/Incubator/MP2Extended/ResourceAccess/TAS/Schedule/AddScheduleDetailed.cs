using System;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Plugins.MP2Extended.Attributes;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "Adds a new Schedule with detailed infomration to TVE.")]
  [ApiFunctionParam(Name = "channelId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "title", Type = typeof(string), Nullable = false)]
  [ApiFunctionParam(Name = "startTime", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "endTime", Type = typeof(DateTime), Nullable = false)]
  [ApiFunctionParam(Name = "scheduleType", Type = typeof(WebScheduleType), Nullable = false)]
  [ApiFunctionParam(Name = "preRecordInterval", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "postRecordInterval", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "directory", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "priority", Type = typeof(int), Nullable = true)]
  internal class AddScheduleDetailed : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string channelId = httpParam["channelId"].Value;
      string title = httpParam["title"].Value;
      string startTime = httpParam["startTime"].Value;
      string endTime = httpParam["endTime"].Value;
      string scheduleType = httpParam["scheduleType"].Value;
      string preRecordInterval = httpParam["preRecordInterval"].Value;
      string postRecordInterval = httpParam["postRecordInterval"].Value;
      string directory = httpParam["directory"].Value;
      string priority = httpParam["priority"].Value;

      if (channelId == null)
        throw new BadRequestException("AddScheduleDetailed: channelId is null");
      if (title == null)
        throw new BadRequestException("AddScheduleDetailed: title is null");
      if (startTime == null)
        throw new BadRequestException("AddScheduleDetailed: startTime is null");
      if (endTime == null)
        throw new BadRequestException("AddScheduleDetailed: endTime is null");
      if (scheduleType == null)
        throw new BadRequestException("AddScheduleDetailed: scheduleType is null");

      int channelIdInt;
      if (!int.TryParse(channelId, out channelIdInt))
        throw new BadRequestException(string.Format("AddScheduleDetailed: Couldn't parse channelId to int: {0}", channelId));
      DateTime startDateTime;
      if (!DateTime.TryParse(startTime, out startDateTime))
        throw new BadRequestException(string.Format("AddScheduleDetailed: Couldn't parse startTime to DateTime: {0}", startTime));
      DateTime endDateTime;
      if (!DateTime.TryParse(endTime, out endDateTime))
        throw new BadRequestException(string.Format("AddScheduleDetailed: Couldn't parse endTime to DateTime: {0}", endTime));

      int preRecordIntervalInt = -1;
      int.TryParse(preRecordInterval, out preRecordIntervalInt);

      int postRecordIntervalInt = -1;
      int.TryParse(postRecordInterval, out postRecordIntervalInt);

      int priorityInt = -1;
      int.TryParse(priority, out priorityInt);

      ScheduleRecordingType scheduleRecordingType = (ScheduleRecordingType)JsonConvert.DeserializeObject(scheduleType, typeof(ScheduleRecordingType));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("AddScheduleDetailed: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      bool result = false;

      IChannel channel;
      ISchedule schedule;
      if (channelAndGroupInfo.GetChannel(channelIdInt, out channel))
        result = scheduleControl.CreateScheduleDetailed(channel, title, startDateTime, endDateTime, scheduleRecordingType, preRecordIntervalInt, postRecordIntervalInt, directory, priorityInt, out schedule);


      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}