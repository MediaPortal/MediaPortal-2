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
using MediaPortal.Core;
using MediaPortal.Core.Commands;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.DataObjects;
using MediaPortal.Presentation.Localization;
using MediaPortal.Presentation.Models;
using MediaPortal.Presentation.Players;
using MediaPortal.Presentation.Screens;

namespace UiComponents.SkinBase
{
  /// <summary>
  /// This model attends the currently-playing and fullscreen-content workflow states for
  /// Video, Audio and Image media players. It is also used as data model for some dialogs to configure
  /// the players like "DialogPlayerConfiguration" and "DialogChooseAudioStream".
  /// </summary>
  public class PlayerModel : BaseTimerControlledUIModel
  {
    public const string PLAYER_MODEL_ID_STR = "A2F24149-B44C-498b-AE93-288213B87A1A";
    public static Guid PLAYER_MODEL_ID = new Guid(PLAYER_MODEL_ID_STR);

    public const string CHOOSE_AUDIO_STREAM_DIALOG_NAME = "DialogChooseAudioStream";
    public const string PLAYER_CONFIGURATION_DIALOG_NAME = "DialogPlayerConfiguration";

    protected const string KEY_NAME = "Name";

    protected const string PLAYER_OF_TYPE_RESOURCE = "[Players.PlayerOfType]";
    protected const string SLOT_NO_RESOURCE = "[Players.SlotNo]";
    protected const string FOCUS_PLAYER_RESOURCE = "[Players.FocusPlayer]";
    protected const string SWITCH_PIP_PLAYERS_RESOURCE = "[Players.SwitchPipPlayers]";
    protected const string CHOOSE_AUDIO_STREAM_RESOURCE = "[Players.ChooseAudioStream]";
    protected const string MUTE_RESOURCE = "[Players.Mute]";
    protected const string MUTE_OFF_RESOURCE = "[Players.MuteOff]";
    protected const string CLOSE_PLAYER_CONTEXT_RESOURCE = "[Players.ClosePlayerContext]";

    protected Property _isPipVisibleProperty;

    protected ItemsList _playerConfigurationMenu = new ItemsList();
    protected ItemsList _audioStreamsMenu = new ItemsList();
    protected object _syncObj = new object();

    public PlayerModel() : base(100)
    {
      _isPipVisibleProperty = new Property(typeof(bool), false);

      Update();
      CheckUpdatePlayerConfigurationData();
      SubscribeToMessages();
    }

    protected override void SubscribeToMessages()
    {
      base.SubscribeToMessages();
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived_Async += OnPlayerManagerMessageReceived;
      broker.GetOrCreate(PlayerContextManagerMessaging.QUEUE).MessageReceived_Async += OnPlayerContextManagerMessageReceived;
    }

    protected override void UnsubscribeFromMessages()
    {
      base.UnsubscribeFromMessages();
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived_Async -= OnPlayerManagerMessageReceived;
      broker.GetOrCreate(PlayerContextManagerMessaging.QUEUE).MessageReceived_Async -= OnPlayerContextManagerMessageReceived;
    }

    protected void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PlayerSlotActivated:
        case PlayerManagerMessaging.MessageType.PlayerSlotDeactivated:
        case PlayerManagerMessaging.MessageType.PlayerStarted:
        case PlayerManagerMessaging.MessageType.PlayerStopped:
        case PlayerManagerMessaging.MessageType.PlayersMuted:
        case PlayerManagerMessaging.MessageType.PlayersResetMute:
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

    protected override void Update()
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
    /// Updates the menu items for the dialogs "DialogPlayerConfiguration" and "DialogChooseAudioStream"
    /// and closes the dialogs when their entries are not valid any more.
    /// </summary>
    protected void CheckUpdatePlayerConfigurationData()
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();

      lock (_syncObj)
      {
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
                Command = new MethodDelegateCommand(PlayersResetMute)
              };
          else
            item = new ListItem(KEY_NAME, MUTE_RESOURCE)
              {
                Command = new MethodDelegateCommand(PlayersMute)
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

    public override Guid ModelId
    {
      get { return PLAYER_MODEL_ID; }
    }

    #region Members to be accessed from the GUI

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

    public void PlayersMute()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted = true;
    }

    public void PlayersResetMute()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.Muted = false;
    }

    public void SwitchPrimarySecondaryPlayer()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.SwitchSlots();
    }

    public void OpenPlayerConfigurationDialog()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.ShowDialog(PLAYER_CONFIGURATION_DIALOG_NAME);
    }

    public void OpenChooseAudioStreamDialog()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.ShowDialog(CHOOSE_AUDIO_STREAM_DIALOG_NAME);
    }

    public void ChooseAudioStream(AudioStreamDescriptor asd)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerContextManager.SetAudioStream(asd);
      playerManager.Muted = false;
    }

    #endregion

    #region Methods for general play controls

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

    #endregion
  }
}
