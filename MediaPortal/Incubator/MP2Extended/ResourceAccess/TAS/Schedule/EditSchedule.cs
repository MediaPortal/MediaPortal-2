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
  internal class EditSchedule
  {
    public WebBoolResult Process(int scheduleId, int? channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      if (!ServiceRegistration.IsRegistered<ITvProvider>())
        throw new BadRequestException("EditSchedule: ITvProvider not found");

      IChannelAndGroupInfo channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfo;
      IScheduleControl scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControl;

      IChannel channel;
      if (!channelAndGroupInfo.GetChannel(channelId.Value, out channel))
        throw new BadRequestException(string.Format("EditSchedule: Couldn't get channel with Id: {0}", channelId));

      IList<ISchedule> schedules;
      ISchedule scheduleSrc;
      if (!scheduleControl.GetSchedules(out schedules))
        throw new BadRequestException("EditSchedule: Couldn't get schedules");
      else
      {
        scheduleSrc = schedules.Single(x => x.ScheduleId == scheduleId);
      }
      
      bool result = scheduleControl.EditSchedule(scheduleSrc, 
        (channelId != null) ? channel : null, 
        title, 
        startTime,
        endTime,
        (scheduleType != null) ? (ScheduleRecordingType?)scheduleType : (ScheduleRecordingType?)null,
        preRecordInterval,
        postRecordInterval,
        directory,
        priority);


      return new WebBoolResult { Result = result };
    }

    internal static ILogger Logger
    {
      get { return ServiceRegistration.Get<ILogger>(); }
    }
  }
}