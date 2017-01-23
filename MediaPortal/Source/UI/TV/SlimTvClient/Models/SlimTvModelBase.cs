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
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
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

    protected IList<IProgram> _programs;
    protected bool _isInitialized;

    protected AbstractProperty _dialogHeaderProperty = null;
    protected readonly ItemsList _dialogActionsList = new ItemsList();

    #endregion

    #region Constructor

    protected SlimTvModelBase()
      : this(5000)
    { }

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
        return ChannelContext.Instance.ChannelGroups.Current;
      }
    }

    /// <summary>
    /// Skips group index to next one.
    /// </summary>
    public void NextGroup()
    {
      ChannelContext.Instance.ChannelGroups.MoveNext();
      SetGroup();
    }

    /// <summary>
    /// Skips group index to previous one.
    /// </summary>
    public void PrevGroup()
    {
      ChannelContext.Instance.ChannelGroups.MovePrevious();
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
      ChannelGroupList.Clear();
      for (int index = 0; index < ChannelContext.Instance.ChannelGroups.Count; index++)
      {
        var channelGroup = ChannelContext.Instance.ChannelGroups[index];
        int groupIndex = index;
        ListItem channel = new ListItem(Consts.KEY_NAME, channelGroup.Name)
        {
          Command = new MethodDelegateCommand(() => ChannelContext.Instance.ChannelGroups.SetIndex(groupIndex)),
          Selected = groupIndex == ChannelContext.Instance.ChannelGroups.CurrentIndex
        };
        ChannelGroupList.Add(channel);
      }
      ChannelGroupList.FireChange();
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseGroup");
    }

    /// <summary>
    /// Gets the currently selected channel, or <c>null</c> if not initilalized.
    /// </summary>
    public IChannel CurrentChannel
    {
      get
      {
        return ChannelContext.Instance.Channels.Current;
      }
    }

    /// <summary>
    /// Skips group index to next one.
    /// </summary>
    public void NextChannel()
    {
      ChannelContext.Instance.Channels.MoveNext();
      SetChannel();
    }

    /// <summary>
    /// Skips group index to previous one.
    /// </summary>
    public void PrevChannel()
    {
      ChannelContext.Instance.Channels.MovePrevious();
      SetChannel();
    }

    /// <summary>
    /// Sets the current channel based on <see cref="ChannelContext"/>
    /// </summary>
    protected virtual void SetChannel()
    {
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
      _channelGroupList.Clear();
      for (int idx = 0; idx < ChannelContext.Instance.ChannelGroups.Count; idx++)
      {
        IChannelGroup group = ChannelContext.Instance.ChannelGroups[idx];
        ListItem channelGroupItem = new ListItem(UiComponents.Media.General.Consts.KEY_NAME, group.Name)
        {
          Command = new MethodDelegateCommand(() =>
          {
            if (ChannelContext.Instance.ChannelGroups.MoveTo(g => g == group))
            {
              //SetGroup();
            }
          }),
          Selected = group == ChannelContext.Instance.ChannelGroups.Current
        };
        _channelGroupList.Add(channelGroupItem);
      }
      _channelGroupList.FireChange();
    }

    protected void GetCurrentChannelGroup()
    {
      if (_tvHandler.ChannelAndGroupInfo != null && _tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId != 0)
        ChannelContext.Instance.ChannelGroups.MoveTo(group => group.ChannelGroupId == _tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId);
    }

    protected void GetCurrentChannel()
    {
      if (_tvHandler.ChannelAndGroupInfo != null && _tvHandler.ChannelAndGroupInfo.SelectedChannelId != 0)
        ChannelContext.Instance.Channels.MoveTo(channel => channel.ChannelId == _tvHandler.ChannelAndGroupInfo.SelectedChannelId);
    }

    protected void SetCurrentChannelGroup()
    {
      if (_tvHandler.ChannelAndGroupInfo != null && CurrentChannelGroup != null)
        _tvHandler.ChannelAndGroupInfo.SelectedChannelGroupId = CurrentChannelGroup.ChannelGroupId;
    }

    protected void SetCurrentChannel()
    {
      if (_tvHandler.ChannelAndGroupInfo != null && CurrentChannel != null)
        _tvHandler.ChannelAndGroupInfo.SelectedChannelId = CurrentChannel.ChannelId;
    }

    #endregion

    #region Recording related

    protected virtual RecordingStatus? CreateOrDeleteSchedule(IProgram program, ScheduleRecordingType recordingType = ScheduleRecordingType.Once)
    {
      IScheduleControl scheduleControl = _tvHandler.ScheduleControl;
      RecordingStatus? newStatus = null;
      if (scheduleControl != null)
      {
        RecordingStatus? recordingStatus = GetRecordingStatus(program);
        if (!recordingStatus.HasValue)
          return null;
        if (recordingStatus.Value.HasFlag(RecordingStatus.Scheduled) || recordingStatus.Value.HasFlag(RecordingStatus.SeriesScheduled))
        {
          if (scheduleControl.RemoveScheduleForProgram(program, recordingType))
            newStatus = RecordingStatus.None;
        }
        else
        {
          ISchedule schedule;
          if (scheduleControl.CreateSchedule(program, recordingType, out schedule))
            newStatus = recordingType == ScheduleRecordingType.Once ? RecordingStatus.Scheduled : RecordingStatus.SeriesScheduled;
        }
      }
      return newStatus;
    }

    protected virtual RecordingStatus? GetRecordingStatus(IProgram program)
    {
      IScheduleControl scheduleControl = _tvHandler.ScheduleControl;
      if (scheduleControl == null)
        return null;

      RecordingStatus recordingStatus;
      if (scheduleControl.GetRecordingStatus(program, out recordingStatus))
        return recordingStatus;
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
      ChannelContext.Instance.ChannelGroups.OnListChanged += OnChannelGroupsChanged;
      ChannelContext.Instance.ChannelGroups.OnCurrentChanged += OnCurrentGroupChanged;
      ChannelContext.Instance.Channels.OnListChanged += OnChannelsChanged;
      ChannelContext.Instance.Channels.OnCurrentChanged += OnCurrentChannelChanged;
    }

    private void Detach()
    {
      ChannelContext.Instance.ChannelGroups.OnListChanged -= OnChannelGroupsChanged;
      ChannelContext.Instance.ChannelGroups.OnCurrentChanged -= OnCurrentGroupChanged;
      ChannelContext.Instance.Channels.OnListChanged -= OnChannelsChanged;
      ChannelContext.Instance.Channels.OnCurrentChanged -= OnCurrentChannelChanged;
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
