#region Copyright (C) 2007-2010 Team MediaPortal

/*
    Copyright (C) 2007-2010 Team MediaPortal
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
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.Core.Localization;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// This model attends the dialogs "DialogPlayerConfiguration", "DialogChooseAudioStream", "DialogPlayerSlotAudio" and
  /// "DialogPlayerChooseGeometry".
  /// </summary>
  public class PlayerConfigurationDialogModel : IDisposable, IWorkflowModel
  {
    #region Enums

    enum NavigationMode
    {
      SimpleChoice,
      SuccessorDialog,
      ExitPCWorkflow,
    }

    #endregion

    #region Consts

    public const string STR_MODEL_ID_PLAYER_CONFIGURATION_DIALOG = "58A7F9E3-1514-47af-8E83-2AD60BA8A037";
    public static Guid MODEL_ID_PLAYER_CONFIGURATION_DIALOG = new Guid(STR_MODEL_ID_PLAYER_CONFIGURATION_DIALOG);

    public const string STR_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG = "A3F53310-4D93-4f93-8B09-D53EE8ACD829";
    public static Guid STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG = new Guid(STR_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG);
    public const string STR_STATE_ID_PLAYER_CONFIGURATION_DIALOG = "D0B79345-69DF-4870-B80E-39050434C8B3";
    public static Guid STATE_ID_PLAYER_CONFIGURATION_DIALOG = new Guid(STR_STATE_ID_PLAYER_CONFIGURATION_DIALOG);
    public const string STR_STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG = "428326CE-9DE1-41ff-A33B-BBB80C8AFAC5";
    public static Guid STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG = new Guid(STR_STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG);
    public const string STR_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG = "D46F66DD-9E91-4788-ADFE-EBD96F1A489E";
    public static Guid STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG = new Guid(STR_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG);

    public const string KEY_PLAYER_SLOT = "PlayerSlot";
    public const string KEY_PLAYER_CONTEXT = "PlayerContext";
    public const string KEY_SHOW_MUTE = "ShowMute";

    protected const string KEY_NAME = "Name";
    protected const string KEY_NAVIGATION_MODE = "NavigationMode";

    protected const string RES_PLAYER_OF_TYPE = "[Players.PlayerOfType]";
    protected const string RES_SLOT_NO = "[Players.SlotNo]";
    protected const string RES_FOCUS_PLAYER = "[Players.FocusPlayer]";
    protected const string RES_SWITCH_PIP_PLAYERS = "[Players.SwitchPipPlayers]";
    protected const string RES_CHOOSE_AUDIO_STREAM = "[Players.ChooseAudioStream]";
    protected const string RES_MUTE = "[Players.Mute]";
    protected const string RES_MUTE_OFF = "[Players.MuteOff]";
    protected const string RES_CLOSE_PLAYER_CONTEXT = "[Players.ClosePlayerContext]";
    protected const string RES_CHOOSE_PLAYER_GEOMETRY = "[Players.ChoosePlayerGeometry]";

    protected const string RES_PLAYER_SLOT_AUDIO_MENU = "[Players.PlayerSlotAudioMenu]";

    #endregion

    #region Protected fields

    protected object _syncObj = new object();

    protected AsynchronousMessageQueue _messageQueue;

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

    #region Ctor & dtor

    public PlayerConfigurationDialogModel()
    {
      InitializeMessageQueue();
    }

    public void Dispose()
    {
      _messageQueue.Shutdown();
    }

    #endregion

    #region Private & protected members

    private void InitializeMessageQueue()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
            PlayerManagerMessaging.CHANNEL,
            PlayerContextManagerMessaging.CHANNEL,
        });
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

    /// <summary>
    /// Gets a name for a player context.
    /// </summary>
    /// <param name="playerContextManager">Player context manager.</param>
    /// <param name="playerSlot">Number of the player slot, <c>0</c> to <c>1</c>.</param>
    /// <returns></returns>
    protected static string GetNameForPlayerContext(IPlayerContextManager playerContextManager, int playerSlot)
    {
      IPlayerContext pc = playerContextManager.GetPlayerContext(playerSlot);
      if (pc == null)
        return null;
      IPlayer player = pc.CurrentPlayer;
      if (player == null)
      {
        IResourceString playerOfType = LocalizationHelper.CreateResourceString(RES_PLAYER_OF_TYPE); // "{0} player"
        IResourceString slotNo = LocalizationHelper.CreateResourceString(RES_SLOT_NO); // "Slot #{0}"
        return playerOfType.Evaluate(pc.AVType.ToString()) + " (" + slotNo.Evaluate((playerSlot + 1).ToString()) + ")"; // "Video player (Slot #1)"
      }
      return player.Name + ": " + player.MediaItemTitle;
    }

    protected void UpdatePlayerConfigurationMenu()
    {
      // Some updates could be avoided if we tracked a "dirty" flag and break execution if !dirty
      lock (_syncObj)
      {
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
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
            ListItem item = new ListItem(KEY_NAME, LocalizationHelper.CreateResourceString(RES_FOCUS_PLAYER).Evaluate(name))
              {
                  Command = new MethodDelegateCommand(() => SetCurrentPlayer(newCurrentPlayer))
              };
            item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.SimpleChoice;
            _playerConfigurationMenu.Add(item);
          }
        }
        // Switch players
        if (numActiveSlots > 1 && playerContextManager.IsPipActive)
        {
          ListItem item = new ListItem(KEY_NAME, RES_SWITCH_PIP_PLAYERS)
            {
                Command = new MethodDelegateCommand(SwitchPrimarySecondaryPlayer)
            };
          item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.SimpleChoice;
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
          string zoomMode = LocalizationHelper.CreateResourceString(RES_CHOOSE_PLAYER_GEOMETRY).Evaluate();
          string entryName = videoPCs.Count > 1 ?
              string.Format("{0} ({1})", zoomMode, GetNameForPlayerContext(playerContextManager, i)) : zoomMode;
          ListItem item = new ListItem(KEY_NAME, entryName)
            {
              Command = new MethodDelegateCommand(() => OpenChooseGeometryDialog(pc))
            };
          item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.SuccessorDialog;
          _playerChooseGeometryHeader = entryName;
          _playerConfigurationMenu.Add(item);
        }
        // Audio streams
        ICollection<AudioStreamDescriptor> audioStreams = playerContextManager.GetAvailableAudioStreams();
        if (audioStreams.Count > 1)
        {
          ListItem item = new ListItem(KEY_NAME, RES_CHOOSE_AUDIO_STREAM)
            {
                Command = new MethodDelegateCommand(OpenChooseAudioStreamDialog)
            };
          item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.SuccessorDialog;
          _playerConfigurationMenu.Add(item);
        }
        // Mute
        if (numActiveSlots > 0)
        {
          ListItem item;
          if (playerManager.Muted)
            item = new ListItem(KEY_NAME, RES_MUTE_OFF)
              {
                  Command = new MethodDelegateCommand(PlayersResetMute)
              };
          else
            item = new ListItem(KEY_NAME, RES_MUTE)
              {
                  Command = new MethodDelegateCommand(PlayersMute)
              };
          item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.SimpleChoice;
          _playerConfigurationMenu.Add(item);
        }
        // Close player
        for (int i = 0; i < numActiveSlots; i++)
        {
          string name = playerNames[i];
          if (name != null)
          {
            int indexClosureCopy = i;
            ListItem item = new ListItem(KEY_NAME, LocalizationHelper.CreateResourceString(RES_CLOSE_PLAYER_CONTEXT).Evaluate(name))
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
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();

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
            item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
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
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();

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
          item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
          _playerSlotAudioMenu.Add(item);
        }
        if (_showToggleMute)
        {
          ListItem item;
          if (playerManager.Muted)
            item = new ListItem(KEY_NAME, RES_MUTE_OFF)
              {
                  Command = new MethodDelegateCommand(PlayersResetMute)
              };
          else
            item = new ListItem(KEY_NAME, RES_MUTE)
              {
                  Command = new MethodDelegateCommand(PlayersMute)
              };
          item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.SimpleChoice;
          _playerSlotAudioMenu.Add(item);
        }

        _playerSlotAudioMenu.FireChange();
        _playerSlotAudioMenuHeader = LocalizationHelper.CreateResourceString(RES_PLAYER_SLOT_AUDIO_MENU).Evaluate(GetNameForPlayerContext(playerContextManager, _playerSlotAudioMenuSlotIndex));
      }
    }

    protected void UpdatePlayerChooseGeometryMenu()
    {
      if (_playerChooseGeometryMenu.Count == 0)
      {
        IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
        foreach (KeyValuePair<string, IGeometry> nameToGeometry in geometryManager.AvailableGeometries)
        {
          IGeometry geometry = nameToGeometry.Value;
          ListItem item = new ListItem(KEY_NAME, nameToGeometry.Key)
            {
                Command = new MethodDelegateCommand(() => SetGeometry(_playerGeometryMenuPlayerContext, geometry))
            };
          item.AdditionalProperties[KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
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
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();

      lock (_syncObj)
      {
        if (_inPlayerConfigurationDialog)
        {
          UpdatePlayerConfigurationMenu();
          if (_playerConfigurationMenu.Count == 0)
            // Automatically close player configuration dialog if no menu items are available any more
            workflowManager.NavigatePopToState(STATE_ID_PLAYER_CONFIGURATION_DIALOG, true);
        }
        if (_inChooseAudioStreamDialog)
        {
          UpdateAudioStreamsMenu();
          if (_audioStreamsMenu.Count <= 1)
            // Automatically close audio stream choice dialog if less than two audio streams are available
            workflowManager.NavigatePopToState(STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG, true);
        }
        if (_inPlayerSlotAudioMenuDialog)
        {
          UpdatePlayerSlotAudioMenu();
          if (_playerSlotAudioMenu.Count <= 1)
            // Automatically close audio stream choice dialog if less than two audio streams are available
            workflowManager.NavigatePopToState(STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG, true);
        }
        if (_inPlayerChooseGeometryMenuDialog)
        {
          // Automatically close geometry choice dialog if current player is no video player
          if (_playerGeometryMenuPlayerContext == null || !_playerGeometryMenuPlayerContext.IsValid ||
              !(_playerGeometryMenuPlayerContext.CurrentPlayer is IVideoPlayer))
            // Automatically close audio stream choice dialog
            workflowManager.NavigatePopToState(STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG, true);
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
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      return pcm.GetPlayerContext(PlayerChoice.CurrentPlayer);
    }

    protected void EnterContext(NavigationContext newContext)
    {
      _messageQueue.Start();
      if (newContext.WorkflowState.StateId == STATE_ID_PLAYER_CONFIGURATION_DIALOG)
      {
        UpdatePlayerConfigurationMenu();
        _inPlayerConfigurationDialog = true;
      }
      else if (newContext.WorkflowState.StateId == STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG)
      {
        UpdateAudioStreamsMenu();
        _inChooseAudioStreamDialog = true;
      }
      else if (newContext.WorkflowState.StateId == STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG)
      {
        int? slotIndex = newContext.GetContextVariable(KEY_PLAYER_SLOT, false) as int?;
        _playerSlotAudioMenuSlotIndex = slotIndex ?? 0;
        bool? showToggleMute = newContext.GetContextVariable(KEY_SHOW_MUTE, false) as bool?;
        _showToggleMute = showToggleMute ?? true;
        UpdatePlayerSlotAudioMenu();
        _inPlayerSlotAudioMenuDialog = true;
      }
      else if (newContext.WorkflowState.StateId == STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG)
      {
        _playerGeometryMenuPlayerContext = newContext.GetContextVariable(KEY_PLAYER_CONTEXT, false) as IPlayerContext;
        UpdatePlayerChooseGeometryMenu();
        _inPlayerChooseGeometryMenuDialog = true;
      }
    }

    protected void ExitContext(NavigationContext oldContext)
    {
      _messageQueue.Shutdown();
      if (oldContext.WorkflowState.StateId == STATE_ID_PLAYER_CONFIGURATION_DIALOG)
      {
        _inPlayerConfigurationDialog = false;
        _playerConfigurationMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG)
      {
        _inChooseAudioStreamDialog = false;
        _audioStreamsMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG)
      {
        _inPlayerSlotAudioMenuDialog = false;
        _playerSlotAudioMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG)
      {
        _inPlayerChooseGeometryMenuDialog = false;
        _playerChooseGeometryMenu.Clear();
      }
    }

    #endregion

    #region Public members to be called from other modules

    public static void OpenChooseGeometryDialog(IPlayerContext playerContext)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG, new NavigationContextConfig
        {
          AdditionalContextVariables = new Dictionary<string, object>
          {
              {KEY_PLAYER_CONTEXT, playerContext}
          }
        });
    }

    public static void OpenPlayerConfigurationDialog()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(STATE_ID_PLAYER_CONFIGURATION_DIALOG);
    }

    public static void OpenAudioMenuDialog(int slotIndex, bool showMute)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG, new NavigationContextConfig
        {
          AdditionalContextVariables = new Dictionary<string, object>
            {
                {KEY_PLAYER_SLOT, slotIndex},
                {KEY_SHOW_MUTE, showMute}
            }
        });
    }

    #endregion

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

    public void Select(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
      object obj;
      NavigationMode mode = NavigationMode.SimpleChoice;
      if (item.AdditionalProperties.TryGetValue(KEY_NAVIGATION_MODE, out obj))
        mode = (NavigationMode) obj;
      switch (mode)
      {
        case NavigationMode.SimpleChoice:
          IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
          screenManager.CloseTopmostDialog();
          break;
        case NavigationMode.ExitPCWorkflow:
          IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
          workflowManager.NavigatePopToState(STATE_ID_PLAYER_CONFIGURATION_DIALOG, true);
          break;
        default:
          break;
      }
    }

    public void SetCurrentPlayer(int playerIndex)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.CurrentPlayerIndex = playerIndex;
    }

    public void ClosePlayerContext(int playerIndex)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.CloseSlot(playerIndex);
    }

    public void PlayersMute()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.Muted = true;
    }

    public void PlayersResetMute()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.Muted = false;
    }

    public void SwitchPrimarySecondaryPlayer()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerManager.SwitchSlots();
    }

    public void OpenChooseAudioStreamDialog()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG);
    }

    public void ChooseAudioStream(AudioStreamDescriptor asd)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      playerContextManager.SetAudioStream(asd);
      playerManager.Muted = false;
    }

    public void SetGeometry(IPlayerContext playerContext, IGeometry geometry)
    {
      if (playerContext != null)
        playerContext.OverrideGeometry(geometry);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_PLAYER_CONFIGURATION_DIALOG; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      if (newContext.WorkflowState.StateId == STATE_ID_PLAYER_CONFIGURATION_DIALOG)
      {
        UpdatePlayerConfigurationMenu();
        return _playerConfigurationMenu.Count > 0;
      }
      else if (newContext.WorkflowState.StateId == STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG)
      {
        UpdateAudioStreamsMenu();
        return _audioStreamsMenu.Count > 0;
      }
      else if (newContext.WorkflowState.StateId == STATE_ID_PLAYER_SLOT_AUDIO_MENU_DIALOG)
      {
        // Check if we got our necessary player slot parameter
        if (newContext.GetContextVariable(KEY_PLAYER_SLOT, false) != null)
          return true;
      }
      else if (newContext.WorkflowState.StateId == STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG)
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
      if (push)
        EnterContext(newContext);
      else
        ExitContext(oldContext);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here: We don't stop the message queue in our sub states, so we don't need to update our properties again
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here: We didn't stop the message queue in our sub states, so we don't need to update our properties again
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
