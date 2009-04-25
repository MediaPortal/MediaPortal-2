#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.Timers;
using MediaPortal.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;
using MediaPortal.Presentation.Workflow;
using Timer=System.Timers.Timer;

namespace UiComponents.SkinBase
{
  /// <summary>
  /// This model attends the currently-playing and fullscreen-content workflow states for
  /// Video, Audio and Image media players.
  /// </summary>
  public class PlayerModel : IDisposable, IWorkflowModel
  {
    public const string PLAYER_MODEL_ID_STR = "A2F24149-B44C-498b-AE93-288213B87A1A";

    public const string CURRENTLY_PLAYING_STATE_ID_STR = "5764A810-F298-4a20-BF84-F03D16F775B1";

    public const string FULLSCREENVIDEO_SCREEN_NAME = "FullScreenVideo";
    public const string FULLSCREENAUDIO_SCREEN_NAME = "FullScreenAudio";
    public const string FULLSCREENPICTURE_SCREEN_NAME = "FullScreenPicture";

    public const string VIDEOCONTEXTMENU_DIALOG_NAME = "DialogVideoContextMenu";

    public const string CHOOSE_AUDIO_STREAM_DIALOG_NAME = "DialogChooseAudioStream";
    public const string PLAYER_CONFIGURATION_DIALOG_NAME = "DialogPlayerConfiguration";

    public static Guid PLAYER_MODEL_ID = new Guid(PLAYER_MODEL_ID_STR);
    public static Guid CURRENTLY_PLAYING_STATE_ID = new Guid(CURRENTLY_PLAYING_STATE_ID_STR);

    protected static TimeSpan VIDEO_INFO_TIMEOUT = new TimeSpan(0, 0, 0, 5);

    protected const string KEY_NAME = "Name";

    protected const string PLAYER_OF_TYPE_RESOURCE = "[Players.PlayerOfType]";
    protected const string SLOT_NO_RESOURCE = "[Players.SlotNo]";
    protected const string FOCUS_PLAYER_RESOURCE = "[Players.FocusPlayer]";
    protected const string SWITCH_PIP_PLAYERS_RESOURCE = "[Players.SwitchPipPlayers]";
    protected const string CHOOSE_AUDIO_STREAM_RESOURCE = "[Players.ChooseAudioStream]";
    protected const string MUTE_RESOURCE = "[Players.Mute]";
    protected const string MUTE_OFF_RESOURCE = "[Players.MuteOff]";
    protected const string CLOSE_PLAYER_CONTEXT_RESOURCE = "[Players.ClosePlayerContext]";

    protected Timer _timer;

    protected Property _isVideoInfoVisibleProperty;
    protected Property _isPipVisibleProperty;

    protected ItemsList _playerConfigurationMenu = new ItemsList();
    protected ItemsList _audioStreamsMenu = new ItemsList();

    protected string _currentlyPlayingScreen = null;

    protected DateTime _lastVideoInfoDemand = DateTime.MinValue;

    public PlayerModel()
    {
      _isVideoInfoVisibleProperty = new Property(typeof(bool), false);
      _isPipVisibleProperty = new Property(typeof(bool), false);

      SubscribeToMessages();
    }

    public void Dispose()
    {
      StopListening();
      UnsubscribeFromMessages();
    }

    protected void SubscribeToMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived += OnPlayerManagerMessageReceived;
      broker.GetOrCreate(PlayerContextManagerMessaging.QUEUE).MessageReceived += OnPlayerContextManagerMessageReceived;
      broker.GetOrCreate(SystemMessaging.QUEUE).MessageReceived += OnSystemMessageReceived;
    }

