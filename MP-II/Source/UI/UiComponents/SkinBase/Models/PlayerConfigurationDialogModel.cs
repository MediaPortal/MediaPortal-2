#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;

namespace UiComponents.SkinBase.Models
{
  /// <summary>
  /// This model attends the dialogs "DialogPlayerConfiguration", "DialogChooseAudioStream", "DialogPlayerSlotAudio" and
  /// "DialogPlayerChooseGeometry".
  /// </summary>
  public class PlayerConfigurationDialogModel : BaseMessageControlledUIModel, IWorkflowModel
  {
    #region Consts

    public const string PLAYER_CONFIGURATION_DIALOG_MODEL_ID_STR = "58A7F9E3-1514-47af-8E83-2AD60BA8A037";
    public static Guid PLAYER_CONFIGURATION_DIALOG_MODEL_ID = new Guid(PLAYER_CONFIGURATION_DIALOG_MODEL_ID_STR);

    public const string CHOOSE_AUDIO_STREAM_DIALOG_STATE_ID_STR = "A3F53310-4D93-4f93-8B09-D53EE8ACD829";
    public static Guid CHOOSE_AUDIO_STREAM_DIALOG_STATE_ID = new Guid(CHOOSE_AUDIO_STREAM_DIALOG_STATE_ID_STR);
    public const string PLAYER_CONFIGURATION_DIALOG_STATE_ID_STR = "D0B79345-69DF-4870-B80E-39050434C8B3";
    public static Guid PLAYER_CONFIGURATION_DIALOG_STATE_ID = new Guid(PLAYER_CONFIGURATION_DIALOG_STATE_ID_STR);
    public const string PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID_STR = "428326CE-9DE1-41ff-A33B-BBB80C8AFAC5";
    public static Guid PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID = new Guid(PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID_STR);
    public const string PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG_STATE_ID_STR = "D46F66DD-9E91-4788-ADFE-EBD96F1A489E";
    public static Guid PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG_STATE_ID = new Guid(PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG_STATE_ID_STR);

    public const string KEY_PLAYER_SLOT = "PlayerSlot";
    public const string KEY_PLAYER_CONTEXT = "PlayerContext";
    public const string KEY_SHOW_MUTE = "ShowMute";

    protected const string KEY_NAME = "Name";

    protected const string PLAYER_OF_TYPE_RESOURCE = "[Players.PlayerOfType]";
    protected const string SLOT_NO_RESOURCE = "[Players.SlotNo]";
    protected const string FOCUS_PLAYER_RESOURCE = "[Players.FocusPlayer]";
    protected const string SWITCH_PIP_PLAYERS_RESOURCE = "[Players.SwitchPipPlayers]";
    protected const string CHOOSE_AUDIO_STREAM_RESOURCE = "[Players.ChooseAudioStream]";
    protected const string MUTE_RESOURCE = "[Players.Mute]";
    protected const string MUTE_OFF_RESOURCE = "[Players.MuteOff]";
    protected const string CLOSE_PLAYER_CONTEXT_RESOURCE = "[Players.ClosePlayerContext]";
    protected const string CHOOSE_PLAYER_GEOMETRY_RESOURCE = "[Players.ChoosePlayerGeometry]";

    protected const string PLAYER_SLOT_AUDIO_MENU_RESOURCE = "[Players.PlayerSlotAudioMenu]";

    #endregion

    #region Protected fields

    protected object _syncObj = new object();

    // Mode 1: Player configuration menu dialog
    protected bool _inPlayerConfigurationDialog = false;
    protected ItemsList _playerConfigurationMenu = new ItemsList();

    // Mode 2: Choose audio streams dialog
    protected bool _inChooseAudioStreamDialog = false;
    protected ItemsList _audioStreamsMenu = new ItemsList();

    // Mode 3: Audio menu for a special player slot
    protected bool _inPlayerSlotAudioMenuDialog = false;
    protected int _playerSlotAudioMenuSlotIndex = 0;
    protected bool _showToggleMute = true;
    protected ItemsList _playerSlotAudioMenu = new ItemsList();
    protected string _playerSlotAudioMenuHeader = null;

    // Mode 4: Choose zoom mode for a special player slot
    protected bool _inPlayerChooseGeometryMenuDialog = false;
    protected IPlayerContext _playerGeometryMenuPlayerContext = null;
    protected ItemsList _playerChooseGeometryMenu = new ItemsList();
    protected string _playerChooseGeometryHeader = null;

    #endregion

