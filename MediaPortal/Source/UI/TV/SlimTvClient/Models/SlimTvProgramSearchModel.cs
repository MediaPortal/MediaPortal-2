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
  /// <see cref="SlimTvProgramSearchModel"/> holds all data for extended scheduling options.
  /// </summary>
  public class SlimTvProgramSearchModel : SlimTvGuideModelBase
  {
    public const string MODEL_ID_STR = "71F1D594-21BF-4639-9F8A-3CE8D8170333";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    #region Fields

    protected int _lastProgramId;
    protected AbstractProperty _channelNameProperty = null;
    protected AbstractProperty _programSearchTextProperty = null;
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
    /// Exposes the current search text.
    /// </summary>
    public string ProgramSearchText
    {
      get { return (string)_programSearchTextProperty.GetValue(); }
      set { _programSearchTextProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel name to the skin.
    /// </summary>
    public AbstractProperty ProgramSearchTextProperty
    {
      get { return _programSearchTextProperty; }
    }

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList ProgramsList
    {
      get { return _programsList; }
    }

    public void RecordSingleProgram(IProgram program)
    {
      CreateOrDeleteSchedule(program);
    }

    public void RecordSeries(IProgram program)
    {
      InitSeriesTypeList(program);
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog("DialogExtSchedule");
    }

    public void RecordOrCancelSeries(IProgram program, ScheduleRecordingType scheduleRecordingType)
    {
      CreateOrDeleteSchedule(program, scheduleRecordingType);
    }

    public void CancelSchedule(IProgram program)
    {
      InitDeleteChoicesList(program);
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog("DialogExtSchedule");
    }

    #endregion

    #region Inits and Updates

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _channelNameProperty = new WProperty(typeof(string), string.Empty);
        _programSearchTextProperty = new WProperty(typeof(string), string.Empty);
        _programSearchTextProperty.Attach(ProgramSearchTextChanged);
      }
      base.InitModel();
    }

    private void ProgramSearchTextChanged(AbstractProperty property, object oldvalue)
    {
      UpdatePrograms();
    }

    private void InitSeriesTypeList(IProgram program)
    {
      var wf = ServiceRegistration.Get<IWorkflowManager>();
      var model = wf.GetModel(SlimTvExtScheduleModel.MODEL_ID) as SlimTvExtScheduleModel;
      if (model == null)
        return;
      DialogHeader = "[SlimTvClient.ChooseScheduleType]";
      model.DialogActionsList.Clear();
      foreach (string name in Enum.GetNames(typeof(ScheduleRecordingType)))
      {
        string currentName = name;
        ListItem recTypeItem = new ListItem(Consts.KEY_NAME, string.Format("[SlimTvClient.ScheduleRecordingType_{0}]", name))
        {
          Command = new MethodDelegateCommand(() => RecordOrCancelSeries(program, (ScheduleRecordingType)Enum.Parse(typeof(ScheduleRecordingType), currentName)))
        };
        model.DialogActionsList.Add(recTypeItem);
      }
      model.DialogActionsList.FireChange();
    }

    private void InitDeleteChoicesList(IProgram program)
    {
      var wf = ServiceRegistration.Get<IWorkflowManager>();
      var model = wf.GetModel(SlimTvExtScheduleModel.MODEL_ID) as SlimTvExtScheduleModel;
      if (model == null)
        return;
      DialogHeader = "[SlimTvClient.DeleteScheduleType]";
      model.DialogActionsList.Clear();
      ListItem recTypeItem = new ListItem(Consts.KEY_NAME, "[SlimTvClient.DeleteSingle]")
          {
            Command = new MethodDelegateCommand(() => RecordOrCancelSeries(program, ScheduleRecordingType.Once))
          };
      model.DialogActionsList.Add(recTypeItem);
      recTypeItem = new ListItem(Consts.KEY_NAME, "[SlimTvClient.DeleteFullSchedule]")
      {
        Command = new MethodDelegateCommand(() => RecordOrCancelSeries(program, ScheduleRecordingType.EveryTimeOnEveryChannel))
      };
      model.DialogActionsList.Add(recTypeItem);
    }

    #endregion

    #region Channel, groups and programs

    protected override void Update()
    {
    }

    //protected override void UpdateCurrentChannel()
    //{
    //}

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
      if (_tvHandler.ProgramInfo == null)
        return;

      if (string.IsNullOrEmpty(ProgramSearchText) || ProgramSearchText.Length < 2)
      {
        SetEmptyPrograms();
        return;
      }

      DateTime dtDay = DateTime.Now;
      if (!_tvHandler.ProgramInfo.GetPrograms(ProgramSearchText, dtDay, dtDay.AddDays(28), out _programs))
      {
        SetEmptyPrograms();
        return;
      }
      FillProgramsList();
    }

    private void SetEmptyPrograms()
    {
      _programs = new List<IProgram>();
      FillProgramsList();
      return;
    }

    private void FillProgramsList()
    {
      _programsList.Clear();

      foreach (IProgram program in _programs)
      {
        // Use local variable, otherwise delegate argument is not fixed
        ProgramProperties programProperties = new ProgramProperties();
        IProgram currentProgram = program;
        programProperties.SetProgram(currentProgram);

        ProgramListItem item = new ProgramListItem(programProperties)
        {
          Command = new MethodDelegateCommand(() =>
          {
            var isSingle = programProperties.IsScheduled;
            var isSeries = programProperties.IsSeriesScheduled;
            if (isSingle || isSeries)
              CancelSchedule(currentProgram);
            else
              RecordSeries(currentProgram);
          })
        };
        item.AdditionalProperties["PROGRAM"] = currentProgram;
        item.Selected = _lastProgramId == program.ProgramId; // Restore focus

        _programsList.Add(item);
      }

      _programsList.FireChange();
    }

    protected override RecordingStatus? CreateOrDeleteSchedule(IProgram program, ScheduleRecordingType recordingType = ScheduleRecordingType.Once)
    {
      _lastProgramId = program.ProgramId;
      var newStatus = base.CreateOrDeleteSchedule(program, recordingType);

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

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    #endregion
  }
}
