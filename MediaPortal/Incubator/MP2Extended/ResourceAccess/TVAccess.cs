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
using System.Threading.Tasks;
using MediaPortal.Common;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS;
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using Microsoft.Owin;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess
{
  internal class TVAccess
  {
    internal static async Task<IList<IChannelGroup>> GetGroupsAsync(IOwinContext context)
    {
      Guid? user = ResourceAccessUtils.GetUser(context);
      List<IChannelGroup> groups = new List<IChannelGroup>();
      var channelGroups = await ChannelAndGroupInfo.GetChannelGroupsAsync();
      if (channelGroups.Success)
        groups.AddRange(channelGroups.Result);
      ResourceAccessUtils.FilterGroups(user, groups);
      return groups;
    }

    internal static async Task<IChannelGroup> GetGroupAsync(IOwinContext context, int id)
    {
      List<IChannelGroup> groups = new List<IChannelGroup>();
      var channelGroups = await GetGroupsAsync(context);
      return channelGroups.FirstOrDefault(g => g.ChannelGroupId == id);
    }

    internal static async Task<IChannel> GetChannelAsync(int id)
    {
      var channel = await ChannelAndGroupInfo.GetChannelAsync(id);
      if (channel.Success)
        return channel.Result;
      return null;
    }

    internal static async Task<IList<IChannel>> GetGroupChannelsAsync(IOwinContext context, int? id = null)
    {
      List<IChannelGroup> channelGroups = new List<IChannelGroup>();
      if (id == null)
        channelGroups.AddRange(await GetGroupsAsync(context));
      else
        channelGroups.Add(new ChannelGroup() { ChannelGroupId = id.Value });

      List<IChannel> groupChannels = new List<IChannel>();
      foreach (var group in channelGroups)
      {
        // get channel for group
        var channels = await ChannelAndGroupInfo.GetChannelsAsync(group);
        if (channels.Success)
          groupChannels.AddRange(channels.Result);
      }
      return groupChannels;
    }

    internal static Task<IProgram> GetProgramAsync(IOwinContext context, int id)
    {
      if (ProgramInfo.GetProgram(id, out IProgram prog))
        return Task.FromResult(prog);
      return Task.FromResult<IProgram>(null);
    }

    internal static async Task<RecordingStatus> GetProgramRecordingStatusAsync(IOwinContext context, int id)
    {
      if (ProgramInfo.GetProgram(id, out IProgram prog))
      {
        var status = await ScheduleControl.GetRecordingStatusAsync(prog);
        if (status.Success)
          return status.Result;
      }
      return RecordingStatus.None;
    }

    internal static async Task<bool> EditScheduleAsync(IOwinContext context, int scheduleId, int? channelId = null, string title = null, DateTime? startTime = null, DateTime? endTime = null, WebScheduleType? scheduleType = null, int? preRecordInterval = null, int? postRecordInterval = null, string directory = null, int? priority = null)
    {
      IChannel channel = null;
      if (channelId.HasValue)
        channel = await GetChannelAsync(channelId.Value);

      var schedules = await GetSchedulesAsync(context);
      ISchedule scheduleSrc = schedules.Single(x => x.ScheduleId == scheduleId);
      return await ScheduleControl.EditScheduleAsync(scheduleSrc,
        channel,
        title,
        startTime,
        endTime,
        scheduleType != null ? (ScheduleRecordingType?)scheduleType : (ScheduleRecordingType?)null,
        preRecordInterval,
        postRecordInterval,
        directory,
        priority);
    }

    internal static async Task<bool> CreateScheduleAsync(IOwinContext context, int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType, int preRecordInterval, int postRecordInterval, string directory, int priority)
    {
      var channel = await GetChannelAsync(channelId);
      if (channel != null)
      {
        var schedule = await ScheduleControl.CreateScheduleDetailedAsync(channel, title, startTime, endTime, (ScheduleRecordingType)scheduleType, preRecordInterval, postRecordInterval, directory, priority);
        return schedule.Success;
      }
      return false;
    }

    internal static async Task<bool> CreateScheduleAsync(IOwinContext context, int channelId, string title, DateTime startTime, DateTime endTime, WebScheduleType scheduleType)
    {
      var channel = await GetChannelAsync(channelId);
      if (channel != null)
      {
        var schedule = await ScheduleControl.CreateScheduleByTimeAsync(channel, title, startTime, endTime, (ScheduleRecordingType)scheduleType);
        return schedule.Success;
      }
      return false;
    }

    internal static async Task<bool> UnCancelScheduleAsync(IOwinContext context, int id)
    {
      IProgram program = await GetProgramAsync(context, id);
      if (program != null)
        return await ScheduleControl.UnCancelScheduleAsync(program);
      return false;
    }

    internal static async Task<bool> CancelScheduleAsync(IOwinContext context, int id)
    {
      IProgram program = await GetProgramAsync(context, id);
      if (program != null)
        return await ScheduleControl.RemoveScheduleForProgramAsync(program, ScheduleRecordingType.Once);
      return false;
    }

    internal static async Task<bool> DeleteScheduleAsync(IOwinContext context, int id)
    {
      var schedules = await GetSchedulesAsync(context);
      if (schedules != null)
        return await ScheduleControl.RemoveScheduleAsync(schedules.Single(s => s.ScheduleId == id));
      return false;
    }

    internal static async Task<ISchedule> GetScheduleAsync(IOwinContext context, int id)
    {
      var schedules = await ScheduleControl.GetSchedulesAsync();
      if (schedules.Success)
        return schedules.Result.FirstOrDefault(s => s.ScheduleId == id);
      return null;
    }

    internal static async Task<IList<ISchedule>> GetSchedulesAsync(IOwinContext context)
    {
      var schedules = await ScheduleControl.GetSchedulesAsync();
      if (schedules.Success)
        return schedules.Result;
      return null;
    }

    internal static async Task<IProgram[]> GetChannelNowNextProgramAsync(IOwinContext context, int id)
    {
      var channel = await GetChannelAsync(id);
      if (channel == null)
        return null;

      var programs = await ProgramInfo.GetNowNextProgramAsync(channel);
      if (programs.Success)
        return programs.Result;

      return null;
    }

    internal static async Task<IList<IProgram>> GetChannelProgramsAsync(IOwinContext context, int id, DateTime start, DateTime end)
    {
      List<IProgram> channelPrograms = new List<IProgram>();
      var channel = await GetChannelAsync(id);
      if (channel == null)
        return channelPrograms;

      var programs = await ProgramInfo.GetProgramsAsync(channel, start, end);
      if (programs.Success)
        channelPrograms.AddRange(programs.Result);

      return channelPrograms;
    }

    internal static async Task<IList<IProgram>> GetGroupProgramsAsync(IOwinContext context, DateTime start, DateTime end, int? id = null)
    {
      List<IProgram> groupPrograms = new List<IProgram>();
      var channels = await GetGroupChannelsAsync(context, id);
      foreach (var channel in channels)
      {
        var programs = await ProgramInfo.GetProgramsAsync(channel, start, end);
        if (programs.Success)
          groupPrograms.AddRange(programs.Result);
      }
      return groupPrograms;
    }

    internal static async Task<MediaItem> StartTimeshiftAsync(IOwinContext context, int id, string userName)
    {
      var channel = await GetChannelAsync(id);
      if (channel == null)
        return null;

      var item = await TimeshiftControl.StartTimeshiftAsync(userName, id, channel);
      if (!item.Success)
        return null;

      return item.Result;
    }

    internal static async Task<bool> StopTimeshiftAsync(IOwinContext context, int id, string userName)
    {
      return await TimeshiftControl.StopTimeshiftAsync(userName, id);
    }

    internal static Task<IList<ICard>> GetTunerCardsAsync(IOwinContext context)
    {
      if (TunerInfo.GetCards(out List<ICard> cards))
        return Task.FromResult<IList<ICard>>(cards);
      return Task.FromResult<IList<ICard>>(new List<ICard>());
    }

    internal static Task<IList<IVirtualCard>> GetVirtualCardsAsync(IOwinContext context)
    {
      if (TunerInfo.GetActiveVirtualCards(out List<IVirtualCard> cards))
        return Task.FromResult<IList<IVirtualCard>>(cards);
      return Task.FromResult<IList<IVirtualCard>>(new List<IVirtualCard>());
    }

    internal static ITunerInfo TunerInfo
    {
      get { return ServiceRegistration.Get<ITvProvider>() as ITunerInfo; }
    }

    internal static IScheduleControlAsync ScheduleControl
    {
      get { return ServiceRegistration.Get<ITvProvider>() as IScheduleControlAsync; }
    }

    internal static ITimeshiftControlEx TimeshiftControl
    {
      get { return ServiceRegistration.Get<ITvProvider>() as ITimeshiftControlEx; }
    }

    internal static IChannelAndGroupInfoAsync ChannelAndGroupInfo
    {
      get { return ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync; }
    }

    internal static IProgramInfoAsync ProgramInfo
    {
      get { return ServiceRegistration.Get<ITvProvider>() as IProgramInfoAsync; }
    }
  }
}
