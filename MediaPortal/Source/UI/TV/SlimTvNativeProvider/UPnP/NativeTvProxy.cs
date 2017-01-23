#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
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
  public class NativeTvProxy : UPnPServiceProxyBase, IDisposable, ITvProvider, ITimeshiftControl, IProgramInfo, IChannelAndGroupInfo, IScheduleControl
  {
    #region Protected fields

    protected UPnPNetworkTracker _networkTracker;
    protected UPnPControlPoint _controlPoint;
    protected readonly IChannel[] _channels = new IChannel[2];
    protected readonly object _syncObj = new object();
    protected readonly ProgramCache _programCache = new ProgramCache();
    protected Dictionary<int, IChannel> _channelCache = new Dictionary<int, IChannel>();

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
      timeshiftMediaItem = null;
      // If we change between radio and tv channels, stop current timeshift before. This is required, so that the new
      // player starts with a new stream from beginning. Otherwise a VideoPlayer could be ran with an older Radio stream part.
      bool mediaTypeChanged = _channels[slotIndex] != null && _channels[slotIndex].MediaType != channel.MediaType;
      if (mediaTypeChanged && !StopTimeshift(slotIndex))
        return false;
      try
      {
        CpAction action = GetAction(Consts.ACTION_START_TIMESHIFT);
        IList<object> inParameters = new List<object>
            {
              slotIndex,
              channel.ChannelId
            };

        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          _channels[slotIndex] = channel;

          // Assign a MediaItem, can be null if streamUrl is the same.
          timeshiftMediaItem = (MediaItem)outParameters[1];
          return true;
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return false;
    }

    public bool StopTimeshift(int slotIndex)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_STOP_TIMESHIFT);
        IList<object> inParameters = new List<object>
            {
              slotIndex
            };

        IList<object> outParameters = action.InvokeAction(inParameters);
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
      ProgramNowNextValue programs;
      _programCache.ClearCache(channel);
      if (_programCache.TryGetPrograms(channel, out programs))
      {
        programNow = programs.ProgramNow;
        programNext = programs.ProgramNext;
        return true;
      }
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_NOW_NEXT_PROGRAM);
        IList<object> inParameters = new List<object>
            {
              channel.ChannelId
            };

        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          programNow = (Program)outParameters[1];
          programNext = (Program)outParameters[2];
          _programCache.TryAdd(channel, programNow, programNext);
          return true;
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      programNow = null;
      programNext = null;
      return false;
    }

    public bool GetNowAndNextForChannelGroup(IChannelGroup channelGroup, out IDictionary<int, IProgram[]> nowNextPrograms)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_NOW_NEXT_PROGRAM_FOR_GROUP);
        IList<object> inParameters = new List<object>
            {
              channelGroup.ChannelGroupId
            };

        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          DateTime now = DateTime.Now;
          nowNextPrograms = new Dictionary<int, IProgram[]>();
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
          return true;
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      nowNextPrograms = null;
      return false;
    }

    public bool GetProgramsGroup(IChannelGroup channelGroup, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      programs = null;
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_PROGRAMS_GROUP);
        IList<object> inParameters = new List<object>
            {
              channelGroup.ChannelGroupId,
              from,
              to
            };

        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          IList<Program> programList = (IList<Program>)outParameters[1];
          programs = programList.Distinct(ProgramComparer.Instance).ToList(); // Using custom comparer to filter out duplicated programs.
          return true;
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return false;
    }

    public bool GetPrograms(string title, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      programs = null;
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_PROGRAMS_BY_TITLE);
        IList<object> inParameters = new List<object>
            {
              title,
              from,
              to
            };

        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          IList<Program> programList = (IList<Program>)outParameters[1];
          programs = programList.Distinct(ProgramComparer.Instance).ToList(); // Using custom comparer to filter out duplicated programs.
          return true;
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return false;
    }

    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      programs = null;
      // We only want to cache single time queries
      if (from == to)
      {
        _programCache.ClearCache(channel);
        ProgramNowNextValue programsCache;
        if (_programCache.TryGetProgramsByTime(channel, from, out programsCache))
        {
          programs = new List<IProgram> { programsCache.ProgramNow };
          return true;
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

        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          IList<Program> programList = (IList<Program>)outParameters[1];
          programs = programList.Distinct(ProgramComparer.Instance).ToList(); // Using custom comparer to filter out duplicated programs.
          if (from == to)
          {
            _programCache.TryAdd(channel, programs[0], null);
          }
          return true;
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      return false;
    }

    public bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_PROGRAMS_FOR_SCHEDULE);
        IList<object> inParameters = new List<object> { schedule };
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        if (success)
        {
          IList<Program> programList = (IList<Program>)outParameters[1];
          programs = programList.Distinct(ProgramComparer.Instance).ToList(); // Using custom comparer to filter out duplicated programs.
          return true;
        }
      }
      catch (Exception ex)
      {
        NotifyException(ex);
      }
      programs = null;
      return false;
    }

    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetChannel(IProgram program, out IChannel channel)
    {
      return GetChannel(program.ChannelId, out channel);
    }

    public bool GetProgram(int programId, out IProgram program)
    {
      throw new NotImplementedException();
    }

    public bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
      groups = null;
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_CHANNELGROUPS);
        IList<object> inParameters = new List<object>();
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        IList<ChannelGroup> channelGroups = (IList<ChannelGroup>)outParameters[1];
        if (success)
        {
          groups = channelGroups.Cast<IChannelGroup>().ToList();
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
    }

    public bool GetChannel(int channelId, out IChannel channel)
    {
      if (_channelCache.TryGetValue(channelId, out channel))
        return true;

      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_CHANNEL);
        IList<object> inParameters = new List<object> { channelId };
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        IList<Channel> channelList = (IList<Channel>)outParameters[1];
        if (success)
        {
          channel = channelList.Cast<IChannel>().First();
          _channelCache[channelId] = channel;
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
    }

    public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
      channels = null;
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_CHANNELS);
        IList<object> inParameters = new List<object> { group.ChannelGroupId };
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        IList<Channel> channelList = (IList<Channel>)outParameters[1];
        if (success)
        {
          channels = channelList.Cast<IChannel>().ToList();
          foreach(var channel in channels)
            _channelCache[channel.ChannelId] = channel;
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
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

    public bool CreateSchedule(IProgram program, ScheduleRecordingType recordingType, out ISchedule schedule)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_CREATE_SCHEDULE);
        IList<object> inParameters = new List<object> { program.ProgramId, (int) recordingType };
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool result = (bool)outParameters[0];
        schedule = result ? (ISchedule)outParameters[1] : null;
        return result;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        schedule = null;
        return false;
      }
    }

    public bool CreateScheduleByTime(IChannel channel, DateTime from, DateTime to, out ISchedule schedule)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_CREATE_SCHEDULE_BY_TIME);
        IList<object> inParameters = new List<object> { channel.ChannelId, from, to };
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool result = (bool)outParameters[0];
        schedule = result ? (ISchedule)outParameters[1] : null;
        return result;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        schedule = null;
        return false;
      }
    }

    public bool RemoveScheduleForProgram(IProgram program, ScheduleRecordingType recordingType)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_REMOVE_SCHEDULE_FOR_PROGRAM);
        IList<object> inParameters = new List<object> { program.ProgramId, (int)recordingType };
        IList<object> outParameters = action.InvokeAction(inParameters);
        return (bool)outParameters[0];
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
    }

    public bool RemoveSchedule(ISchedule schedule)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_REMOVE_SCHEDULE);
        IList<object> inParameters = new List<object> { schedule };
        IList<object> outParameters = action.InvokeAction(inParameters);
        return (bool)outParameters[0];
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
      }
    }

    public bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_REC_STATUS);
        IList<object> inParameters = new List<object> { program.ProgramId };
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool result = (bool)outParameters[0];
        recordingStatus = (RecordingStatus)Enum.Parse(typeof(RecordingStatus), outParameters[1].ToString());
        return result;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        recordingStatus = RecordingStatus.None;
        return false;
      }
    }

    public bool GetRecordingFileOrStream(IProgram program, out string fileOrStream)
    {
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_REC_FILE_OR_STREAM);
        IList<object> inParameters = new List<object> { program.ProgramId };
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool result = (bool)outParameters[0];
        fileOrStream = (string)outParameters[1];
        return result;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        fileOrStream = null;
        return false;
      }
    }

    public bool GetSchedules(out IList<ISchedule> schedules)
    {
      schedules = null;
      try
      {
        CpAction action = GetAction(Consts.ACTION_GET_SCHEDULES);
        IList<object> inParameters = new List<object>();
        IList<object> outParameters = action.InvokeAction(inParameters);
        bool success = (bool)outParameters[0];
        IList<Schedule> scheduleList = (List<Schedule>)outParameters[1];
        if (success)
        {
          schedules = scheduleList.Cast<ISchedule>().ToList();
          return true;
        }
        return false;
      }
      catch (Exception ex)
      {
        NotifyException(ex);
        return false;
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
