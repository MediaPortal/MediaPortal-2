#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Extensions;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Media.General;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// <see cref="SlimTvGuideModelBase"/> acts as base class for all TvGuide models (single channel, multi channel).
  /// </summary>
  public abstract class SlimTvGuideModelBase : SlimTvModelBase
  {
    #region Protected fields

    protected IPluginItemStateTracker _slimTvExtensionsPluginItemStateTracker;
    protected Dictionary<Guid, TvExtension> _programExtensions = new Dictionary<Guid, TvExtension>();

    protected AbstractProperty _groupNameProperty = null;
    protected AbstractProperty _currentProgramProperty = null;

    public struct TvExtension
    {
      public string Caption;
      public IProgramAction Extension;
    }

    #endregion

    #region Variables

    protected ItemsList _programActions;
    protected string _programActionsDialogName = "DialogProgramActions";

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current group name to the skin.
    /// </summary>
    public string GroupName
    {
      get { return (string) _groupNameProperty.GetValue(); }
      set { _groupNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current group name to the skin.
    /// </summary>
    public AbstractProperty GroupNameProperty
    {
      get { return _groupNameProperty; }
    }

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList ProgramActions
    {
      get { return _programActions; }
    }

    /// <summary>
    /// Exposes the current program of tuned channel to the skin.
    /// </summary>
    public ProgramProperties CurrentProgram
    {
      get { return (ProgramProperties) _currentProgramProperty.GetValue(); }
      set { _currentProgramProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current program of tuned channel to the skin.
    /// </summary>
    public AbstractProperty CurrentProgramProperty
    {
      get { return _currentProgramProperty; }
    }

    public void UpdateProgram(ListItem selectedItem)
    {
      if (selectedItem != null)
      {
        IProgram program = (IProgram) selectedItem.AdditionalProperties["PROGRAM"];
        UpdateSingleProgramInfo(program);
      }
    }

    protected void SetGroupName()
    {
      if (_webChannelGroupIndex < _channelGroups.Count)
      {
        IChannelGroup group = _channelGroups[_webChannelGroupIndex];
        GroupName = group.Name;
      }
    }

    protected void ShowProgramActions(IProgram program)
    {
      if (program == null)
        return;

      ILocalization loc = ServiceRegistration.Get<ILocalization>();

      _programActions = new ItemsList();
      // if program is over already, there is nothing to do.
      if (program.EndTime < DateTime.Now)
      {
        _programActions.Add(new ListItem(Consts.KEY_NAME, loc.ToString("[SlimTvClient.ProgramOver]")));
      }
      else
      {
        // Check if program is currently running.
        bool isRunning = DateTime.Now >= program.StartTime && DateTime.Now <= program.EndTime;
        if (isRunning)
        {
          _programActions.Add(new ListItem(Consts.KEY_NAME, loc.ToString("[SlimTvClient.WatchNow]"))
                                {
                                  Command =
                                    new MethodDelegateCommand(() =>
                                                                {
                                                                  IChannel channel;
                                                                  if (_tvHandler.ProgramInfo.GetChannel(program, out channel))
                                                                    _tvHandler.StartTimeshift(PlayerManagerConsts.PRIMARY_SLOT, channel);
                                                                })
                                });
        }

        if (_tvHandler.ScheduleControl != null)
        {
          RecordingStatus recordingStatus;
          if (_tvHandler.ScheduleControl.GetRecordingStatus(program, out recordingStatus) && recordingStatus != RecordingStatus.None)
          {
            _programActions.Add(
              new ListItem(Consts.KEY_NAME, loc.ToString("[SlimTvClient.DeleteSchedule]"))
                {
                  Command = new MethodDelegateCommand(() =>
                                                        {
                                                          if (_tvHandler.ScheduleControl.RemoveSchedule(program))
                                                            UpdateRecordingStatus(program, RecordingStatus.None);
                                                        }
                    )
                });
          }
          else
          {
            _programActions.Add(
              new ListItem(Consts.KEY_NAME, loc.ToString(isRunning ? "[SlimTvClient.RecordNow]" : "[SlimTvClient.CreateSchedule]"))
                {
                  Command = new MethodDelegateCommand(() =>
                                                        {
                                                          if (_tvHandler.ScheduleControl.CreateSchedule(program))
                                                            UpdateRecordingStatus(program, RecordingStatus.Scheduled);
                                                        }
                    )
                });
          }
        }
      }

      // Add list entries for extensions
      foreach (KeyValuePair<Guid, TvExtension> programExtension in _programExtensions)
      {
        TvExtension extension = programExtension.Value;
        // First check if this extension applies for the selected program
        if (!extension.Extension.IsAvailable(program))
          continue;

        _programActions.Add(
            new ListItem(Consts.KEY_NAME, loc.ToString(extension.Caption))
            {
              Command = new MethodDelegateCommand(() => extension.Extension.ProgramAction(program))
            });
      }

      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ShowDialog(_programActionsDialogName);
    }

    protected virtual bool UpdateRecordingStatus(IProgram program, RecordingStatus newStatus)
    {
      IProgramRecordingStatus status = program as IProgramRecordingStatus;
      if (status == null)
        return false;
      
      status.RecordingStatus = newStatus;
      return true;
    }
    #endregion

    #region Members

    #region Inits and Updates

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _groupNameProperty = new WProperty(typeof(string), String.Empty);
        _currentProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());

        BuildExtensions();

        _isInitialized = true;
      }
      base.InitModel();
    }

    protected void BuildExtensions()
    {
      _slimTvExtensionsPluginItemStateTracker = new FixedItemStateTracker("SlimTvHandler - Extension registration");

      IPluginManager pluginManager = ServiceRegistration.Get<IPluginManager>();
      foreach (PluginItemMetadata itemMetadata in pluginManager.GetAllPluginItemMetadata(SlimTvExtensionBuilder.SLIMTVEXTENSIONPATH))
      {
        try
        {
          SlimTvProgramExtension slimTvProgramExtension = pluginManager.RequestPluginItem<SlimTvProgramExtension>(
              SlimTvExtensionBuilder.SLIMTVEXTENSIONPATH, itemMetadata.Id, _slimTvExtensionsPluginItemStateTracker);
          if (slimTvProgramExtension == null)
            ServiceRegistration.Get<ILogger>().Warn("Could not instantiate SlimTv extension with id '{0}'", itemMetadata.Id);
          else
          {
            IProgramAction action = Activator.CreateInstance(slimTvProgramExtension.ExtensionClass) as IProgramAction;
            if (action == null)
              throw new PluginInvalidStateException("Could not create IProgramAction instance of class {0}", slimTvProgramExtension.ExtensionClass);
            _programExtensions[slimTvProgramExtension.Id] = new TvExtension { Caption = slimTvProgramExtension.Caption, Extension = action };
          }
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add SlimTv extension with id '{0}'", e, itemMetadata.Id);
        }
      }
    }

    #endregion

    #region Channel, groups and programs

    private void UpdateSingleProgramInfo(IProgram program)
    {
      CurrentProgram.SetProgram(program);
    }

    #endregion

    #endregion
  }
}