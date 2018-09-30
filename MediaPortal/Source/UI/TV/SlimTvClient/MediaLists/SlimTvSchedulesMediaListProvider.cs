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

using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.MediaLists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvSchedulesMediaListProvider : SlimTvMediaListProviderBase
  {
    ICollection<Tuple<ISchedule, IChannel>> _currentSchedules = new List<Tuple<ISchedule, IChannel>>();

    private async Task<ListItem> CreateScheduleItem(ISchedule schedule, IChannel channel)
    {
      ListItem item = null;
      if (channel != null)
      {
        var programResult = await _tvHandler.ProgramInfo.GetProgramsAsync(channel, schedule.StartTime, schedule.EndTime);
        if (programResult.Success)
        {
          ProgramProperties programProperties = new ProgramProperties();
          programProperties.SetProgram(programResult.Result.First(), channel);
          item = new ProgramListItem(programProperties)
          {
            Command = new MethodDelegateCommand(() => ShowSchedules()),
          };
          item.SetLabel("Name", schedule.Name);
        }
      }
      if (item == null)
      {
        item = new ListItem("Name", schedule.Name)
        {
          Command = new MethodDelegateCommand(() => ShowSchedules()),
        };
      }
      item.SetLabel("ChannelName", channel?.Name ?? "");
      item.SetLabel("StartTime", schedule.StartTime.FormatProgramStartTime());
      item.SetLabel("EndTime", schedule.EndTime.FormatProgramEndTime());
      item.SetLabel("ScheduleType", string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", schedule.RecordingType));
      item.AdditionalProperties["SCHEDULE"] = schedule;
      return item;
    }

    private int ChannelAndProgramStartTimeComparison(Tuple<ISchedule, IChannel> p1, Tuple<ISchedule, IChannel> p2)
    {
      var schedule1 = p1.Item1;
      var schedule2 = p2.Item1;

      // The "Once" schedule should appear first
      if (schedule1.RecordingType == ScheduleRecordingType.Once && schedule2.RecordingType != ScheduleRecordingType.Once)
        return -1;
      if (schedule1.RecordingType != ScheduleRecordingType.Once && schedule2.RecordingType == ScheduleRecordingType.Once)
        return 1;

      int res;
      if (schedule1.RecordingType == ScheduleRecordingType.Once && schedule2.RecordingType == ScheduleRecordingType.Once)
      {
        res = DateTime.Compare(schedule1.StartTime, schedule2.StartTime);
        if (res != 0)
          return res;
      }

      res = String.Compare(schedule1.Name, schedule2.Name, StringComparison.InvariantCultureIgnoreCase);
      if (res != 0)
        return res;

      string channel1Name = p1.Item2 != null ? p1.Item2.Name : string.Empty;
      string channel2Name = p2.Item2 != null ? p2.Item2.Name : string.Empty;
      return String.Compare(channel1Name, channel2Name, StringComparison.InvariantCultureIgnoreCase);
    }

    public static void ShowSchedules()
    {
      Guid WF_STATE_ID_SCHEDULE_LIST = new Guid("88842E97-2EF9-4658-AD35-8D74E3C689A4");
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(WF_STATE_ID_SCHEDULE_LIST);
    }

    public override async Task<bool> UpdateItemsAsync(int maxItems, UpdateReason updateReason)
    {
      if (!TryInitTvHandler() || _tvHandler.ScheduleControl == null)
        return false;

      if (!updateReason.HasFlag(UpdateReason.Forced) && !updateReason.HasFlag(UpdateReason.PeriodicMinute))
        return true;

      var scheduleResult = await _tvHandler.ScheduleControl.GetSchedulesAsync();
      if (!scheduleResult.Success)
        return false;

      var schedules = scheduleResult.Result;
      var scheduleSortList = new List<Tuple<ISchedule, IChannel>>();
      foreach (ISchedule schedule in schedules.Take(maxItems))
      {
        var channelResult = await _tvHandler.ChannelAndGroupInfo.GetChannelAsync(schedule.ChannelId);
        var channel = channelResult.Success ? channelResult.Result : null;
        scheduleSortList.Add(new Tuple<ISchedule, IChannel>(schedule, channel));
      }
      scheduleSortList.Sort(ChannelAndProgramStartTimeComparison);

      var scheduleList = new List<Tuple<ISchedule, IChannel>>(scheduleSortList.Take(maxItems));

      if (_currentSchedules.Select(s => s.Item1.ScheduleId).SequenceEqual(scheduleList.Select(s => s.Item1.ScheduleId)))
        return true;

      // Async calls need to be outside of locks
      ListItem[]  items = await Task.WhenAll(scheduleList.Select(s => CreateScheduleItem(s.Item1, s.Item2)));
      lock (_allItems.SyncRoot)
      {
        _currentSchedules = scheduleList;
        _allItems.Clear();
        CollectionUtils.AddAll(_allItems, items);
      }
      _allItems.FireChange();
      return true;
    }
  }
}
