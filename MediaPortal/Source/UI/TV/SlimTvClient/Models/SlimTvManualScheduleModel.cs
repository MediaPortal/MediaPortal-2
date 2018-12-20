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
  public class SlimTvManualScheduleModel : SlimTvModelBase
  {
    #region Constants

    public const string MODEL_ID_STR = "B2428C91-6B70-42E1-9519-1D5AA9D558A3";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string STATE_MANUAL_SCHEDULE_STR = "DFAFCA6B-92AC-432D-98E7-3E50E3AD2F61";
    public static readonly Guid STATE_MANUAL_SCHEDULE = new Guid(STATE_MANUAL_SCHEDULE_STR);

    #endregion

    #region Protected fields
    
    protected IList<IChannelGroup> _channelGroups;
    protected IList<IChannel> _channels;

    protected AbstractProperty _channelGroupProperty;
    protected AbstractProperty _channelProperty;
    protected AbstractProperty _recordingTypeProperty;
    protected AbstractProperty _recordingTypeNameProperty;
    protected AbstractProperty _isScheduleValidProperty;

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
      _startTimeProperty.Attach(OnTimeChanged);
      _endTimeProperty.Attach(OnTimeChanged);
      _recordingTypeProperty.Attach(OnRecordingTypeChanged);
    }

    protected void Reset()
    {
      IChannelGroup channelGroup = ChannelGroup;
      if (channelGroup == null)
        ChannelGroup = _channelGroups.FirstOrDefault();
      Channel = _channels?.FirstOrDefault();
      RecordingType = ScheduleRecordingType.Once;

      DateTime now = DateTime.Now;
      DateTime start = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, now.Kind);
      StartTime = start;
      EndTime = start.AddHours(1);
    }

    protected override void Update()
    {
    }

    protected void UpdateFromProgram(IProgram program)
    {
      Channel = _channels?.FirstOrDefault(c => c.ChannelId == program.ChannelId);
      RecordingType = ScheduleRecordingType.Once;
      StartTime = program.StartTime;
      EndTime = program.EndTime;
    }

    protected void InitChannelGroups()
    {
      var channelGroups = ChannelContext.Instance.ChannelGroups;
      if (channelGroups == null)
      {
        _channelGroups = new List<IChannelGroup>();
        ChannelGroup = null;
        return;
      }

      _channelGroups = new List<IChannelGroup>(channelGroups);
      var currentGroup = ChannelContext.Instance.ChannelGroups.Current;
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

      var result = await _tvHandler.ChannelAndGroupInfo.GetChannelsAsync(channelGroup);
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
      IsScheduleValid = CheckScheduleValid(Channel, StartTime, EndTime);
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
    /// Creates a schedule using the configured properties.
    /// </summary>
    public async Task CreateSchedule()
    {
      IChannel channel = Channel;
      DateTime startTime = StartTime;
      DateTime endTime = EndTime;
      ScheduleRecordingType recordingType = RecordingType;

      if (!CheckScheduleValid(channel, startTime, endTime))
        return;

      ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
      var result = await tvHandler.ScheduleControl.CreateScheduleByTimeAsync(channel, startTime, endTime, recordingType);
      if (!result.Success)
        ServiceRegistration.Get<ILogger>().Warn("SlimTvManualScheduleModel: Could not create schedule.");

      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToState(STATE_MANUAL_SCHEDULE, true);
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
      screenManager.ShowDialog("DialogManualSchedule");
    }

    /// <summary>
    /// Shows the channel selection dialog.
    /// </summary>
    public void ShowChannelDialog()
    {
      InitChannelList();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog("DialogManualSchedule");
    }

    /// <summary>
    /// Shows the recording type selection dialog.
    /// </summary>
    public void ShowRecordingTypeDialog()
    {
      InitRecordingTypeList();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog("DialogManualSchedule");
    }

    public static void Show()
    {
      Show(null);
    }

    public static void Show(IProgram program)
    {
      NavigationContextConfig context = null;
      if (program != null)
      {
        context = new NavigationContextConfig();
        context.AdditionalContextVariables = new Dictionary<string, object>();
        context.AdditionalContextVariables[SlimTvClientModel.KEY_PROGRAM] = program;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(STATE_MANUAL_SCHEDULE, context);
    }

    #endregion

    #region Protected methods

    protected void InitRecordingTypeList()
    {
      DialogHeader = "[SlimTvClient.ScheduleType]";
      ScheduleRecordingType previousType = RecordingType;
      _dialogActionsList.Clear();
      foreach (ScheduleRecordingType recordingType in Enum.GetValues(typeof(ScheduleRecordingType)))
      {
        ScheduleRecordingType currentType = recordingType;
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

    protected static bool CheckScheduleValid(IChannel channel, DateTime startTime, DateTime endTime)
    {
      return channel != null && endTime > startTime && endTime > DateTime.Now;
    }

    #endregion

    #region IWorkflow

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

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
      if (newContext.ContextVariables.TryGetValue(SlimTvClientModel.KEY_PROGRAM, out programObject) && programObject != null)
        UpdateFromProgram((IProgram)programObject);
      else
        Reset();
    }

    #endregion
  }
}