    public PlayerConfigurationDialogModel()
    {
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue.SubscribeToMessageChannel(PlayerManagerMessaging.CHANNEL);
      _messageQueue.SubscribeToMessageChannel(PlayerContextManagerMessaging.CHANNEL);
      _messageQueue.MessageReceived += OnMessageReceived;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType =
            (PlayerManagerMessaging.MessageType) message.MessageType;
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
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
      {
        PlayerContextManagerMessaging.MessageType messageType =
            (PlayerContextManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged:
            CheckUpdatePlayerConfigurationData();
            break;
        }
      }
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

    protected void UpdatePlayerConfigurationMenu()
    {
      // Some updates could be avoided if we tracked a "dirty" flag and break execution if !dirty
      lock (_syncObj)
      {
        IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        int numActiveSlots = playerManager.NumActiveSlots;
        IList<string> playerNames = new List<string>(2);
        for (int i = 0; i < numActiveSlots; i++)
          playerNames.Add(GetNameForPlayerContext(playerContextManager, i));

        _playerConfigurationMenu.Clear();
        // Change player focus
        if (numActiveSlots > 1)
        {
          // Set player focus
          int newCurrentPlayer = 1 - playerContextManager.CurrentPlayerIndex;
          string name = playerNames[newCurrentPlayer];
          if (name != null)
          {
            ListItem item = new ListItem(KEY_NAME, LocalizationHelper.CreateResourceString(FOCUS_PLAYER_RESOURCE).Evaluate(name))
              {
                  Command = new MethodDelegateCommand(() => SetCurrentPlayer(newCurrentPlayer))
              };
            _playerConfigurationMenu.Add(item);
          }
        }
        // Switch players
        if (numActiveSlots > 1 && playerContextManager.IsPipActive)
        {
          ListItem item = new ListItem(KEY_NAME, SWITCH_PIP_PLAYERS_RESOURCE)
            {
                Command = new MethodDelegateCommand(SwitchPrimarySecondaryPlayer)
            };
          _playerConfigurationMenu.Add(item);
        }
        // Change geometry
        IList<IPlayerContext> videoPCs = new List<IPlayerContext>();
        for (int i = 0; i < numActiveSlots; i++)
        {
          IPlayerContext pc = playerContextManager.GetPlayerContext(i);
          if (pc != null && pc.CurrentPlayer is IVideoPlayer)
            videoPCs.Add(pc);
        }
        for (int i = 0; i < videoPCs.Count; i++)
        {
          IPlayerContext pc = videoPCs[i];
          string zoomMode = LocalizationHelper.CreateResourceString(CHOOSE_PLAYER_GEOMETRY_RESOURCE).Evaluate();
          string entryName = videoPCs.Count > 1 ?
              string.Format("{0} ({1})", zoomMode, GetNameForPlayerContext(playerContextManager, i)) : zoomMode;
          ListItem item = new ListItem(KEY_NAME, entryName)
            {
              Command = new MethodDelegateCommand(() => OpenChooseGeometryDialog(pc))
            };
          _playerChooseGeometryHeader = entryName;
          _playerConfigurationMenu.Add(item);
        }
        // Audio streams
        ICollection<AudioStreamDescriptor> audioStreams = playerContextManager.GetAvailableAudioStreams();
        if (audioStreams.Count > 1)
        {
          ListItem item = new ListItem(KEY_NAME, CHOOSE_AUDIO_STREAM_RESOURCE)
            {
                Command = new MethodDelegateCommand(OpenChooseAudioStreamDialog)
            };
          _playerConfigurationMenu.Add(item);
        }
        // TODO: Handle subtitles same as audio streams
        // Mute
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
        // Close player
        for (int i = 0; i < numActiveSlots; i++)
        {
          string name = playerNames[i];
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
      }
    }

    protected void UpdateAudioStreamsMenu()
    {
      // Some updates could be avoided if we tracked a "dirty" flag and break execution if !dirty
      lock (_syncObj)
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();

        _audioStreamsMenu.Clear();
        for (int i = 0; i < playerManager.NumActiveSlots; i++)
        {
          IPlayerContext pc = playerContextManager.GetPlayerContext(i);
          IPlayer player = pc.CurrentPlayer;
          IList<AudioStreamDescriptor> asds = new List<AudioStreamDescriptor>(pc.GetAudioStreamDescriptors());
          foreach (AudioStreamDescriptor asd in asds)
          {
            string playedItem = player == null ? null : player.MediaItemTitle;
            if (playedItem == null)
              playedItem = pc.Name;
            string choiceItemName;
            if (asds.Count > 1)
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
      }
    }

    protected void UpdatePlayerSlotAudioMenu()
    {
      // Some updates could be avoided if we tracked a "dirty" flag and break execution if !dirty
      lock (_syncObj)
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();

        _playerSlotAudioMenu.Clear();
        IPlayerContext pc = playerContextManager.GetPlayerContext(_playerSlotAudioMenuSlotIndex);
        IPlayer player = pc.CurrentPlayer;
        IList<AudioStreamDescriptor> asds = new List<AudioStreamDescriptor>(pc.GetAudioStreamDescriptors());
        foreach (AudioStreamDescriptor asd in asds)
        {
          string playedItem = player == null ? null : player.MediaItemTitle;
          if (playedItem == null)
            playedItem = pc.Name;
          string choiceItemName;
          if (asds.Count > 1)
              // Only display the audio stream name if the player has more than one audio stream
            choiceItemName = playedItem + ": " + asd.AudioStreamName;
          else
            choiceItemName = playedItem;
          AudioStreamDescriptor asdClosureCopy = asd;
          ListItem item = new ListItem(KEY_NAME, choiceItemName)
            {
                Command = new MethodDelegateCommand(() => ChooseAudioStream(asdClosureCopy))
            };
          _playerSlotAudioMenu.Add(item);
        }
        if (_showToggleMute)
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
          _playerSlotAudioMenu.Add(item);
        }

        _playerSlotAudioMenu.FireChange();
        _playerSlotAudioMenuHeader = LocalizationHelper.CreateResourceString(PLAYER_SLOT_AUDIO_MENU_RESOURCE).Evaluate(GetNameForPlayerContext(playerContextManager, _playerSlotAudioMenuSlotIndex));
      }
    }

    protected void UpdatePlayerChooseGeometryMenu()
    {
      if (_playerChooseGeometryMenu.Count == 0)
      {
        IGeometryManager geometryManager = ServiceScope.Get<IGeometryManager>();
        foreach (KeyValuePair<string, IGeometry> nameToGeometry in geometryManager.AvailableGeometries)
        {
          IGeometry geometry = nameToGeometry.Value;
          ListItem item = new ListItem(KEY_NAME, nameToGeometry.Key)
            {
                Command = new MethodDelegateCommand(() => SetGeometry(_playerGeometryMenuPlayerContext, geometry))
            };
          _playerChooseGeometryMenu.Add(item);
        }
      }
    }

    /// <summary>
    /// Updates the menu items for the dialogs "DialogPlayerConfiguration" and "DialogChooseAudioStream"
    /// and closes the dialogs when their entries are not valid any more.
    /// </summary>
    protected void CheckUpdatePlayerConfigurationData()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();

      lock (_syncObj)
      {
        if (_inPlayerConfigurationDialog)
        {
          UpdatePlayerConfigurationMenu();
          if (_playerConfigurationMenu.Count == 0)
          {
            // Automatically close player configuration dialog if no menu items are available any more
            while (_inPlayerConfigurationDialog)
              workflowManager.NavigatePop(1);
          }
        }
        if (_inChooseAudioStreamDialog)
        {
          UpdateAudioStreamsMenu();
          if (_audioStreamsMenu.Count <= 1)
          {
            // Automatically close audio stream choice dialog if less than two audio streams are available
            while (_inChooseAudioStreamDialog)
              workflowManager.NavigatePop(1);
          }
        }
        if (_inPlayerConfigurationDialog)
        {
          UpdatePlayerSlotAudioMenu();
          if (_playerSlotAudioMenu.Count <= 1)
          {
            // Automatically close audio stream choice dialog if less than two audio streams are available
            while (_inPlayerSlotAudioMenuDialog)
              workflowManager.NavigatePop(1);
          }
        }
        if (_inPlayerChooseGeometryMenuDialog)
        {
          // Automatically close geometry choice dialog if current player is no video player
          if (_playerGeometryMenuPlayerContext == null || !_playerGeometryMenuPlayerContext.IsValid ||
              !(_playerGeometryMenuPlayerContext.CurrentPlayer is IVideoPlayer))
            // Automatically close audio stream choice dialog
            while (_inPlayerChooseGeometryMenuDialog)
              workflowManager.NavigatePop(1);
          else
            UpdatePlayerChooseGeometryMenu();
        }
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

    public void EnterContext(NavigationContext newContext)
    {
      if (newContext.WorkflowState.StateId == PLAYER_CONFIGURATION_DIALOG_STATE_ID)
      {
        UpdatePlayerConfigurationMenu();
        _inPlayerConfigurationDialog = true;
      }
      else if (newContext.WorkflowState.StateId == CHOOSE_AUDIO_STREAM_DIALOG_STATE_ID)
      {
        UpdateAudioStreamsMenu();
        _inChooseAudioStreamDialog = true;
      }
      else if (newContext.WorkflowState.StateId == PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID)
      {
        int? slotIndex = newContext.GetContextVariable(KEY_PLAYER_SLOT, false) as int?;
        if (slotIndex.HasValue)
          _playerSlotAudioMenuSlotIndex = slotIndex.Value;
        else
          _playerSlotAudioMenuSlotIndex = 0;
        bool? showToggleMute = newContext.GetContextVariable(KEY_SHOW_MUTE, false) as bool?;
        if (showToggleMute.HasValue)
          _showToggleMute = showToggleMute.Value;
        else
          _showToggleMute = true;
        UpdatePlayerSlotAudioMenu();
        _inPlayerSlotAudioMenuDialog = true;
      }
      else if (newContext.WorkflowState.StateId == PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG_STATE_ID)
      {
        _playerGeometryMenuPlayerContext = newContext.GetContextVariable(KEY_PLAYER_CONTEXT, false) as IPlayerContext;
        UpdatePlayerChooseGeometryMenu();
        _inPlayerChooseGeometryMenuDialog = true;
      }
    }

    public void ExitContext(NavigationContext oldContext)
    {
      if (oldContext.WorkflowState.StateId == PLAYER_CONFIGURATION_DIALOG_STATE_ID)
      {
        _inPlayerConfigurationDialog = false;
        _playerConfigurationMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == CHOOSE_AUDIO_STREAM_DIALOG_STATE_ID)
      {
        _inChooseAudioStreamDialog = false;
        _audioStreamsMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID)
      {
        _inPlayerSlotAudioMenuDialog = false;
        _playerSlotAudioMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG_STATE_ID)
      {
        _inPlayerChooseGeometryMenuDialog = false;
        _playerChooseGeometryMenu.Clear();
      }
    }

    #region Members to be accessed from the GUI

    public ItemsList PlayerConfigurationMenu
    {
      get { return _playerConfigurationMenu; }
    }

    public ItemsList AudioStreamsMenu
    {
      get { return _audioStreamsMenu; }
    }

    public ItemsList PlayerSlotAudioMenu
    {
      get { return _playerSlotAudioMenu; }
    }

    public string PlayerSlotAudioMenuHeader
    {
      get { return _playerSlotAudioMenuHeader; }
    }

    public ItemsList PlayerChooseGeometryMenu
    {
      get { return _playerChooseGeometryMenu; }
    }

    public string PlayerChooseGeometryHeader
    {
      get { return _playerChooseGeometryHeader; }
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

    public void OpenChooseAudioStreamDialog()
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(CHOOSE_AUDIO_STREAM_DIALOG_STATE_ID, null);
    }

    public void ChooseAudioStream(AudioStreamDescriptor asd)
    {
      IPlayerContextManager playerContextManager = ServiceScope.Get<IPlayerContextManager>();
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerContextManager.SetAudioStream(asd);
      playerManager.Muted = false;
    }

    public void OpenChooseGeometryDialog(IPlayerContext playerContext)
    {
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG_STATE_ID, null, new Dictionary<string, object>
        {
            {KEY_PLAYER_CONTEXT, playerContext}
        });
    }

    public void SetGeometry(IPlayerContext playerContext, IGeometry geometry)
    {
      if (playerContext != null)
        playerContext.OverrideGeometry(geometry);
    }

    #endregion

    #region IWorkflowModel implementation

    public override Guid ModelId
    {
      get { return PLAYER_CONFIGURATION_DIALOG_MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      if (newContext.WorkflowState.StateId == PLAYER_CONFIGURATION_DIALOG_STATE_ID)
      {
        UpdatePlayerConfigurationMenu();
        return _playerConfigurationMenu.Count > 0;
      }
      else if (newContext.WorkflowState.StateId == CHOOSE_AUDIO_STREAM_DIALOG_STATE_ID)
      {
        UpdateAudioStreamsMenu();
        return _audioStreamsMenu.Count > 0;
      }
      else if (newContext.WorkflowState.StateId == PLAYER_SLOT_AUDIO_MENU_DIALOG_STATE_ID)
      {
        // Check if we got our necessary player slot parameter
        if (newContext.GetContextVariable(KEY_PLAYER_SLOT, false) != null)
          return true;
      }
      else if (newContext.WorkflowState.StateId == PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG_STATE_ID)
      {
        // Check if we got our necessary player context parameter
        if (newContext.GetContextVariable(KEY_PLAYER_CONTEXT, false) != null)
          return true;
      }
      return false;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      EnterContext(newContext);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      ExitContext(oldContext);
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      if (!push)
        ExitContext(oldContext);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do here
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}