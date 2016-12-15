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
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Geometries;
using MediaPortal.Common.Localization;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UiComponents.SkinBase.General;

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
      SuccessorDialog,
      ExitPCWorkflow,
    }

    #endregion

    #region Consts

    public const string STR_MODEL_ID_PLAYER_CONFIGURATION_DIALOG = "58A7F9E3-1514-47af-8E83-2AD60BA8A037";
    public static Guid MODEL_ID_PLAYER_CONFIGURATION_DIALOG = new Guid(STR_MODEL_ID_PLAYER_CONFIGURATION_DIALOG);

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

    protected IPlayerContext _playerAudioMenuPlayerContext = null;
    protected bool _showToggleMute = true;
    protected ItemsList _playerSlotAudioMenu = new ItemsList();
    protected string _playerSlotAudioMenuHeader = null;

    // Mode 4: Choose zoom mode for a special player slot
    protected bool _inPlayerChooseGeometryMenuDialog = false;
    protected IPlayerContext _playerGeometryMenuPlayerContext = null;
    protected ItemsList _playerChooseGeometryMenu = new ItemsList();
    protected string _playerChooseGeometryHeader = null;

    // Mode 5: Choose rendering effect for a special player slot
    protected bool _inPlayerChooseEffectMenuDialog = false;
    protected IPlayerContext _playerEffectMenuPlayerContext = null;
    protected ItemsList _playerChooseEffectMenu = new ItemsList();
    protected string _playerChooseEffectHeader = null;

    // Mode 6: Choose subtitle for a special player slot
    protected bool _inPlayerChooseSubtitleMenuDialog = false;
    protected IPlayerContext _playerSubtitleMenuPlayerContext = null;
    protected ItemsList _playerChooseSubtitleMenu = new ItemsList();
    protected string _playerChooseSubtitleHeader = null;

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
          case PlayerManagerMessaging.MessageType.PlayerSlotStarted:
          case PlayerManagerMessaging.MessageType.PlayerSlotClosed:
          case PlayerManagerMessaging.MessageType.PlayerStarted:
          case PlayerManagerMessaging.MessageType.PlayerStopped:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
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
          case PlayerContextManagerMessaging.MessageType.PlayerSlotsChanged:
            CheckUpdatePlayerConfigurationData();
            break;
        }
      }
    }

    /// <summary>
    /// Gets a name for a player context.
    /// </summary>
    /// <param name="playerContext">Player context to return the name for.</param>
    /// <param name="slotNo">Number of the player slot of the given <paramref name="playerContext"/>.</param>
    /// <returns>Human readable name for the given <paramref name="playerContext"/>.</returns>
    protected static string GetNameForPlayerContext(IPlayerContext playerContext, int slotNo)
    {
      IPlayer player = playerContext.CurrentPlayer;
      if (player == null)
      {
        IResourceString playerOfType = LocalizationHelper.CreateResourceString(Consts.RES_PLAYER_OF_TYPE); // "{0} player"
        IResourceString slotNoRes = LocalizationHelper.CreateResourceString(Consts.RES_SLOT_NO); // "Slot #{0}"
        return playerOfType.Evaluate(playerContext.AVType.ToString()) + " (" + slotNoRes.Evaluate((slotNo + 1).ToString()) + ")"; // "Video player (Slot #1)"
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
        int numActivePlayers = playerContextManager.NumActivePlayerContexts;
        int slotNo = 0;
        IList<IPlayerContext> playerContexts = playerContextManager.PlayerContexts;
        IList<string> playerNames = new List<string>(playerContexts.Select(pc => GetNameForPlayerContext(pc, slotNo++)));

        _playerConfigurationMenu.Clear();
        // Change player focus
        if (numActivePlayers > 1)
        {
          // Set player focus
          int newCurrentPlayer = 1 - playerContextManager.CurrentPlayerIndex;
          string name = playerNames[newCurrentPlayer];
          if (name != null)
          {
            ListItem item = new ListItem(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(Consts.RES_FOCUS_PLAYER).Evaluate(name))
              {
                  Command = new MethodDelegateCommand(() => SetCurrentPlayer(newCurrentPlayer))
              };
            item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
            _playerConfigurationMenu.Add(item);
          }
        }
        // Switch players
        if (numActivePlayers > 1 && playerContextManager.IsPipActive)
        {
          ListItem item = new ListItem(Consts.KEY_NAME, Consts.RES_SWITCH_PIP_PLAYERS)
            {
                Command = new MethodDelegateCommand(SwitchPrimarySecondaryPlayer)
            };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
          _playerConfigurationMenu.Add(item);
        }
        // Change geometry
        IList<IPlayerContext> videoPCs = GetVideoPlayerContexts();
        for (int i = 0; i < videoPCs.Count; i++)
        {
          IPlayerContext pc = videoPCs[i];
          string geometry = LocalizationHelper.CreateResourceString(Consts.RES_CHOOSE_PLAYER_GEOMETRY).Evaluate();
          string entryName = videoPCs.Count > 1 ?
              string.Format("{0} ({1})", geometry, GetNameForPlayerContext(pc, i)) : geometry;
          ListItem item = new ListItem(Consts.KEY_NAME, entryName)
            {
              Command = new MethodDelegateCommand(() => OpenChooseGeometryDialog(pc))
            };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.SuccessorDialog;
          _playerConfigurationMenu.Add(item);
        }
        // Change rendering effect
        for (int i = 0; i < videoPCs.Count; i++)
        {
          IPlayerContext pc = videoPCs[i];
          string effect = LocalizationHelper.CreateResourceString(Consts.RES_CHOOSE_PLAYER_EFFECT).Evaluate();
          string entryName = videoPCs.Count > 1 ?
              string.Format("{0} ({1})", effect, GetNameForPlayerContext(pc, i)) : effect;
          ListItem item = new ListItem(Consts.KEY_NAME, entryName)
          {
            Command = new MethodDelegateCommand(() => OpenChooseEffectDialog(pc))
          };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.SuccessorDialog;
          _playerConfigurationMenu.Add(item);
        }
        // Subtitle streams
        for (int i = 0; i < videoPCs.Count; i++)
        {
          IPlayerContext pc = videoPCs[i];
          string sub = LocalizationHelper.CreateResourceString(Consts.RES_CHOOSE_PLAYER_SUBTITLE).Evaluate();
          string entryName = videoPCs.Count > 1 ?
              string.Format("{0} ({1})", sub, GetNameForPlayerContext(pc, i)) : sub;
          ListItem item = new ListItem(Consts.KEY_NAME, entryName)
          {
            Command = new MethodDelegateCommand(() => OpenChooseSubtitleDialog(pc))
          };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.SuccessorDialog;
          _playerConfigurationMenu.Add(item);
        }
        // Audio streams
        AudioStreamDescriptor currentAudioStream;
        ICollection<AudioStreamDescriptor> audioStreams = playerContextManager.GetAvailableAudioStreams(out currentAudioStream);
        if (audioStreams.Count > 1)
        {
          ListItem item = new ListItem(Consts.KEY_NAME, Consts.RES_CHOOSE_AUDIO_STREAM)
            {
                Command = new MethodDelegateCommand(OpenChooseAudioStreamDialog)
            };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.SuccessorDialog;
          _playerConfigurationMenu.Add(item);
        }
        // Mute
        if (numActivePlayers > 0)
        {
          ListItem item;
          if (playerManager.Muted)
            item = new ListItem(Consts.KEY_NAME, Consts.RES_MUTE_OFF)
              {
                  Command = new MethodDelegateCommand(PlayersResetMute)
              };
          else
            item = new ListItem(Consts.KEY_NAME, Consts.RES_MUTE)
              {
                  Command = new MethodDelegateCommand(PlayersMute)
              };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
          _playerConfigurationMenu.Add(item);
        }
        // Close player
        for (int i = 0; i < numActivePlayers; i++)
        {
          string name = numActivePlayers > 1 ? playerNames[i] : string.Empty;
          IPlayerContext pc = playerContexts[i];
          if (name != null)
          {
            ListItem item = new ListItem(Consts.KEY_NAME, LocalizationHelper.CreateResourceString(Consts.RES_CLOSE_PLAYER_CONTEXT).Evaluate(name))
              {
                  Command = new MethodDelegateCommand(pc.Close)
              };
            _playerConfigurationMenu.Add(item);
          }
        }
        _playerConfigurationMenu.FireChange();
      }
    }

    protected IList<IPlayerContext> GetVideoPlayerContexts()
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      IList<IPlayerContext> videoPCs = new List<IPlayerContext>();
      for (int i = 0; i < pcm.NumActivePlayerContexts; i++)
      {
        IPlayerContext pc = pcm.GetPlayerContext(i);
        if (pc != null && pc.CurrentPlayer is IVideoPlayer)
          videoPCs.Add(pc);
      }
      return videoPCs;
    }

    protected int GetTotalNumberOfAudioStreams()
    {
      int numberOfAudioStreams = 0;
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      for (int i = 0; i < playerContextManager.NumActivePlayerContexts; i++)
      {
        IPlayerContext pc = playerContextManager.GetPlayerContext(i);
        if (pc == null || !pc.IsActive)
          continue;
        AudioStreamDescriptor currentAudioStream;
        numberOfAudioStreams += pc.GetAudioStreamDescriptors(out currentAudioStream).Count;
      }
      return numberOfAudioStreams;
    }

    protected void UpdateAudioStreamsMenu()
    {
      // Some updates could be avoided if we tracked a "dirty" flag and break execution if !dirty
      lock (_syncObj)
      {
        IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
        int numActivePlayers = playerContextManager.NumActivePlayerContexts;
        int numberOfAudioStreams = GetTotalNumberOfAudioStreams();

        _audioStreamsMenu.Clear();
        for (int i = 0; i < numActivePlayers; i++)
        {
          IPlayerContext pc = playerContextManager.GetPlayerContext(i);
          if (pc == null || !pc.IsActive)
            continue;
          IPlayer player = pc.CurrentPlayer;
          AudioStreamDescriptor currentAudioStream;
          IList<AudioStreamDescriptor> asds = new List<AudioStreamDescriptor>(pc.GetAudioStreamDescriptors(out currentAudioStream));
          foreach (AudioStreamDescriptor asd in asds)
          {
            string playedItem = player == null ? null : player.MediaItemTitle ?? pc.Name;
            string choiceItemName;
            int count = asds.Count;
            if (numActivePlayers > 1 && count > 1 && count != numberOfAudioStreams)
              // Only display the playedItem name if more than one player is able to provide audio streams. If a single player provides
              // multiple streams, they will be distinguished by the VideoPlayer.
              choiceItemName = playedItem + ": " + asd.AudioStreamName;
            else
              choiceItemName = count != numberOfAudioStreams ? playedItem : asd.AudioStreamName;

            AudioStreamDescriptor asdClosureCopy = asd;
            ListItem item = new ListItem(Consts.KEY_NAME, choiceItemName)
              {
                  Command = new MethodDelegateCommand(() => ChooseAudioStream(asdClosureCopy)),
                  Selected = asd == currentAudioStream,
              };
            item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
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
        IPlayerContext pc = _playerAudioMenuPlayerContext;
        if (pc == null || !pc.IsActive)
        {
          LeaveAudioMenuWorkflow();
          return;
        }
        IPlayer player = pc.CurrentPlayer;
        AudioStreamDescriptor currentAudioStream;
        IList<AudioStreamDescriptor> asds = new List<AudioStreamDescriptor>(pc.GetAudioStreamDescriptors(out currentAudioStream));
        foreach (AudioStreamDescriptor asd in asds)
        {
          string playedItem = player == null ? null : player.MediaItemTitle ?? pc.Name;
          string choiceItemName;
          if (asds.Count > 1)
              // Only display the audio stream name if the player has more than one audio stream
            choiceItemName = playedItem + ": " + asd.AudioStreamName;
          else
            choiceItemName = playedItem;
          AudioStreamDescriptor asdClosureCopy = asd;
          ListItem item = new ListItem(Consts.KEY_NAME, choiceItemName)
            {
                Command = new MethodDelegateCommand(() => ChooseAudioStream(asdClosureCopy)),
                Selected = asd == currentAudioStream,
            };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
          _playerSlotAudioMenu.Add(item);
        }
        if (_showToggleMute)
        {
          ListItem item;
          if (playerManager.Muted)
            item = new ListItem(Consts.KEY_NAME, Consts.RES_MUTE_OFF)
              {
                  Command = new MethodDelegateCommand(PlayersResetMute),
                  Selected = true,
              };
          else
            item = new ListItem(Consts.KEY_NAME, Consts.RES_MUTE)
              {
                  Command = new MethodDelegateCommand(PlayersMute)
              };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
          _playerSlotAudioMenu.Add(item);
        }

        IList<IPlayerContext> playerContexts = playerContextManager.PlayerContexts.ToList();
        _playerSlotAudioMenu.FireChange();
        _playerSlotAudioMenuHeader = LocalizationHelper.CreateResourceString(Consts.RES_PLAYER_SLOT_AUDIO_MENU).Evaluate(
            GetNameForPlayerContext(_playerAudioMenuPlayerContext, playerContexts.IndexOf(_playerAudioMenuPlayerContext)));
      }
    }

    protected void UpdatePlayerChooseGeometryMenu()
    {
      if (_playerChooseGeometryMenu.Count == 0)
      {
        IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
        IGeometry defaultGeometry = geometryManager.DefaultVideoGeometry;
        IPlayerContext pc = _playerGeometryMenuPlayerContext;
        if (pc == null || !pc.IsActive)
        {
          LeaveChooseGeometryWorkflow();
          return;
        }
        string geometryStr = LocalizationHelper.CreateResourceString(Consts.RES_CHOOSE_PLAYER_GEOMETRY).Evaluate();
        IList<IPlayerContext> videoPCs = GetVideoPlayerContexts();
        _playerChooseGeometryHeader = videoPCs.Count > 1 ?
          string.Format("{0} ({1})", geometryStr, GetNameForPlayerContext(pc, pc.IsPrimaryPlayerContext ? 0 : 1))
          : geometryStr;
        IVideoPlayer videoPlayer = pc.CurrentPlayer as IVideoPlayer;
        foreach (KeyValuePair<string, IGeometry> nameToGeometry in geometryManager.AvailableGeometries)
        {
          IGeometry geometry = nameToGeometry.Value;
          IGeometry vpGeometry = videoPlayer == null ? null : videoPlayer.GeometryOverride ?? defaultGeometry;
          ListItem item = new ListItem(Consts.KEY_NAME, nameToGeometry.Key)
            {
                Command = new MethodDelegateCommand(() => SetGeometry(_playerGeometryMenuPlayerContext, geometry)),
                Selected = vpGeometry == geometry,
            };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
          _playerChooseGeometryMenu.Add(item);
        }
      }
    }

    protected void UpdatePlayerChooseEffectMenu()
    {
      if (_playerChooseEffectMenu.Count == 0)
      {
        IGeometryManager geometryManager = ServiceRegistration.Get<IGeometryManager>();
        string standardEffectFile = geometryManager.StandardEffectFile;
        IPlayerContext pc = _playerEffectMenuPlayerContext;
        if (pc == null || !pc.IsActive)
        {
          LeaveChooseEffectWorkflow();
          return;
        }
        string effectStr = LocalizationHelper.CreateResourceString(Consts.RES_CHOOSE_PLAYER_EFFECT).Evaluate();
        IList<IPlayerContext> videoPCs = GetVideoPlayerContexts();
        _playerChooseEffectHeader = videoPCs.Count > 1 ?
          string.Format("{0} ({1})", effectStr, GetNameForPlayerContext(pc, pc.IsPrimaryPlayerContext ? 0 : 1))
          : effectStr;
        IVideoPlayer videoPlayer = pc.CurrentPlayer as IVideoPlayer;
        foreach (KeyValuePair<string, string> nameToEffect in geometryManager.AvailableEffects)
        {
          string file = nameToEffect.Key;
          string vpEffectFile = videoPlayer == null ? null : videoPlayer.EffectOverride ?? standardEffectFile;
          ListItem item = new ListItem(Consts.KEY_NAME, nameToEffect.Value)
          {
            Command = new MethodDelegateCommand(() => SetEffect(_playerEffectMenuPlayerContext, file)),
            Selected = file == vpEffectFile,
          };
          item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
          _playerChooseEffectMenu.Add(item);
        }
      }
    }

    protected void UpdatePlayerChooseSubtitleMenu()
    {
      if (_playerChooseSubtitleMenu.Count == 0)
      {        
        IPlayerContext pc = _playerSubtitleMenuPlayerContext;
        if (pc == null || !pc.IsActive)
        {
          LeaveChooseSubtitleWorkflow();
          return;
        }

        string subtitleStr = LocalizationHelper.CreateResourceString(Consts.RES_CHOOSE_PLAYER_SUBTITLE).Evaluate();
        IList<IPlayerContext> videoPCs = GetVideoPlayerContexts();
        _playerChooseSubtitleHeader = videoPCs.Count > 1 ?
          string.Format("{0} ({1})", subtitleStr, GetNameForPlayerContext(pc, pc.IsPrimaryPlayerContext ? 0 : 1))
          : subtitleStr;
        ISubtitlePlayer subtitlePlayer = pc.CurrentPlayer as ISubtitlePlayer;
        if(subtitlePlayer != null) // should not be happen, but we get sure :)
        {
          string[] subtitles = subtitlePlayer.Subtitles;
          foreach(string subtitle in subtitles)
          {
            ListItem item = new ListItem(Consts.KEY_NAME, subtitle)
            {
              Command = new MethodDelegateCommand(() => SetSubtitle(_playerSubtitleMenuPlayerContext, subtitle)),
              Selected = subtitlePlayer.CurrentSubtitle == subtitle,
            };
            item.AdditionalProperties[Consts.KEY_NAVIGATION_MODE] = NavigationMode.ExitPCWorkflow;
            _playerChooseSubtitleMenu.Add(item);
          }
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
            workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG, true);
        }
        if (_inChooseAudioStreamDialog)
        {
          UpdateAudioStreamsMenu();
          if (_audioStreamsMenu.Count <= 1)
            // Automatically close audio stream choice dialog if less than two audio streams are available
            workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG, true);
        }
        if (_inPlayerSlotAudioMenuDialog)
        {
          UpdatePlayerSlotAudioMenu();
          if (_playerSlotAudioMenu.Count <= 1)
            // Automatically close audio stream choice dialog if less than two audio streams are available
            workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG, true);
        }
        if (_inPlayerChooseGeometryMenuDialog)
        {
          // Automatically close geometry choice dialog if current player is no video player
          if (_playerGeometryMenuPlayerContext == null || !_playerGeometryMenuPlayerContext.IsActive ||
              !(_playerGeometryMenuPlayerContext.CurrentPlayer is IVideoPlayer))
            // Automatically close geometry stream choice dialog
            workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG, true);
          else
            UpdatePlayerChooseGeometryMenu();
        }
        if (_inPlayerChooseEffectMenuDialog)
        {
          // Automatically close Effect choice dialog if current player is no video player
          if (_playerEffectMenuPlayerContext == null || !_playerEffectMenuPlayerContext.IsActive ||
              !(_playerEffectMenuPlayerContext.CurrentPlayer is IVideoPlayer))
            // Automatically close effect stream choice dialog
            while (_inPlayerChooseEffectMenuDialog)
              workflowManager.NavigatePop(1);
          else
            UpdatePlayerChooseEffectMenu();
        }
        if (_inPlayerChooseSubtitleMenuDialog)
        {
          // Automatically close Subtitle choice dialog if current player is no video player
          if (_playerSubtitleMenuPlayerContext == null || !_playerSubtitleMenuPlayerContext.IsActive ||
              !(_playerSubtitleMenuPlayerContext.CurrentPlayer is ISubtitlePlayer))
            // Automatically close effect stream choice dialog
            while (_inPlayerChooseSubtitleMenuDialog)
              workflowManager.NavigatePop(1);
          else
            UpdatePlayerChooseSubtitleMenu();
        }
      }
    }

    protected static void LeaveAudioMenuWorkflow()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG, true);
    }

    protected static void LeaveChooseGeometryWorkflow()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG, true);
    }

    protected static void LeaveChooseEffectWorkflow()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_PLAYER_CHOOSE_EFFECT_MENU_DIALOG, true);
    }

    protected static void LeaveChooseSubtitleWorkflow()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePopToStateAsync(Consts.WF_STATE_ID_PLAYER_CHOOSE_SUBTITLE_MENU_DIALOG, true);
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
      if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG)
      {
        UpdatePlayerConfigurationMenu();
        _inPlayerConfigurationDialog = true;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG)
      {
        UpdateAudioStreamsMenu();
        _inChooseAudioStreamDialog = true;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG)
      {
        _playerAudioMenuPlayerContext = newContext.GetContextVariable(Consts.KEY_PLAYER_CONTEXT, false) as IPlayerContext;
        bool? showToggleMute = newContext.GetContextVariable(Consts.KEY_SHOW_MUTE, false) as bool?;
        _showToggleMute = showToggleMute ?? true;
        UpdatePlayerSlotAudioMenu();
        _inPlayerSlotAudioMenuDialog = true;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG)
      {
        _playerGeometryMenuPlayerContext = newContext.GetContextVariable(Consts.KEY_PLAYER_CONTEXT, false) as IPlayerContext;
        UpdatePlayerChooseGeometryMenu();
        _inPlayerChooseGeometryMenuDialog = true;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_EFFECT_MENU_DIALOG)
      {
        _playerEffectMenuPlayerContext = newContext.GetContextVariable(Consts.KEY_PLAYER_CONTEXT, false) as IPlayerContext;
        UpdatePlayerChooseEffectMenu();
        _inPlayerChooseEffectMenuDialog = true;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_SUBTITLE_MENU_DIALOG)
      {
        _playerSubtitleMenuPlayerContext = newContext.GetContextVariable(Consts.KEY_PLAYER_CONTEXT, false) as IPlayerContext;
        UpdatePlayerChooseSubtitleMenu();
        _inPlayerChooseSubtitleMenuDialog = true;
      }
    }

    protected void ExitContext(NavigationContext oldContext)
    {
      _messageQueue.Shutdown();
      if (oldContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG)
      {
        _inPlayerConfigurationDialog = false;
        _playerConfigurationMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == Consts.WF_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG)
      {
        _inChooseAudioStreamDialog = false;
        _audioStreamsMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG)
      {
        _inPlayerSlotAudioMenuDialog = false;
        _playerSlotAudioMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG)
      {
        _inPlayerChooseGeometryMenuDialog = false;
        _playerChooseGeometryMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_EFFECT_MENU_DIALOG)
      {
        _inPlayerChooseEffectMenuDialog = false;
        _playerChooseEffectMenu.Clear();
      }
      else if (oldContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_SUBTITLE_MENU_DIALOG)
      {
        _inPlayerChooseSubtitleMenuDialog = false;
        _playerChooseSubtitleMenu.Clear();
      }
    }

    #endregion

    #region Public members to be called from other modules

    public static void OpenChooseGeometryDialog(IPlayerContext playerContext)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG, new NavigationContextConfig
        {
          AdditionalContextVariables = new Dictionary<string, object>
          {
              {Consts.KEY_PLAYER_CONTEXT, playerContext},
          }
        });
    }

    public static void OpenChooseEffectDialog(IPlayerContext playerContext)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYER_CHOOSE_EFFECT_MENU_DIALOG, new NavigationContextConfig
      {
        AdditionalContextVariables = new Dictionary<string, object>
          {
              {Consts.KEY_PLAYER_CONTEXT, playerContext},
          }
      });
    }

    public static void OpenChooseSubtitleDialog(IPlayerContext playerContext)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYER_CHOOSE_SUBTITLE_MENU_DIALOG, new NavigationContextConfig
      {
        AdditionalContextVariables = new Dictionary<string, object>
          {
              {Consts.KEY_PLAYER_CONTEXT, playerContext},
          }
      });
    }

    public static void OpenPlayerConfigurationDialog()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG);
    }

    public static void OpenAudioMenuDialog(IPlayerContext playerContext, bool showMute)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG, new NavigationContextConfig
        {
          AdditionalContextVariables = new Dictionary<string, object>
            {
                {Consts.KEY_PLAYER_CONTEXT, playerContext},
                {Consts.KEY_SHOW_MUTE, showMute}
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

    public ItemsList PlayerChooseEffectMenu
    {
      get { return _playerChooseEffectMenu; }
    }

    public ItemsList PlayerChooseSubtitleMenu
    {
      get { return _playerChooseSubtitleMenu; }
    }

    public string PlayerChooseGeometryHeader
    {
      get { return _playerChooseGeometryHeader; }
    }

    public string PlayerChooseEffectHeader
    {
      get { return _playerChooseEffectHeader; }
    }

    public string PlayerChooseSubtitleHeader
    {
      get { return _playerChooseSubtitleHeader; }
    }

    public void Select(ListItem item)
    {
      if (item == null)
        return;
      ICommand command = item.Command;
      if (command != null)
        command.Execute();
      object obj;
      NavigationMode mode = NavigationMode.ExitPCWorkflow;
      if (item.AdditionalProperties.TryGetValue(Consts.KEY_NAVIGATION_MODE, out obj))
        mode = (NavigationMode) obj;
      if (mode == NavigationMode.ExitPCWorkflow)
      {
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        workflowManager.NavigatePopToState(Consts.WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG, true);
      }
    }

    public void SetCurrentPlayer(int playerIndex)
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.CurrentPlayerIndex = playerIndex;
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
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      playerContextManager.SwitchPipPlayers();
    }

    public void OpenChooseAudioStreamDialog()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG);
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

    public void SetEffect(IPlayerContext playerContext, string effect)
    {
      if (playerContext != null)
        playerContext.OverrideEffect(effect);
    }

    public void SetSubtitle(IPlayerContext playerContext, string subtitle)
    {
      if (playerContext != null)
      {
        ISubtitlePlayer sp = playerContext.CurrentPlayer as ISubtitlePlayer;
        if (sp != null)
          sp.SetSubtitle(subtitle);
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_PLAYER_CONFIGURATION_DIALOG; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CONFIGURATION_DIALOG)
      {
        UpdatePlayerConfigurationMenu();
        return _playerConfigurationMenu.Count > 0;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_CHOOSE_AUDIO_STREAM_DIALOG)
      {
        UpdateAudioStreamsMenu();
        return _audioStreamsMenu.Count > 0;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_AUDIO_MENU_DIALOG)
      {
        // Check if we got our necessary player slot parameter
        if (newContext.GetContextVariable(Consts.KEY_PLAYER_CONTEXT, false) != null)
          return true;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_GEOMETRY_MENU_DIALOG)
      {
        // Check if we got our necessary player context parameter
        if (newContext.GetContextVariable(Consts.KEY_PLAYER_CONTEXT, false) != null)
          return true;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_EFFECT_MENU_DIALOG)
      {
        // Check if we got our necessary player context parameter
        if (newContext.GetContextVariable(Consts.KEY_PLAYER_CONTEXT, false) != null)
          return true;
      }
      else if (newContext.WorkflowState.StateId == Consts.WF_STATE_ID_PLAYER_CHOOSE_SUBTITLE_MENU_DIALOG)
      {
        // Check if we got our necessary player context parameter
        if (newContext.GetContextVariable(Consts.KEY_PLAYER_CONTEXT, false) != null)
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
