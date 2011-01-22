#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Plugins.SlimTvClient.Helpers;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.Plugins.SlimTvClient.Interfaces.LiveTvMediaItem;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Plugins.SlimTvClient.Interfaces;

namespace MediaPortal.Plugins.SlimTvClient
{
  /// <summary>
  /// Model which holds the GUI state for the GUI test state.
  /// </summary>
  public class SlimTvClientModel : BaseTimerControlledModel, IWorkflowModel
  {
    public const string MODEL_ID_STR = "8BEC1372-1C76-484c-8A69-C7F3103708EC";

    #region Protected fields

    protected AbstractProperty _currentGroupNameProperty = null;

    // properties for channel browsing and program preview
    protected AbstractProperty _selectedCurrentProgramProperty = null;
    protected AbstractProperty _selectedNextProgramProperty = null;


    // properties for playing channel and program (OSD)
    protected AbstractProperty _currentProgramProperty = null;
    protected AbstractProperty _nextProgramProperty = null;
    protected AbstractProperty _programProgressProperty = null;
    protected AbstractProperty _timeshiftProgressProperty = null;
    protected AbstractProperty _currentChannelNameProperty = null;

    // PiP Control properties
    protected AbstractProperty _piPAvailableProperty = null;
    protected AbstractProperty _piPEnabledProperty = null;

    #endregion

    #region Variables

    private ITvHandler _tvHandler;
    private IList<IChannelGroup> _channelGroups;
    private int _channelGroupIndex;

    private readonly ItemsList _channelList = new ItemsList();

    private IList<IChannel> _channels;
    private bool _active;
    private bool _isInitialized;

    #endregion

