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
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using Newtonsoft.Json;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule
{
  [ApiFunctionDescription(Type = ApiFunctionDescription.FunctionType.Json, Summary = "Enables you to edit a already existend schedule.")]
  [ApiFunctionParam(Name = "scheduleId", Type = typeof(int), Nullable = false)]
  [ApiFunctionParam(Name = "channelId", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "title", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "startTime", Type = typeof(DateTime), Nullable = true)]
  [ApiFunctionParam(Name = "endTime", Type = typeof(DateTime), Nullable = true)]
  [ApiFunctionParam(Name = "scheduleType", Type = typeof(WebScheduleType), Nullable = true)]
  [ApiFunctionParam(Name = "preRecordInterval", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "postRecordInterval", Type = typeof(int), Nullable = true)]
  [ApiFunctionParam(Name = "directory", Type = typeof(string), Nullable = true)]
  [ApiFunctionParam(Name = "priority", Type = typeof(int), Nullable = true)]
  internal class EditSchedule : IRequestMicroModuleHandler
  {
    public dynamic Process(IHttpRequest request, IHttpSession session)
    {
      HttpParam httpParam = request.Param;
      string scheduleId = httpParam["scheduleId"].Value;
      string channelId = httpParam["channelId"].Value;
      string title = httpParam["title"].Value;
      string startTime = httpParam["startTime"].Value;
      string endTime = httpParam["endTime"].Value;
      string scheduleType = httpParam["scheduleType"].Value;
      string preRecordInterval = httpParam["preRecordInterval"].Value;
      string postRecordInterval = httpParam["postRecordInterval"].Value;
      string directory = httpParam["directory"].Value;
      string priority = httpParam["priority"].Value;

      int scheduleIdInt;
      if (!int.TryParse(scheduleId, out scheduleIdInt))
        throw new BadRequestException(string.Format("EditSchedule: Couldn't parse scheduleId to int: {0}", scheduleId));

      int channelIdInt;
      int.TryParse(channelId, out channelIdInt);

      DateTime startDateTime;
      DateTime.TryParse(startTime, out startDateTime);

      DateTime endDateTime;
      DateTime.TryParse(endTime, out endDateTime);

      int preRecordIntervalInt = -1;
      int.TryParse(preRecordInterval, out preRecordIntervalInt);

      int postRecordIntervalInt = -1;
      int.TryParse(postRecordInterval, out postRecordIntervalInt);

      int priorityInt = -1;
      int.TryParse(priority, out priorityInt);

      ScheduleRecordingType scheduleRecordingType = ScheduleRecordingType.Once;
      if (scheduleType != null)
        scheduleRecordingType = (ScheduleRecordingType)JsonConvert.DeserializeObject(scheduleType, typeof(ScheduleRecordingType));

      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("EditSchedule: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IChannel channel;
      if (!channelAndGroupInfo.GetChannel(channelIdInt, out channel))
        throw new BadRequestException(string.Format("EditSchedule: Couldn't get channel with Id: {0}", channelIdInt));

      IList<ISchedule> schedules;
      ISchedule scheduleSrc;
      if (!scheduleControl.GetSchedules(out schedules))
        throw new BadRequestException("EditSchedule: Couldn't get schedules");
      else
      {
        scheduleSrc = schedules.Single(x => x.ScheduleId == scheduleIdInt);
      }
      
      bool result = scheduleControl.EditSchedule(scheduleSrc, 
        (channelId != null) ? channel : null, 
        title, 
        (startTime != null) ? startDateTime : (DateTime?)null,
        (endTime != null) ? endDateTime : (DateTime?)null,
        (scheduleType != null) ? scheduleRecordingType: (ScheduleRecordingType?)null,
        (preRecordInterval != null) ? preRecordIntervalInt : (int?)null,
        (postRecordInterval != null) ? postRecordIntervalInt : (int?)null,
        directory,
        (priority != null) ? priorityInt : (int?)null);


      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}