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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.Async;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Services.ServerCommunication;
using MediaPortal.Common.Settings;
using MediaPortal.Common.UPnP;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.Plugins.SlimTv.Providers.Helpers;
using MediaPortal.Plugins.SlimTv.Providers.Settings;
using MediaPortal.Plugins.SlimTv.UPnP;
using MediaPortal.UI.Presentation.UiNotifications;
using UPnP.Infrastructure.CP;
using UPnP.Infrastructure.CP.DeviceTree;

namespace MediaPortal.Plugins.SlimTv.Providers.UPnP
{
  public class NativeTvProxy : UPnPServiceProxyBase, IDisposable, ITvProvider, ITimeshiftControlAsync, IProgramInfoAsync, IChannelAndGroupInfoAsync, IScheduleControlAsync
  {
    #region Protected fields

    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;
    protected readonly IChannel[] _channels = new IChannel[2];
    protected readonly object _syncObj = new object();
    protected readonly ProgramCache _programCache = new ProgramCache();
    protected IDictionary<int, IChannel> _channelCache = new ConcurrentDictionary<int, IChannel>();

    #endregion

    public NativeTvProxy(CpService serviceStub)
      : base(serviceStub, "NativeTv")
    {
      ServiceRegistration.Set<ITvProvider>(this);
    }

    public void Dispose()
    {
      DeInit();
    }

    public string Name
    {
      get { return "NativeTvProxy"; }
    }

    public bool Init()
    {
      return true;
    }

