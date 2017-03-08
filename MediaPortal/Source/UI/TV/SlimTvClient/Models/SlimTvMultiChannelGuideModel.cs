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
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Messaging;
using MediaPortal.Plugins.SlimTv.Client.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// Model which holds the GUI state for the GUI test state.
  /// </summary>
  public class SlimTvMultiChannelGuideModel : SlimTvGuideModelBase
  {
    public const string MODEL_ID_STR = "5054408D-C2A9-451f-A702-E84AFCD29C10";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);
    
    protected double _bufferHours = 1.5;
    protected double _programWidthFactor = 6;
    protected double _programsStartOffset = 370;

    #region Constructor

    public SlimTvMultiChannelGuideModel()
    {
      _programActionsDialogName = "DialogProgramActionsFull"; // for MultiChannelGuide we need another dialog
    }

    #endregion

    #region Protected fields

    protected AbstractProperty _guideStartTimeProperty = null;
    protected AbstractProperty _visibleHoursProperty = null;
    protected AbstractProperty _channelNameProperty = null;

    protected DateTime _bufferStartTime;
    protected DateTime _bufferEndTime;
    protected int _bufferGroupIndex;

    public DateTime GuideEndTime
    {
      get { return GuideStartTime.AddHours(VisibleHours); }
    }

    #endregion

    #region Variables

    private readonly ItemsList _channelList = new ItemsList();
    private IList<IProgram> _groupPrograms;

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
    public ItemsList ChannelList
    {
      get { return _channelList; }
    }

    public DateTime GuideStartTime
    {
      get { return (DateTime)_guideStartTimeProperty.GetValue(); }
      set { _guideStartTimeProperty.SetValue(value); }
    }

    public AbstractProperty GuideStartTimeProperty
    {
      get { return _guideStartTimeProperty; }
    }

    public double VisibleHours
    {
      get { return (double)_visibleHoursProperty.GetValue(); }
      set { _visibleHoursProperty.SetValue(value); }
    }

    public AbstractProperty VisibleHoursProperty
    {
      get { return _visibleHoursProperty; }
    }

    public void ScrollForward()
    {
      Scroll(TimeSpan.FromDays(1));
    }

    public void ScrollBackward()
    {
      Scroll(TimeSpan.FromDays(-1));
    }

    public void Scroll(TimeSpan difference)
    {
      GuideStartTime = GuideStartTime + difference;
      UpdatePrograms();
    }

    #endregion

    #region Members

    #region Inits and Updates

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        DateTime startDate = DateTime.Now.RoundDateTime(15, DateFormatExtension.RoundingDirection.Down);
        _guideStartTimeProperty = new WProperty(typeof(DateTime), startDate);
        // User defined layout settings.
        var settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
        _visibleHoursProperty = new WProperty(typeof(double), settings.EpgVisibleHours);
        _channelNameProperty = new WProperty(typeof(string), string.Empty);
      }
      base.InitModel();
    }

    #endregion

    #region Channel, groups and programs

    protected void UpdateChannels()
    {
      UpdateGuiProperties();
      BackgroundUpdateChannels();
    }

    private void BackgroundUpdateChannels()
    {
      //base.UpdateChannels();
      _channelList.Clear();
      foreach (IChannel channel in ChannelContext.Instance.Channels)
      {
        IChannel localChannel = channel;
        var channelProgramsItem = new ChannelProgramListItem(channel, new ItemsList())
          {
            Command = new MethodDelegateCommand(() => ShowSingleChannelGuide(localChannel))
          };
        UpdateChannelPrograms(channelProgramsItem);
        _channelList.Add(channelProgramsItem);
      }
      _channelList.FireChange();
    }

    private void ShowSingleChannelGuide(IChannel channel)
    {
      if (channel == null || CurrentChannelGroup == null)
        return;

      int channelId = channel.ChannelId;
      int groupId = CurrentChannelGroup.ChannelGroupId;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      NavigationContextConfig navigationContextConfig = new NavigationContextConfig();
      navigationContextConfig.AdditionalContextVariables = new Dictionary<string, object>();
      navigationContextConfig.AdditionalContextVariables[SlimTvClientModel.KEY_CHANNEL_ID] = channelId;
      navigationContextConfig.AdditionalContextVariables[SlimTvClientModel.KEY_GROUP_ID] = groupId;
      Guid stateId = new Guid("A40F05BB-022E-4247-8BEE-16EB3E0B39C5");
      if (workflowManager.IsAnyStateContainedInNavigationStack(new Guid[] { stateId }))
        workflowManager.NavigatePopToState(stateId, false);
      else
        workflowManager.NavigatePush(stateId, navigationContextConfig);
    }

    private ProgramListItem BuildProgramListItem(IProgram program, IChannel channel)
    {
      ProgramProperties programProperties = new ProgramProperties();
      IProgram currentProgram = program;
      programProperties.SetProgram(currentProgram, channel);

      ProgramListItem item = new ProgramListItem(programProperties)
        {
          Command = new MethodDelegateCommand(() => ShowProgramActions(currentProgram))
        };
      item.AdditionalProperties["PROGRAM"] = currentProgram;
      return item;
    }

    private PlaceholderListItem NoProgramPlaceholder(IChannel channel, DateTime? startTime, DateTime? endTime)
    {
      ILocalization loc = ServiceRegistration.Get<ILocalization>();
      DateTime today = GuideStartTime.GetDay();
      ProgramProperties programProperties = new ProgramProperties();
      Program placeholderProgram = new Program
                              {
                                ProgramId = -1,
                                ChannelId = channel.ChannelId,
                                Title = loc.ToString("[SlimTvClient.NoProgram]"),
                                StartTime = startTime ?? today,
                                EndTime = endTime ?? today.AddDays(1)
                              };
      programProperties.SetProgram(placeholderProgram, channel);

      var item = new PlaceholderListItem(programProperties)
      {
        Command = new MethodDelegateCommand(() => ShowProgramActions(placeholderProgram))
      };
      item.AdditionalProperties["PROGRAM"] = placeholderProgram;

      return item;
    }

    protected override void Update()
    {
      if (!_isInitialized)
        return;
      UpdateProgramsState();
    }

    protected override void UpdateProgramStatus(IProgram program)
    {
      base.UpdateProgramStatus(program);
      if (program == null || _tvHandler == null || _tvHandler.ChannelAndGroupInfo == null)
        return;
      IChannel currentChannel;
      if (_tvHandler.ChannelAndGroupInfo.GetChannel(program.ChannelId, out currentChannel))
        ChannelName = currentChannel.Name;
    }

    protected void UpdatePrograms()
    {
      UpdateProgramsForGroup();
      foreach (ChannelProgramListItem channel in _channelList)
        UpdateChannelPrograms(channel);

      _channelList.FireChange();
      SlimTvClientMessaging.SendSlimTvClientMessage(SlimTvClientMessaging.MessageType.ProgramsChanged);
      UpdateProgramsState();
    }

    protected void UpdateProgramsForGroup()
    {
      if (
        _bufferGroupIndex != ChannelContext.Instance.ChannelGroups.CurrentIndex || /* Group changed */
        _bufferStartTime == DateTime.MinValue || _bufferEndTime == DateTime.MinValue || /* Buffer not set */
        GuideStartTime < _bufferStartTime || GuideStartTime > _bufferEndTime || /* Cache is out of request range */
        GuideEndTime < _bufferStartTime || GuideEndTime > _bufferEndTime
        )
      {
        _bufferGroupIndex = ChannelContext.Instance.ChannelGroups.CurrentIndex;
        _bufferStartTime = GuideStartTime.AddHours(-_bufferHours);
        _bufferEndTime = GuideEndTime.AddHours(_bufferHours);
        IChannelGroup group = CurrentChannelGroup;
        if (group != null)
          _tvHandler.ProgramInfo.GetProgramsGroup(group, _bufferStartTime, _bufferEndTime, out _groupPrograms);
      }
    }

    protected override bool UpdateRecordingStatus(IProgram program, RecordingStatus newStatus)
    {
      bool changed = base.UpdateRecordingStatus(program, newStatus);
      return changed && UpdateRecordingStatus(program);
    }

    protected override bool UpdateRecordingStatus(IProgram program)
    {
      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;
      if (recordingStatus == null)
        return false;

      ChannelProgramListItem programChannel = _channelList.OfType<ChannelProgramListItem>().FirstOrDefault(c => c.Channel.ChannelId == program.ChannelId);
      if (programChannel == null)
        return false;

      ProgramListItem listProgram;
      lock (programChannel.Programs.SyncRoot)
      {
        listProgram = programChannel.Programs.OfType<ProgramListItem>().FirstOrDefault(p => p.Program.ProgramId == program.ProgramId);
        if (listProgram == null)
          return false;
      }
      listProgram.Program.UpdateState(recordingStatus.RecordingStatus);
      return true;
    }

    private void UpdateProgramsState()
    {
      lock (_channelList.SyncRoot)
        foreach (ChannelProgramListItem channel in _channelList)
          UpdateChannelProgramsState(channel);
    }

    /// <summary>
    /// Sets the "IsRunning" state of all programs.
    /// </summary>
    /// <param name="channel"></param>
    private static void UpdateChannelProgramsState(ChannelProgramListItem channel)
    {
      lock (channel.Programs.SyncRoot)
        foreach (ProgramListItem program in channel.Programs)
          program.Update();
    }

    private void UpdateChannelPrograms(ChannelProgramListItem channel)
    {
      lock (channel.Programs.SyncRoot)
      {
        channel.Programs.Clear();
        if (_groupPrograms != null)
          _groupPrograms.Where(p => p.ChannelId == channel.Channel.ChannelId && p.StartTime < GuideEndTime).ToList()
            .ForEach(p => channel.Programs.Add(BuildProgramListItem(p, channel.Channel)));
        FillNoPrograms(channel, GuideStartTime, GuideEndTime);
      }
      // Don't notify about every channel programs list changes, only for channel list
      // channel.Programs.FireChange();
    }

    private void FillNoPrograms(ChannelProgramListItem channel, DateTime viewPortStart, DateTime viewPortEnd)
    {
      var programs = channel.Programs;
      if (programs.Count == 0)
      {
        programs.Add(NoProgramPlaceholder(channel.Channel, null, null));
        return;
      }
      ProgramListItem firstItem = programs.Cast<ProgramListItem>().First();
      ProgramListItem lastItem = programs.Cast<ProgramListItem>().Last();
      if (firstItem.Program.StartTime > viewPortStart)
        programs.Insert(0, NoProgramPlaceholder(channel.Channel, null, firstItem.Program.StartTime));

      if (lastItem.Program.EndTime < viewPortEnd)
        programs.Add(NoProgramPlaceholder(channel.Channel, lastItem.Program.EndTime, null));
    }

    #endregion

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return MODEL_ID; }
    }

    protected override void OnCurrentGroupChanged(int oldindex, int newindex)
    {
      base.OnCurrentGroupChanged(oldindex, newindex);
      UpdateChannels();
      UpdatePrograms();
      // Notify listeners about group change
      SlimTvClientMessaging.SendSlimTvClientMessage(SlimTvClientMessaging.MessageType.GroupChanged);
    }

    /// <summary>
    /// Sets the guide time viewport to current time.
    /// </summary>
    protected void SetCurrentViewTime()
    {
      GuideStartTime = DateTime.Now.RoundDateTime(15, DateFormatExtension.RoundingDirection.Down);
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      VisibleHours = settings.EpgVisibleHours;
      _bufferStartTime = _bufferEndTime = DateTime.MinValue;
    }

    public override void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      base.Reactivate(oldContext, newContext);
      // Check if the time viewport is left already, then set it to current time.
      bool timeChanged = false;
      if (DateTime.Now >= GuideEndTime)
      {
        SetCurrentViewTime();
        timeChanged = true;
      }

      // Only recreate content if group was changed in mean time
      if (timeChanged || _bufferGroupIndex != ChannelContext.Instance.ChannelGroups.CurrentIndex)
      {
        UpdateChannels();
        UpdatePrograms();
      }
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      // Init viewport to start with current time.
      SetCurrentViewTime();
      UpdateChannels();
      UpdatePrograms();
    }

    #endregion
  }
}
