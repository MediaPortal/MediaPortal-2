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
using System.Threading.Tasks;
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
using MediaPortal.Plugins.SlimTv.Client.Messaging;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// <see cref="SlimTvModelBase"/> provides basic features for all derived models, i.e. channel group and channel selection.
  /// </summary>
  public abstract class SlimTvModelBase : BaseTimerControlledModel, IWorkflowModel
  {
    #region Protected fields

    protected ITvHandler _tvHandler;
    protected ItemsList _channelGroupList = new ItemsList();
    protected IPluginItemStateTracker _slimTvExtensionsPluginItemStateTracker;
    protected Dictionary<Guid, TvExtension> _programExtensions;

    protected IList<IProgram> _programs;
    protected bool _isInitialized;

    protected AbstractProperty _dialogHeaderProperty = null;
    protected readonly ItemsList _dialogActionsList = new ItemsList();
    protected MediaMode _mediaMode = MediaMode.All;

    public enum MediaMode
    {
      All,
      Tv,
      Radio
    }

    public struct TvExtension
    {
      public string Caption;
      public IProgramAction Extension;
    }

    #endregion

    #region Constructor

    protected SlimTvModelBase()
      : this(5000)
    {
    }

    protected SlimTvModelBase(long updateInterval)
      : base(true, updateInterval)
    {
    }

    #endregion

    #region GUI properties and methods

    /// <summary>
    /// Gets the list of all available channel groups.
    /// </summary>
    public ItemsList ChannelGroupList
    {
      get { return _channelGroupList; }
    }

    /// <summary>
    /// Gets the currently selected channel group, or <c>null</c> if not initilalized.
    /// </summary>
    public IChannelGroup CurrentChannelGroup
    {
      get
      {
        var groups = GetGroupNavigationList();
        return groups.Current;
      }
    }

    /// <summary>
    /// Skips group index to next one.
    /// </summary>
    public void NextGroup()
    {
      var groups = GetGroupNavigationList();
      groups.MoveNext();
      SetGroup();
    }

    /// <summary>
    /// Skips group index to previous one.
    /// </summary>
    public void PrevGroup()
    {
      var groups = GetGroupNavigationList();
      groups.MovePrevious();
      SetGroup();
    }

    protected virtual void SetGroup()
    {
    }

    /// <summary>
    /// Opens the group selection dialog.
    /// </summary>
    public void SelectGroup()
    {
      SelectGroupByType();
    }

    private void SelectGroupByType()
    {
      var groups = GetGroupNavigationList();
      ChannelGroupList.Clear();
      for (int index = 0; index < groups.Count; index++)
      {
        var channelGroup = groups[index];
        if (channelGroup == null)
          continue;
        if (_mediaMode == MediaMode.Tv && channelGroup.MediaType != MediaType.TV)
          continue;
        if (_mediaMode == MediaMode.Radio && channelGroup.MediaType != MediaType.Radio)
          continue;

        int groupIndex = index;
        ListItem channel = new ListItem(Consts.KEY_NAME, channelGroup.Name)
        {
          Command = new MethodDelegateCommand(() => groups.SetIndex(groupIndex)),
          Selected = groupIndex == groups.CurrentIndex
        };
        ChannelGroupList.Add(channel);
      }
      ChannelGroupList.FireChange();
      if (_mediaMode == MediaMode.Radio)
        ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseGroupRadio");
      else
        ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseGroup");
    }

    /// <summary>
    /// Gets the currently selected channel, or <c>null</c> if not initilalized.
    /// </summary>
    public IChannel CurrentChannel
    {
      get
      {
        var channels = GetChannelNavigationList();
        return channels.Current;
      }
    }

    /// <summary>
    /// Skips group index to next one.
    /// </summary>
    public void NextChannel()
    {
      var channels = GetChannelNavigationList();
      channels.MoveNext();
      SetChannel();
    }

    /// <summary>
    /// Skips group index to previous one.
    /// </summary>
    public void PrevChannel()
    {
      var channels = GetChannelNavigationList();
      channels.MovePrevious();
      SetChannel();
    }

    /// <summary>
    /// Sets the current channel based on <see cref="ChannelContext"/>
    /// </summary>
    protected virtual void SetChannel()
    {
    }

    public static async Task TuneChannel(IChannel channel)
    {
      var modelId = channel.MediaType == MediaType.Radio ? SlimTvRadioClientModel.MODEL_ID : SlimTvClientModel.MODEL_ID;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (workflowManager.GetModel(modelId) is SlimTvClientModelBase model)
      {
        await model.Tune(channel);
        // Always switch to fullscreen
        workflowManager.NavigatePush(Consts.WF_STATE_ID_FULLSCREEN_VIDEO);
      }
    }

    /// <summary>
    /// Exposes the list of available series recording types or other user choices.
    /// </summary>
    public ItemsList DialogActionsList
    {
      get { return _dialogActionsList; }
    }

    /// <summary>
    /// Exposes the user dialog header.
    /// </summary>
    public string DialogHeader
    {
      get { return (string)_dialogHeaderProperty.GetValue(); }
      set { _dialogHeaderProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the user dialog header.
    /// </summary>
    public AbstractProperty DialogHeaderProperty
    {
      get { return _dialogHeaderProperty; }
    }

    public void ExecProgramAction(ListItem item)
    {
      if (item == null)
        return;
      if (item.Command != null)
        item.Command.Execute();
    }

    #endregion

    #region Members

    #region Inits and Updates

    protected virtual void InitModel()
    {
      if (_tvHandler == null)
      {
        ITvHandler tvHandler = ServiceRegistration.Get<ITvHandler>();
        tvHandler.Initialize();
        if (tvHandler.ChannelAndGroupInfo == null)
          return;
        _tvHandler = tvHandler;
      }

      _dialogHeaderProperty = new WProperty(typeof(string), string.Empty);
    }

    protected void FillChannelGroupList()
    {
      var groups = GetGroupNavigationList();
      _channelGroupList.Clear();
      for (int idx = 0; idx < groups.Count; idx++)
      {
        IChannelGroup group = groups[idx];
        ListItem channelGroupItem = new ListItem(Consts.KEY_NAME, group.Name)
        {
          Command = new MethodDelegateCommand(() =>
          {
            if (groups.MoveTo(g => g == group))
            {
              //SetGroup();
            }
          }),
          Selected = group == groups.Current
        };
        _channelGroupList.Add(channelGroupItem);
      }
      _channelGroupList.FireChange();
    }

    protected void GetCurrentChannelGroup()
    {
      var groups = GetGroupNavigationList();
      var selectedChannelGroupId = GetSelectedChannelGroupId();
      if (_tvHandler.ChannelAndGroupInfo != null && selectedChannelGroupId != 0)
        groups.MoveTo(group => group.ChannelGroupId == selectedChannelGroupId);
    }

    protected void GetCurrentChannel()
    {
      var channels = GetChannelNavigationList();
      var selectedChannelId = GetSelectedChannelId();
      if (_tvHandler.ChannelAndGroupInfo != null && selectedChannelId != 0)
        channels.MoveTo(channel => channel.ChannelId == selectedChannelId);
    }

    protected void SetCurrentChannelGroup()
    {
      if (_tvHandler.ChannelAndGroupInfo != null && CurrentChannelGroup != null)
      {
        if (_mediaMode == MediaMode.Radio)
          _tvHandler.ChannelAndGroupInfo.SelectedRadioChannelGroupId = CurrentChannelGroup.ChannelGroupId;
        else
          _tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId = CurrentChannelGroup.ChannelGroupId;
      }
    }

    protected void SetCurrentChannel()
    {
      if (_tvHandler.ChannelAndGroupInfo != null && CurrentChannel != null)
      {
        if (_mediaMode == MediaMode.Radio)
          _tvHandler.ChannelAndGroupInfo.SelectedRadioChannelId = CurrentChannel.ChannelId;
        else
          _tvHandler.ChannelAndGroupInfo.SelectedChannelId = CurrentChannel.ChannelId;
      }
    }

    #endregion

    #region Recording related

    /// <summary>
    /// Add series recording options for program
    /// </summary>
    protected void AddRecordingOptions(ItemsList items, IProgram program, RecordingStatus status)
    {
      bool isRecording = status.HasFlag(RecordingStatus.Recording);
      if (program != null)
      {
        bool isRunning = DateTime.Now >= program.StartTime && DateTime.Now <= program.EndTime;
        ILocalization localization = ServiceRegistration.Get<ILocalization>();
        if (status.HasFlag(RecordingStatus.SeriesScheduled))
        {
          if(isRecording || program.EndTime > DateTime.Now)
            items.Add(new ListItem(Consts.KEY_NAME, localization.ToString(isRecording ? "[SlimTvClient.StopCurrentRecording]"
              : "[SlimTvClient.DeleteSingle]", program.Title))
            {
              Command = new AsyncMethodDelegateCommand(() => CreateOrDeleteSchedule(program, ScheduleRecordingType.Once))
            });
          items.Add(new ListItem(Consts.KEY_NAME, "[SlimTvClient.DeleteFullSchedule]")
          {
            Command = new AsyncMethodDelegateCommand(() => CreateOrDeleteSchedule(program, ScheduleRecordingType.EveryTimeOnEveryChannel))
          });
        }
        else
        {
          string prompt = null;
          if (isRecording)
            prompt = "[SlimTvClient.StopCurrentRecording]";
          else if (isRunning)
            prompt = "[SlimTvClient.RecordCurrentProgram]";
          else if (status.HasFlag(RecordingStatus.Scheduled))
            prompt = "[SlimTvClient.DeleteSchedule]";
          else if (program.EndTime > DateTime.Now)
            prompt = "[SlimTvClient.CreateSchedule]";
          if(prompt != null)
            items.Add(new ListItem(Consts.KEY_NAME, localization.ToString(prompt, program.Title))
            {
              Command = new AsyncMethodDelegateCommand(() => CreateOrDeleteSchedule(program))
            });
          items.Add(
            new ListItem(Consts.KEY_NAME, "[SlimTvClient.RecordSeries]")
            {
              Command = new MethodDelegateCommand(() => SlimTvExtScheduleModel.RecordSeries(program))
            });
        }
      }

      if (_programExtensions == null)
        BuildExtensions();

      ILocalization loc = ServiceRegistration.Get<ILocalization>();
      foreach (KeyValuePair<Guid, TvExtension> programExtension in _programExtensions)
      {
        TvExtension extension = programExtension.Value;
        // First check if this extension applies for the selected program
        if (!extension.Extension.IsAvailable(program))
          continue;

        items.Add(
            new ListItem(Consts.KEY_NAME, loc.ToString(extension.Caption))
            {
              Command = new MethodDelegateCommand(() => extension.Extension.ProgramAction(program))
            });
      }
    }

    protected void BuildExtensions()
    {
      _programExtensions = new Dictionary<Guid, TvExtension>();
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
        } catch (PluginInvalidStateException e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Cannot add SlimTv extension with id '{0}'", e, itemMetadata.Id);
        }
      }
    }

    protected virtual async Task<RecordingStatus?> CreateOrDeleteSchedule(IProgram program, ScheduleRecordingType recordingType = ScheduleRecordingType.Once)
    {
      IScheduleControlAsync scheduleControl = _tvHandler.ScheduleControl;
      RecordingStatus? newStatus = null;
      if (scheduleControl != null)
      {
        RecordingStatus? recordingStatus = await GetRecordingStatusAsync(program);
        if (recordingStatus.HasValue && (recordingStatus.Value.HasFlag(RecordingStatus.Scheduled) || recordingStatus.Value.HasFlag(RecordingStatus.SeriesScheduled)))
        {
          if (await scheduleControl.RemoveScheduleForProgramAsync(program, recordingType))
            newStatus = RecordingStatus.None;
        }
        else
        {
          var result = await scheduleControl.CreateScheduleAsync(program, recordingType);
          if (result.Success)
            newStatus = recordingType == ScheduleRecordingType.Once ? RecordingStatus.Scheduled : RecordingStatus.SeriesScheduled;
        }
        if (newStatus != null)
        {
          UpdateRecordingStatus(program, (RecordingStatus)newStatus, recordingType);
          SlimTvClientMessaging.SendSlimTvProgramChangedMessage(program);
        }
      }
      return newStatus;
    }

    protected virtual async Task<RecordingStatus?> CreateSchedule(IProgram program, ScheduleRecordingType recordingType = ScheduleRecordingType.Once)
    {
      IScheduleControlAsync scheduleControl = _tvHandler.ScheduleControl;
      RecordingStatus? newStatus = null;
      if (scheduleControl != null)
      {
        RecordingStatus? recordingStatus = await GetRecordingStatusAsync(program);
        if (recordingStatus.HasValue && (recordingStatus.Value.HasFlag(RecordingStatus.Scheduled) || recordingStatus.Value.HasFlag(RecordingStatus.SeriesScheduled)))
        {
          // Delete any existing schedule
          if (await scheduleControl.RemoveScheduleForProgramAsync(program, recordingStatus.Value.HasFlag(RecordingStatus.SeriesScheduled) ? ScheduleRecordingType.EveryTimeOnEveryChannel : ScheduleRecordingType.Once))
            newStatus = RecordingStatus.None;
        }
        // Now create new schedule
        var result = await scheduleControl.CreateScheduleAsync(program, recordingType);
        if (result.Success)
        { 
          newStatus = recordingType == ScheduleRecordingType.Once ? RecordingStatus.Scheduled : RecordingStatus.SeriesScheduled;
          UpdateRecordingStatus(program, (RecordingStatus)newStatus, recordingType);
          SlimTvClientMessaging.SendSlimTvProgramChangedMessage(program);
        }
      }
      return newStatus;
    }

    protected virtual bool UpdateRecordingStatus(IProgram program, RecordingStatus status, ScheduleRecordingType type)
    {
      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;
      if (recordingStatus == null || recordingStatus.RecordingStatus == status)
        return false;
      RecordingStatus oldStatus = recordingStatus.RecordingStatus;
      recordingStatus.RecordingStatus = (RecordingStatus)status;
      OnRecordingStatusChanged(program, oldStatus, status, type);
      return true;
    }

    protected virtual void OnRecordingStatusChanged(IProgram program, RecordingStatus oldStatus, RecordingStatus newStatus, ScheduleRecordingType type)
    {
    }

    protected virtual async Task<RecordingStatus?> GetRecordingStatusAsync(IProgram program)
    {
      IScheduleControlAsync scheduleControl = _tvHandler.ScheduleControl;
      if (scheduleControl == null)
        return null;

      var result = await scheduleControl.GetRecordingStatusAsync(program);
      if (result.Success)
        return result.Result;
      return null;
    }

    #endregion

    #region Channel, groups and programs

    //protected abstract void UpdateCurrentChannel();

    //protected virtual void UpdateChannels()
    //{
    //  IChannelGroup group = CurrentChannelGroup;
    //  if (group != null)
    //  {
    //    //IList<IChannel> channels;
    //    //if (_tvHandler.ChannelAndGroupInfo.GetChannels(group, out channels))
    //    //{
    //    //  ChannelContext.Instance.Channels.Clear();
    //    //  ChannelContext.Instance.Channels.AddRange(channels);
    //    //}

    //    //// Now current channel group / channel is only set for tuning
    //    //// SetCurrentChannelGroup();
    //    //UpdateCurrentChannel();
    //    UpdatePrograms();
    //  }
    //}

    protected NavigationList<IChannel> GetChannelNavigationList()
    {
      if (_mediaMode == MediaMode.Tv)
        return ChannelContext.Instance.TvChannels;
      else if (_mediaMode == MediaMode.Radio)
        return ChannelContext.Instance.RadioChannels;

      return ChannelContext.Instance.Channels;
    }

    protected NavigationList<IChannelGroup> GetGroupNavigationList()
    {
      if (_mediaMode == MediaMode.Tv)
        return ChannelContext.Instance.TvChannelGroups;
      else if (_mediaMode == MediaMode.Radio)
        return ChannelContext.Instance.RadioChannelGroups;

      return ChannelContext.Instance.ChannelGroups;
    }

    protected int GetSelectedChannelGroupId()
    {
      if (_mediaMode == MediaMode.Radio)
        return _tvHandler.ChannelAndGroupInfo.SelectedRadioChannelGroupId;

      return _tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId;
    }

    protected int GetSelectedChannelId()
    {
      if (_mediaMode == MediaMode.Radio)
        return _tvHandler.ChannelAndGroupInfo.SelectedRadioChannelId;

      return _tvHandler.ChannelAndGroupInfo.SelectedChannelId;
    }

    #endregion

    #endregion

    #region IWorkflowModel implementation

    public abstract Guid ModelId { get; }

    public virtual bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
      return _tvHandler != null;
    }

    public virtual void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Attach();
    }

    public virtual void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      Detach();
    }

    public virtual void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
    }

    public virtual void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      Detach();
    }

    public virtual void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
      Attach();
    }

    private void Attach()
    {
      var groups = GetGroupNavigationList();
      var channels = GetChannelNavigationList();
      groups.OnListChanged += OnChannelGroupsChanged;
      groups.OnCurrentChanged += OnCurrentGroupChanged;
      channels.OnListChanged += OnChannelsChanged;
      channels.OnCurrentChanged += OnCurrentChannelChanged;
    }

    private void Detach()
    {
      var groups = GetGroupNavigationList();
      var channels = GetChannelNavigationList();
      groups.OnListChanged -= OnChannelGroupsChanged;
      groups.OnCurrentChanged -= OnCurrentGroupChanged;
      channels.OnListChanged -= OnChannelsChanged;
      channels.OnCurrentChanged -= OnCurrentChannelChanged;
    }

    protected virtual void OnChannelGroupsChanged(object sender, EventArgs e)
    {
      FillChannelGroupList();
    }

    protected virtual void OnChannelsChanged(object sender, EventArgs e)
    {
    }

    protected virtual void OnCurrentGroupChanged(int oldindex, int newindex)
    {
    }

    protected virtual void OnCurrentChannelChanged(int oldindex, int newindex)
    {
    }

    public virtual void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