    public bool DeInit()
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_DEINIT);
        // DeInit will also be called if the service is disposed after server disconnection. In this case we cannot call another action.
        if (action.IsConnected && action.ParentService != null && action.ParentService.IsConnected)
        {
          IList<object> inParameters = new List<object>();
          IList<object> outParameters = action.InvokeAction(inParameters);
          return (bool)outParameters[0];
        }
        return false;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return false;
    }

    public bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
    {
      var result = StartTimeshiftAsync(slotIndex, channel).Result;
      if (result.Success)
      {
        timeshiftMediaItem = result.Result;
        return true;
      }
      timeshiftMediaItem = null;
      return false;
    }

    public async Task<AsyncResult<MediaItem>> StartTimeshiftAsync(int slotIndex, IChannel channel)
    {
      // If we change between radio and tv channels, stop current timeshift before. This is required, so that the new
      // player starts with a new stream from beginning. Otherwise a VideoPlayer could be ran with an older Radio stream part.
      bool mediaTypeChanged = _channels[slotIndex] != null && _channels[slotIndex].MediaType != channel.MediaType;
      if (mediaTypeChanged && !StopTimeshift(slotIndex))
        return new AsyncResult<MediaItem>(false, null);
      try
      {
        CpAction action = GetAction(Consts.ACTION_START_TIMESHIFT);
        IList<object> inParameters = new List<object>
            {
              slotIndex,
              channel.ChannelId
            };

        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          _channels[slotIndex] = channel;

          // Assign a MediaItem, can be null if streamUrl is the same.
          var timeshiftMediaItem = (MediaItem)outParameters[1];
          return new AsyncResult<MediaItem>(true, timeshiftMediaItem);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<MediaItem>(false, null);
    }

    public bool StopTimeshift(int slotIndex)
    {
      return StopTimeshiftAsync(slotIndex).Result;
    }

    public async Task<bool> StopTimeshiftAsync(int slotIndex)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_STOP_TIMESHIFT);
        IList<object> inParameters = new List<object>
            {
              slotIndex
            };

        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          _channels[slotIndex] = null;
          return true;
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return false;
    }

    public IChannel GetChannel(int slotIndex)
    {
      return _channels[slotIndex];
    }

    public bool GetCurrentProgram(IChannel channel, out IProgram program)
    {
      IProgram programNext;
      return GetNowNextProgram(channel, out program, out programNext);
    }

    public bool GetNextProgram(IChannel channel, out IProgram program)
    {
      IProgram programNow;
      return GetNowNextProgram(channel, out programNow, out program);
    }

    public bool GetNowNextProgram(IChannel channel, out IProgram programNow, out IProgram programNext)
    {
      var result = GetNowNextProgramAsync(channel).Result;
      if (result.Success && result.Result?.Length == 2)
      {
        programNow = result.Result[0];
        programNext = result.Result[1];
        return true;
      }
      programNow = programNext = null;
      return false;
    }

    public async Task<AsyncResult<IProgram[]>> GetNowNextProgramAsync(IChannel channel)
    {
      ProgramNowNextValue programs;
      IProgram[] programNowNext = new IProgram[2];

      _programCache.ClearCache(channel);
      if (_programCache.TryGetPrograms(channel, out programs))
      {
        programNowNext[0] = programs.ProgramNow;
        programNowNext[1] = programs.ProgramNext;
        return new AsyncResult<IProgram[]>(true, programNowNext);
      }
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_NOW_NEXT_PROGRAM);
        IList<object> inParameters = new List<object>
            {
              channel.ChannelId
            };

        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          programNowNext[0] = (Program)outParameters[1];
          programNowNext[1] = (Program)outParameters[2];
          _programCache.TryAdd(channel, programNowNext[0], programNowNext[1]);
          return new AsyncResult<IProgram[]>(true, programNowNext);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IProgram[]>(false, null);
    }

    public async Task<AsyncResult<IDictionary<int, IProgram[]>>> GetNowAndNextForChannelGroupAsync(IChannelGroup channelGroup)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_NOW_NEXT_PROGRAM_FOR_GROUP);
        IList<object> inParameters = new List<object>
            {
              channelGroup.ChannelGroupId
            };

        var nowNextPrograms = new Dictionary<int, IProgram[]>();

        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          DateTime now = DateTime.Now;
          var programs = (IList<Program>)outParameters[1];
          foreach (Program program in programs)
          {
            IProgram[] nowNext;
            int channelId = program.ChannelId;
            if (!nowNextPrograms.TryGetValue(channelId, out nowNext))
              nowNext = new IProgram[2];

            if (program.StartTime > now)
              nowNext[1] = program;
            else
              nowNext[0] = program;

            nowNextPrograms[channelId] = nowNext;
          }
          return new AsyncResult<IDictionary<int, IProgram[]>>(true, nowNextPrograms);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IDictionary<int, IProgram[]>>(false, null);
    }

    public bool GetNowAndNextForChannelGroup(IChannelGroup channelGroup, out IDictionary<int, IProgram[]> nowNextPrograms)
    {
      var result = GetNowAndNextForChannelGroupAsync(channelGroup).Result;
      nowNextPrograms = result.Result;
      return result.Success;
    }

    public bool GetProgramsGroup(IChannelGroup channelGroup, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      var result = GetProgramsGroupAsync(channelGroup, from, to).Result;
      programs = result.Result;
      return result.Success;
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsGroupAsync(IChannelGroup channelGroup, DateTime from, DateTime to)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_PROGRAMS_GROUP);
        IList<object> inParameters = new List<object>
            {
              channelGroup.ChannelGroupId,
              from,
              to
            };

        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          IList<Program> programList = (IList<Program>)outParameters[1];
          IList<IProgram> programs = programList.Distinct(ProgramComparer.Instance).ToList();
          return new AsyncResult<IList<IProgram>>(true, programs);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public bool GetPrograms(string title, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      var result = GetProgramsAsync(title, from, to).Result;
      programs = result.Result;
      return result.Success;
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(string title, DateTime from, DateTime to)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_PROGRAMS_BY_TITLE);
        IList<object> inParameters = new List<object>
            {
              title,
              from,
              to
            };

        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          IList<Program> programList = (IList<Program>)outParameters[1];
          var programs = programList.Distinct(ProgramComparer.Instance).ToList(); // Using custom comparer to filter out duplicated programs.
          return new AsyncResult<IList<IProgram>>(true, programs);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      var result = GetProgramsAsync(channel, from, to).Result;
      programs = result.Result;
      return result.Success;
    }

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsAsync(IChannel channel, DateTime from, DateTime to)
    {
      // We only want to cache single time queries
      if (from == to)
      {
        _programCache.ClearCache(channel);
        ProgramNowNextValue programsCache;
        if (_programCache.TryGetProgramsByTime(channel, from, out programsCache))
        {
          var programs = new List<IProgram> { programsCache.ProgramNow };
          return new AsyncResult<IList<IProgram>>(true, programs);
        }
      }
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_PROGRAMS);
        IList<object> inParameters = new List<object>
            {
              channel.ChannelId,
              from,
              to
            };

        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          IList<Program> programList = (IList<Program>)outParameters[1];
          var programs = programList.Distinct(ProgramComparer.Instance).ToList(); // Using custom comparer to filter out duplicated programs.
          if (from == to)
          {
            _programCache.TryAdd(channel, programs[0], null);
          }
          return new AsyncResult<IList<IProgram>>(true, programs);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    //public bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    //{
    //  var result = GetProgramsForScheduleAsync(schedule).Result;
    //  programs = result.Result;
    //  return result.Success;
    //}

    public async Task<AsyncResult<IList<IProgram>>> GetProgramsForScheduleAsync(ISchedule schedule)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_PROGRAMS_FOR_SCHEDULE);
        IList<object> inParameters = new List<object> { schedule };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          IList<Program> programList = (IList<Program>)outParameters[1];
          var programs = programList.Distinct(ProgramComparer.Instance).ToList(); // Using custom comparer to filter out duplicated programs.
          return new AsyncResult<IList<IProgram>>(true, programs);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IList<IProgram>>(false, null);
    }

    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public async Task<AsyncResult<IChannel>> GetChannelAsync(IProgram program)
    {
      return await GetChannelAsync(program.ChannelId);
    }

    public bool GetProgram(int programId, out IProgram program)
    {
      throw new NotImplementedException();
    }

    //public bool GetChannelGroups(out IList<IChannelGroup> groups)
    public async Task<AsyncResult<IList<IChannelGroup>>> GetChannelGroupsAsync()
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_CHANNELGROUPS);
        IList<object> inParameters = new List<object>();
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        IList<ChannelGroup> channelGroups = (IList<ChannelGroup>)outParameters[1];
        if (success)
        {
          var groups = channelGroups.Cast<IChannelGroup>().ToList();
          return new AsyncResult<IList<IChannelGroup>>(true, groups);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IList<IChannelGroup>>(false, null);
    }

    //public bool GetChannel(int channelId, out IChannel channel)
    public async Task<AsyncResult<IChannel>> GetChannelAsync(int channelId)
    {
      IChannel channel;
      if (_channelCache.TryGetValue(channelId, out channel))
        return new AsyncResult<IChannel>(true, channel);

      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_CHANNEL);
        IList<object> inParameters = new List<object> { channelId };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        IList<Channel> channelList = (IList<Channel>)outParameters[1];
        if (success)
        {
          channel = channelList.Cast<IChannel>().First();
          _channelCache[channelId] = channel;
          return new AsyncResult<IChannel>(true, channel);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IChannel>(false, null);
    }

    //public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    public async Task<AsyncResult<IList<IChannel>>> GetChannelsAsync(IChannelGroup group)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_CHANNELS);
        IList<object> inParameters = new List<object> { group.ChannelGroupId };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        IList<Channel> channelList = (IList<Channel>)outParameters[1];
        if (success)
        {
          var channels = channelList.Cast<IChannel>().ToList();
          foreach (var channel in channels)
            _channelCache[channel.ChannelId] = channel;
          return new AsyncResult<IList<IChannel>>(true, channels);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IList<IChannel>>(false, null);
    }

    public int SelectedChannelId
    {
      get
      {
        NativeProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NativeProviderSettings>();
        return settings.LastChannelId;
      }
      set
      {
        NativeProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NativeProviderSettings>();
        settings.LastChannelId = value;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
    }

    public int SelectedChannelGroupId
    {
      get
      {
        NativeProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NativeProviderSettings>();
        return settings.LastChannelGroupId;
      }
      set
      {
        NativeProviderSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<NativeProviderSettings>();
        settings.LastChannelGroupId = value;
        ServiceRegistration.Get<ISettingsManager>().Save(settings);
      }
    }

    //public bool CreateSchedule(IProgram program, ScheduleRecordingType recordingType, out ISchedule schedule)
    public async Task<AsyncResult<ISchedule>> CreateScheduleAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_CREATE_SCHEDULE);
        IList<object> inParameters = new List<object> { program.ProgramId, (int)recordingType };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool result = (bool)outParameters[0];
        var schedule = result ? (ISchedule)outParameters[1] : null;
        return new AsyncResult<ISchedule>(result, schedule);
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return new AsyncResult<ISchedule>(false, null);
      }
    }

    //public bool CreateScheduleByTime(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType, out ISchedule schedule)
    public async Task<AsyncResult<ISchedule>> CreateScheduleByTimeAsync(IChannel channel, DateTime from, DateTime to, ScheduleRecordingType recordingType)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_CREATE_SCHEDULE_BY_TIME);
        IList<object> inParameters = new List<object> { channel.ChannelId, from, to, (int)recordingType };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool result = (bool)outParameters[0];
        var schedule = result ? (ISchedule)outParameters[1] : null;
        return new AsyncResult<ISchedule>(result, schedule);
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return new AsyncResult<ISchedule>(false, null);
      }
    }

    //public bool RemoveScheduleForProgram(IProgram program, ScheduleRecordingType recordingType)
    public async Task<bool> RemoveScheduleForProgramAsync(IProgram program, ScheduleRecordingType recordingType)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_REMOVE_SCHEDULE_FOR_PROGRAM);
        IList<object> inParameters = new List<object> { program.ProgramId, (int)recordingType };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        return (bool)outParameters[0];
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
    }

    //public bool RemoveSchedule(ISchedule schedule)
    public async Task<bool> RemoveScheduleAsync(ISchedule schedule)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_REMOVE_SCHEDULE);
        IList<object> inParameters = new List<object> { schedule };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        return (bool)outParameters[0];
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
    }

    //public bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
    public async Task<AsyncResult<RecordingStatus>> GetRecordingStatusAsync(IProgram program)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_REC_STATUS);
        IList<object> inParameters = new List<object> { program.ProgramId };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool result = (bool)outParameters[0];
        var recordingStatus = (RecordingStatus)Enum.Parse(typeof(RecordingStatus), outParameters[1].ToString());
        return new AsyncResult<RecordingStatus>(result, recordingStatus);
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return new AsyncResult<RecordingStatus>(false, RecordingStatus.None);
      }
    }

    //public bool GetRecordingFileOrStream(IProgram program, out string fileOrStream)
    public async Task<AsyncResult<string>> GetRecordingFileOrStreamAsync(IProgram program)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_REC_FILE_OR_STREAM);
        IList<object> inParameters = new List<object> { program.ProgramId };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool result = (bool)outParameters[0];
        var fileOrStream = (string)outParameters[1];
        return new AsyncResult<string>(result, fileOrStream);
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return new AsyncResult<string>(false, null);
      }
    }

    //public bool GetSchedules(out IList<ISchedule> schedules)
    public async Task<AsyncResult<IList<ISchedule>>> GetSchedulesAsync()
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_SCHEDULES);
        IList<object> inParameters = new List<object>();
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        IList<Schedule> scheduleList = (List<Schedule>)outParameters[1];
        if (success)
        {
          var schedules = scheduleList.Cast<ISchedule>().ToList();
          return new AsyncResult<IList<ISchedule>>(true, schedules);
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return new AsyncResult<IList<ISchedule>>(false, null);
    }

    //public bool IsCurrentlyRecording(string fileName, out ISchedule schedule)
    public async Task<AsyncResult<ISchedule>> IsCurrentlyRecordingAsync(string fileName)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_IS_CURRENT_REC);
        IList<object> inParameters = new List<object> { fileName };
        IList<object> outParameters = await action.InvokeAsync(inParameters);
        bool success = (bool)outParameters[0];
        var schedule = (Schedule)outParameters[1];
        return new AsyncResult<ISchedule>(success, schedule);
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return new AsyncResult<ISchedule>(false, null);
      }
    }

    #region Exeption handling

    private void NotifyException(Exception ex, string localizationMessage = null)
    {
      string notification = string.IsNullOrEmpty(localizationMessage)
                              ? ex.Message
                              : ServiceRegistration.Get<ILocalization>().ToString(localizationMessage, ex.Message);

      ServiceRegistration.Get<INotificationService>().EnqueueNotification(NotificationType.Error, "Error", notification, true);
      ServiceRegistration.Get<ILogger>().Error(notification);
    }

    #endregion
  }
}
