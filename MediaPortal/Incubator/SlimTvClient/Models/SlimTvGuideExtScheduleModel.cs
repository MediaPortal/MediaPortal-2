#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
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
    protected AbstractProperty _channelNameProperty = null;
    protected AbstractProperty _isSingleRecordingScheduledProperty = null;
    protected AbstractProperty _isSeriesRecordingScheduledProperty = null;
    protected readonly ItemsList _programsList = new ItemsList();
    protected readonly ItemsList _seriesTypesList = new ItemsList();

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

    /// <summary>
    /// Exposes the list of available series recording types.
    /// </summary>
    public ItemsList SeriesTypesList
    {
      get { return _seriesTypesList; }
    }

    public void RecordSingleProgram()
    {
      CreateOrDeleteSingleSchedule(_selectedProgram);
    }

    public void RecordSeries()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog("DialogChooseScheduleType");
    }

    public void RecordSeries(ScheduleRecordingType scheduleRecordingType)
    {
    }

    public void CancelSchedule()
    {
      // TODO: Ask for cancel single or full schedule
      CreateOrDeleteSingleSchedule(_selectedProgram);
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
        InitSeriesTypeList();
      }
      base.InitModel();
    }

    private void InitSeriesTypeList()
    {
      foreach (string name in Enum.GetNames(typeof(ScheduleRecordingType)))
      {
        // Single recordings are handled separately
        if (name == ScheduleRecordingType.Once.ToString())
          continue;

        string currentName = name;
        ListItem recTypeItem = new ListItem(Consts.KEY_NAME, string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", name))
        {
          Command = new MethodDelegateCommand(() => RecordSeries((ScheduleRecordingType)Enum.Parse(typeof(ScheduleRecordingType), currentName)))
        };
        _seriesTypesList.Add(recTypeItem);
      }
    }

    #endregion

    #region Channel, groups and programs

    protected override void Update()
    {
    }

    protected override void UpdateCurrentChannel()
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
        programItem.Program.IsScheduled = recordingStatus.RecordingStatus != RecordingStatus.None;
      return true;
    }

    /// <summary>
    /// For extended scheduling we will load all programs with same title independent from channel.
    /// </summary>
    protected override void UpdatePrograms()
    {
      if (_selectedProgram == null)
        return;

      _programsList.Clear();

      if (_tvHandler.ProgramInfo == null)
        return;

      DateTime dtDay = DateTime.Now;
      if (!_tvHandler.ProgramInfo.GetPrograms(_selectedProgram.Title, dtDay, dtDay.AddDays(28), out _programs))
        return;

      foreach (IProgram program in _programs)
      {
        // Use local variable, otherwise delegate argument is not fixed
        ProgramProperties programProperties = new ProgramProperties();
        IProgram currentProgram = program;
        programProperties.SetProgram(currentProgram);

        ProgramListItem item = new ProgramListItem(programProperties)
        {
          Command = new MethodDelegateCommand(() => CreateOrDeleteSingleSchedule(currentProgram))
        };
        item.AdditionalProperties["PROGRAM"] = currentProgram;

        _programsList.Add(item);
      }
      _programsList.FireChange();
    }

    private RecordingStatus CreateOrDeleteSingleSchedule(IProgram program)
    {
      IScheduleControl scheduleControl = _tvHandler.ScheduleControl;
      RecordingStatus? newStatus = null;
      if (scheduleControl != null)
      {
        RecordingStatus recordingStatus;
        if (scheduleControl.GetRecordingStatus(program, out recordingStatus) && recordingStatus.HasFlag(RecordingStatus.Scheduled))
        {
          if (scheduleControl.RemoveSchedule(program))
            newStatus = RecordingStatus.None;
        }
        else
        {
          ISchedule schedule;
          if (scheduleControl.CreateSchedule(program, out schedule))
            newStatus = RecordingStatus.Scheduled;
        }
      }

      if (!newStatus.HasValue)
        return RecordingStatus.None;

      UpdateRecordingStatus(program, newStatus.Value);
      return newStatus.Value;
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
      base.EnterModelContext(oldContext, newContext);
      object programObject;
      if (newContext.ContextVariables.TryGetValue(SlimTvClientModel.KEY_PROGRAM, out programObject))
      {
        _selectedProgram = (IProgram)programObject;
        UpdatePrograms();
      }
    }

    #endregion
  }
}