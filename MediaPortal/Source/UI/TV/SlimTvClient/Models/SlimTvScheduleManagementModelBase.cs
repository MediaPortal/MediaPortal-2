#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  public abstract class SlimTvScheduleManagementModelBase : SlimTvScheduleManagementBase
  {
    #region Fields

    protected ISchedule _selectedSchedule;

    protected AbstractProperty _scheduleSeriesModeProperty = null;
    protected AbstractProperty _channelNameProperty = null;
    protected AbstractProperty _channelNumberProperty = null;
    protected AbstractProperty _channelLogoTypeProperty = null;
    protected AbstractProperty _scheduleNameProperty = null;
    protected AbstractProperty _scheduleTypeProperty = null;
    protected AbstractProperty _startTimeProperty = null;
    protected AbstractProperty _endTimeProperty = null;
    protected AbstractProperty _currentProgramProperty = null;
    protected readonly ItemsList _schedulesList = new ItemsList();

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current program of tuned channel to the skin.
    /// </summary>
    public ProgramProperties CurrentProgram
    {
      get { return (ProgramProperties)_currentProgramProperty.GetValue(); }
      set { _currentProgramProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current program of tuned channel to the skin.
    /// </summary>
    public AbstractProperty CurrentProgramProperty
    {
      get { return _currentProgramProperty; }
    }

    /// <summary>
    /// Enables series schedule mode (<c>true</c>) or shows all single upcoming programs (<c>false</c>).
    /// </summary>
    public bool ScheduleSeriesMode
    {
      get { return (bool)_scheduleSeriesModeProperty.GetValue(); }
      set { _scheduleSeriesModeProperty.SetValue(value); }
    }

    /// <summary>
    /// Enables series schedule mode (<c>true</c>) or shows all single upcoming programs (<c>false</c>).
    /// </summary>
    public AbstractProperty ScheduleSeriesModeProperty
    {
      get { return _scheduleSeriesModeProperty; }
    }

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
    /// Exposes the current channel logo type to the skin.
    /// </summary>
    public string ChannelLogoType
    {
      get { return (string)_channelLogoTypeProperty.GetValue(); }
      set { _channelLogoTypeProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel logo type to the skin.
    /// </summary>
    public AbstractProperty ChannelLogoTypeProperty
    {
      get { return _channelLogoTypeProperty; }
    }

    /// <summary>
    /// Exposes the current channel number to the skin.
    /// </summary>
    public int ChannelNumber
    {
      get { return (int)_channelNumberProperty.GetValue(); }
      set { _channelNumberProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel number to the skin.
    /// </summary>
    public AbstractProperty ChannelNumberProperty
    {
      get { return _channelNumberProperty; }
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

    public void UpdateSchedule(object sender, SelectionChangedEventArgs e)
    {
      var selectedItem = e.FirstAddedItem as ListItem;
      if (selectedItem == null)
        return;
      ISchedule schedule = (ISchedule)selectedItem.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_SCHEDULE];
      UpdateScheduleDetails(schedule).Wait();
      if (selectedItem.AdditionalProperties.ContainsKey(SlimTvClientModelBase.KEY_PROP_PROGRAM))
      {
        IProgram program = (IProgram)selectedItem.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_PROGRAM];
        CurrentProgram.SetProgram(program);
      }
      else
      {
        CurrentProgram.SetProgram(null);
      }
    }

    private Task UpdateScheduleDetails(ISchedule schedule)
    {
      // Clear properties if no schedule is given
      if (schedule == null)
      {
        StartTime = EndTime = DateTime.MinValue;
        ChannelName = ChannelLogoType = ScheduleName = ScheduleType = string.Empty;
        ChannelNumber = 0;
        return Task.CompletedTask;
      }

      IChannel channel = null;
      if (_channels.ContainsKey(schedule.ChannelId))
        channel = _channels[schedule.ChannelId];

      StartTime = schedule.StartTime;
      EndTime = schedule.EndTime;
      ChannelName = channel?.Name ?? String.Empty;
      ChannelNumber = channel?.ChannelNumber ?? 0;
      ChannelLogoType = channel?.GetFanArtMediaType() ?? String.Empty;
      ScheduleName = schedule.Name;
      ScheduleType = string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", schedule.RecordingType);

      return Task.CompletedTask;
    }
    #endregion

    private void ToggleSeriesMode(AbstractProperty property, object oldValue)
    {
      _ = LoadSchedules();
    }

    protected async Task LoadSchedules()
    {
      await UpdateScheduleDetails(null);
      await LoadChannels();

      if (_tvHandler.ScheduleControl == null)
        return;

      var result = await _tvHandler.ScheduleControl.GetSchedulesAsync();
      if (!result.Success)
        return;

      IList<ISchedule> schedules = result.Result;
      if (_mediaMode == MediaMode.Tv)
        schedules = result.Result.Where(s => s.ChannelId == 0 || (_channels.ContainsKey(s.ChannelId) && _channels[s.ChannelId].MediaType == MediaType.TV)).ToList();
      else if (_mediaMode == MediaMode.Radio)
        schedules = result.Result.Where(s => s.ChannelId == 0 || (_channels.ContainsKey(s.ChannelId) && _channels[s.ChannelId].MediaType == MediaType.Radio)).ToList();

      _schedulesList.Clear();

      // Temporary list for sorting
      List<ListItem> sortList = new List<ListItem>();
      Comparison<ListItem> sortMode;

      if (!ScheduleSeriesMode)
      {
        Dictionary<ISchedule, IList<IProgram>> allPrograms = new Dictionary<ISchedule, IList<IProgram>>();
        // Load all series type schedules and their programs which will be recorded.
        foreach (ISchedule schedule in schedules)
        {
          var progResult = await _tvHandler.ScheduleControl.GetProgramsForScheduleAsync(schedule);
          if (!progResult.Success || progResult.Result.Count == 0)
          {
            if(!IsManualRecording(schedule.Name) || schedule.EndTime < DateTime.Now)
              continue;

            // Make dummy program for manual schedule
            MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Program program = new Interfaces.UPnP.Items.Program()
            {
              ChannelId = schedule.ChannelId,
              Title = GetScheduleName(schedule.Name),
              StartTime = schedule.StartTime,
              EndTime = schedule.EndTime,
              RecordingStatus = RecordingStatus.Scheduled
            };
            if (program.StartTime <= DateTime.Now && program.EndTime >= DateTime.Now)
              program.RecordingStatus |= RecordingStatus.Recording;
            if (schedule.RecordingType != ScheduleRecordingType.Once)
              program.RecordingStatus |= RecordingStatus.SeriesScheduled;
            progResult.Result = new List<IProgram>();
            progResult.Result.Add(program);
          }

          IList<IProgram> schedulePrograms = progResult.Result;

          // The GetProgramsForSchedule returns all matching programs, also the canceled ones. So we need to filter them out here.
          allPrograms[schedule] = schedulePrograms.OfType<IProgramRecordingStatus>().Where(p => p.RecordingStatus != RecordingStatus.None).Cast<IProgram>().ToList();
        }

        foreach (var schedule in allPrograms.Keys)
        {
          foreach (var program in allPrograms[schedule])
          {
            var item = CreateProgramItem(program, schedule);
            sortList.Add(item);
          }
        }

        sortMode = ProgramStartTimeComparison;
      }
      else
      {
        foreach (ISchedule schedule in schedules)
        {
          var item = CreateScheduleItem(schedule);
          sortList.Add(item);
        }

        sortMode = ChannelAndProgramStartTimeComparison;
      }
      sortList.Sort(sortMode);
      CollectionUtils.AddAll(_schedulesList, sortList);
      _schedulesList.FireChange();
    }

    protected string GetScheduleName(string scheduleName)
    {
      string title = GetManualRecordingTitle(scheduleName);
      string manualRec = LocalizationHelper.Translate("[SlimTvClient.ManualRecordingTitle]");
      return IsManualRecording(scheduleName) ? $"{manualRec}: {title}" : title;
    }

    /// <summary>
    /// Default sorting for single programs: first by <see cref="IProgram.StartTime"/>, then by <see cref="IChannel.Name"/>.
    /// </summary>
    private int ProgramStartTimeComparison(ListItem p1, ListItem p2)
    {
      var program1 = (IProgram)p1.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_PROGRAM];
      var program2 = (IProgram)p2.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_PROGRAM];
      int res = DateTime.Compare(program1.StartTime, program2.StartTime);
      if (res != 0)
        return res;

      var ch1 = _channels.ContainsKey(program1.ChannelId) ? _channels[program1.ChannelId] : null;
      var ch2 = _channels.ContainsKey(program1.ChannelId) ? _channels[program2.ChannelId] : null;
      if (ch1 != null && ch2 != null)
        return String.Compare(ch1.Name, ch2.Name, StringComparison.InvariantCultureIgnoreCase);

      return 0;
    }

    /// <summary>
    /// Default sorting for single programs: first by <see cref="ISchedule.RecordingType"/>=="Once", then <see cref="ISchedule.Name"/>, then by <see cref="IChannel.Name"/>.
    /// </summary>
    private int ChannelAndProgramStartTimeComparison(ListItem p1, ListItem p2)
    {
      var schedule1 = (ISchedule)p1.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_SCHEDULE];
      var schedule2 = (ISchedule)p2.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_SCHEDULE];

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

      var ch1 = _channels.ContainsKey(schedule1.ChannelId) ? _channels[schedule1.ChannelId] : null;
      var ch2 = _channels.ContainsKey(schedule2.ChannelId) ? _channels[schedule2.ChannelId] : null;
      if (ch1 != null && ch2 != null)
        return String.Compare(ch1.Name, ch2.Name, StringComparison.InvariantCultureIgnoreCase);

      return 0;
    }

    private ListItem CreateScheduleItem(ISchedule schedule)
    {
      ISchedule currentSchedule = schedule;
      ListItem item = new ListItem("Name", GetScheduleName(schedule.Name));
      if (_channels.ContainsKey(currentSchedule.ChannelId))
        item.SetLabel("ChannelName", _channels[currentSchedule.ChannelId].Name);
      item.SetLabel("StartTime", schedule.StartTime.FormatProgramStartTime());
      item.SetLabel("EndTime", schedule.EndTime.FormatProgramEndTime());
      item.SetLabel("ScheduleType", string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", schedule.RecordingType));
      item.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_SCHEDULE] = currentSchedule;
      item.Command = new MethodDelegateCommand(() => ShowActions(item));
      return item;
    }

    private ListItem CreateProgramItem(IProgram program, ISchedule schedule)
    {
      ProgramProperties programProperties = new ProgramProperties();
      IProgram currentProgram = program;
      IChannel channel = null;
      if (_channels.ContainsKey(currentProgram.ChannelId))
        channel = _channels[currentProgram.ChannelId];
      programProperties.SetProgram(currentProgram, channel);

      ListItem item = new ProgramListItem(programProperties);
      if (channel != null)
        item.SetLabel("ChannelName", channel.Name);
      item.SetLabel("ScheduleType", string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", schedule.RecordingType));
      item.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_PROGRAM] = currentProgram;
      item.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_SCHEDULE] = schedule;
      item.Command = new MethodDelegateCommand(() => ShowActions(item));
      return item;
    }

    protected override void ShowActions(ListItem item)
    {
      var currentSchedule = item.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_SCHEDULE] as ISchedule;
      var program = item.AdditionalProperties[SlimTvClientModelBase.KEY_PROP_PROGRAM] as IProgram;

      DialogHeader = currentSchedule?.Name;
      _dialogActionsList.Clear();

      AddScheduleOptions(_dialogActionsList, currentSchedule);

      _dialogActionsList.Add(new ListItem(Consts.KEY_NAME, currentSchedule.IsSeries ? "[SlimTvClient.DeleteFullSchedule]" : "[SlimTvClient.DeleteSingle]")
      {
        Command = new AsyncMethodDelegateCommand(() => DeleteSchedule(currentSchedule))
      });

      if (program != null && currentSchedule.IsSeries)
      {
        // In program list, offer to delete single program of series
        _dialogActionsList.Add(new ListItem(Consts.KEY_NAME, "[SlimTvClient.DeleteSingle]")
        {
          Command = new AsyncMethodDelegateCommand(() => CreateOrDeleteSchedule(program))
        });
      }
      // Always offer to delete schedule (prompt is same as single program if recording isn't a series)
      _dialogActionsList.Add(new ListItem(Consts.KEY_NAME, currentSchedule.IsSeries ? "[SlimTvClient.DeleteFullSchedule]" : "[SlimTvClient.DeleteSingle]")
      {
        Command = new AsyncMethodDelegateCommand(() => DeleteSchedule(currentSchedule))
      });
      if (program == null && currentSchedule.IsSeries)
      {
        // In series list - offer to delete individual programs - will go to ExtSchedule to show all the programs
        _dialogActionsList.Add(new ListItem(Consts.KEY_NAME, "[SlimTvClient.CancelProgramsOfSeriesSchedule]")
        {
          Command = new MethodDelegateCommand(() => ShowAndEditPrograms(currentSchedule))
        });
      }
      _dialogActionsList.FireChange();

      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (_mediaMode == MediaMode.Radio)
        screenManager.ShowDialog("DialogScheduleManagementRadio");
      else
        screenManager.ShowDialog("DialogScheduleManagement");
    }

    private void EditSchedule(ISchedule schedule)
    {
      SlimTvManualScheduleModel.Show(schedule);
    }

    private void ShowAndEditPrograms(ISchedule schedule)
    {
      SlimTvExtScheduleModel.Show(schedule);
    }

    private async Task DeleteSchedule(ISchedule schedule)
    {
      if (_tvHandler.ScheduleControl != null && await _tvHandler.ScheduleControl.RemoveScheduleAsync(schedule))
      {
        await LoadSchedules();
      }
    }

    protected override async Task<RecordingStatus?> CreateOrDeleteSchedule(IProgram program, ScheduleRecordingType recordingType = ScheduleRecordingType.Once)
    {
      var result = await base.CreateOrDeleteSchedule(program, recordingType);
      await LoadSchedules();
      return result;
    }

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _scheduleSeriesModeProperty = new WProperty(typeof(bool), false);
        _scheduleSeriesModeProperty.Attach(ToggleSeriesMode);
        _channelNameProperty = new WProperty(typeof(string), string.Empty);
        _channelNumberProperty = new WProperty(typeof(int), 0);
        _channelLogoTypeProperty = new WProperty(typeof(string), string.Empty);
        _scheduleNameProperty = new WProperty(typeof(string), string.Empty);
        _scheduleTypeProperty = new WProperty(typeof(string), string.Empty);
        _startTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
        _endTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
        _currentProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());
        _isInitialized = true;
      }
      base.InitModel();
    }

    protected override void Update()
    {
    }

    #region IWorkflowModel implementation

    public abstract override Guid ModelId { get; }

    public override void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      base.Reactivate(oldContext, newContext);
      _loadChannels = true;
      _ = LoadSchedules();
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      _loadChannels = true;
      _ = LoadSchedules();
    }

    #endregion
  }
}
