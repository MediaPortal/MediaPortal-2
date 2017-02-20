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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.PluginManager.Exceptions;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Messaging;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Extensions;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Common.Services.Settings;
using MediaPortal.Plugins.SlimTv.Client.Settings;

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
    protected AbstractProperty _showChannelNamesProperty = null;
    protected AbstractProperty _showChannelNumbersProperty = null;
    protected AbstractProperty _showChannelLogosProperty = null;

    protected SettingsChangeWatcher<SlimTvClientSettings> _settings = null;

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
      get { return (string)_groupNameProperty.GetValue(); }
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
    /// Exposes the list of actions for a program, i.e. watch now, schedule.
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
    /// Exposes whether channel names should be shown by the skin.
    /// </summary>
    public bool ShowChannelNames
    {
      get { return (bool)_showChannelNamesProperty.GetValue(); }
      protected set { _showChannelNamesProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes whether channel names should be shown by the skin.
    /// </summary>
    public AbstractProperty ShowChannelNamesProperty
    {
      get { return _showChannelNamesProperty; }
    }

    /// <summary>
    /// Exposes whether channel numbers should be shown by the skin.
    /// </summary>
    public bool ShowChannelNumbers
    {
      get { return (bool)_showChannelNumbersProperty.GetValue(); }
      protected set { _showChannelNumbersProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes whether channel numbers should be shown by the skin.
    /// </summary>
    public AbstractProperty ShowChannelNumbersProperty
    {
      get { return _showChannelNumbersProperty; }
    }

    /// <summary>
    /// Exposes whether channel logos should be shown by the skin.
    /// </summary>
    public bool ShowChannelLogos
    {
      get { return (bool)_showChannelLogosProperty.GetValue(); }
      protected set { _showChannelLogosProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes whether channel logos should be shown by the skin.
    /// </summary>
    public AbstractProperty ShowChannelLogosProperty
    {
      get { return _showChannelLogosProperty; }
    }

    // this overload is used by MultiChannelGuide in got focus trigger
    public void UpdateProgram(ListItem selectedItem)
    {
      if (selectedItem != null)
      {
        IProgram program = (IProgram)selectedItem.AdditionalProperties["PROGRAM"];
        UpdateProgramStatus(program);
      }
    }

    // this overload is used by events
    public void UpdateProgram(object sender, SelectionChangedEventArgs e)
    {
      UpdateProgram(e.FirstAddedItem as ListItem);
    }

    protected virtual void UpdateGuiProperties()
    {
      GroupName = CurrentChannelGroup != null ? CurrentChannelGroup.Name : string.Empty;
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
                Command = new MethodDelegateCommand(() =>
                    {
                      IChannel channel;
                      if (_tvHandler.ProgramInfo.GetChannel(program, out channel))
                      {
                        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
                        SlimTvClientModel model = workflowManager.GetModel(SlimTvClientModel.MODEL_ID) as SlimTvClientModel;
                        if (model != null)
                        {
                          model.Tune(channel);
                          // Always switch to fullscreen
                          workflowManager.NavigatePush(Consts.WF_STATE_ID_FULLSCREEN_VIDEO);
                        }
                      }
                    })
              });
        }

        if (_tvHandler.ScheduleControl != null)
        {
          RecordingStatus recordingStatus;
          if (_tvHandler.ScheduleControl.GetRecordingStatus(program, out recordingStatus) && recordingStatus != RecordingStatus.None)
          {
            if (isRunning)
              _programActions.Add(
                new ListItem(Consts.KEY_NAME, loc.ToString("[SlimTvClient.WatchFromBeginning]"))
                  {
                    Command = new MethodDelegateCommand(() => _tvHandler.WatchRecordingFromBeginning(program))
                  });

            _programActions.Add(
              new ListItem(Consts.KEY_NAME, loc.ToString(isRunning ? "[SlimTvClient.StopCurrentRecording]" : "[SlimTvClient.DeleteSchedule]", program.Title))
                {
                  Command = new MethodDelegateCommand(() =>
                                                        {
                                                          if (_tvHandler.ScheduleControl.RemoveScheduleForProgram(program, ScheduleRecordingType.Once))
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
                                                          ISchedule schedule;
                                                          bool result;
                                                          // "No Program" placeholder
                                                          if (program.ProgramId == -1)
                                                            result = _tvHandler.ScheduleControl.CreateScheduleByTime(new Channel { ChannelId = program.ChannelId }, program.StartTime, program.EndTime, out schedule);
                                                          else
                                                            result = _tvHandler.ScheduleControl.CreateSchedule(program, ScheduleRecordingType.Once, out schedule);

                                                          if (result)
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

    protected virtual bool UpdateRecordingStatus(IProgram program)
    {
      return true;
    }

    protected virtual bool UpdateRecordingStatus(IProgram program, RecordingStatus newStatus)
    {
      IProgramRecordingStatus status = program as IProgramRecordingStatus;
      if (status == null)
        return false;

      status.RecordingStatus = newStatus;
      SlimTvClientMessaging.SendSlimTvProgramChangedMessage(program);
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
        _showChannelNamesProperty = new WProperty(typeof(bool), true);
        _showChannelNumbersProperty = new WProperty(typeof(bool), true);
        _showChannelLogosProperty = new WProperty(typeof(bool), true);
        InitSettingsWatcher();

        BuildExtensions();

        _isInitialized = true;
      }
      SubscribeToMessages();
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
            Type extensionClass = slimTvProgramExtension.ExtensionClass;
            if (extensionClass == null)
              throw new PluginInvalidStateException("Could not find class type for extension {0}", slimTvProgramExtension.Caption);
            IProgramAction action = Activator.CreateInstance(extensionClass) as IProgramAction;
            if (action == null)
              throw new PluginInvalidStateException("Could not create IProgramAction instance of class {0}", extensionClass);
            _programExtensions[slimTvProgramExtension.Id] = new TvExtension { Caption = slimTvProgramExtension.Caption, Extension = action };
          }
        }
        catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add SlimTv extension with id '{0}'", e, itemMetadata.Id);
        }
      }
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(SlimTvClientMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SlimTvClientMessaging.CHANNEL)
      {
        SlimTvClientMessaging.MessageType messageType = (SlimTvClientMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SlimTvClientMessaging.MessageType.ProgramStatusChanged:
            IProgram program = (IProgram)message.MessageData[SlimTvClientMessaging.KEY_PROGRAM];
            UpdateRecordingStatus(program);
            break;
        }
      }
    }

    void InitSettingsWatcher()
    {
      if (_settings != null)
        return;
      _settings = new SettingsChangeWatcher<SlimTvClientSettings>();
      UpdatePropertiesFromSettings(_settings.Settings);
      _settings.SettingsChanged = OnSettingsChanged;
    }

    protected void OnSettingsChanged(object sender, EventArgs e)
    {
      UpdatePropertiesFromSettings(_settings.Settings);
    }

    protected void UpdatePropertiesFromSettings(SlimTvClientSettings settings)
    {
      ShowChannelNames = settings.EpgShowChannelNames;
      ShowChannelNumbers = settings.EpgShowChannelNumbers;
      ShowChannelLogos = settings.EpgShowChannelLogos;
    }

    #endregion

    #region Channel, groups and programs

    protected virtual void UpdateProgramStatus(IProgram program)
    {
      CurrentProgram.SetProgram(program);
    }

    #endregion

    #endregion
  }
}
