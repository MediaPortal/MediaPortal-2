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
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Messaging;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// <see cref="SlimTvExtScheduleModel"/> holds all data for extended scheduling options.
  /// </summary>
  public class SlimTvExtScheduleModel : SlimTvGuideModelBase
  {
    public const string MODEL_ID_STR = "EB9CB370-9CD6-4D72-8354-73E446104438";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #region Fields

    protected IProgram _selectedProgram;
    protected ISchedule _selectedSchedule;
    protected bool _isScheduleMode = false;
    protected int _lastProgramId;
    protected AbstractProperty _channelNameProperty = null;
    protected AbstractProperty _isSingleRecordingScheduledProperty = null;
    protected AbstractProperty _isSeriesRecordingScheduledProperty = null;
    protected readonly ItemsList _programsList = new ItemsList();

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
    /// Indicates if the current program is scheduled as single recording.
    /// </summary>
    public bool IsSingleRecordingScheduled
    {
      get { return (bool)_isSingleRecordingScheduledProperty.GetValue(); }
      set { _isSingleRecordingScheduledProperty.SetValue(value); }
    }

    public AbstractProperty IsSingleRecordingScheduledProperty
    {
      get { return _isSingleRecordingScheduledProperty; }
    }

    /// <summary>
    /// Indicates if the current program is scheduled as series recording.
    /// </summary>
    public bool IsSeriesRecordingScheduled
    {
      get { return (bool)_isSeriesRecordingScheduledProperty.GetValue(); }
      set { _isSeriesRecordingScheduledProperty.SetValue(value); }
    }

    public AbstractProperty IsSeriesRecordingScheduledProperty
    {
      get { return _isSeriesRecordingScheduledProperty; }
    }

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList ProgramsList
    {
      get { return _programsList; }
    }

    public void RecordSingleProgram()
    {
      CreateOrDeleteSchedule(_selectedProgram);
    }

    public void RecordSeries()
    {
      InitSeriesTypeList();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog("DialogExtSchedule");
    }

    public void RecordOrCancelSeries(ScheduleRecordingType scheduleRecordingType)
    {
      CreateOrDeleteSchedule(_selectedProgram, scheduleRecordingType);
      _selectedSchedule = null;
      UpdateButtonStateForSchedule();
    }

    public void CancelSchedule()
    {
      InitDeleteChoicesList();
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog("DialogExtSchedule");
    }

    public static void Show(ISchedule schedule)
    {
      NavigationContextConfig navigationContextConfig = new NavigationContextConfig();
      navigationContextConfig.AdditionalContextVariables = new Dictionary<string, object>();
      navigationContextConfig.AdditionalContextVariables[SlimTvClientModel.KEY_SCHEDULE] = schedule;
      navigationContextConfig.AdditionalContextVariables[SlimTvClientModel.KEY_MODE] = true;
      Show(navigationContextConfig);
    }

    public static void Show(IProgram program)
    {
      NavigationContextConfig navigationContextConfig = new NavigationContextConfig();
      navigationContextConfig.AdditionalContextVariables = new Dictionary<string, object>();
      navigationContextConfig.AdditionalContextVariables[SlimTvClientModel.KEY_PROGRAM] = program;
      navigationContextConfig.AdditionalContextVariables[SlimTvClientModel.KEY_MODE] = false;
      Show(navigationContextConfig);
    }

    private static void Show(NavigationContextConfig context)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      Guid stateId = new Guid("3C6081CB-88DC-44A7-9E17-8D7BFE006EE5");
      if (workflowManager.IsAnyStateContainedInNavigationStack(new Guid[] { stateId }))
        workflowManager.NavigatePopToState(stateId, false);
      else
        workflowManager.NavigatePush(stateId, context);
    }

    #endregion

    #region Inits and Updates

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _channelNameProperty = new WProperty(typeof(string), string.Empty);
        _isSingleRecordingScheduledProperty = new WProperty(typeof(bool), false);
        _isSeriesRecordingScheduledProperty = new WProperty(typeof(bool), false);
      }
      base.InitModel();
    }

    private void InitSeriesTypeList()
    {
      DialogHeader = "[SlimTvClient.ChooseScheduleType]";
      _dialogActionsList.Clear();
      foreach (string name in Enum.GetNames(typeof(ScheduleRecordingType)))
      {
        // Single recordings are handled separately
        if (name == ScheduleRecordingType.Once.ToString())
          continue;

        string currentName = name;
        ListItem recTypeItem = new ListItem(Consts.KEY_NAME, string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", name))
        {
          Command = new MethodDelegateCommand(() => RecordOrCancelSeries((ScheduleRecordingType)Enum.Parse(typeof(ScheduleRecordingType), currentName)))
        };
        _dialogActionsList.Add(recTypeItem);
      }
    }

    private void InitDeleteChoicesList()
    {
      DialogHeader = "[SlimTvClient.DeleteScheduleType]";
      _dialogActionsList.Clear();
      ListItem recTypeItem = new ListItem(Consts.KEY_NAME, "[SlimTvClient.DeleteSingle]")
          {
            Command = new MethodDelegateCommand(() => RecordOrCancelSeries(ScheduleRecordingType.Once))
          };
      _dialogActionsList.Add(recTypeItem);
      recTypeItem = new ListItem(Consts.KEY_NAME, "[SlimTvClient.DeleteFullSchedule]")
      {
        Command = new MethodDelegateCommand(() => RecordOrCancelSeries(ScheduleRecordingType.EveryTimeOnEveryChannel))
      };
      _dialogActionsList.Add(recTypeItem);
    }

    #endregion

    #region Channel, groups and programs

    protected override void Update()
    {
    }

    protected override void UpdateProgramStatus(IProgram program)
    {
      base.UpdateProgramStatus(program);

      IChannel channel;
      ChannelName = _tvHandler.ChannelAndGroupInfo.GetChannel(program.ChannelId, out channel) ? channel.Name : string.Empty;
    }

    protected override bool UpdateRecordingStatus(IProgram program)
    {
      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;
      if (recordingStatus == null)
        return false;

      foreach (var programItem in _programsList.OfType<ProgramListItem>().Where(programItem => programItem.Program.ProgramId == program.ProgramId))
        programItem.Program.UpdateState(recordingStatus.RecordingStatus);
      return true;
    }

    /// <summary>
    /// For extended scheduling we will load all programs with same title independent from channel.
    /// </summary>
    protected void UpdatePrograms()
    {
      if (_selectedProgram == null)
        return;

      if (_tvHandler.ProgramInfo == null)
        return;

      DateTime dtDay = DateTime.Now;
      if (!_tvHandler.ProgramInfo.GetPrograms(_selectedProgram.Title, dtDay, dtDay.AddDays(28), out _programs))
        return;

      FillProgramsList();
    }

    /// <summary>
    /// Loads all programs that are affected by the given series schedule.
    /// </summary>
    protected void UpdateProgramsForSchedule()
    {
      if (_selectedSchedule == null)
        return;

      if (_tvHandler.ScheduleControl == null)
        return;

      if (!_tvHandler.ScheduleControl.GetProgramsForSchedule(_selectedSchedule, out _programs))
        return;

      FillProgramsList();
      _selectedProgram = _programs.FirstOrDefault();
      UpdateButtonStateForSchedule();
    }

    protected void UpdateButtonStateForSchedule()
    {
      if (!_isScheduleMode)
        return;

      IsSeriesRecordingScheduled = IsSingleRecordingScheduled = _selectedSchedule != null;
    }

    private void FillProgramsList()
    {
      _programsList.Clear();

      bool isSingle = false;
      bool isSeries = false;
      foreach (IProgram program in _programs)
      {
        IChannel channel;
        if (!_tvHandler.ChannelAndGroupInfo.GetChannel(program.ChannelId, out channel))
          channel = null;
        // Use local variable, otherwise delegate argument is not fixed
        ProgramProperties programProperties = new ProgramProperties();
        IProgram currentProgram = program;
        programProperties.SetProgram(currentProgram, channel);

        if (ProgramComparer.Instance.Equals(_selectedProgram, program))
        {
          isSingle = programProperties.IsScheduled;
          isSeries = programProperties.IsSeriesScheduled;
        }

        ProgramListItem item = new ProgramListItem(programProperties)
        {
          Command = new MethodDelegateCommand(() => CreateOrDeleteSchedule(currentProgram))
        };
        item.AdditionalProperties["PROGRAM"] = currentProgram;
        item.Selected = _lastProgramId == program.ProgramId; // Restore focus
        if (channel != null)
          item.SetLabel("ChannelName", channel.Name);

        _programsList.Add(item);
      }

      // "Record" buttons are related to properties, for schedules we need to keep them to "Cancel record" state.
      if (_isScheduleMode)
        isSingle = isSeries = _selectedSchedule != null;

      IsSingleRecordingScheduled = isSingle;
      IsSeriesRecordingScheduled = isSeries;

      _programsList.FireChange();
    }

    protected override RecordingStatus? CreateOrDeleteSchedule(IProgram program, ScheduleRecordingType recordingType = ScheduleRecordingType.Once)
    {
      _lastProgramId = program.ProgramId;
      var newStatus = base.CreateOrDeleteSchedule(program, recordingType);

      UpdateButtonStateForSchedule();

      if (!newStatus.HasValue)
        return RecordingStatus.None;

      UpdatePrograms(); // Reload all programs, as series scheduling will affect multiple programs
      NotifyAllPrograms();
      return newStatus.Value;
    }

    private void NotifyAllPrograms()
    {
      // Send message to all listeners that programs might have been changed
      foreach (IProgram program in _programs)
        SlimTvClientMessaging.SendSlimTvProgramChangedMessage(program);
    }

    protected override bool UpdateRecordingStatus(IProgram program, RecordingStatus newStatus)
    {
      if (ProgramComparer.Instance.Equals(_selectedProgram, program))
      {
        IsSingleRecordingScheduled = newStatus.HasFlag(RecordingStatus.Scheduled);
        IsSeriesRecordingScheduled = newStatus.HasFlag(RecordingStatus.SeriesScheduled);
      }
      return base.UpdateRecordingStatus(program, newStatus);
    }

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _selectedProgram = null;
      _selectedSchedule = null;
      _programsList.Clear();
      base.EnterModelContext(oldContext, newContext);
      object mode;
      if (newContext.ContextVariables.TryGetValue(SlimTvClientModel.KEY_MODE, out mode))
      {
        _isScheduleMode = (bool)mode;

        object programObject;
        if (newContext.ContextVariables.TryGetValue(SlimTvClientModel.KEY_PROGRAM, out programObject))
        {
          _selectedProgram = (IProgram)programObject;
          UpdatePrograms();
        }

        object scheduleObject;
        if (newContext.ContextVariables.TryGetValue(SlimTvClientModel.KEY_SCHEDULE, out scheduleObject))
        {
          _selectedSchedule = (ISchedule)scheduleObject;
          UpdateProgramsForSchedule();
        }
      }
    }

    #endregion
  }
}
