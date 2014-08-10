#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  public class SlimTvScheduleManagement : SlimTvModelBase
  {
    public const string MODEL_ID_STR = "7610403A-4488-47A4-8C27-FD1FE833E52B";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #region Fields

    protected ISchedule _selectedSchedule;
    protected AbstractProperty _channelNameProperty = null;
    protected AbstractProperty _scheduleNameProperty = null;
    protected AbstractProperty _scheduleTypeProperty = null;
    protected AbstractProperty _startTimeProperty = null;
    protected AbstractProperty _endTimeProperty = null;
    protected readonly ItemsList _schedulesList = new ItemsList();

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current channel name to the skin.
    /// </summary>
    public string ChannelName
    {
      get { return (string)_channelNameProperty.GetValue(); }
      set { _channelNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel name to the skin.
    /// </summary>
    public AbstractProperty ChannelNameProperty
    {
      get { return _channelNameProperty; }
    }

    /// <summary>
    /// Exposes the schedule type of current schedule to the skin.
    /// </summary>
    public string ScheduleName
    {
      get { return (string)_scheduleNameProperty.GetValue(); }
      set { _scheduleNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the schedule type of current schedule to the skin.
    /// </summary>
    public AbstractProperty ScheduleNameProperty
    {
      get { return _scheduleNameProperty; }
    }

    /// <summary>
    /// Exposes the schedule type of current schedule to the skin.
    /// </summary>
    public string ScheduleType
    {
      get { return (string)_scheduleTypeProperty.GetValue(); }
      set { _scheduleTypeProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the schedule type of current schedule to the skin.
    /// </summary>
    public AbstractProperty ScheduleTypeProperty
    {
      get { return _scheduleTypeProperty; }
    }

    /// <summary>
    /// Exposes the list of schedules.
    /// </summary>
    public ItemsList SchedulesList
    {
      get { return _schedulesList; }
    }

    /// <summary>
    /// Exposes the schedule's start time.
    /// </summary>
    public DateTime StartTime
    {
      get { return (DateTime)_startTimeProperty.GetValue(); }
      set { _startTimeProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the schedule's start time.
    /// </summary>
    public AbstractProperty StartTimeProperty
    {
      get { return _startTimeProperty; }
    }

    /// <summary>
    /// Exposes the schedule's end time.
    /// </summary>
    public DateTime EndTime
    {
      get { return (DateTime)_endTimeProperty.GetValue(); }
      set { _endTimeProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the schedule's start time.
    /// </summary>
    public AbstractProperty EndTimeProperty
    {
      get { return _endTimeProperty; }
    }

    public void UpdateSchedule(ListItem selectedItem)
    {
      if (selectedItem == null)
        return;
      ISchedule schedule = (ISchedule)selectedItem.AdditionalProperties["SCHEDULE"];
      UpdateScheduleDetails(schedule);
    }

    private void UpdateScheduleDetails(ISchedule schedule)
    {
      string channelName = string.Empty;
      IChannel channel;
      if (_tvHandler.ChannelAndGroupInfo != null && _tvHandler.ChannelAndGroupInfo.GetChannel(schedule.ChannelId, out channel))
        channelName = channel.Name;

      StartTime = schedule.StartTime;
      EndTime = schedule.EndTime;
      ChannelName = channelName;
      ScheduleName = schedule.Name;
      ScheduleType = string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", schedule.RecordingType);
    }
    #endregion

    protected void LoadSchedules()
    {
      if (_tvHandler.ScheduleControl == null)
        return;

      IList<ISchedule> schedules;
      if (!_tvHandler.ScheduleControl.GetSchedules(out schedules))
        return;

      _schedulesList.Clear();
      foreach (ISchedule schedule in schedules)
      {
        ISchedule currentSchedule = schedule;
        ListItem item = new ListItem("Name", schedule.Name)
        {
          Command = new MethodDelegateCommand(() => ShowActions(currentSchedule))
        };
        IChannel channel;
        if(_tvHandler.ChannelAndGroupInfo.GetChannel(currentSchedule.ChannelId, out channel))
          item.SetLabel("ChannelName", channel.Name);
        item.SetLabel("StartTime", schedule.StartTime.FormatProgramTime());
        item.SetLabel("ScheduleType", string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", schedule.RecordingType));
        item.AdditionalProperties["SCHEDULE"] = currentSchedule;
        _schedulesList.Add(item);
      }
      _schedulesList.FireChange();
    }

    private void ShowActions(ISchedule currentSchedule)
    {
      DialogHeader = currentSchedule.Name;
      _dialogActionsList.Clear();

      ListItem item = new ListItem(Consts.KEY_NAME, currentSchedule.IsSeries ? "[SlimTvClient.DeleteFullSchedule]" : "[SlimTvClient.DeleteSingle]")
      {
        Command = new MethodDelegateCommand(() => DeleteSchedule(currentSchedule))
      };
      _dialogActionsList.Add(item);
      if (currentSchedule.IsSeries)
      {
        item = new ListItem(Consts.KEY_NAME, "[SlimTvClient.CancelProgramsOfSeriesSchedule]")
        {
          Command = new MethodDelegateCommand(() => ShowAndEditPrograms(currentSchedule))
        };
        _dialogActionsList.Add(item);
      }
      _dialogActionsList.FireChange();

      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog("DialogScheduleManagement");
    }

    private void ShowAndEditPrograms(ISchedule schedule)
    {
      SlimTvExtScheduleModel.Show(schedule);
    }

    private void DeleteSchedule(ISchedule schedule)
    {
      if (_tvHandler.ScheduleControl != null && _tvHandler.ScheduleControl.RemoveSchedule(schedule))
      {
        LoadSchedules();
      }
    }

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _channelNameProperty = new WProperty(typeof(string), string.Empty);
        _scheduleNameProperty = new WProperty(typeof(string), string.Empty);
        _scheduleTypeProperty = new WProperty(typeof(string), string.Empty);
        _startTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
        _endTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
      }
      base.InitModel();
    }

    protected override void Update()
    {
    }

    protected override void UpdateCurrentChannel()
    {
    }

    protected override void UpdatePrograms()
    {
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      LoadSchedules();
    }

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }
  }
}
