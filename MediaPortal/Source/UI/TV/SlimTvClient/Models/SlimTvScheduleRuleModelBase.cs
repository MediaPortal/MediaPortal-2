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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaPortal.Common.Logging;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// Model that allows the configuration and creation of a manual schedule.
  /// </summary>
  public abstract class SlimTvScheduleRuleModelBase : SlimTvModelBase
  {
    #region Protected fields
    
    protected IList<IChannelGroup> _channelGroups;
    protected IList<IChannel> _channels;

    protected ISchedule _editSchedule;

    protected AbstractProperty _channelGroupProperty;
    protected AbstractProperty _channelProperty;
    protected AbstractProperty _recordingTypeProperty;
    protected AbstractProperty _recordingTypeNameProperty;
    protected AbstractProperty _isScheduleValidProperty;
    protected AbstractProperty _scheduleNameProperty;
    protected AbstractProperty _isManualProperty;
    protected AbstractProperty _isEditProperty;
    protected AbstractProperty _preRecordIntervalProperty;
    protected AbstractProperty _postRecordIntervalProperty;
    protected AbstractProperty _priorityProperty;
    protected AbstractProperty _priorityNameProperty;

    protected AbstractProperty _startTimeProperty;
    protected AbstractProperty _endTimeProperty;
    protected AbstractProperty _startsTodayProperty;
    protected AbstractProperty _endsOnSameDayProperty;

    #endregion

    #region Init/Update

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _channelGroupProperty = new WProperty(typeof(IChannelGroup), null);
        _channelProperty = new WProperty(typeof(IChannel), null);
        _recordingTypeProperty = new WProperty(typeof(ScheduleRecordingType), ScheduleRecordingType.Once);
        _recordingTypeNameProperty = new WProperty(typeof(string), GetLocalizedRecordingTypeName(ScheduleRecordingType.Once));
        _isScheduleValidProperty = new WProperty(typeof(bool), false);
        _isManualProperty = new WProperty(typeof(bool), false);
        _isEditProperty = new WProperty(typeof(bool), false);
        _scheduleNameProperty = new WProperty(typeof(string), "");
        _preRecordIntervalProperty = new WProperty(typeof(int), 0);
        _postRecordIntervalProperty = new WProperty(typeof(int), 0);
        _priorityProperty = new WProperty(typeof(PriorityType), PriorityType.Normal);
        _priorityNameProperty = new WProperty(typeof(string), GetLocalizedPriorityName(PriorityType.Normal));

        _startTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
        _endTimeProperty = new WProperty(typeof(DateTime), DateTime.MinValue);
        _startsTodayProperty = new WProperty(typeof(bool), false);
        _endsOnSameDayProperty = new WProperty(typeof(bool), false);

        Attach();
        _isInitialized = true;
      }
      base.InitModel();
    }

    private void Attach()
    {
      _channelGroupProperty.Attach(OnChannelGroupChanged);
      _channelProperty.Attach(OnPropertyChanged);
      _scheduleNameProperty.Attach(OnPropertyChanged);
      _preRecordIntervalProperty.Attach(OnPropertyChanged);
      _postRecordIntervalProperty.Attach(OnPropertyChanged);
      _startTimeProperty.Attach(OnTimeChanged);
      _endTimeProperty.Attach(OnTimeChanged);
      _recordingTypeProperty.Attach(OnRecordingTypeChanged);
      _priorityProperty.Attach(OnPriorityChanged);
    }

    protected void Reset()
    {
      _editSchedule = null;
      IChannelGroup channelGroup = ChannelGroup;
      if (channelGroup == null)
        ChannelGroup = _channelGroups.FirstOrDefault();
      Channel = _channels?.FirstOrDefault();
      RecordingType = ScheduleRecordingType.Once;

      DateTime now = DateTime.Now;
      DateTime start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Kind);
      StartTime = start;
      EndTime = start.AddHours(1);
      ScheduleName = "";
      IsEdit = false;
      PreRecordingInterval = 0;
      PostRecordingInterval = 0;
      Priority = PriorityType.Normal;
    }

    protected void Manual()
    {
      Reset();
      IsManual = true;
    }

    protected override void Update()
    {
    }

    protected void UpdateFromProgram(IProgram program)
    {
      _editSchedule = null;
      Channel = _channels?.FirstOrDefault(c => c.ChannelId == program.ChannelId);
      RecordingType = ScheduleRecordingType.Once;
      StartTime = program.StartTime;
      EndTime = program.EndTime;
      ScheduleName = program.Title;
      IsManual = false;
      IsEdit = false;
      PreRecordingInterval = 0;
      PostRecordingInterval = 0;
      Priority = PriorityType.Normal;
    }

    protected void UpdateFromSchedule(ISchedule schedule)
    {
      _editSchedule = schedule;
      Channel = _channels?.FirstOrDefault(c => c.ChannelId == schedule.ChannelId);
      RecordingType = schedule.RecordingType;
      StartTime = schedule.StartTime;
      EndTime = schedule.EndTime;
      ScheduleName = GetManualRecordingTitle(schedule.Name);
      IsManual = IsManualRecording(schedule.Name);
      IsEdit = true;
      PreRecordingInterval = (int)schedule.PreRecordInterval.TotalMinutes;
      PostRecordingInterval = (int)schedule.PostRecordInterval.TotalMinutes;
      Priority = schedule.Priority;
    }

    protected void OnPropertyChanged(AbstractProperty property, object oldValue)
    {
      UpdateIsScheduleValid();
    }

    protected void OnChannelGroupChanged(AbstractProperty property, object oldValue)
    {
      InitChannels(ChannelGroup).Wait();
      UpdateIsScheduleValid();
    }

    protected void OnRecordingTypeChanged(AbstractProperty property, object oldValue)
    {
      RecordingTypeName = GetLocalizedRecordingTypeName(RecordingType);
      UpdateIsScheduleValid();
    }

    protected void OnPriorityChanged(AbstractProperty property, object oldValue)
    {
      PriorityName = GetLocalizedPriorityName(Priority);
      UpdateIsScheduleValid();
    }

    private void OnTimeChanged(AbstractProperty property, object oldValue)
    {
      DateTime now = DateTime.Now;
      DateTime startTime = StartTime;      
      StartsToday = startTime.Date == now.Date;
      EndsOnSameDay = EndTime.Date == startTime.Date;
      UpdateIsScheduleValid();
    }

    protected void UpdateIsScheduleValid()
    {
      IsScheduleValid = CheckScheduleValid(Channel, StartTime, EndTime, RecordingType, ScheduleName);
    }

    #endregion

    #region GUI properties/methods

    public AbstractProperty ChannelGroupProperty
    {
      get { return _channelGroupProperty; }
    }

    /// <summary>
    /// Exposes the current channel group to the skin.
    /// </summary>
    public IChannelGroup ChannelGroup
    {
      get { return (IChannelGroup)_channelGroupProperty.GetValue(); }
      set { _channelGroupProperty.SetValue(value); }
    }

    public AbstractProperty ChannelProperty
    {
      get { return _channelProperty; }
    }

    /// <summary>
    /// Exposes the current channel to the skin.
    /// </summary>
    public IChannel Channel
    {
      get { return (IChannel)_channelProperty.GetValue(); }
      set { _channelProperty.SetValue(value); }
    }

    public AbstractProperty StartTimeProperty
    {
      get { return _startTimeProperty; }
    }

    /// <summary>
    /// Exposes the schedule start time to the skin.
    /// </summary>
    public DateTime StartTime
    {
      get { return (DateTime)_startTimeProperty.GetValue(); }
      set { _startTimeProperty.SetValue(value); }
    }

    public AbstractProperty EndTimeProperty
    {
      get { return _endTimeProperty; }
    }

    /// <summary>
    /// Exposes the schedule end time to the skin.
    /// </summary>
    public DateTime EndTime
    {
      get { return (DateTime)_endTimeProperty.GetValue(); }
      set { _endTimeProperty.SetValue(value); }
    }

    public AbstractProperty RecordingTypeProperty
    {
      get { return _recordingTypeProperty; }
    }

    /// <summary>
    /// Exposes the recording type to the skin.
    /// </summary>
    public ScheduleRecordingType RecordingType
    {
      get { return (ScheduleRecordingType)_recordingTypeProperty.GetValue(); }
      set { _recordingTypeProperty.SetValue(value); }
    }

    public AbstractProperty RecordingTypeNameProperty
    {
      get { return _recordingTypeNameProperty; }
    }

    /// <summary>
    /// Exposes the localized name of the recording type to the skin.
    /// </summary>
    public string RecordingTypeName
    {
      get { return (string)_recordingTypeNameProperty.GetValue(); }
      set { _recordingTypeNameProperty.SetValue(value); }
    }

    public AbstractProperty ScheduleNameProperty
    {
      get { return _scheduleNameProperty; }
    }

    /// <summary>
    /// Exposes the schedule name to the skin.
    /// </summary>
    public string ScheduleName
    {
      get { return (string)_scheduleNameProperty.GetValue(); }
      set { _scheduleNameProperty.SetValue(value); }
    }

    public AbstractProperty PreRecordingIntervalProperty
    {
      get { return _preRecordIntervalProperty; }
    }

    /// <summary>
    /// Exposes the pre-recording interval to the skin.
    /// </summary>
    public int PreRecordingInterval
    {
      get { return (int)_preRecordIntervalProperty.GetValue(); }
      set { _preRecordIntervalProperty.SetValue(value); }
    }

    public AbstractProperty PostRecordingIntervalProperty
    {
      get { return _postRecordIntervalProperty; }
    }

    /// <summary>
    /// Exposes the post-recording interval to the skin.
    /// </summary>
    public int PostRecordingInterval
    {
      get { return (int)_postRecordIntervalProperty.GetValue(); }
      set { _postRecordIntervalProperty.SetValue(value); }
    }

    public AbstractProperty PriorityProperty
    {
      get { return _priorityProperty; }
    }

    /// <summary>
    /// Exposes the priority to the skin.
    /// </summary>
    public PriorityType Priority
    {
      get { return (PriorityType)_priorityProperty.GetValue(); }
      set { _priorityProperty.SetValue(value); }
    }


    public AbstractProperty PriorityNameProperty
    {
      get { return _priorityNameProperty; }
    }

    /// <summary>
    /// Exposes the priority name to the skin.
    /// </summary>
    public string PriorityName
    {
      get { return (string)_priorityNameProperty.GetValue(); }
      set { _priorityNameProperty.SetValue(value); }
    }

    public AbstractProperty IsManualProperty
    {
      get { return _isManualProperty; }
    }

    /// <summary>
    /// Indicates whether the current schedule is manual.
    /// </summary>
    public bool IsManual
    {
      get { return (bool)_isManualProperty.GetValue(); }
      set { _isManualProperty.SetValue(value); }
    }

    public AbstractProperty IsEditProperty
    {
      get { return _isEditProperty; }
    }

    /// <summary>
    /// Indicates whether the current schedule is being edited or created.
    /// </summary>
    public bool IsEdit
    {
      get { return (bool)_isEditProperty.GetValue(); }
      set { _isEditProperty.SetValue(value); }
    }

    public AbstractProperty IsScheduleValidProperty
    {
      get { return _isScheduleValidProperty; }
    }

    /// <summary>
    /// Indicates whether the current schedule configuration is valid.
    /// </summary>
    public bool IsScheduleValid
    {
      get { return (bool)_isScheduleValidProperty.GetValue(); }
      set { _isScheduleValidProperty.SetValue(value); }
    }

    public AbstractProperty StartsTodayProperty
    {
      get { return _startsTodayProperty; }
    }

    /// <summary>
    /// Indicates whether the current start date is today.
    /// </summary>
    public bool StartsToday
    {
      get { return (bool)_startsTodayProperty.GetValue(); }
      set { _startsTodayProperty.SetValue(value); }
    }

    public AbstractProperty EndsOnSameDayProperty
    {
      get { return _endsOnSameDayProperty; }
    }

    /// <summary>
    /// Indicates whether the current end date is the same as the start date.
    /// </summary>
    public bool EndsOnSameDay
    {
      get { return (bool)_endsOnSameDayProperty.GetValue(); }
      set { _endsOnSameDayProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the localized time separator to the skin.
    /// </summary>
    public string TimeSeparator
    {
      get { return ServiceRegistration.Get<ILocalization>().CurrentCulture.DateTimeFormat.TimeSeparator; }
    }

    /// <summary>
    /// Adds the specified number of minutes to the schedule start time.
    /// </summary>
    /// <param name="minutes">The number of minutes to add.</param>
    public void AddStartTime(int minutes)
    {
      DateTime newStartTime = StartTime.AddMinutes(minutes);
      StartTime = newStartTime;
      if (newStartTime >= EndTime)
        EndTime = newStartTime.AddMinutes(1);
    }

    /// <summary>
    /// Adds the specified number of minutes to the schedule end time.
    /// </summary>
    /// <param name="minutes">The number of minutes to add.</param>
    public void AddEndTime(int minutes)
    {
      DateTime newEndTime = EndTime.AddMinutes(minutes);
      DateTime startTime = StartTime;
      EndTime = newEndTime > startTime ? newEndTime : startTime.AddMinutes(1);
    }

    /// <summary>
    /// Shows the channel group selection dialog.
    /// </summary>
    public void ShowChannelGroupDialog()
    {
      InitChannelGroupList();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (_mediaMode == MediaMode.Radio)
        screenManager.ShowDialog("DialogManualScheduleRadio");
      else
        screenManager.ShowDialog("DialogManualSchedule");
    }

    /// <summary>
    /// Shows the channel selection dialog.
    /// </summary>
    public void ShowChannelDialog()
    {
      InitChannelList();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (_mediaMode == MediaMode.Radio)
        screenManager.ShowDialog("DialogManualScheduleRadio");
      else
        screenManager.ShowDialog("DialogManualSchedule");
    }

    /// <summary>
    /// Shows the recording type selection dialog.
    /// </summary>
    public void ShowRecordingTypeDialog()
    {
      InitRecordingTypeList();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (_mediaMode == MediaMode.Radio)
        screenManager.ShowDialog("DialogManualScheduleRadio");
      else
        screenManager.ShowDialog("DialogManualSchedule");
    }

    /// <summary>
    /// Shows the priority selection dialog.
    /// </summary>
    public void ShowPriorityDialog()
    {
      InitPriorityList();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      if (_mediaMode == MediaMode.Radio)
        screenManager.ShowDialog("DialogManualScheduleRadio");
      else
        screenManager.ShowDialog("DialogManualSchedule");
    }

    #endregion

    #region Protected methods

    /// <summary>
    /// Creates a schedule using the configured properties.
    /// </summary>
    protected async Task CreateSchedule(Guid stateId)
    {
      IChannel channel = Channel;
      DateTime startTime = StartTime;
      DateTime endTime = EndTime;
      ScheduleRecordingType recordingType = RecordingType;
      string title = ScheduleName;
      int preInterval = PreRecordingInterval;
      int postInterval = PostRecordingInterval;
      int priority = (int)Priority;

      if (!CheckScheduleValid(channel, startTime, endTime, recordingType, title))
        return;

      if (IsManual)
        title = SetManualRecordingTitle(title);

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      if (_editSchedule != null)
      {
        var result = await tvHandler.ScheduleControl.EditScheduleAsync(_editSchedule, channel, title, startTime, endTime, recordingType, preInterval, postInterval, null, priority);
        if (!result)
          ServiceRegistration.Get<ILogger>().Warn("SlimTvManualScheduleModel: Could not edit schedule.");
      }
      else
      {
        var result = await tvHandler.ScheduleControl.CreateScheduleDetailedAsync(channel, title, startTime, endTime, recordingType, preInterval, postInterval, null, priority);
        if (!result.Success)
          ServiceRegistration.Get<ILogger>().Warn("SlimTvManualScheduleModel: Could not create schedule.");
      }
      
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(stateId, true);
    }

    protected static void Show(Guid stateId)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(stateId, null);
    }

    protected static void Show(IProgram program, Guid stateId)
    {
      NavigationContextConfig context = null;
      if (program != null)
      {
        context = new NavigationContextConfig();
        context.AdditionalContextVariables = new Dictionary<string, object>();
        context.AdditionalContextVariables[SlimTvClientModel.KEY_PROGRAM] = program;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(stateId, context);
    }

    protected static void Show(IScheduleRule scheduleRule, Guid stateId)
    {
      NavigationContextConfig context = null;
      if (scheduleRule != null)
      {
        context = new NavigationContextConfig();
        context.AdditionalContextVariables = new Dictionary<string, object>();
        context.AdditionalContextVariables[SlimTvClientModel.KEY_SCHEDULE_RULE] = scheduleRule;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(stateId, context);
    }

    protected void InitChannelGroups()
    {
      var channelGroups = GetGroupNavigationList();
      if (channelGroups == null)
      {
        _channelGroups = new List<IChannelGroup>();
        ChannelGroup = null;
        return;
      }

      _channelGroups = new List<IChannelGroup>(channelGroups);
      var currentGroup = channelGroups.Current;
      if (currentGroup != null)
        ChannelGroup = _channelGroups.FirstOrDefault(g => g.ChannelGroupId == currentGroup.ChannelGroupId);
      else
        ChannelGroup = _channelGroups.FirstOrDefault();
    }

    protected async Task InitChannels(IChannelGroup channelGroup)
    {
      _channels = new List<IChannel>();
      Channel = null;

      if (channelGroup == null)
        return;

      var result = await _tvHandler.ChannelAndGroupInfo.GetChannelsByGroupAsync(channelGroup);
      if (result.Success)
        _channels = result.Result;

      IChannel channel = Channel;
      if (channel != null)
        channel = _channels?.FirstOrDefault(c => c.ChannelId == channel.ChannelId);
      if (channel == null)
        channel = _channels?.FirstOrDefault();
      Channel = channel;
      return;
    }

    protected void InitPriorityList()
    {
      DialogHeader = "[SlimTvClient.Priority]";
      PriorityType previousType = Priority;
      _dialogActionsList.Clear();
      foreach (PriorityType priorityType in Enum.GetValues(typeof(PriorityType)))
      {
        PriorityType currentType = priorityType;
        ListItem priTypeItem = new ListItem(Consts.KEY_NAME, GetLocalizedPriorityName(currentType))
        {
          Command = new MethodDelegateCommand(() => Priority = currentType),
          Selected = currentType == previousType
        };
        _dialogActionsList.Add(priTypeItem);
      }
      _dialogActionsList.FireChange();
    }

    protected void InitRecordingTypeList()
    {
      DialogHeader = "[SlimTvClient.ScheduleType]";
      ScheduleRecordingType previousType = RecordingType;
      _dialogActionsList.Clear();
      foreach (ScheduleRecordingType recordingType in Enum.GetValues(typeof(ScheduleRecordingType)))
      {
        ScheduleRecordingType currentType = recordingType;
        if (IsManual && currentType.ToString().Contains("EveryTime"))
          continue;     // Cannot use every time options with manual recording, as they require a program name
        ListItem recTypeItem = new ListItem(Consts.KEY_NAME, GetLocalizedRecordingTypeName(currentType))
        {
          Command = new MethodDelegateCommand(() => RecordingType = currentType),
          Selected = currentType == previousType
        };
        _dialogActionsList.Add(recTypeItem);
      }
      _dialogActionsList.FireChange();
    }

    protected void InitChannelGroupList()
    {
      DialogHeader = "[SlimTvClient.ChannelGroupButton]";
      IChannelGroup previousChannelGroup = ChannelGroup;
      _dialogActionsList.Clear();
      foreach (IChannelGroup channelGroup in _channelGroups)
      {
        IChannelGroup currentChannelGroup = channelGroup;
        ListItem channelItem = new ListItem(Consts.KEY_NAME, channelGroup.Name)
        {
          Command = new MethodDelegateCommand(() => ChannelGroup = currentChannelGroup),
          Selected = previousChannelGroup != null && previousChannelGroup.ChannelGroupId == currentChannelGroup.ChannelGroupId
        };
        _dialogActionsList.Add(channelItem);
      }
      _dialogActionsList.FireChange();
    }

    protected void InitChannelList()
    {
      DialogHeader = "[SlimTvClient.ChannelLabel]";
      IChannel previousChannel = Channel;
      _dialogActionsList.Clear();
      if (_channels != null)
        foreach (IChannel channel in _channels)
        {
          IChannel currentChannel = channel;
          ListItem channelItem = new ListItem(Consts.KEY_NAME, channel.Name)
          {
            Command = new MethodDelegateCommand(() => Channel = currentChannel),
            Selected = previousChannel != null && previousChannel.ChannelId == currentChannel.ChannelId
          };
          _dialogActionsList.Add(channelItem);
        }
      _dialogActionsList.FireChange();
    }

    protected static string GetLocalizedRecordingTypeName(ScheduleRecordingType recordingType)
    {
      return string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", Enum.GetName(typeof(ScheduleRecordingType), recordingType));
    }

    protected static string GetLocalizedPriorityName(PriorityType priorityType)
    {
      return string.Format("[SlimTvClient.PriorityType_{0}]", Enum.GetName(typeof(PriorityType), priorityType));
    }

    protected static bool CheckScheduleValid(IChannel channel, DateTime startTime, DateTime endTime, ScheduleRecordingType recordingType, string scheduleName)
    {
      return channel != null && endTime > startTime && (endTime > DateTime.Now || recordingType != ScheduleRecordingType.Once) && !string.IsNullOrWhiteSpace(scheduleName);
    }

    #endregion

    #region IWorkflow

    public abstract override Guid ModelId { get; }

    public override bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      bool result = base.CanEnterState(oldContext, newContext);
      if (result)
        InitChannelGroups();
      return result;
    }

    public override void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      base.Reactivate(oldContext, newContext);
      OnContextChanged(newContext);
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      OnContextChanged(newContext);
    }

    protected void OnContextChanged(NavigationContext newContext)
    {
      object programObject;
      object scheduleObject;
      if (newContext.ContextVariables.TryGetValue(SlimTvClientModel.KEY_PROGRAM, out programObject) && programObject != null)
        UpdateFromProgram((IProgram)programObject);
      else if (newContext.ContextVariables.TryGetValue(SlimTvClientModel.KEY_SCHEDULE, out scheduleObject) && scheduleObject != null)
        UpdateFromSchedule((ISchedule)scheduleObject);
      else
        Manual();
    }

    #endregion
  }
}
