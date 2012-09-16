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
using System.Timers;
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Plugins.SlimTv.Client.Helpers;
using MediaPortal.Plugins.SlimTv.Client.Player;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.Plugins.SlimTv.Client.Models
{
  /// <summary>
  /// <see cref="SlimTvClientModel"/> is the main entry model for SlimTV. It provides channel group and channel selection and 
  /// acts as backing model for the Live-TV OSD to provide program information.
  /// </summary>
  public class SlimTvClientModel : SlimTvModelBase
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

    // OSD Control properties
    private AbstractProperty _isOSDVisibleProperty = null;
    private AbstractProperty _isOSDLevel0Property = null;
    private AbstractProperty _isOSDLevel1Property = null;
    private AbstractProperty _isOSDLevel2Property = null;

    // Channel zapping
    protected const double ZAP_TIMEOUT_SECONDS = 2.0d;
    protected Timer _zapTimer;
    protected int _zapChannelIndex;

    #endregion

    #region Variables

    private readonly ItemsList _channelList = new ItemsList();
    private bool _active;

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
      get { return (string) _currentGroupNameProperty.GetValue(); }
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
      get { return (string) _currentChannelNameProperty.GetValue(); }
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
      get { return (string) _selectedCurrentProgramProperty.GetValue(); }
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
      get { return (string) _selectedNextProgramProperty.GetValue(); }
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

    /// <summary>
    /// Gets a value (range 0 to 100) which denotes the current fraction of played content.
    /// </summary>
    public double ProgramProgress
    {
      get { return (double) _programProgressProperty.GetValue(); }
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
      get { return (double) _timeshiftProgressProperty.GetValue(); }
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
      get { return (ProgramProperties) _nextProgramProperty.GetValue(); }
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
      get { return (bool) _piPEnabledProperty.GetValue(); }
      set { _piPEnabledProperty.SetValue(value); }
    }

    public AbstractProperty PiPEnabledProperty
    {
      get { return _piPEnabledProperty; }
    }

    public bool IsOSDVisible
    {
      get { return (bool) _isOSDVisibleProperty.GetValue(); }
      set { _isOSDVisibleProperty.SetValue(value); }
    }

    public AbstractProperty IsOSDVisibleProperty
    {
      get { return _isOSDVisibleProperty; }
    }

    public bool IsOSDLevel0
    {
      get { return (bool) _isOSDLevel0Property.GetValue(); }
      set { _isOSDLevel0Property.SetValue(value); }
    }

    public AbstractProperty IsOSDLevel0Property
    {
      get { return _isOSDLevel0Property; }
    }

    public bool IsOSDLevel1
    {
      get { return (bool) _isOSDLevel1Property.GetValue(); }
      set { _isOSDLevel1Property.SetValue(value); }
    }

    public AbstractProperty IsOSDLevel1Property
    {
      get { return _isOSDLevel1Property; }
    }

    public bool IsOSDLevel2
    {
      get { return (bool) _isOSDLevel2Property.GetValue(); }
      set { _isOSDLevel2Property.SetValue(value); }
    }

    public AbstractProperty IsOSDLevel2Property
    {
      get { return _isOSDLevel2Property; }
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

      // Set the current index of the tuned channel
      _webChannelIndex = 0;
      foreach (IChannel currentChannel in _channels)
        if (currentChannel != channel)
          _webChannelIndex++;
        else
          break;

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
        SlotPlayer.ChannelZap();
      }
    }

    /// <summary>
    /// Starts the zap process to tune the next channel in the current channel group.
    /// </summary>
    public void ZapNextChannel()
    {
      if (_channels == null)
        return;

      _zapChannelIndex++;
      if (_zapChannelIndex >= _channels.Count)
        _zapChannelIndex = 0;

      ReSetSkipTimer();
    }

    /// <summary>
    /// Starts the zap process to tune the previous channel in the current channel group.
    /// </summary>
    public void ZapPrevChannel()
    {
      if (_channels == null)
        return;

      _zapChannelIndex--;
      if (_zapChannelIndex < 0)
        _zapChannelIndex = _channels.Count - 1;

      ReSetSkipTimer();
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

      UpdateForChannel(_channels[_zapChannelIndex]);

      if (_zapTimer == null)
      {
        _zapTimer = new Timer(ZAP_TIMEOUT_SECONDS * 1000) { Enabled = true, AutoReset = false };
        _zapTimer.Elapsed += ZapTimerElapsed;
      }
      else
      {
        // In case of new user action, reset the timer.
        _zapTimer.Stop();
        _zapTimer.Start();
      }
    }

    private void ZapTimerElapsed(object sender, ElapsedEventArgs e)
    {
      CloseOSD();

      if (_zapChannelIndex != _webChannelIndex)
        Tune(_channels[_zapChannelIndex]);

      _zapTimer.Close();
      _zapTimer = null;

      // When not zapped the previous channel information is restored during the next Update() call
    }

    protected void UpdateForChannel(IChannel channel)
    {
      ChannelName = channel.Name;
      IProgram currentProgram;
      IProgram nextProgram;
      if (_tvHandler.ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram))
      {
        CurrentProgram.SetProgram(currentProgram);
        double progress = (DateTime.Now - currentProgram.StartTime).TotalSeconds /
                          (currentProgram.EndTime - currentProgram.StartTime).TotalSeconds * 100;
        _programProgressProperty.SetValue(progress);

        NextProgram.SetProgram(nextProgram);
      }
      else
      {
        CurrentProgram.SetProgram(null);
        NextProgram.SetProgram(null);
        _programProgressProperty.SetValue(100d);
      }
    }

    #endregion

    #region Inits and Updates

    protected override void InitModel()
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

        _isOSDVisibleProperty = new WProperty(typeof(bool), false);
        _isOSDLevel0Property = new WProperty(typeof(bool), false);
        _isOSDLevel1Property = new WProperty(typeof(bool), false);
        _isOSDLevel2Property = new WProperty(typeof(bool), false);

        _isInitialized = true;
      }
      base.InitModel();
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
      // Don't update the current channel and program information if we are in zap osd.
      if (!_active || _zapTimer != null)
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
              double progress = (DateTime.Now - currentProgram.StartTime).TotalSeconds /
                                (currentProgram.EndTime - currentProgram.StartTime).TotalSeconds * 100;
              _programProgressProperty.SetValue(progress);

              IList<IProgram> nextPrograms;
              DateTime nextTime = currentProgram.EndTime.Add(TimeSpan.FromSeconds(10));
              if (_tvHandler.ProgramInfo.GetPrograms(context.Channel, nextTime, nextTime, out nextPrograms))
                NextProgram.SetProgram(nextPrograms[0]);
            }
          }
        }
      }
    }

    #endregion

    #region Channel, groups and programs

    protected override void UpdateCurrentChannel()
    { }

    protected override void UpdatePrograms()
    { }

    protected override void UpdateChannels()
    {
      base.UpdateChannels();
      if (_webChannelGroupIndex < _channelGroups.Count)
      {
        IChannelGroup currentGroup = _channelGroups[_webChannelGroupIndex];
        CurrentGroupName = currentGroup.Name;
      }
      _channelList.Clear();
      if (_channels == null)
        return;

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

    private void UpdateProgramForChannel(IChannel channel)
    {
      IProgram currentProgram;
      IProgram nextProgram;
      if (_tvHandler.ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram))
      {
        _selectedCurrentProgramProperty.SetValue(FormatProgram(currentProgram));
        _selectedNextProgramProperty.SetValue(FormatProgram(nextProgram));
      }
    }

    protected ItemsList GetNowAndNextProgramsList(IChannel channel)
    {
      ItemsList channelPrograms = new ItemsList();
      IProgram currentProgram;
      IProgram nextProgram;
      // We do not check return code here. Results for currentProgram or nextProgram can be null, this is ok here, as Program will be filled with placeholder.
      _tvHandler.ProgramInfo.GetNowNextProgram(channel, out currentProgram, out nextProgram);
      CreateProgramListItem(currentProgram, channelPrograms);
      CreateProgramListItem(nextProgram, channelPrograms, currentProgram);
      return channelPrograms;
    }

    private static void CreateProgramListItem(IProgram program, ItemsList channelPrograms, IProgram previousProgram = null)
    {
      ProgramListItem item;
      if (program == null)
        item = GetNoProgramPlaceholder(previousProgram);
      else
      {
        ProgramProperties programProperties = new ProgramProperties();
        programProperties.SetProgram(program);
        item = new ProgramListItem(programProperties);
      }
      item.AdditionalProperties["PROGRAM"] = program;
      channelPrograms.Add(item);
    }

    private static ProgramListItem GetNoProgramPlaceholder(IProgram previousProgram = null)
    {
      ILocalization loc = ServiceRegistration.Get<ILocalization>();
      DateTime from;
      DateTime to;
      if (previousProgram != null)
      {
        from = previousProgram.EndTime;
        to = FormatHelper.GetDay(DateTime.Now).AddDays(1);
      }
      else
      {
        from = FormatHelper.GetDay(DateTime.Now);
        to = from.AddDays(1);
      }

      ProgramProperties programProperties = new ProgramProperties(from, to)
      {
        Title = loc.ToString("[SlimTvClient.NoProgram]"),
        StartTime = from,
        EndTime = to
      };
      return new ProgramListItem(programProperties);
    }

    private static string FormatProgram(IProgram program)
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

    public override Guid ModelId
    {
      get { return new Guid(MODEL_ID_STR); }
    }

    public override void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.EnterModelContext(oldContext, newContext);
      _active = true;
    }

    public override void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      base.ExitModelContext(oldContext, newContext);
      _active = false;
    }

    public override void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      GetCurrentChannelGroup();
      UpdateChannels();
    }
    #endregion
  }
}