    protected void UnsubscribeFromMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>(false);
      if (broker == null)
        return;
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived -= OnPlayerManagerMessageReceived;
      broker.GetOrCreate(PlayerContextManagerMessaging.QUEUE).MessageReceived -= OnPlayerContextManagerMessageReceived;
      broker.GetOrCreate(SystemMessaging.QUEUE).MessageReceived -= OnSystemMessageReceived;
    }

    protected void StartListening()
    {
      // Setup timer to update the properties
      _timer = new Timer(500);
      _timer.Elapsed += OnTimerElapsed;
      _timer.Enabled = true;

      CheckVideoInfoVisible();
    }

    protected void StopListening()
    {
      _timer.Enabled = false;
      _timer.Elapsed -= OnTimerElapsed;
    }

    protected void OnSystemMessageReceived(QueueMessage message)
    {
      SystemMessaging.MessageType messageType =
          (SystemMessaging.MessageType) message.MessageData[SystemMessaging.MESSAGE_TYPE];
      if (messageType == SystemMessaging.MessageType.SystemStarted)
        StartListening();
      else if (messageType == SystemMessaging.MessageType.SystemShutdown)
      {
        StopListening();
        UnsubscribeFromMessages();
      }
    }

    protected void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PlayerSlotActivated:
          CheckUpdatePlayerConfigurationData();
          break;
        case PlayerManagerMessaging.MessageType.PlayerSlotDeactivated:
          int slotIndex = (int) message.MessageData[PlayerManagerMessaging.PARAM];
          if (_currentlyPlayingScreen != null && slotIndex == playerContextManager.CurrentPlayerIndex)
          {
            IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
            // Maybe we should do more handling in this case - show dialogs "do you want to delete"
            // etc.? At the moment we'll simply return to the last workflow state.
            workflowManager.NavigatePop(1);
            // _currentlyPlayingIndex will be reset by ExitModelContext
          }
          CheckUpdatePlayerConfigurationData();
          break;
        case PlayerManagerMessaging.MessageType.PlayerStarted:
          if (_currentlyPlayingScreen != null)
            // Automatically switch "currently playing" screen if another player is started. This will
            // ensure that the screen is correctly updated when the playlist progresses.
            UpdateCurrentlyPlayingScreen();
          break;
        case PlayerManagerMessaging.MessageType.PlayerStopped:
          CheckUpdatePlayerConfigurationData();
          break;
        case PlayerManagerMessaging.MessageType.PlayersMuted:
        case PlayerManagerMessaging.MessageType.PlayersUnmuted:
          CheckUpdatePlayerConfigurationData();
          break;
      }
    }

    protected void OnPlayerContextManagerMessageReceived(QueueMessage message)
    {
      PlayerContextManagerMessaging.MessageType messageType =
          (PlayerContextManagerMessaging.MessageType) message.MessageData[PlayerContextManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged:
          CheckUpdatePlayerConfigurationData();
          break;
      }
    }

    protected void OnTimerElapsed(object sender, ElapsedEventArgs e)
    {
      UpdateProperties();
      CheckVideoInfoVisible();
    }

    protected void UpdateProperties()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IsPipVisible = playerContextManager.IsPipActive;
    }

    protected static string GetNameForPlayerContext(IPlayerContextManager playerContextManager, int playerSlot)
    {
      IPlayerContext pc = playerContextManager.GetPlayerContext(playerSlot);
      if (pc == null)
        return null;
      IPlayer player = pc.CurrentPlayer;
      if (player == null)
      {
        IResourceString playerOfType = LocalizationHelper.CreateResourceString(PLAYER_OF_TYPE_RESOURCE); // "{0} player"
        IResourceString slotNo = LocalizationHelper.CreateResourceString(SLOT_NO_RESOURCE); // "Slot #{0}"
        return playerOfType.Evaluate(pc.MediaType.ToString()) + " (" + slotNo.Evaluate(playerSlot.ToString()) + ")"; // "Video player (Slot #1)"
      }
      else
        return player.Name + ": " + player.MediaItemTitle;
    }

    /// <summary>
    /// Updates the menu items for the dialogs "dialogPlayerConfiguration" and "dialogChooseAudioStream"
    /// and closes the dialogs when their entries are not valid any more.
    /// </summary>
    protected void CheckUpdatePlayerConfigurationData()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();

      int numActiveSlots = playerManager.NumActiveSlots;
      // Build player configuration menu
      _playerConfigurationMenu.Clear();
      if (numActiveSlots > 1)
      {
        // Set player focus
        int newCurrentPlayer = 1 - playerContextManager.CurrentPlayerIndex;
        string name = GetNameForPlayerContext(playerContextManager, newCurrentPlayer);
        if (name != null)
        {
          ListItem item = new ListItem(KEY_NAME, LocalizationHelper.CreateResourceString(FOCUS_PLAYER_RESOURCE).Evaluate(name))
            {
                Command = new MethodDelegateCommand(() => SetCurrentPlayer(newCurrentPlayer))
            };
          _playerConfigurationMenu.Add(item);
        }
      }
      if (numActiveSlots > 1 && playerContextManager.IsPipActive)
      {
        ListItem item = new ListItem(KEY_NAME, SWITCH_PIP_PLAYERS_RESOURCE)
          {
              Command = new MethodDelegateCommand(SwitchPrimarySecondaryPlayer)
          };
        _playerConfigurationMenu.Add(item);
      }
      ICollection<AudioStreamDescriptor> audioStreams = playerContextManager.GetAvailableAudioStreams();
      if (audioStreams.Count > 1)
      {
        ListItem item = new ListItem(KEY_NAME, CHOOSE_AUDIO_STREAM_RESOURCE)
          {
              Command = new MethodDelegateCommand(OpenChooseAudioStreamDialog)
          };
        _playerConfigurationMenu.Add(item);
      }
      if (numActiveSlots > 0)
      {
        ListItem item;
        if (playerManager.Muted)
          item = new ListItem(KEY_NAME, MUTE_OFF_RESOURCE)
            {
                Command = new MethodDelegateCommand(UnmutePlayers)
            };
        else
          item = new ListItem(KEY_NAME, MUTE_RESOURCE)
            {
                Command = new MethodDelegateCommand(MutePlayers)
            };
        _playerConfigurationMenu.Add(item);
      }
      // TODO: Handle subtitles same as audio streams
      for (int i = 0; i < numActiveSlots; i++)
      {
        string name = GetNameForPlayerContext(playerContextManager, i);
        if (name != null)
        {
          int indexClosureCopy = i;
          ListItem item = new ListItem(KEY_NAME, LocalizationHelper.CreateResourceString(CLOSE_PLAYER_CONTEXT_RESOURCE).Evaluate(name))
            {
                Command = new MethodDelegateCommand(() => ClosePlayerContext(indexClosureCopy))
            };
          _playerConfigurationMenu.Add(item);
        }
      }
      _playerConfigurationMenu.FireChange();

      // Build audio streams menu
      _audioStreamsMenu.Clear();
      // Cluster by player
      IDictionary<IPlayerContext, ICollection<AudioStreamDescriptor>> streamsByPlayerContext =
          new Dictionary<IPlayerContext, ICollection<AudioStreamDescriptor>>();
      foreach (AudioStreamDescriptor asd in audioStreams)
      {
        IPlayerContext pc = asd.PlayerContext;
        ICollection<AudioStreamDescriptor> asds;
        if (!streamsByPlayerContext.TryGetValue(pc, out asds))
          streamsByPlayerContext[pc] = asds = new List<AudioStreamDescriptor>();
        asds.Add(asd);
      }
      foreach (KeyValuePair<IPlayerContext, ICollection<AudioStreamDescriptor>> pasds in streamsByPlayerContext)
      {
        IPlayerContext pc = pasds.Key;
        IPlayer player = pc.CurrentPlayer;
        foreach (AudioStreamDescriptor asd in pasds.Value)
        {
          string playedItem = player == null ? null : player.MediaItemTitle;
          if (playedItem == null)
            playedItem = pc.Name;
          string choiceItemName;
          if (pasds.Value.Count > 1)
            // Only display the audio stream name if the player has more than one audio stream
            choiceItemName = playedItem + ": " + asd.AudioStreamName;
          else
            choiceItemName = playedItem;
          AudioStreamDescriptor asdClosureCopy = asd;
          ListItem item = new ListItem(KEY_NAME, choiceItemName)
            {
                Command = new MethodDelegateCommand(() => ChooseAudioStream(asdClosureCopy))
            };
          _audioStreamsMenu.Add(item);
        }
      }
      _audioStreamsMenu.FireChange();

      if (_audioStreamsMenu.Count == 0 && screenManager.ActiveScreenName == CHOOSE_AUDIO_STREAM_DIALOG_NAME)
        // Automatically close audio stream choice dialog
        screenManager.CloseDialog();
      if (_playerConfigurationMenu.Count == 0 && screenManager.ActiveScreenName == PLAYER_CONFIGURATION_DIALOG_NAME)
        // Automatically close player configuration dialog
        screenManager.CloseDialog();
    }

    protected void CheckVideoInfoVisible()
    {
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      IsVideoInfoVisible = inputManager.IsMouseUsed || DateTime.Now - _lastVideoInfoDemand < VIDEO_INFO_TIMEOUT;
    }

    /// <summary>
    /// Returns the player context for the current focused player. The current player governs which
    /// "currently playing" screen is shown.
    /// </summary>
    /// <returns>Player context for the current player or <c>null</c>, if there is no current player.</returns>
    protected static IPlayerContext GetCurrentPlayerContext()
    {
      IPlayerContextManager pcm = ServiceScope.Get<IPlayerContextManager>();
      int currentPlayerSlot = pcm.CurrentPlayerIndex;
      if (currentPlayerSlot == -1)
        currentPlayerSlot = PlayerManagerConsts.PRIMARY_SLOT;
      return pcm.GetPlayerContext(currentPlayerSlot);
    }

    protected static bool CanHandlePlayer(IPlayer player)
    {
      return player is IVideoPlayer || player is IAudioPlayer || player is IPicturePlayer;
    }

    protected void UpdateCurrentlyPlayingScreen()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      string targetScreen;
      IPlayer currentPlayer = pc.CurrentPlayer;
      if (!CanHandlePlayer(currentPlayer))
        return;
      if (currentPlayer is IVideoPlayer)
        targetScreen = FULLSCREENVIDEO_SCREEN_NAME;
      else if (currentPlayer is IAudioPlayer)
        targetScreen = FULLSCREENAUDIO_SCREEN_NAME;
      else if (currentPlayer is IPicturePlayer)
        targetScreen = FULLSCREENPICTURE_SCREEN_NAME;
      else
          // Error case: The current player isn't recognized - its none of our supported players
        targetScreen = FULLSCREENVIDEO_SCREEN_NAME;
      if (_currentlyPlayingScreen != targetScreen)
      {
        IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
        screenManager.ShowScreen(targetScreen);
        _currentlyPlayingScreen = targetScreen;
      }
    }

    public Property IsVideoInfoVisibleProperty
    {
      get { return _isVideoInfoVisibleProperty; }
    }

    public bool IsVideoInfoVisible
    {
      get { return (bool) _isVideoInfoVisibleProperty.GetValue(); }
      set { _isVideoInfoVisibleProperty.SetValue(value); }
    }

    public Property IsPipVisibleProperty
    {
      get { return _isPipVisibleProperty; }
    }

    public bool IsPipVisible
    {
      get { return (bool) _isPipVisibleProperty.GetValue(); }
      set { _isPipVisibleProperty.SetValue(value); }
    }

    public ItemsList PlayerConfigurationMenu
    {
      get { return _playerConfigurationMenu; }
    }

    public ItemsList AudioStreamsMenu
    {
      get { return _audioStreamsMenu; }
    }

    public void ExecuteMenuItem(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
    }

    public void ShowCurrentlyPlaying()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(new Guid(CURRENTLY_PLAYING_STATE_ID_STR));
    }

    public void ShowVideoInfo()
    {
      _lastVideoInfoDemand = DateTime.Now;
      if (IsVideoInfoVisible)
      { // Pressing the info button twice will bring up the context menu
        IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
        screenManager.ShowDialog(VIDEOCONTEXTMENU_DIALOG_NAME);
      }
      CheckVideoInfoVisible();
    }

    public void SetCurrentPlayer(int playerIndex)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.CurrentPlayerIndex = playerIndex;
    }

    public void ClosePlayerContext(int playerIndex)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.CloseSlot(playerIndex);
    }

    public void MutePlayers()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted = true;
    }

    public void UnmutePlayers()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted = false;
    }

    public void SwitchPrimarySecondaryPlayer()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.SwitchSlots();
    }

    public void OpenChooseAudioStreamDialog()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.ShowDialog(CHOOSE_AUDIO_STREAM_DIALOG_NAME);
    }

    public void ChooseAudioStream(AudioStreamDescriptor asd)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.SetAudioStream(asd);
    }

    public static void Play()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      if (pc.PlayerState == PlaybackState.Paused)
        pc.Pause();
      else
        pc.Restart();
    }

    public static void Pause()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      pc.Pause();
    }

    public static void TogglePause()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.TogglePlayPause();
    }

    public static void Stop()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      pc.Stop();
    }

    public static void SeekBackward()
    {
      // TODO
      IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
      dialogManager.ShowDialog("Not implemented", "The BKWD command is not implemented yet", DialogType.OkDialog, false);
    }

    public static void SeekForward()
    {
      // TODO
      IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
      dialogManager.ShowDialog("Not implemented", "The FWD command is not implemented yet", DialogType.OkDialog, false);
    }

    public static void Previous()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      pc.PreviousItem();
    }

    public static void Next()
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      if (pc == null)
        return;
      pc.NextItem();
    }

    public static void VolumeUp()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.VolumeUp();
    }

    public static void VolumeDown()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.VolumeDown();
    }

    public static void ToggleMute()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted ^= true;
    }

    public static void ToggleCurrentPlayer()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      playerContextManager.ToggleCurrentPlayer();
    }

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return PLAYER_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      IPlayerContext pc = GetCurrentPlayerContext();
      return pc != null && CanHandlePlayer(pc.CurrentPlayer);
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      if (newContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
        UpdateCurrentlyPlayingScreen();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      if (oldContext.WorkflowState.StateId == CURRENTLY_PLAYING_STATE_ID)
        _currentlyPlayingScreen = null;
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Not implemented
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Not implemented
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Not implemented
    }

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowAction> actions)
    {
      // Not implemented yet
    }

    #endregion
  }
}
