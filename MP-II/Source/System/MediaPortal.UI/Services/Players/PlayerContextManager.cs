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
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Messaging;
using MediaPortal.Presentation.Players;
using MediaPortal.Utilities;

namespace MediaPortal.Services.Players
{
  public class PlayerContextManager : IPlayerContextManager, IDisposable
  {
    #region Consts

    protected const string KEY_PLAYER_CONTEXT = "PlayerContextHandler: PlayerContext";

    #endregion

    #region Protected fields

    private int _currentPlayerIndex = -1; // Set this value via the CurrentPlayerIndex property to correctly raise the update event

    #endregion

    public PlayerContextManager()
    {
      SubscribeToMessages();
    }

    protected void SubscribeToMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>();
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived += OnPlayerManagerMessageReceived;
    }

    protected void UnsubscribeFromMessages()
    {
      IMessageBroker broker = ServiceScope.Get<IMessageBroker>(false);
      if (broker == null)
        return;
      broker.GetOrCreate(PlayerManagerMessaging.QUEUE).MessageReceived -= OnPlayerManagerMessageReceived;
      // SytemMessaging queue is unregistered as soon as the system is started
    }

    protected void OnPlayerManagerMessageReceived(QueueMessage message)
    {
      PlayerManagerMessaging.MessageType messageType =
          (PlayerManagerMessaging.MessageType) message.MessageData[PlayerManagerMessaging.MESSAGE_TYPE];
      PlayerContext pc;
      int slotIndex;
      switch (messageType)
      {
        case PlayerManagerMessaging.MessageType.PlayerEnded:
          slotIndex = (int) message.MessageData[PlayerManagerMessaging.PARAM];
          pc = GetPlayerContextInternal(slotIndex);
          if (pc != null && !pc.NextItem())
            if (pc.CloseWhenFinished)
              ClosePlayerContext(slotIndex);
          break;
        case PlayerManagerMessaging.MessageType.PlayerStopped:
          slotIndex = (int) message.MessageData[PlayerManagerMessaging.PARAM];
          pc = GetPlayerContextInternal(slotIndex);
          if (pc != null && pc.CloseWhenFinished)
            ClosePlayerContext(slotIndex);
          break;
      }
      CheckCurrentPlayerSlot();
    }