    public SlimTvClientModel()
      : base(500)
    {
    }

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current group name to the skin.
    /// </summary>
    public string CurrentGroupName
    {
      get { return (string)_currentGroupNameProperty.GetValue(); }
      set { _currentGroupNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current group name to the skin.
    /// </summary>
    public AbstractProperty CurrentGroupNameProperty
    {
      get { return _currentGroupNameProperty; }
    }

    /// <summary>
    /// Exposes the current channel name to the skin.
    /// </summary>
    public string ChannelName
    {
      get { return (string)_currentChannelNameProperty.GetValue(); }
      set { _currentChannelNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current channel name to the skin.
    /// </summary>
    public AbstractProperty ChannelNameProperty
    {
      get { return _currentChannelNameProperty; }
    }

    /// <summary>
    /// Exposes the list of channels in current group.
    /// </summary>
    public ItemsList CurrentGroupChannels
    {
      get { return _channelList; }
    }

    /// <summary>
    /// Exposes the current program to the skin.
    /// </summary>
    public string SelectedCurrentProgram
    {
      get { return (string)_selectedCurrentProgramProperty.GetValue(); }
      set { _selectedCurrentProgramProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current program to the skin.
    /// </summary>
    public AbstractProperty SelectedCurrentProgramProperty
    {
      get { return _selectedCurrentProgramProperty; }
    }


    /// <summary>
    /// Exposes the next program to the skin.
    /// </summary>
    public string SelectedNextProgram
    {
      get { return (string)_selectedNextProgramProperty.GetValue(); }
      set { _selectedNextProgramProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the next program to the skin.
    /// </summary>
    public AbstractProperty SelectedNextProgramProperty
    {
      get { return _selectedNextProgramProperty; }
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
    /// Gets a value (range 0 to 100) which denotes the current fraction of played content.
    /// </summary>
    public double ProgramProgress
    {
      get { return (double)_programProgressProperty.GetValue(); }
      internal set { _programProgressProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets a value (range 0 to 100) which denotes the current fraction of played content.
    /// </summary>
    public AbstractProperty ProgramProgressProperty
    {
      get { return _programProgressProperty; }
    }

    /// <summary>
    /// Gets a value (range 0 to 100) which denotes the current fraction of played content.
    /// </summary>
    public double TimeshiftProgress
    {
      get { return (double)_timeshiftProgressProperty.GetValue(); }
      internal set { _timeshiftProgressProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets a value (range 0 to 100) which denotes the current fraction of played content.
    /// </summary>
    public AbstractProperty TimeshiftProgressProperty
    {
      get { return _timeshiftProgressProperty; }
    }
    
    /// <summary>
    /// Exposes the next program to the skin.
    /// </summary>
    public ProgramProperties NextProgram
    {
      get { return (ProgramProperties)_nextProgramProperty.GetValue(); }
      set { _nextProgramProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the next program to the skin.
    /// </summary>
    public AbstractProperty NextProgramProperty
    {
      get { return _nextProgramProperty; }
    }

    public bool PiPAvailable
    {
      get { return (bool) _piPAvailableProperty.GetValue(); }
      set { _piPAvailableProperty.SetValue(value); }
    }

    public AbstractProperty PiPAvailableProperty
    {
      get { return _piPAvailableProperty; }
    }

    public bool PiPEnabled
    {
      get { return (bool)_piPEnabledProperty.GetValue(); }
      set { _piPEnabledProperty.SetValue(value); }
    }

    public AbstractProperty PiPEnabledProperty
    {
      get { return _piPEnabledProperty; }
    }


    /// <summary>
    /// Skips group index to next one.
    /// </summary>
    public void NextGroup()
    {
      if (_channelGroups == null)
        return;

      _channelGroupIndex++;
      if (_channelGroupIndex >= _channelGroups.Count)
        _channelGroupIndex = 0;

      UpdateGroupAndChannels();
    }

    /// <summary>
    /// Skips group index to previous one.
    /// </summary>
    public void PrevGroup()
    {
      if (_channelGroups == null)
        return;

      _channelGroupIndex--;
      if (_channelGroupIndex < 0)
        _channelGroupIndex = _channelGroups.Count - 1;

      UpdateGroupAndChannels();
    }

    public void UpdateProgram(ListItem selectedItem)
    {
      if (selectedItem != null)
      {
        IChannel channel = (IChannel) selectedItem.AdditionalProperties["CHANNEL"];
        UpdateProgramForChannel(channel);
      }
    }

    public void TogglePiP()
    {
      if (!PiPAvailable)
      {
        PiPEnabled = false;
        return;
      }
      PiPEnabled = !PiPEnabled;
    }

    #endregion

    #region Members

    #region TV control methods

    protected LiveTvPlayer SlotPlayer
    {
      get
      {
        IPlayerManager pm = ServiceRegistration.Get<IPlayerManager>();
        if (pm == null)
          return null;
        LiveTvPlayer player = pm[SlotIndex] as LiveTvPlayer;
        return player;
      }
    }

    private void Tune(IChannel channel)
    {
      //if (SlotPlayer != null)
      //  SlotPlayer.Pause();

      if (_tvHandler.StartTimeshift(SlotIndex, channel))
      {
        SeekToEndAndPlay();
        Update();
      }
    }

    private void SeekToEndAndPlay()
    {
      if (SlotPlayer != null)
      {
        SlotPlayer.CurrentTime = SlotPlayer.Duration;
        //SlotPlayer.Resume();
      }
    }

    #endregion

    #region Inits and Updates

    private void InitModel()
    {
      if (!_isInitialized)
      {
        _currentGroupNameProperty = new WProperty(typeof(string), string.Empty);
        _selectedCurrentProgramProperty = new WProperty(typeof(string), string.Empty);
        _selectedNextProgramProperty = new WProperty(typeof(string), string.Empty);

        _currentChannelNameProperty = new WProperty(typeof(string), string.Empty);
        _currentProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());
        _nextProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());
        _programProgressProperty = new WProperty(typeof(double), 0d);
        _timeshiftProgressProperty = new WProperty(typeof(double), 0d);

        _piPAvailableProperty = new WProperty(typeof(bool), false);
        _piPEnabledProperty = new WProperty(typeof(bool), false);

        if (!ServiceRegistration.IsRegistered<ITvHandler>())
          ServiceRegistration.Set<ITvHandler>(new SlimTvHandler());

        _tvHandler = ServiceRegistration.Get<ITvHandler>();

        _tvHandler.ChannelAndGroupInfo.GetChannelGroups(out _channelGroups);

        _channelGroupIndex = 0;
        UpdateGroupAndChannels();
        _isInitialized = true;
      }
    }

    protected int SlotIndex
    {
      get
      {
        return PiPEnabled ? PlayerManagerConsts.SECONDARY_SLOT : PlayerManagerConsts.PRIMARY_SLOT;
      }
    }

    protected override void Update()
    {
      if (!_active)
        return;

      if (_tvHandler.NumberOfActiveSlots < 1)
      {
        PiPAvailable = false;
        PiPEnabled = false;
        return;
      }

      PiPAvailable = true;

      // get the current channel and program out of the LiveTvMediaItems' TimeshiftContexes
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext playerContext = playerContextManager.GetPlayerContext(PlayerChoice.PrimaryPlayer);
      if (playerContext != null)
      {
        LiveTvMediaItem liveTvMediaItem = playerContext.CurrentMediaItem as LiveTvMediaItem;
        LiveTvPlayer player = playerContext.CurrentPlayer as LiveTvPlayer;
        if (liveTvMediaItem != null && player != null)
        {
          ITimeshiftContext context = player.CurrentTimeshiftContext;
          if (context != null)
          {
            ChannelName = context.Channel.Name;
            CurrentProgram.SetProgram(context.Program);
            if (context.Program != null)
            {
              IProgram currentProgram = context.Program;
              double progress = (DateTime.Now - currentProgram.StartTime).TotalSeconds/
                                (currentProgram.EndTime - currentProgram.StartTime).TotalSeconds*100;
              _programProgressProperty.SetValue(progress);
            }
          }
        }
      }
    }

    #endregion

    #region Channel, groups and programs

    private void UpdateGroupAndChannels()
    {
      if (_channelGroupIndex < _channelGroups.Count)
      {
        IChannelGroup currentGroup = _channelGroups[_channelGroupIndex];
        CurrentGroupName = currentGroup.Name;

        _tvHandler.ChannelAndGroupInfo.GetChannels(currentGroup, out _channels);

        _channelList.Clear();
        foreach (IChannel channel in _channels)
        {
          // Use local variable, otherwise delegate argument is not fixed
          IChannel currentChannel = channel;

          ChannelProgramListItem item = new ChannelProgramListItem(currentChannel, GetNowAndNextProgramsList(currentChannel))
          {
            Command = new MethodDelegateCommand(() => Tune(currentChannel))
          };
          item.AdditionalProperties["CHANNEL"] = channel;

          _channelList.Add(item);
        }
        CurrentGroupChannels.FireChange();
      }
      else
      {
        CurrentGroupName = "No Connection";
        _channels = null;
        _channelList.Clear();
      }
    }

    private void UpdateProgramForChannel(IChannel channel)
    {
      IProgram program;
      if (_tvHandler.ProgramInfo.GetCurrentProgram(channel, out program))
        _selectedCurrentProgramProperty.SetValue(FormatProgram(program));

      if (_tvHandler.ProgramInfo.GetNextProgram(channel, out program))
        _selectedNextProgramProperty.SetValue(FormatProgram(program));
    }

    protected ItemsList GetNowAndNextProgramsList(IChannel channel)
    {
      ItemsList channelPrograms = new ItemsList();
      IProgram program;
      _tvHandler.ProgramInfo.GetCurrentProgram(channel, out program);
      CreateProgramListItem(program, channelPrograms);

      _tvHandler.ProgramInfo.GetNextProgram(channel, out program);
      CreateProgramListItem(program, channelPrograms);

      return channelPrograms;
    }

    private void CreateProgramListItem(IProgram program, ItemsList channelPrograms)
    {
      ProgramListItem item;
      if (program == null)
        item = NoProgramPlaceholder();
      else
      {
        ProgramProperties programProperties = new ProgramProperties();
        programProperties.SetProgram(program);
        item = new ProgramListItem(programProperties);
      }
      item.AdditionalProperties["PROGRAM"] = program;
      channelPrograms.Add(item);
    }

    private ProgramListItem NoProgramPlaceholder()
    {
      ILocalization loc = ServiceRegistration.Get<ILocalization>();
      DateTime today = FormatHelper.GetDay(DateTime.Now);

      ProgramProperties programProperties = new ProgramProperties(today, today.AddDays(1))
      {
        Title = loc.ToString("[SlimTvClient.NoProgram]"),
        StartTime = today,
        EndTime = today.AddDays(1)
      };
      return new ProgramListItem(programProperties);
    }

    private string FormatProgram(IProgram program)
    {
      if (program == null)
        return ServiceRegistration.Get<ILocalization>().ToString("[SlimTvClient.NoProgram]");

      return String.Format("{0} - {1} : {2}", 
                           program.StartTime.ToString("t"),
                           program.EndTime.ToString("t"),
                           program.Title);
    }

    #endregion

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      InitModel();
      _active = true;

      // We could initialize some data here when entering the media navigation state
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _active = false;
      // We could dispose some data here when exiting media navigation context
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // We could initialize some data here when changing the media navigation state
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion

    #region IDisposable Member

    public override void Dispose()
    {
      if (_tvHandler != null)
        _tvHandler.Dispose();
      
      base.Dispose();
    }

    #endregion
  }
}