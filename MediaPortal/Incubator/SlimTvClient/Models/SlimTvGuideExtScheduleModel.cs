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
using System.Collections.Generic;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;

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
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList ProgramsList
    {
      get { return _programsList; }
    }

    #endregion

    #region Inits and Updates

    protected override void InitModel()
    {
      if (!_isInitialized)
        _channelNameProperty = new WProperty(typeof(string), string.Empty);

      base.InitModel();
    }

    #endregion

    #region Channel, groups and programs

    protected override void Update()
    {
    }

    protected override void UpdateCurrentChannel()
    {
    }

    protected override void UpdateSingleProgramInfo(IProgram program)
    {
      base.UpdateSingleProgramInfo(program);
      IChannel channel;
      ChannelName = _tvHandler.ChannelAndGroupInfo.GetChannel(program.ChannelId, out channel) ? channel.Name : string.Empty;
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
          //          Command = new MethodDelegateCommand(() => ShowProgramActions(currentProgram))
        };
        item.AdditionalProperties["PROGRAM"] = currentProgram;

        _programsList.Add(item);
      }
      _programsList.FireChange();
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