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
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Player;
using MediaPortal.Plugins.SlimTv.Client.Settings;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.Media.General;
using MediaPortal.UiComponents.SkinBase.Models;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.SkinEngine.MpfElements;
using MediaPortal.Utilities.Events;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// <see cref="SlimTvClientModel"/> is the main entry model for SlimTV. It provides channel group and channel selection and 
  /// acts as backing model for the Live-TV OSD to provide program information.
  /// </summary>
  public class SlimTvClientModel : SlimTvModelBase
  {
    public const string MODEL_ID_STR = "8BEC1372-1C76-484c-8A69-C7F3103708EC";
    public static readonly Guid MODEL_ID = new Guid(MODEL_ID_STR);

    public const string KEY_PROGRAM = "Program";
    public const string KEY_PROGRAM_ID = "ProgramId";
    public const string KEY_CHANNEL_ID = "ChannelId";
    public const string KEY_GROUP_ID = "GroupId";
    public const string KEY_SCHEDULE = "Schedule";
    public const string KEY_MODE = "Mode";

    #region Constants

    protected const int PROGRAM_UPDATE_SEC = 30; // Update frequency for current running programs

    #endregion

    #region Protected fields

    protected AbstractProperty _serverStateProperty = null;
    protected AbstractProperty _currentGroupNameProperty = null;

    // properties for channel browsing and program preview
    protected AbstractProperty _selectedCurrentProgramProperty = null;
    protected AbstractProperty _selectedNextProgramProperty = null;
    protected AbstractProperty _selectedChannelNameProperty = null;
    protected AbstractProperty _selectedProgramProgressProperty = null;

    // properties for playing channel and program (OSD)
    protected AbstractProperty _currentProgramProperty = null;
    protected AbstractProperty _nextProgramProperty = null;
    protected AbstractProperty _programProgressProperty = null;
    protected AbstractProperty _timeshiftProgressProperty = null;
    protected AbstractProperty _currentChannelNameProperty = null;

    // PiP Control properties
    protected AbstractProperty _piPAvailableProperty = null;
    protected AbstractProperty _piPEnabledProperty = null;

    // OSD Control properties
    private AbstractProperty _isOSDVisibleProperty = null;
    private AbstractProperty _isOSDLevel0Property = null;
    private AbstractProperty _isOSDLevel1Property = null;
    private AbstractProperty _isOSDLevel2Property = null;

    // Channel zapping
    protected DelayedEvent _zapTimer;
    protected int _zapChannelIndex;

    // Contains the channel that was tuned the last time. Used for selecting channels in group list.
    protected IChannel _lastTunedChannel;

    // Counter for updates
    protected int _updateCounter = 0;

    // Resume handling
    protected DelayedEvent _resumeEvent = new DelayedEvent(2000);
    protected bool _tvWasActive;

    #endregion

    #region Variables

    private readonly ItemsList _channelList = new ItemsList();
    private DateTime _lastChannelListUpdate = DateTime.MinValue;

    #endregion

    public SlimTvClientModel()
      : base(500)
    {
    }

    #region GUI properties and methods

    /// <summary>
    /// Exposes the current server state to the skin.
    /// </summary>
    public TvServerState ServerState
    {
      get { return (TvServerState)_serverStateProperty.GetValue(); }
      set { _serverStateProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the current server state to the skin.
    /// </summary>
    public AbstractProperty ServerStateProperty
    {
      get { return _serverStateProperty; }
    }

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
    /// Exposes the selected channel name to the skin.
    /// </summary>
    public string SelectedChannelName
    {
      get { return (string)_selectedChannelNameProperty.GetValue(); }
      set { _selectedChannelNameProperty.SetValue(value); }
    }

    /// <summary>
    /// Exposes the selected channel name to the skin.
    /// </summary>
    public AbstractProperty SelectedChannelNameProperty
    {
      get { return _selectedChannelNameProperty; }
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
    public ProgramProperties SelectedCurrentProgram
    {
      get { return (ProgramProperties)_selectedCurrentProgramProperty.GetValue(); }
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
    public ProgramProperties SelectedNextProgram
    {
      get { return (ProgramProperties)_selectedNextProgramProperty.GetValue(); }
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
    public double SelectedProgramProgress
    {
      get { return (double)_selectedProgramProgressProperty.GetValue(); }
      internal set { _selectedProgramProgressProperty.SetValue(value); }
    }

    /// <summary>
    /// Gets a value (range 0 to 100) which denotes the current fraction of played content.
    /// </summary>
    public AbstractProperty SelectedProgramProgressProperty
    {
      get { return _selectedProgramProgressProperty; }
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
      get { return (bool)_piPAvailableProperty.GetValue(); }
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

    public bool IsOSDVisible
    {
      get { return (bool)_isOSDVisibleProperty.GetValue(); }
      set { _isOSDVisibleProperty.SetValue(value); }
    }

    public AbstractProperty IsOSDVisibleProperty
    {
      get { return _isOSDVisibleProperty; }
    }

    public bool IsOSDLevel0
    {
      get { return (bool)_isOSDLevel0Property.GetValue(); }
      set { _isOSDLevel0Property.SetValue(value); }
    }

    public AbstractProperty IsOSDLevel0Property
    {
      get { return _isOSDLevel0Property; }
    }

    public bool IsOSDLevel1
    {
      get { return (bool)_isOSDLevel1Property.GetValue(); }
      set { _isOSDLevel1Property.SetValue(value); }
    }

    public AbstractProperty IsOSDLevel1Property
    {
      get { return _isOSDLevel1Property; }
    }

    public bool IsOSDLevel2
    {
      get { return (bool)_isOSDLevel2Property.GetValue(); }
      set { _isOSDLevel2Property.SetValue(value); }
    }

    public AbstractProperty IsOSDLevel2Property
    {
      get { return _isOSDLevel2Property; }
    }

    public void UpdateProgram(object sender, SelectionChangedEventArgs e)
    {
      var channelItem = e.FirstAddedItem as ChannelProgramListItem;
      if (channelItem != null)
      {
        IProgram currentProgram = null;
        IProgram nextProgram = null;
        if (channelItem.Programs != null)
        {
          lock (channelItem.Programs.SyncRoot)
            if (channelItem.Programs.Count == 2)
            {
              currentProgram = channelItem.Programs[0].AdditionalProperties["PROGRAM"] as IProgram;
              nextProgram = channelItem.Programs[1].AdditionalProperties["PROGRAM"] as IProgram;
            }
          SelectedChannelName = channelItem.Channel.Name;
          SelectedCurrentProgram.SetProgram(currentProgram, channelItem.Channel);
          SelectedNextProgram.SetProgram(nextProgram, channelItem.Channel);
          double progress = currentProgram != null ?
            (DateTime.Now - currentProgram.StartTime).TotalSeconds / (currentProgram.EndTime - currentProgram.StartTime).TotalSeconds * 100 : 100d;
          SelectedProgramProgress = progress;
        }
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

    public void ShowVideoInfo()
    {
      if (!IsOSDVisible)
      {
        IsOSDVisible = IsOSDLevel0 = true;
        IsOSDLevel1 = IsOSDLevel2 = false;
        Update();
        return;
      }

      if (IsOSDLevel0)
      {
        IsOSDVisible = IsOSDLevel1 = true;
        IsOSDLevel0 = IsOSDLevel2 = false;
        Update();
        return;
      }

      if (IsOSDLevel1)
      {
        IsOSDVisible = IsOSDLevel2 = true;
        IsOSDLevel0 = IsOSDLevel1 = false;
        Update();
        return;
      }

      if (IsOSDLevel2)
      {
        // Hide OSD
        IsOSDVisible = IsOSDLevel0 = IsOSDLevel1 = IsOSDLevel2 = false;

        // Pressing the info button twice will bring up the context menu
        PlayerConfigurationDialogModel.OpenPlayerConfigurationDialog();
      }
      Update();
    }

    public void CloseOSD()
    {
      // Makes sure to always have model initialized first and the property created
      InitModel();
      if (IsOSDVisible)
      {
        // Hide OSD
        IsOSDVisible = IsOSDLevel0 = IsOSDLevel1 = IsOSDLevel2 = false;
        Update();
      }
    }

    #endregion

    #region Members

    #region TV control methods

    protected LiveTvPlayer SlotPlayer
    {
      get
      {
        IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
        if (pcm == null)
          return null;
        LiveTvPlayer player = pcm[SlotIndex] as LiveTvPlayer;
        return player;
      }
    }

    public bool TuneByIndex(int channelIndex)
    {
      if (channelIndex >= ChannelContext.Instance.Channels.Count)
        return false;
      Tune(ChannelContext.Instance.Channels[channelIndex]);
      return true;
    }

    public bool TuneByChannelNumber(int channelNumber)
    {
      IChannel channel = ChannelContext.Instance.Channels.FirstOrDefault(c => c.ChannelNumber == channelNumber);
      if (channel == null)
        return false;
      Tune(channel);
      return true;
    }

    public void Tune(IChannel channel)
    {
      // Specical case of this model, which is also used as normal backing model for OSD, where no WorkflowManager action was performed.
      if (!_isInitialized) InitModel();

      // Avoid subsequent tune requests to same channel, it will only cause delays.
      if (ChannelContext.IsSameChannel(channel, _tvHandler.GetChannel(SlotIndex)))
        return;

      if (SlotPlayer != null)
        SlotPlayer.Pause();

      // Set the current index of the tuned channel
      if (ChannelContext.Instance.Channels.MoveTo(c => ChannelContext.IsSameChannel(c, channel)))
        _zapChannelIndex = ChannelContext.Instance.Channels.CurrentIndex; // Needs to be the same to start zapping from current offset
      else
        _zapChannelIndex = 0;

      BeginZap();
      if (_tvHandler.StartTimeshift(SlotIndex, channel))
      {
        _lastTunedChannel = channel;
        EndZap();
        Update();
        UpdateChannelGroupSelection(channel);
      }
    }

    protected bool ShouldAutoTune()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      var settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
      if (!settings.AutoStartTV)
        return false;
      return playerContextManager.NumActivePlayerContexts == 0;
    }

    protected void AutoTuneLastChannel()
    {
      GetCurrentChannelGroup();
      GetCurrentChannel();
      IChannel current = ChannelContext.Instance.Channels.Current;
      if (current != null)
        Tune(current);
    }

    protected override void SetGroup()
    {
      base.SetGroup();
      OnCurrentGroupChanged(0, 0);
    }

    protected override void SetChannel()
    {
      base.SetChannel();
      _zapChannelIndex = ChannelContext.Instance.Channels.CurrentIndex; // Use field, as parameter might be changed by base method
    }

    private void BeginZap()
    {
      if (SlotPlayer != null)
      {
        SlotPlayer.BeginZap();
      }
    }

    private void EndZap()
    {
      if (SlotPlayer != null)
      {
        SlotPlayer.EndZap();
      }
    }

    /// <summary>
    /// Starts the zap process to tune the next channel in the current channel group.
    /// </summary>
    public void ZapNextChannel()
    {
      _zapChannelIndex++;
      if (_zapChannelIndex >= ChannelContext.Instance.Channels.Count)
        _zapChannelIndex = 0;

      ReSetSkipTimer();
    }

    /// <summary>
    /// Starts the zap process to tune the previous channel in the current channel group.
    /// </summary>
    public void ZapPrevChannel()
    {
      _zapChannelIndex--;
      if (_zapChannelIndex < 0)
        _zapChannelIndex = ChannelContext.Instance.Channels.Count - 1;

      ReSetSkipTimer();
    }

    /// <summary>
    /// Presents a dialog with recording options.
    /// </summary>
    public void RecordDialog()
    {
      if (InitActionsList())
      {
        IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
        screenManager.ShowDialog("DialogClientModel");
      }
    }

    private bool InitActionsList()
    {
      _dialogActionsList.Clear();
      DialogHeader = "[SlimTvClient.RecordActions]";
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerContext playerContext = playerContextManager.GetPlayerContext(PlayerChoice.PrimaryPlayer);
      if (playerContext == null)
        return false;
      LiveTvMediaItem liveTvMediaItem = playerContext.CurrentMediaItem as LiveTvMediaItem;
      LiveTvPlayer player = playerContext.CurrentPlayer as LiveTvPlayer;
      if (liveTvMediaItem == null || player == null)
        return false;

      ITimeshiftContext context = player.TimeshiftContexes.LastOrDefault();
      if (context == null || context.Channel == null)
        return false;

      IProgram programNow;
      IProgram programNext;
      ListItem item;
      ILocalization localization = ServiceRegistration.Get<ILocalization>();
      bool isRecording = false;
      if (_tvHandler.ProgramInfo.GetNowNextProgram(context.Channel, out programNow, out programNext))
      {
        var recStatus = GetRecordingStatus(programNow);
        isRecording = recStatus.HasValue && recStatus.Value.HasFlag(RecordingStatus.Scheduled | RecordingStatus.Recording);
        item = new ListItem(Consts.KEY_NAME, localization.ToString(isRecording ? "[SlimTvClient.StopCurrentRecording]" : "[SlimTvClient.RecordCurrentProgram]", programNow.Title))
        {
          Command = new MethodDelegateCommand(() => CreateOrDeleteSchedule(programNow))
        };
        _dialogActionsList.Add(item);
      }
      if (!isRecording)
      {
        item = new ListItem(Consts.KEY_NAME, "[SlimTvClient.RecordManual]")
        {
          Command = new MethodDelegateCommand(() => CreateOrDeleteSchedule(new Program
          {
            Title = localization.ToString("[SlimTvClient.ManualRecordingTitle]"),
            ChannelId = context.Channel.ChannelId,
            StartTime = DateTime.Now,
            EndTime = DateTime.Now.AddDays(1)
          }))
        };
        _dialogActionsList.Add(item);
      }
      _dialogActionsList.FireChange();
      return true;
    }

    /// <summary>
    /// Sets or resets the zap timer. When the timer elapses, the new selected channel is tuned.
    /// </summary>
    private void ReSetSkipTimer()
    {
      IsOSDVisible = true;
      IsOSDLevel0 = true;
      IsOSDLevel1 = false;
      IsOSDLevel2 = false;

      UpdateRunningChannelPrograms(ChannelContext.Instance.Channels[_zapChannelIndex]);

      if (_zapTimer == null)
      {
        SlimTvClientSettings settings = ServiceRegistration.Get<ISettingsManager>().Load<SlimTvClientSettings>();
        _zapTimer = new DelayedEvent(settings.ZapTimeout * 1000);
        _zapTimer.OnEventHandler += ZapTimerElapsed;
      }
      // In case of new user action, reset the timer.
      _zapTimer.EnqueueEvent(this, EventArgs.Empty);
    }

    private void ZapTimerElapsed(object sender, EventArgs e)
    {
      CloseOSD();

      if (!ChannelContext.IsSameChannel(ChannelContext.Instance.Channels[_zapChannelIndex], _lastTunedChannel))
      {
        ChannelContext.Instance.Channels.SetIndex(_zapChannelIndex);
        Tune(ChannelContext.Instance.Channels[_zapChannelIndex]);
      }

      // When not zapped the previous channel information is restored during the next Update() call
    }

    protected void UpdateSelectedChannelPrograms(IChannel channel)
    {
      UpdateForChannel(channel, SelectedCurrentProgram, SelectedNextProgram, SelectedChannelNameProperty, SelectedProgramProgressProperty);
    }

    protected void UpdateRunningChannelPrograms(IChannel channel)
    {
      UpdateForChannel(channel, CurrentProgram, NextProgram, ChannelNameProperty, ProgramProgressProperty);
    }

    protected void UpdateForChannel(IChannel channel, ProgramProperties current, ProgramProperties next, AbstractProperty channelNameProperty, AbstractProperty progressProperty)
    {
      channelNameProperty.SetValue(channel.Name);
      IProgram currentProgram;
      IProgram nextProgram;
      if (_tvHandler.ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram))
      {
        current.SetProgram(currentProgram, channel);
        next.SetProgram(nextProgram, channel);
        double progress = (DateTime.Now - currentProgram.StartTime).TotalSeconds / (currentProgram.EndTime - currentProgram.StartTime).TotalSeconds * 100;
        progressProperty.SetValue(progress);
      }
      else
      {
        current.SetProgram(null);
        next.SetProgram(null);
        progressProperty.SetValue(100d);
      }
    }

    #endregion

    #region Inits and Updates

    protected override void InitModel()
    {
      if (!_isInitialized)
      {
        _currentGroupNameProperty = new WProperty(typeof(string), string.Empty);

        _selectedChannelNameProperty = new WProperty(typeof(string), string.Empty);
        _selectedCurrentProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());
        _selectedNextProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());
        _selectedProgramProgressProperty = new WProperty(typeof(double), 0d);

        _currentChannelNameProperty = new WProperty(typeof(string), string.Empty);
        _currentProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());
        _nextProgramProperty = new WProperty(typeof(ProgramProperties), new ProgramProperties());
        _programProgressProperty = new WProperty(typeof(double), 0d);
        _timeshiftProgressProperty = new WProperty(typeof(double), 0d);

        _piPAvailableProperty = new WProperty(typeof(bool), false);
        _piPEnabledProperty = new WProperty(typeof(bool), false);

        _isOSDVisibleProperty = new WProperty(typeof(bool), false);
        _isOSDLevel0Property = new WProperty(typeof(bool), false);
        _isOSDLevel1Property = new WProperty(typeof(bool), false);
        _isOSDLevel2Property = new WProperty(typeof(bool), false);

        //Get current Tv Server state
        var ssm = ServiceRegistration.Get<IServerStateManager>();
        TvServerState state;
        if (!ssm.TryGetState(TvServerState.STATE_ID, out state))
          state = null;
        _serverStateProperty = new WProperty(typeof(TvServerState), state);

        _isInitialized = true;

        _resumeEvent.OnEventHandler = OnResume;
        SubscribeToMessages();
      }
      base.InitModel();
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(SystemMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(ServerStateMessaging.CHANNEL);
      _messageQueue.PreviewMessage += OnMessageReceived;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if (newState == SystemState.Resuming)
            {
              // Signal the event, callback is executed after timeout, see OnResume
              _resumeEvent.EnqueueEvent(this, EventArgs.Empty);
            }
            if (newState == SystemState.Suspending)
            {
              ServiceRegistration.Get<ILogger>().Info("SlimTvClientModel: System suspending, stopping all SlimTV players");
              IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
              for (int index = 0; index < playerContextManager.NumActivePlayerContexts; index++)
              {
                IPlayerContext playerContext = playerContextManager.GetPlayerContext(index);
                if (playerContext != null && playerContext.CurrentMediaItem is LiveTvMediaItem)
                {
                  playerContext.Stop();
                  _tvWasActive = true;
                }
              }
            }
            break;
        }
      }
      else if (message.ChannelName == ServerStateMessaging.CHANNEL)
      {
        //Check if Tv Server state has changed and update if necessary
        ServerStateMessaging.MessageType messageType = (ServerStateMessaging.MessageType)message.MessageType;
        if (messageType == ServerStateMessaging.MessageType.StatesChanged)
        {
          var states = message.MessageData[ServerStateMessaging.STATES] as IDictionary<Guid, object>;
          if (states != null && states.ContainsKey(TvServerState.STATE_ID))
            ServerState = states[TvServerState.STATE_ID] as TvServerState;
        }
      }
    }

    private void OnResume(object sender, EventArgs e)
    {
      var shouldAutoTune = _tvWasActive && ShouldAutoTune();
      ServiceRegistration.Get<ILogger>().Info("SlimTvClientModel: System resuming, autotune: {0}", shouldAutoTune);
      if (shouldAutoTune)
        AutoTuneLastChannel();

      _tvWasActive = false;
    }

    protected int SlotIndex
    {
      get { return PiPEnabled ? PlayerContextIndex.SECONDARY : PlayerContextIndex.PRIMARY; }
    }

    protected override void Update()
    {
      // Don't update the current channel and program information if we are in zap osd.
      if (_tvHandler == null || (_zapTimer != null && _zapTimer.IsEventPending))
        return;

      // Update current programs for all channels of current group (visible inside MiniGuide).
      UpdateAllCurrentPrograms();

      _zapChannelIndex = ChannelContext.Instance.Channels.CurrentIndex;

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
        LiveTvPlayer player = playerContext.CurrentPlayer as LiveTvPlayer;
        if (player != null)
        {
          ITimeshiftContext context = player.TimeshiftContexes.LastOrDefault();
          IProgram currentProgram = null;
          IProgram nextProgram = null;
          IChannel channel = null;
          if (context != null && context.Channel != null)
          {
            channel = context.Channel;
            ChannelName = channel.Name;
            if (_tvHandler.ProgramInfo != null && _tvHandler.ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram) && currentProgram != null)
            {
              double progress = (DateTime.Now - currentProgram.StartTime).TotalSeconds /
                                (currentProgram.EndTime - currentProgram.StartTime).TotalSeconds * 100;
              _programProgressProperty.SetValue(progress);
            }
          }
          CurrentProgram.SetProgram(currentProgram, channel);
          NextProgram.SetProgram(nextProgram, channel);
        }
      }
    }

    private void UpdateChannelGroupSelection(IChannel channel)
    {
      if (channel == null)
        return;

      lock (CurrentGroupChannels.SyncRoot)
        foreach (ChannelProgramListItem currentGroupChannel in CurrentGroupChannels)
          currentGroupChannel.Selected = ChannelContext.IsSameChannel(currentGroupChannel.Channel, channel);

      CurrentGroupChannels.FireChange();

      SetCurrentChannelGroup();
      SetCurrentChannel();
    }

    #endregion

    #region Dispose

    public override void Dispose()
    {
      if (_resumeEvent != null)
        _resumeEvent.Dispose();
      if (_zapTimer != null)
        _zapTimer.Dispose();
      _isInitialized = false;
      base.Dispose();
    }

    #endregion

    #region Channel, groups and programs

    /// <summary>
    /// Helper method to make sure the model updates the channel list when opening the MiniGuide.
    /// Usually the update logic is done in Workflow events, but the MiniGuide is opened as dialog
    /// in current workflow state (which doesn't invoke workflow transistions).
    /// </summary>
    public void UpdateChannelsMiniGuide()
    {
      UpdateChannels();
    }
    protected virtual void UpdateGuiProperties()
    {
      CurrentGroupName = CurrentChannelGroup != null ? CurrentChannelGroup.Name : string.Empty;
    }

    protected void UpdateChannels()
    {
      UpdateGuiProperties();

      bool isOneSelected = false;
      lock (_channelList.SyncRoot)
      {
        _channelList.Clear();
        foreach (IChannel channel in ChannelContext.Instance.Channels)
        {
          // Use local variable, otherwise delegate argument is not fixed
          IChannel currentChannel = channel;

          bool isCurrentSelected = ChannelContext.IsSameChannel(currentChannel, _lastTunedChannel);
          isOneSelected |= isCurrentSelected;
          ChannelProgramListItem item = new ChannelProgramListItem(currentChannel, null)
          {
            Programs = new ItemsList { GetNoProgramPlaceholder(channel.ChannelId), GetNoProgramPlaceholder(channel.ChannelId) },
            Command = new MethodDelegateCommand(() => Tune(currentChannel)),
            Selected = isCurrentSelected
          };
          item.AdditionalProperties["CHANNEL"] = channel;
          _channelList.Add(item);
        }
      }
      // Adjust channel list position
      ChannelContext.Instance.Channels.MoveTo(c => ChannelContext.IsSameChannel(c, _lastTunedChannel));

      // If the current watched channel is not part of the channel group, set the "selected" property to first list item to make sure focus will be set to the list view
      if (!isOneSelected && _channelList.Count > 0)
        _channelList.First().Selected = true;

      // Load programs asynchronously, this increases performance of list building
      GetNowAndNextProgramsList_Async();
      CurrentGroupChannels.FireChange();
    }

    protected void UpdateAllCurrentPrograms()
    {
      DateTime now = DateTime.Now;
      if ((now - _lastChannelListUpdate).TotalSeconds > PROGRAM_UPDATE_SEC)
      {
        _lastChannelListUpdate = now;
        GetNowAndNextProgramsList_Async();
      }
    }

    protected void GetNowAndNextProgramsList_Async()
    {
      IThreadPool threadPool = ServiceRegistration.Get<IThreadPool>();
      threadPool.Add(GetNowAndNextProgramsList);
    }

    protected void GetNowAndNextProgramsList()
    {
      IChannelGroup currentChannelGroup = CurrentChannelGroup;
      if (_tvHandler.ProgramInfo == null || currentChannelGroup == null)
        return;
      IDictionary<int, IProgram[]> programs;

      _tvHandler.ProgramInfo.GetNowAndNextForChannelGroup(currentChannelGroup, out programs);
      lock (CurrentGroupChannels.SyncRoot)
        foreach (ChannelProgramListItem channelItem in CurrentGroupChannels)
        {
          IProgram[] nowNext;
          IProgram currentProgram = null;
          IProgram nextProgram = null;
          IChannel channel = channelItem.Channel;
          if (programs != null && programs.TryGetValue(channel.ChannelId, out nowNext))
          {
            currentProgram = nowNext.Length > 0 ? nowNext[0] : null;
            nextProgram = nowNext.Length > 1 ? nowNext[1] : null;
          }

          CreateProgramListItem(currentProgram, channelItem.Programs[0], channel);
          CreateProgramListItem(nextProgram, channelItem.Programs[1], channel, currentProgram);
        }
    }

    private static void CreateProgramListItem(IProgram program, ListItem itemToUpdate, IChannel channel, IProgram previousProgram = null)
    {
      ProgramListItem item = itemToUpdate as ProgramListItem;
      if (item == null)
        return;
      item.Program.SetProgram(program ?? GetNoProgram(channel.ChannelId, previousProgram), channel);
      item.AdditionalProperties["PROGRAM"] = program;
      item.Update();
    }

    private static ProgramListItem GetNoProgramPlaceholder(int channelId, IProgram previousProgram = null)
    {
      IProgram placeHolder = GetNoProgram(channelId, previousProgram);
      ProgramProperties programProperties = new ProgramProperties
      {
        Title = placeHolder.Title,
        StartTime = placeHolder.StartTime,
        EndTime = placeHolder.EndTime
      };
      return new ProgramListItem(programProperties);
    }

    private static IProgram GetNoProgram(int channelId, IProgram previousProgram = null)
    {
      ILocalization loc = ServiceRegistration.Get<ILocalization>();
      DateTime from;
      DateTime to;
      if (previousProgram != null)
      {
        from = previousProgram.EndTime;
        to = DateTime.Now.GetDay().AddDays(1);
      }
      else
      {
        from = DateTime.Now.GetDay();
        to = from.AddDays(1);
      }

      return new Program
      {
        ChannelId = channelId,
        Title = loc.ToString("[SlimTvClient.NoProgram]"),
        StartTime = from,
        EndTime = to
      };
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
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      UpdateChannels();

      if (!ShouldAutoTune())
        return;

      AutoTuneLastChannel();
    }

    public override void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      base.Reactivate(oldContext, newContext);
      UpdateChannels();
    }

    #endregion
  }
}
