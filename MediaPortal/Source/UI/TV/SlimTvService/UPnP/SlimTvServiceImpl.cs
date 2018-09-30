#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.DataTypes;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Plugins.SlimTv.UPnP;
using UPnP.Infrastructure.Common;
using UPnP.Infrastructure.Dv;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace MediaPortal.Plugins.SlimTv.Service.UPnP
{
  public class SlimTvServiceImpl : DvService
  {
    public SlimTvServiceImpl()
      : base(Consts.SLIMTV_SERVICE_TYPE, Consts.SLIMTV_SERVICE_TYPE_VERSION, Consts.SLIMTV_SERVICE_ID)
    {
      #region DvStateVariable definitions

      DvStateVariable A_ARG_TYPE_SlotIndex = new DvStateVariable("A_ARG_TYPE_SlotIndex", new DvStandardDataType(UPnPStandardDataType.Int)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_SlotIndex);

      DvStateVariable A_ARG_TYPE_ChannelId = new DvStateVariable("A_ARG_TYPE_ChannelId", new DvStandardDataType(UPnPStandardDataType.Int)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_ChannelId);

      DvStateVariable A_ARG_TYPE_ProgramId = new DvStateVariable("A_ARG_TYPE_ProgramId", new DvStandardDataType(UPnPStandardDataType.Int)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_ProgramId);

      DvStateVariable A_ARG_TYPE_ChannelGroupId = new DvStateVariable("A_ARG_TYPE_ChannelGroupId", new DvStandardDataType(UPnPStandardDataType.Int)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_ChannelGroupId);

      DvStateVariable A_ARG_TYPE_Bool = new DvStateVariable("A_ARG_TYPE_Bool", new DvStandardDataType(UPnPStandardDataType.Boolean)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Bool);

      DvStateVariable A_ARG_TYPE_String = new DvStateVariable("A_ARG_TYPE_String", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_String);

      DvStateVariable A_ARG_TYPE_DateTime = new DvStateVariable("A_ARG_TYPE_DateTime", new DvStandardDataType(UPnPStandardDataType.DateTime)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_DateTime);

      DvStateVariable A_ARG_TYPE_MediaItem = new DvStateVariable("A_ARG_TYPE_MediaItem", new DvExtendedDataType(UPnPDtLiveTvMediaItem.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_MediaItem);

      DvStateVariable A_ARG_TYPE_ChannelGroups = new DvStateVariable("A_ARG_TYPE_ChannelGroups", new DvExtendedDataType(UPnPDtChannelGroupList.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_ChannelGroups);

      DvStateVariable A_ARG_TYPE_Channels = new DvStateVariable("A_ARG_TYPE_Channels", new DvExtendedDataType(UPnPDtChannelList.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Channels);

      DvStateVariable A_ARG_TYPE_Program = new DvStateVariable("A_ARG_TYPE_Program", new DvExtendedDataType(UPnPDtProgram.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Program);

      DvStateVariable A_ARG_TYPE_Programs = new DvStateVariable("A_ARG_TYPE_Programs", new DvExtendedDataType(UPnPDtProgramList.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Programs);

      DvStateVariable A_ARG_TYPE_Schedules = new DvStateVariable("A_ARG_TYPE_Schedules", new DvExtendedDataType(UPnPDtScheduleList.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Schedules);

      DvStateVariable A_ARG_TYPE_Schedule = new DvStateVariable("A_ARG_TYPE_Schedule", new DvExtendedDataType(UPnPDtSchedule.Instance)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_Schedule);

      DvStateVariable A_ARG_TYPE_ScheduleRecordingType = new DvStateVariable("A_ARG_TYPE_ScheduleRecordingType", new DvStandardDataType(UPnPStandardDataType.Int)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_ScheduleRecordingType);

      DvStateVariable A_ARG_TYPE_RecordingStatus = new DvStateVariable("A_ARG_TYPE_RecordingStatus", new DvStandardDataType(UPnPStandardDataType.String)) { SendEvents = false };
      AddStateVariable(A_ARG_TYPE_RecordingStatus);

      #endregion

      #region Init / Deinit

      DvAction deInit = new DvAction(Consts.ACTION_DEINIT, OnDeInit,
                             new DvArgument[] { },
                             new DvArgument[] { new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true) });

      AddAction(deInit);

      #endregion

      #region ITimeshiftControl actions

      DvAction startTimeshift = new DvAction(Consts.ACTION_START_TIMESHIFT, OnStartTimeshift,
                                   new[]
                                     {
                                       new DvArgument("SlotIndex", A_ARG_TYPE_SlotIndex, ArgumentDirection.In),
                                       new DvArgument("ChannelId", A_ARG_TYPE_ChannelId, ArgumentDirection.In),
                                     },
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("TimeshiftMediaItem", A_ARG_TYPE_MediaItem, ArgumentDirection.Out, false)
                                     });
      AddAction(startTimeshift);

      DvAction stopTimeshift = new DvAction(Consts.ACTION_STOP_TIMESHIFT, OnStopTimeshift,
                                   new[]
                                     {
                                       new DvArgument("SlotIndex", A_ARG_TYPE_SlotIndex, ArgumentDirection.In)
                                     },
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
                                     });
      AddAction(stopTimeshift);

      #endregion

      #region IChannelAndGroupInfo actions

      DvAction getChannelGroups = new DvAction(Consts.ACTION_GET_CHANNELGROUPS, OnGetChannelGroups,
                                   new DvArgument[0],
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("ChannelGroups", A_ARG_TYPE_ChannelGroups, ArgumentDirection.Out, false)
                                     });
      AddAction(getChannelGroups);

      DvAction getChannel = new DvAction(Consts.ACTION_GET_CHANNEL, OnGetChannel,
                                   new[]
                                     {
                                       new DvArgument("ChannelId", A_ARG_TYPE_ChannelId, ArgumentDirection.In)
                                     },
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Channels", A_ARG_TYPE_Channels, ArgumentDirection.Out, false)
                                     });
      AddAction(getChannel);

      DvAction getChannels = new DvAction(Consts.ACTION_GET_CHANNELS, OnGetChannels,
                                   new[]
                                     {
                                       new DvArgument("ChannelGroupId", A_ARG_TYPE_ChannelGroupId, ArgumentDirection.In)
                                     },
                                   new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Channels", A_ARG_TYPE_Channels, ArgumentDirection.Out, false)
                                     });
      AddAction(getChannels);

      #endregion

      #region IProgramInfo members

      DvAction getPrograms = new DvAction(Consts.ACTION_GET_PROGRAMS, OnGetPrograms,
                             new[]
                                     {
                                       new DvArgument("ChannelId", A_ARG_TYPE_ChannelId, ArgumentDirection.In),
                                       new DvArgument("TimeFrom", A_ARG_TYPE_DateTime, ArgumentDirection.In),
                                       new DvArgument("TimeTo", A_ARG_TYPE_DateTime, ArgumentDirection.In)
                                     },
                             new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Programs", A_ARG_TYPE_Programs, ArgumentDirection.Out, false)
                                     });
      AddAction(getPrograms);

      DvAction getProgramsByTitle = new DvAction(Consts.ACTION_GET_PROGRAMS_BY_TITLE, OnGetProgramsByTitle,
                             new[]
                                     {
                                       new DvArgument("Title", A_ARG_TYPE_String, ArgumentDirection.In),
                                       new DvArgument("TimeFrom", A_ARG_TYPE_DateTime, ArgumentDirection.In),
                                       new DvArgument("TimeTo", A_ARG_TYPE_DateTime, ArgumentDirection.In)
                                     },
                             new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Programs", A_ARG_TYPE_Programs, ArgumentDirection.Out, false)
                                     });
      AddAction(getProgramsByTitle);

      DvAction getProgramsGroup = new DvAction(Consts.ACTION_GET_PROGRAMS_GROUP, OnGetProgramsGroup,
                             new[]
                                     {
                                       new DvArgument("ChannelGroupId", A_ARG_TYPE_ChannelGroupId, ArgumentDirection.In),
                                       new DvArgument("TimeFrom", A_ARG_TYPE_DateTime, ArgumentDirection.In),
                                       new DvArgument("TimeTo", A_ARG_TYPE_DateTime, ArgumentDirection.In)
                                     },
                             new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Programs", A_ARG_TYPE_Programs, ArgumentDirection.Out, false)
                                     });
      AddAction(getProgramsGroup);

      DvAction getNowNextProgram = new DvAction(Consts.ACTION_GET_NOW_NEXT_PROGRAM, OnGetNowNextProgram,
                             new[]
                                     {
                                       new DvArgument("ChannelId", A_ARG_TYPE_ChannelId, ArgumentDirection.In)
                                     },
                             new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("ProgramNow", A_ARG_TYPE_Program, ArgumentDirection.Out, false),
                                       new DvArgument("ProgramNext", A_ARG_TYPE_Program, ArgumentDirection.Out, false)
                                     });
      AddAction(getNowNextProgram);

      DvAction getNowNextProgramForGroup = new DvAction(Consts.ACTION_GET_NOW_NEXT_PROGRAM_FOR_GROUP, OnGetNowNextProgramForGroup,
                             new[]
                                     {
                                       new DvArgument("ChannelGroupId", A_ARG_TYPE_ChannelGroupId, ArgumentDirection.In)
                                     },
                             new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Programs", A_ARG_TYPE_Programs, ArgumentDirection.Out, false),
                                     });
      AddAction(getNowNextProgramForGroup);

      #endregion

      #region IScheduleControl members

      DvAction getSchedules = new DvAction(Consts.ACTION_GET_SCHEDULES, OnGetSchedules,
                            new DvArgument[0],
                            new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Schedules", A_ARG_TYPE_Schedules, ArgumentDirection.Out, false)
                                     });
      AddAction(getSchedules);

      DvAction createSchedule = new DvAction(Consts.ACTION_CREATE_SCHEDULE, OnCreateSchedule,
                            new[]
                                     {
                                       new DvArgument("ProgramId", A_ARG_TYPE_ProgramId, ArgumentDirection.In),
                                       new DvArgument("ScheduleRecordingType", A_ARG_TYPE_ScheduleRecordingType, ArgumentDirection.In)
                                     },
                            new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Schedule", A_ARG_TYPE_Schedule, ArgumentDirection.Out, false)
                                     });
      AddAction(createSchedule);

      DvAction createScheduleByTime = new DvAction(Consts.ACTION_CREATE_SCHEDULE_BY_TIME, OnCreateScheduleByTime,
                            new[]
                                     {
                                       new DvArgument("ChannelId", A_ARG_TYPE_ChannelId, ArgumentDirection.In),
                                       new DvArgument("TimeFrom", A_ARG_TYPE_DateTime, ArgumentDirection.In),
                                       new DvArgument("TimeTo", A_ARG_TYPE_DateTime, ArgumentDirection.In),
                                       new DvArgument("ScheduleRecordingType", A_ARG_TYPE_ScheduleRecordingType, ArgumentDirection.In)
                                     },
                            new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Schedule", A_ARG_TYPE_Schedule, ArgumentDirection.Out, false)
                                     });
      AddAction(createScheduleByTime);

      DvAction removeScheduleForProgram = new DvAction(Consts.ACTION_REMOVE_SCHEDULE_FOR_PROGRAM, OnRemoveScheduleForProgram,
                            new[]
                                     {
                                       new DvArgument("ProgramId", A_ARG_TYPE_ProgramId, ArgumentDirection.In),
                                       new DvArgument("ScheduleRecordingType", A_ARG_TYPE_ScheduleRecordingType, ArgumentDirection.In)
                                     },
                            new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
                                     });
      AddAction(removeScheduleForProgram);

      DvAction removeSchedule = new DvAction(Consts.ACTION_REMOVE_SCHEDULE, OnRemoveSchedule,
                            new[]
                                     {
                                       new DvArgument("Schedule", A_ARG_TYPE_Schedule, ArgumentDirection.In),
                                     },
                            new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true)
                                     });
      AddAction(removeSchedule);

      DvAction getProgramsForSchedule = new DvAction(Consts.ACTION_GET_PROGRAMS_FOR_SCHEDULE, OnGetProgramsForSchedule,
                       new[]
                                     {
                                       new DvArgument("Schedule", A_ARG_TYPE_Schedule, ArgumentDirection.In),
                                     },
                       new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Programs", A_ARG_TYPE_Programs, ArgumentDirection.Out, false)
                                     });
      AddAction(getProgramsForSchedule);

      DvAction getRecordingStatus = new DvAction(Consts.ACTION_GET_REC_STATUS, OnGetRecordingStatus,
                            new[]
                                     {
                                       new DvArgument("ProgramId", A_ARG_TYPE_ProgramId, ArgumentDirection.In),
                                     },
                            new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("RecordingStatus", A_ARG_TYPE_RecordingStatus, ArgumentDirection.Out, false)
                                     });
      AddAction(getRecordingStatus);

      DvAction getRecordingFileOrStream = new DvAction(Consts.ACTION_GET_REC_FILE_OR_STREAM, OnGetRecordingFileOrStream,
                            new[]
                                     {
                                       new DvArgument("ProgramId", A_ARG_TYPE_ProgramId, ArgumentDirection.In),
                                     },
                            new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("FileOrStream", A_ARG_TYPE_String, ArgumentDirection.Out, false)
                                     });
      AddAction(getRecordingFileOrStream);

      DvAction isCurrentlyRecording = new DvAction(Consts.ACTION_GET_IS_CURRENT_REC, OnIsCurrentlyRecording,
                            new[]
                                     {
                                       new DvArgument("FileName", A_ARG_TYPE_String, ArgumentDirection.In)
                                     },
                            new[]
                                     {
                                       new DvArgument("Result", A_ARG_TYPE_Bool, ArgumentDirection.Out, true),
                                       new DvArgument("Schedule", A_ARG_TYPE_Schedule, ArgumentDirection.Out, false)
                                     });
      AddAction(isCurrentlyRecording);

      #endregion
    }

    private UPnPError OnStartTimeshift(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
      if (timeshiftControl == null)
        return new UPnPError(500, "ITimeshiftControl service not available");

      int slotIndex = (int)inParams[0];
      int channelId = (int)inParams[1];

      // We use the client's RemoteAdress as unique "user name", so we do not need to pass this argument from clients via UPnP.
      AsyncResult<MediaItem> result = timeshiftControl.StartTimeshiftAsync(BuildUserName(context), slotIndex, new Channel { ChannelId = channelId }).Result;
      outParams = new List<object> { result.Success, result.Result };
      return null;
    }

    private UPnPError OnStopTimeshift(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
      if (timeshiftControl == null)
        return new UPnPError(500, "ITimeshiftControl service not available");

      int slotIndex = (int)inParams[0];

      // We use the client's RemoteAdress as unique "user name", so we do not need to pass this argument from clients via UPnP.
      bool result = timeshiftControl.StopTimeshiftAsync(BuildUserName(context), slotIndex).Result;
      outParams = new List<object> { result };
      return null;
    }

    private string BuildUserName(CallContext context)
    {
      return IsLocalClient(context) ? AbstractSlimTvService.LOCAL_USERNAME : context.RemoteAddress;
    }

    private static bool IsLocalClient(CallContext context)
    {
      return String.Equals(context.RemoteAddress, context.Endpoint.EndPointIPAddress.ToString(), StringComparison.InvariantCultureIgnoreCase);
    }

    private UPnPError OnDeInit(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      ITimeshiftControlEx timeshiftControl = ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx;
      if (timeshiftControl == null)
        return new UPnPError(500, "ITimeshiftControl service not available");

      // We use the client's RemoteAdress as unique "user name", so we do not need to pass this argument from clients via UPnP.
      var t1 = timeshiftControl.StopTimeshiftAsync(BuildUserName(context), 0);
      var t2 = timeshiftControl.StopTimeshiftAsync(BuildUserName(context), 1);
      Task.WaitAll(t1, t2);
      outParams = new List<object> { true };
      return null;
    }

    private UPnPError OnGetChannelGroups(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
      if (channelAndGroupInfo == null)
        return new UPnPError(500, "IChannelAndGroupInfo service not available");

      AsyncResult<IList<IChannelGroup>> result = channelAndGroupInfo.GetChannelGroupsAsync().Result;
      outParams = new List<object> { result.Success, result.Result };
      return null;
    }

    private UPnPError OnGetChannel(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
      if (channelAndGroupInfo == null)
        return new UPnPError(500, "IChannelAndGroupInfo service not available");

      int channelId = (int)inParams[0];

      AsyncResult<IChannel> result = channelAndGroupInfo.GetChannelAsync(channelId).Result;
      outParams = new List<object> { result.Success, new List<IChannel> { result.Result } };
      return null;
    }

    private UPnPError OnGetChannels(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
      if (channelAndGroupInfo == null)
        return new UPnPError(500, "IChannelAndGroupInfo service not available");

      int channelGroupId = (int)inParams[0];

      AsyncResult<IList<IChannel>> result = channelAndGroupInfo.GetChannelsAsync(new ChannelGroup { ChannelGroupId = channelGroupId }).Result;
      outParams = new List<object> { result.Success, result.Result };
      return null;
    }

    private UPnPError OnGetPrograms(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      if (programInfo == null)
        return new UPnPError(500, "IProgramInfo service not available");

      int channelId = (int)inParams[0];
      DateTime timeFrom = (DateTime)inParams[1];
      DateTime timeTo = (DateTime)inParams[2];

      AsyncResult<IList<IProgram>> result = programInfo.GetProgramsAsync(new Channel { ChannelId = channelId }, timeFrom, timeTo).Result;
      outParams = new List<object> { result.Success, result.Result };
      return null;
    }

    private UPnPError OnGetProgramsByTitle(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      if (programInfo == null)
        return new UPnPError(500, "IProgramInfo service not available");

      string title = (string)inParams[0];
      DateTime timeFrom = (DateTime)inParams[1];
      DateTime timeTo = (DateTime)inParams[2];

      AsyncResult<IList<IProgram>> result = programInfo.GetProgramsAsync(title, timeFrom, timeTo).Result;
      outParams = new List<object> { result.Success, result.Result };
      return null;
    }

    private UPnPError OnGetProgramsGroup(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      if (programInfo == null)
        return new UPnPError(500, "IProgramInfo service not available");

      int channelGroupId = (int)inParams[0];
      DateTime timeFrom = (DateTime)inParams[1];
      DateTime timeTo = (DateTime)inParams[2];

      AsyncResult<IList<IProgram>> result = programInfo.GetProgramsGroupAsync(new ChannelGroup { ChannelGroupId = channelGroupId }, timeFrom, timeTo).Result;
      outParams = new List<object> { result.Success, result.Result };
      return null;
    }

    private UPnPError OnGetNowNextProgram(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      if (programInfo == null)
        return new UPnPError(500, "IProgramInfo service not available");

      int channelId = (int)inParams[0];
      AsyncResult<IProgram[]> result = programInfo.GetNowNextProgramAsync(new Channel { ChannelId = channelId }).Result;
      outParams = new List<object> { result.Success, result.Result[0], result.Result[1] };
      return null;
    }

    private UPnPError OnGetNowNextProgramForGroup(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      if (programInfo == null)
        return new UPnPError(500, "IProgramInfo service not available");

      int channelGroupId = (int)inParams[0];
      IList<IProgram> programs = new List<IProgram>();
      // Flatten to simple list for UPnP transfer
      var result = programInfo.GetNowAndNextForChannelGroupAsync(new ChannelGroup { ChannelGroupId = channelGroupId }).Result;
      if (result.Success)
      {
        foreach (KeyValuePair<int, IProgram[]> nowNextProgram in result.Result)
          foreach (var program in nowNextProgram.Value)
            if (program != null)
              programs.Add(program);
      }
      outParams = new List<object> { true, programs };
      return null;
    }

    private UPnPError OnGetSchedules(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      if (programInfo == null || scheduleControl == null)
        return new UPnPError(500, "IProgramInfo or IScheduleControl service not available");

      AsyncResult<IList<ISchedule>> result = scheduleControl.GetSchedulesAsync().Result;

      outParams = new List<object> { result.Success, result.Result };
      return null;
    }

    private UPnPError OnCreateSchedule(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      if (programInfo == null || scheduleControl == null)
        return new UPnPError(500, "IProgramInfo or IScheduleControl service not available");

      int programId = (int)inParams[0];
      ScheduleRecordingType recordingType = (ScheduleRecordingType)inParams[1];
      IProgram program;
      ISchedule schedule = null;
      bool result = false;
      if (programInfo.GetProgram(programId, out program))
      {
        var scheduleResult = scheduleControl.CreateScheduleAsync(program, recordingType).Result;
        result = scheduleResult.Success;
        schedule = scheduleResult.Result;
      }

      outParams = new List<object> { result, schedule };
      return null;
    }

    private UPnPError OnCreateScheduleByTime(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
      if (scheduleControl == null || channelAndGroupInfo == null)
        return new UPnPError(500, "IChannelAndGroupInfo or IScheduleControl service not available");

      int channelId = (int)inParams[0];
      DateTime startTime = (DateTime)inParams[1];
      DateTime endTime = (DateTime)inParams[2];
      ScheduleRecordingType recordingType = (ScheduleRecordingType)inParams[3];
      ISchedule schedule = null;

      AsyncResult<IChannel> result = channelAndGroupInfo.GetChannelAsync(channelId).Result;
      if (result.Success)
      {
        var scheduleResult = scheduleControl.CreateScheduleByTimeAsync(result.Result, startTime, endTime, recordingType).Result;
        if (scheduleResult.Success)
          schedule = scheduleResult.Result;
      }

      outParams = new List<object> { result.Success, schedule };
      return null;
    }

    private UPnPError OnRemoveScheduleForProgram(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      if (programInfo == null || scheduleControl == null)
        return new UPnPError(500, "IProgramInfo or IScheduleControl service not available");

      int programId = (int)inParams[0];
      ScheduleRecordingType recordingType = (ScheduleRecordingType)inParams[1];
      IProgram program;
      var result = programInfo.GetProgram(programId, out program) && scheduleControl.RemoveScheduleForProgramAsync(program, recordingType).Result;

      outParams = new List<object> { result };
      return null;
    }

    private UPnPError OnRemoveSchedule(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      if (programInfo == null || scheduleControl == null)
        return new UPnPError(500, "IProgramInfo or IScheduleControl service not available");

      ISchedule schedule = (ISchedule)inParams[0];
      bool result = scheduleControl.RemoveScheduleAsync(schedule).Result;

      outParams = new List<object> { result };
      return null;
    }

    private UPnPError OnGetProgramsForSchedule(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      if (scheduleControl == null)
        return new UPnPError(500, "IProgramInfo or IScheduleControl service not available");

      ISchedule schedule = (ISchedule)inParams[0];

      AsyncResult<IList<IProgram>> result = scheduleControl.GetProgramsForScheduleAsync(schedule).Result;

      outParams = new List<object> { result.Success, result.Result };
      return null;
    }

    private UPnPError OnGetRecordingStatus(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      if (programInfo == null || scheduleControl == null)
        return new UPnPError(500, "IProgramInfo or IScheduleControl service not available");

      int programId = (int)inParams[0];

      bool result = false;
      IProgram program;
      RecordingStatus recordingStatus = RecordingStatus.None;
      if (programInfo.GetProgram(programId, out program))
      {
        var scheduleResult = scheduleControl.GetRecordingStatusAsync(program).Result;
        if (scheduleResult.Success)
        {
          result = true;
          recordingStatus = scheduleResult.Result;
        }
      }

      outParams = new List<object> { result, recordingStatus.ToString() };
      return null;
    }

    private UPnPError OnGetRecordingFileOrStream(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IProgramInfoAsync programInfo = ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync;
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      if (programInfo == null || scheduleControl == null)
        return new UPnPError(500, "IProgramInfo or IScheduleControl service not available");

      int programId = (int)inParams[0];
      IProgram program;
      bool result = false;
      string fileOrStream = null;
      if (programInfo.GetProgram(programId, out program))
      {
        var scheduleResult = scheduleControl.GetRecordingFileOrStreamAsync(program).Result;
        if (scheduleResult.Success)
        {
          result = true;
          fileOrStream = scheduleResult.Result;
        }
      }

      outParams = new List<object> { result, fileOrStream };
      return null;
    }

    private UPnPError OnIsCurrentlyRecording(DvAction action, IList<object> inParams, out IList<object> outParams, CallContext context)
    {
      outParams = new List<object>();
      IScheduleControlAsync scheduleControl = ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync;
      if (scheduleControl == null)
        return new UPnPError(500, "IProgramInfo or IScheduleControl service not available");

      string fileName = (string)inParams[0];
      AsyncResult<ISchedule> result = scheduleControl.IsCurrentlyRecordingAsync(fileName).Result;

      outParams = new List<object> { result.Success, result.Result };
      return null;
    }
  }
}
