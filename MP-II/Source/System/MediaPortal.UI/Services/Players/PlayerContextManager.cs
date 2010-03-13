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
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.Utilities;

namespace MediaPortal.UI.Services.Players
{
  public class PlayerContextManager : IPlayerContextManager, IDisposable
  {
    #region Consts

    protected const string KEY_PLAYER_CONTEXT = "PlayerContextManager: PlayerContext";

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;
    private int _currentPlayerIndex = -1; // Set this value via the CurrentPlayerIndex property to correctly raise the update event

    // Remember the state id when we are in a "currently playing" or "fullscreen content" state
    protected Guid? _inCurrentlyPlayingState = null;
    protected Guid? _inFullscreenContentState = null;

    #endregion

    public PlayerContextManager()
    {
      SubscribeToMessages();
    }

    void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           WorkflowManagerMessaging.CHANNEL,
           PlayerManagerMessaging.CHANNEL,
        });
      _messageQueue.PreviewMessage += OnPreviewMessage;
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnPreviewMessage(IMessageReceiver queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        WorkflowManagerMessaging.MessageType messageType =
            (WorkflowManagerMessaging.MessageType) message.MessageType;
        if (messageType == WorkflowManagerMessaging.MessageType.StatesPopped)
        {
          ICollection<Guid> statesRemoved = new List<Guid>(((IDictionary<Guid, NavigationContext>) message.MessageData[WorkflowManagerMessaging.CONTEXTS]).Keys);
          // Don't request the lock here, because we're in a synchronous message notification method
          if (_inCurrentlyPlayingState.HasValue && statesRemoved.Contains(_inCurrentlyPlayingState.Value))
            _inCurrentlyPlayingState = null;
          if (_inFullscreenContentState.HasValue && statesRemoved.Contains(_inFullscreenContentState.Value))
            _inFullscreenContentState = null;
        }
      }
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        PlayerManagerMessaging.MessageType messageType =
            (PlayerManagerMessaging.MessageType) message.MessageType;
        PlayerContext pc;
        IPlayerSlotController psc;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerError:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PARAM];
            pc = GetPlayerContext(psc);
            if (pc == null)
              return;
            if (!pc.NextItem())
              if (pc.CloseWhenFinished)
                pc.Close();
            break;
          case PlayerManagerMessaging.MessageType.PlayerStopped:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PARAM];
            pc = GetPlayerContext(psc);
            if (pc == null)
              return;
            // We get the player message asynchronously, so we have to check the state of the slot again to ensure
            // we close the correct one
            if (pc.CloseWhenFinished && pc.CurrentPlayer == null)
              pc.Close();
            break;
          case PlayerManagerMessaging.MessageType.RequestNextItem:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PARAM];
            pc = GetPlayerContext(psc);
            if (pc == null)
              return;
            pc.RequestNextItem();
            break;
          case PlayerManagerMessaging.MessageType.PlayerSlotsChanged:
            _currentPlayerIndex = 1 - _currentPlayerIndex;
            break;
        }
        CheckCurrentPlayerSlot();
        CheckMediaWorkflowStates_NoLock();
      }
    }

    /// <summary>
    /// Checks if the current player context contains a current player and returns the player. Else returns
    /// <c>null</c>.
    /// </summary>
    /// <returns>Current player of the current player context, if present. Else <c>null</c>.</returns>
    protected IPlayer GetCurrentPlayer()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return null;
      return playerContext.CurrentPlayer;
    }

    /// <summary>
    /// Sets the audio signal to the most plausible player. This is the audio player, if present, else it is the
    /// primary video player, if present.
    /// </summary>
    protected static void CheckAudio()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        if (playerManager.AudioSlotIndex != -1)
          return;
        IPlayer primaryPlayer = playerManager[PlayerManagerConsts.PRIMARY_SLOT];
        IPlayer secondaryPlayer = playerManager[PlayerManagerConsts.SECONDARY_SLOT];
        if (primaryPlayer is IAudioPlayer)
          playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
        else if (secondaryPlayer is IAudioPlayer)
          playerManager.AudioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
        else if (primaryPlayer is IVideoPlayer)
          playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
        else if (secondaryPlayer is IVideoPlayer)
          playerManager.AudioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
        else
          playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
      }
    }

    protected void CheckCurrentPlayerSlot()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayerSlotController primaryPSC = playerManager.GetPlayerSlotController(PlayerManagerConsts.PRIMARY_SLOT);
      IPlayerSlotController secondaryPSC = playerManager.GetPlayerSlotController(PlayerManagerConsts.SECONDARY_SLOT);
      int currentPlayerSlot;
      lock (playerManager.SyncObj)
      {
        bool primaryPlayerActive = primaryPSC.IsActive;
        bool secondaryPlayerActive = secondaryPSC.IsActive;
        currentPlayerSlot = CurrentPlayerIndex;
        if (currentPlayerSlot == PlayerManagerConsts.PRIMARY_SLOT && !primaryPlayerActive)
          currentPlayerSlot = -1;
        else if (currentPlayerSlot == PlayerManagerConsts.SECONDARY_SLOT && !secondaryPlayerActive)
          currentPlayerSlot = -1;
        if (currentPlayerSlot == -1)
          if (primaryPlayerActive)
            currentPlayerSlot = PlayerManagerConsts.PRIMARY_SLOT;
      }
      SetCurrentPlayerIndex_NoLock(currentPlayerSlot);
    }

    /// <summary>
    /// Checks if our "currently playing" and "fullscreen content" states still fit to the
    /// appropriate players, i.e. if we are in a "currently playing" state and the current player context was
    /// changed, the workflow state will be adapted to match the new current player context's "currently playing" state.
    /// The same check will happen for the primary player context and the "fullscreen content" state.
    /// </summary>
    protected void CheckMediaWorkflowStates_NoLock()
    {
      Guid? oldCPStateId = _inCurrentlyPlayingState;
      Guid? oldFSCStateId = _inFullscreenContentState;
      Guid? newCPStateId = null;
      Guid? newFSCStateId = null;
      if (oldCPStateId.HasValue)
      {
        IPlayerContext currentPC = CurrentPlayerContext;
        newCPStateId = currentPC == null ? new Guid?() : currentPC.CurrentlyPlayingWorkflowStateId;
      }
      if (oldFSCStateId.HasValue)
      {
        IPlayerContext primaryPC = GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT);
        newFSCStateId = primaryPC == null ? new Guid?() : primaryPC.FullscreenContentWorkflowStateId;
      }
      // Don't hold the lock while doing the workflow navigation below - see threading policy
      ILogger log = ServiceScope.Get<ILogger>();
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      if (oldCPStateId.HasValue && (!newCPStateId.HasValue || newCPStateId.Value != oldCPStateId.Value))
      {
        log.Debug("PlayerContextManager: Currently Playing Workflow State '{0}' doesn't fit any more to the current situation. Leaving workflow state...",
            oldCPStateId.Value);
        _inCurrentlyPlayingState = null;
        workflowManager.NavigatePopToState(oldCPStateId.Value, true);
      }
      if (oldFSCStateId.HasValue && (!newFSCStateId.HasValue || newFSCStateId.Value != oldFSCStateId.Value))
      {
        log.Debug("PlayerContextManager: Fullscreen Content Workflow State '{0}' doesn't fit any more to the current situation. Leaving workflow state...",
            oldFSCStateId.Value);
        _inFullscreenContentState = null;
        workflowManager.NavigatePopToState(oldFSCStateId.Value, true);
      }
      if (newCPStateId.HasValue && newCPStateId.Value != oldCPStateId.Value)
      {
        _inCurrentlyPlayingState = newCPStateId;
        log.Debug("PlayerContextManager: ... Auto-switching to new 'Currently Playing' Workflow State '{0}'",
            newCPStateId.Value);
        workflowManager.NavigatePush(newCPStateId.Value, null);
      }
      if (newFSCStateId.HasValue && newFSCStateId.Value != oldFSCStateId.Value)
      {
        _inFullscreenContentState = newFSCStateId;
        log.Debug("PlayerContextManager: ... Auto-switching to new 'Fullscreen Content' Workflow State '{0}'",
            newFSCStateId.Value);
        workflowManager.NavigatePush(newFSCStateId.Value, null);
      }
    }

    protected static PlayerContext GetPlayerContext(IPlayerSlotController psc)
    {
      if (psc == null)
        return null;
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        if (!psc.IsActive)
          return null;
        object result;
        if (psc.ContextVariables.TryGetValue(KEY_PLAYER_CONTEXT, out result))
          return result as PlayerContext;
      }
      return null;
    }

    protected static PlayerContext GetPlayerContextInternal(int slotIndex)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      return GetPlayerContext(playerManager.GetPlayerSlotController(slotIndex));
    }

    protected void SetCurrentPlayerIndex_NoLock(int value)
    {
      lock (SyncObj)
      {
        if (_currentPlayerIndex == value)
          return;
        PlayerContext newCurrent = GetPlayerContextInternal(value);
        if (newCurrent == null || !newCurrent.IsValid)
          return;
        _currentPlayerIndex = value;
      }
      CheckMediaWorkflowStates_NoLock();
      PlayerContextManagerMessaging.SendPlayerContextManagerMessage(
          PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged, value);
    }

    #region IDisposable implementation

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    #endregion

    #region IPlayerContextManager implementation

    public object SyncObj
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        return playerManager.SyncObj;
      }
    }

    public bool IsAudioPlayerActive
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        // We don't need to lock here because of the ||
        return playerManager[PlayerManagerConsts.PRIMARY_SLOT] is IAudioPlayer ||
            playerManager[PlayerManagerConsts.SECONDARY_SLOT] is IAudioPlayer;
      }
    }

    public bool IsVideoPlayerActive
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        // No locking necessary
        return playerManager[PlayerManagerConsts.PRIMARY_SLOT] is IVideoPlayer;
      }
    }

    public bool IsPipActive
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        // No locking necessary
        return playerManager[PlayerManagerConsts.SECONDARY_SLOT] is IVideoPlayer;
      }
    }

    public IPlayerContext CurrentPlayerContext
    {
      get { return GetPlayerContextInternal(_currentPlayerIndex); }
    }

    public int CurrentPlayerIndex
    {
      get
      {
        lock (SyncObj)
          return _currentPlayerIndex;
      }
      set { SetCurrentPlayerIndex_NoLock(value); }
    }

    public int NumActivePlayerContexts
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        lock (playerManager.SyncObj)
        {
          int result = 0;
          for (int i = 0; i < 2; i++)
            if (GetPlayerContext(i) != null)
              result++;
          return result;
        }
      }
    }

    public void Shutdown()
    {
      UnsubscribeFromMessages();
    }

    public int NumPlayerContextsOfMediaType(PlayerContextType mediaType)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int result = 0;
        for (int i = 0; i < 2; i++)
        {
          IPlayerContext pc = GetPlayerContext(i);
          if (pc != null && pc.MediaType == mediaType)
            result++;
        }
        return result;
      }
    }

    public IPlayerContext OpenAudioPlayerContext(Guid mediaModuleId, string name, bool concurrent, Guid currentlyPlayingWorkflowStateId,
        Guid fullscreenContentWorkflowStateId)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int numActive = playerManager.NumActiveSlots;
        if (concurrent)
        {
          // Solve conflicts - close conflicting slots
          if (numActive > 1)
            playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
          if (numActive > 0 && GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT).MediaType == PlayerContextType.Audio)
            playerManager.CloseSlot(PlayerManagerConsts.PRIMARY_SLOT);
        }
        else // !concurrent
          // Don't enable concurrent controllers: Close all except the primary slot controller
          playerManager.CloseAllSlots();
        // Open new slot
        int slotIndex;
        IPlayerSlotController slotController;
        playerManager.OpenSlot(out slotIndex, out slotController);
        playerManager.AudioSlotIndex = slotController.SlotIndex;
        PlayerContext result = new PlayerContext(this, slotController, mediaModuleId, name, PlayerContextType.Audio, currentlyPlayingWorkflowStateId, fullscreenContentWorkflowStateId);
        result.SetContextVariable(KEY_PLAYER_CONTEXT, result);
        return result;
      }
    }

    public IPlayerContext OpenVideoPlayerContext(Guid mediaModuleId, string name, bool concurrent, bool subordinatedVideo,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int numActive = playerManager.NumActiveSlots;
        IPlayerSlotController slotController;
        int slotIndex;
        if (concurrent)
          // Solve conflicts - close conflicting slots
          if (numActive > 1)
            if (GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT).MediaType == PlayerContextType.Audio)
              if (subordinatedVideo)
              {
                playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
                playerManager.OpenSlot(out slotIndex, out slotController);
                playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
              }
              else
              {
                playerManager.CloseSlot(PlayerManagerConsts.PRIMARY_SLOT);
                playerManager.OpenSlot(out slotIndex, out slotController);
                playerManager.SwitchSlots();
                playerManager.AudioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
              }
            else // PC(SECONDARY).Type != Audio
            {
              playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
              if (!subordinatedVideo)
                playerManager.CloseSlot(PlayerManagerConsts.PRIMARY_SLOT);
              playerManager.OpenSlot(out slotIndex, out slotController);
              playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            }
          else if (numActive == 1)
            if (GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT).MediaType == PlayerContextType.Audio)
            {
              playerManager.OpenSlot(out slotIndex, out slotController);
              // Make new video slot the primary slot
              playerManager.SwitchSlots();
              playerManager.AudioSlotIndex = PlayerManagerConsts.SECONDARY_SLOT;
            }
            else // PC(PRIMARY).Type != Audio
            {
              if (!subordinatedVideo)
                playerManager.CloseSlot(PlayerManagerConsts.PRIMARY_SLOT);
              playerManager.OpenSlot(out slotIndex, out slotController);
              playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            }
          else // numActive == 0
          {
            playerManager.OpenSlot(out slotIndex, out slotController);
            playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
          }
        else // !concurrent
        {
          // Don't enable concurrent controllers: Close all except the primary slot controller
          playerManager.CloseAllSlots();
          playerManager.OpenSlot(out slotIndex, out slotController);
          playerManager.AudioSlotIndex = PlayerManagerConsts.PRIMARY_SLOT;
        }
        PlayerContext result = new PlayerContext(this, slotController, mediaModuleId, name, PlayerContextType.Video,
            currentlyPlayingWorkflowStateId, fullscreenContentWorkflowStateId);
        result.SetContextVariable(KEY_PLAYER_CONTEXT, result);
        return result;
      }
    }

    public IEnumerable<IPlayerContext> GetPlayerContextsByMediaModuleId(Guid mediaModuleId)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        for (int i = 0; i < 2; i++)
        {
          IPlayerContext pc = GetPlayerContext(i);
          if (pc != null && pc.MediaModuleId == mediaModuleId)
            yield return pc;
        }
      }
    }

    public void ShowCurrentlyPlaying(PlayerChoice player)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        if (_inCurrentlyPlayingState.HasValue)
          return;
        IPlayerContext pc = GetPlayerContext(player);
        if (pc == null)
          return;
        _inCurrentlyPlayingState = pc.CurrentlyPlayingWorkflowStateId;
      }
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(_inCurrentlyPlayingState.Value, null);
    }

    public void ShowFullscreenContent(PlayerChoice player)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        if (_inFullscreenContentState.HasValue)
          return;
        IPlayerContext pc = GetPlayerContext(player);
        if (pc == null)
          return;
        _inFullscreenContentState = pc.FullscreenContentWorkflowStateId;
      }
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePush(_inFullscreenContentState.Value, null);
    }

    public PlayerContextType GetTypeOfMediaItem(MediaItem item)
    {
      // No locking necessary
      if (item.Aspects.ContainsKey(VideoAspect.Metadata.AspectId) ||
          item.Aspects.ContainsKey(PictureAspect.Metadata.AspectId))
        return PlayerContextType.Video;
      else if (item.Aspects.ContainsKey(AudioAspect.Metadata.AspectId))
        return PlayerContextType.Audio;
      else
        return PlayerContextType.None;
    }

    public IPlayerContext GetPlayerContext(PlayerChoice player)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int slotIndex = PlayerManagerConsts.PRIMARY_SLOT;
        switch (player)
        {
          case PlayerChoice.PrimaryPlayer:
            slotIndex = PlayerManagerConsts.PRIMARY_SLOT;
            break;
          case PlayerChoice.SecondaryPlayer:
            slotIndex = PlayerManagerConsts.SECONDARY_SLOT;
            break;
          case PlayerChoice.CurrentPlayer:
            slotIndex = _currentPlayerIndex;
            break;
        }
        return GetPlayerContextInternal(slotIndex);
      }
    }

    public IPlayerContext GetPlayerContext(int slotIndex)
    {
      return GetPlayerContextInternal(slotIndex);
    }

    public ICollection<AudioStreamDescriptor> GetAvailableAudioStreams()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        ICollection<AudioStreamDescriptor> result = new List<AudioStreamDescriptor>();
        for (int i = 0; i < 2; i++)
        {
          IPlayerContext playerContext = GetPlayerContext(i);
          if (playerContext == null)
            continue;
          CollectionUtils.AddAll(result, playerContext.GetAudioStreamDescriptors());
        }
        return result;
      }
    }

    public bool SetAudioStream(AudioStreamDescriptor stream)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      IPlayerContext playerContext = stream.PlayerContext;
      lock (playerManager.SyncObj)
      {
        if (!playerContext.IsValid)
          return false;
        IVideoPlayer player = playerContext.CurrentPlayer as IVideoPlayer;
        if (player == null || player.Name != stream.PlayerName)
          return false;
        player.SetAudioStream(stream.AudioStreamName);
        playerManager.AudioSlotIndex = playerContext.PlayerSlotController.SlotIndex;
        return true;
      }
    }

    public void Stop()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.Stop();
    }

    public void Pause()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.Pause();
    }

    public void Play()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.Play();
    }

    public void TogglePlayPause()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.TogglePlayPause();
    }

    public void Restart()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.Restart();
    }

    public void SeekForward()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.SeekForward();
    }

    public void SeekBackward()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return;
      playerContext.SeekBackward();
    }

    public bool PreviousItem()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return false;
      return playerContext.PreviousItem();
    }

    public bool NextItem()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null)
        return false;
      return playerContext.NextItem();
    }

    public void ToggleCurrentPlayer()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        if (_currentPlayerIndex != -1)
          CurrentPlayerIndex = 1 - _currentPlayerIndex;
        CheckCurrentPlayerSlot();
      }
    }

    public void SwitchPipPlayers()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int numActive = playerManager.NumActiveSlots;
        if (numActive > 1 &&
            GetPlayerContext(PlayerManagerConsts.SECONDARY_SLOT).MediaType == PlayerContextType.Video &&
            GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT).MediaType == PlayerContextType.Video)
          playerManager.SwitchSlots();
      }
    }

    #endregion
  }
}
