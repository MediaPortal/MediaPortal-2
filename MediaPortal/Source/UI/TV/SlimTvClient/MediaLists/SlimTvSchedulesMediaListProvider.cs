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

using MediaPortal.Common;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UiComponents.Media.MediaLists;
using System;
using System.Collections.Generic;
using System.Linq;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Common.Commands;

namespace MediaPortal.Plugins.SlimTv.Client.MediaLists
{
  public class SlimTvSchedulesMediaListProvider : IMediaListProvider
  {
    protected ITvHandler _tvHandler;

    public SlimTvSchedulesMediaListProvider()
    {
      AllItems = new ItemsList();
    }

    public ItemsList AllItems { get; private set; }

    private ListItem CreateScheduleItem(ISchedule schedule)
    {
      ISchedule currentSchedule = schedule;
      ListItem item = new ListItem("Name", schedule.Name)
      {
        Command = new MethodDelegateCommand(() => ShowSchedules()),
      };
      IChannel channel = ChannelContext.Instance.Channels.FirstOrDefault(c => c.ChannelId == currentSchedule.ChannelId && c.MediaType == MediaType.TV);
      if (channel != null)
        item.SetLabel("ChannelName", channel.Name);
      item.SetLabel("StartTime", schedule.StartTime.FormatProgramStartTime());
      item.SetLabel("EndTime", schedule.EndTime.FormatProgramEndTime());
      item.SetLabel("ScheduleType", string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", schedule.RecordingType));
      item.AdditionalProperties["SCHEDULE"] = currentSchedule;
      return item;
    }

    private int ChannelAndProgramStartTimeComparison(ListItem p1, ListItem p2)
    {
      var schedule1 = ((ISchedule)p1.AdditionalProperties["SCHEDULE"]);
      var schedule2 = ((ISchedule)p2.AdditionalProperties["SCHEDULE"]);

      // The "Once" schedule should appear first
      if (schedule1.RecordingType == ScheduleRecordingType.Once && schedule2.RecordingType != ScheduleRecordingType.Once)
        return -1;
      if (schedule1.RecordingType != ScheduleRecordingType.Once && schedule2.RecordingType == ScheduleRecordingType.Once)
        return +1;

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

      string channel1Name = p1.Labels["ChannelName"].Evaluate();
      string channel2Name = p2.Labels["ChannelName"].Evaluate();
      return String.Compare(channel1Name, channel2Name, StringComparison.InvariantCultureIgnoreCase);
    }

    public static void ShowSchedules()
    {
      Guid WF_STATE_ID_SCHEDULE_LIST = new Guid("88842E97-2EF9-4658-AD35-8D74E3C689A4");
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(WF_STATE_ID_SCHEDULE_LIST);
    }

    public bool UpdateItems(int maxItems, UpdateReason updateReason)
    {
      var contentDirectory = ServiceRegistration.Get<IServerConnectionManager>().ContentDirectory;
      if (contentDirectory == null)
        return false;

      if (_tvHandler == null)
      {
        ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
        tvHandler.Initialize();
        if (tvHandler.ChannelAndGroupInfo == null)
          return false;
        _tvHandler = tvHandler;
      }

      if (_tvHandler.ScheduleControl == null)
        return false;

      if ((updateReason & UpdateReason.Forced) == UpdateReason.Forced ||
          (updateReason & UpdateReason.PeriodicMinute) == UpdateReason.PeriodicMinute)
      {
        IList<ISchedule> schedules;
        if (!_tvHandler.ScheduleControl.GetSchedules(out schedules))
          return false;

        List<ListItem> sortList = new List<ListItem>();
        Comparison<ListItem> sortMode;
        foreach (ISchedule schedule in schedules)
        {
          var item = CreateScheduleItem(schedule);
          sortList.Add(item);
        }
        sortMode = ChannelAndProgramStartTimeComparison;
        sortList.Sort(sortMode);

        if (!AllItems.Select(s => ((ISchedule)s.AdditionalProperties["SCHEDULE"]).ScheduleId).SequenceEqual(sortList.Select(si => ((ISchedule)si.AdditionalProperties["SCHEDULE"]).ScheduleId)))
        {
          AllItems.Clear();
          foreach (var schedule in sortList)
          {
            AllItems.Add(schedule);
            if (AllItems.Count >= maxItems)
              break;
          }
          AllItems.FireChange();
        }
      }
      return true;
    }
  }
}