    /// <summary>
    /// Checks if the current player context contains a current player and returns the player. Else returns
    /// <c>null</c>.
    /// </summary>
    /// <returns>Current player of the current player context, if present. Else <c>null</c>.</returns>
    protected IPlayer GetCurrentPlayer()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null || !playerContext.IsValid)
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

    protected void CheckCurrentPlayerSlot()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      bool primaryPlayerActive = playerManager.GetPlayerSlotController(PlayerManagerConsts.PRIMARY_SLOT).IsActive;
      bool secondaryPlayerActive = playerManager.GetPlayerSlotController(PlayerManagerConsts.SECONDARY_SLOT).IsActive;
      int currentPlayerSlot = CurrentPlayerIndex;
      if (currentPlayerSlot == PlayerManagerConsts.PRIMARY_SLOT && !primaryPlayerActive)
        currentPlayerSlot = -1;
      else if (currentPlayerSlot == PlayerManagerConsts.SECONDARY_SLOT && !secondaryPlayerActive)
        currentPlayerSlot = -1;
      if (currentPlayerSlot == -1)
        if (secondaryPlayerActive)
          currentPlayerSlot = PlayerManagerConsts.SECONDARY_SLOT;
        else if (primaryPlayerActive)
          currentPlayerSlot = PlayerManagerConsts.PRIMARY_SLOT;
      IPlayerContextManager pcm = ServiceScope.Get<IPlayerContextManager>();
      pcm.CurrentPlayerIndex = currentPlayerSlot;
      CurrentPlayerIndex = currentPlayerSlot;
    }

    protected static PlayerContext GetPlayerContext(IPlayerSlotController psc)
    {
      if (psc == null || !psc.IsActive)
        return null;
      object result;
      if (psc.ContextVariables.TryGetValue(KEY_PLAYER_CONTEXT, out result))
        return result as PlayerContext;
      return null;
    }

    protected static PlayerContext GetPlayerContextInternal(int slotIndex)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      return GetPlayerContext(playerManager.GetPlayerSlotController(slotIndex));
    }

    #region IDisposable implementation

    public void Dispose()
    {
      UnsubscribeFromMessages();
    }

    #endregion

    #region IPlayerContextManager implementation

    public IPlayerContext CurrentPlayerContext
    {
      get { return GetPlayerContextInternal(_currentPlayerIndex); }
    }

    public bool IsAudioPlayerActive
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        return playerManager[PlayerManagerConsts.PRIMARY_SLOT] is IAudioPlayer ||
            playerManager[PlayerManagerConsts.SECONDARY_SLOT] is IAudioPlayer;
      }
    }

    public bool IsVideoPlayerActive
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        return playerManager[PlayerManagerConsts.PRIMARY_SLOT] is IVideoPlayer;
      }
    }

    public bool IsPipActive
    {
      get
      {
        IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
        return playerManager[PlayerManagerConsts.SECONDARY_SLOT] is IVideoPlayer;
      }
    }

    public int CurrentPlayerIndex
    {
      get { return _currentPlayerIndex; }
      set
      {
        if (_currentPlayerIndex == value)
          return;
        PlayerContext newCurrent = GetPlayerContextInternal(value);
        if (newCurrent == null || !newCurrent.IsValid)
          return;
        _currentPlayerIndex = value;
        PlayerContextManagerMessaging.SendPlayerContextManagerMessage(PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged);
      }
    }

    public int NumActivePlayerContexts
    {
      get
      {
        int result = 0;
        for (int i = 0; i < 2; i++)
          if (GetPlayerContext(i) != null)
            result++;
        return result;
      }
    }

    public void Shutdown()
    {
      UnsubscribeFromMessages();
    }

    public int NumPlayerContextsOfMediaType(PlayerContextType mediaType)
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

    public IPlayerContext OpenAudioPlayerContext(bool concurrent)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      int numActive = playerManager.NumActiveSlots;
      if (concurrent)
      {
        // Solve conflicts - close conflicting slots
        if (numActive > 1)
          playerManager.CloseSlot(PlayerManagerConsts.SECONDARY_SLOT);
        if (numActive > 0 && GetPlayerContext(PlayerManagerConsts.PRIMARY_SLOT).MediaType == PlayerContextType.Audio)
          playerManager.CloseSlot(PlayerManagerConsts.PRIMARY_SLOT);
      }
      else
        // Don't enable concurrent controllers: Close all except the primary slot controller
        playerManager.CloseAllSlots();
      // Open new slot
      int slotIndex;
      IPlayerSlotController slotController;
      playerManager.OpenSlot(out slotIndex, out slotController);
      playerManager.AudioSlotIndex = slotController.SlotIndex;
      PlayerContext result = new PlayerContext(this, slotController, PlayerContextType.Audio);
      result.SetContextVariable(KEY_PLAYER_CONTEXT, result);
      return result;
    }

    public IPlayerContext OpenVideoPlayerContext(bool concurrent, bool subordinatedVideo)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
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
              playerManager.SwitchPlayers();
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
            playerManager.SwitchPlayers();
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
      PlayerContext result = new PlayerContext(this, slotController, PlayerContextType.Video);
      result.SetContextVariable(KEY_PLAYER_CONTEXT, result);
      return result;
    }

    public void ClosePlayerContext(int slotIndex)
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      playerManager.CloseSlot(slotIndex);
    }

    public PlayerContextType GetTypeOfMediaItem(MediaItem item)
    {
      if (item.Aspects.ContainsKey(MovieAspect.Metadata.AspectId) ||
          item.Aspects.ContainsKey(PictureAspect.Metadata.AspectId))
        return PlayerContextType.Video;
      else if (item.Aspects.ContainsKey(MusicAspect.Metadata.AspectId))
        return PlayerContextType.Audio;
      else
        return PlayerContextType.None;
    }

    public IPlayerContext GetPlayerContext(int slotIndex)
    {
      return GetPlayerContextInternal(slotIndex);
    }

    public ICollection<AudioStreamDescriptor> GetAvailableAudioStreams()
    {
      ICollection<AudioStreamDescriptor> result = new List<AudioStreamDescriptor>();
      for (int i = 0; i < 2; i++)
      {
        IPlayerContext playerContext = GetPlayerContext(i);
        if (playerContext == null)
          continue;
        CollectionUtils.AddAll(result, AudioStreamDescriptor.GetAudioStreamDescriptors(playerContext));
      }
      return result;
    }

    public void SetAudioStream(AudioStreamDescriptor stream)
    {
      IPlayerContext playerContext = stream.PlayerContext;
      if (!playerContext.IsValid)
        return;
      IVideoPlayer player = playerContext.CurrentPlayer as IVideoPlayer;
      if (player == null || player.Name != stream.PlayerName)
        return;
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      player.SetAudioStream(stream.AudioStreamName);
      playerManager.AudioSlotIndex = playerContext.PlayerSlotController.SlotIndex;
    }

    public void Stop()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null || !playerContext.IsValid)
        return;
      playerContext.Stop();
    }

    public void Pause()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null || !playerContext.IsValid)
        return;
      playerContext.Pause();
    }

    public void Play()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null || !playerContext.IsValid)
        return;
      playerContext.Play();
    }

    public void TogglePlayPause()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null || !playerContext.IsValid)
        return;
      playerContext.TogglePlayPause();
    }

    public void Restart()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null || !playerContext.IsValid)
        return;
      playerContext.Restart();
    }

    public bool PreviousItem()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null || !playerContext.IsValid)
        return false;
      return playerContext.PreviousItem();
    }

    public bool NextItem()
    {
      IPlayerContext playerContext = CurrentPlayerContext;
      if (playerContext == null || !playerContext.IsValid)
        return false;
      return playerContext.NextItem();
    }

    #endregion
  }
}
