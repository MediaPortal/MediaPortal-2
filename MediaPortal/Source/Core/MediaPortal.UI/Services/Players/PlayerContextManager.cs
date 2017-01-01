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
using System.Reflection;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.MediaManagement.DefaultItemAspects;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.Settings;
using MediaPortal.Common.Threading;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Players.ResumeState;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.Services.Players.PCMOpenPlayerStrategy;
using MediaPortal.UI.Services.Players.Settings;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.Utilities;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.UI.Services.Players
{
  /// <summary>
  /// Implementation service of the <see cref="IPlayerContextManager"/> interface.
  /// </summary>
  public class PlayerContextManager : IPlayerContextManager, IDisposable
  {
    #region Enums, delegates & classes

    protected enum PlayerWFStateType
    {
      CurrentlyPlaying,
      FullscreenContent
    }

    /// <summary>
    /// Stores a descriptor for an instance of a "currently playing" or "fullscreen content" workflow navigation state
    /// on the workflow navigation context stack.
    /// </summary>
    protected class PlayerWFStateInstance
    {
      protected PlayerWFStateType _type;
      protected Guid _wfStateId;

      public PlayerWFStateInstance(PlayerWFStateType type, Guid wfStateId)
      {
        _type = type;
        _wfStateId = wfStateId;
      }

      public PlayerWFStateType WFStateType
      {
        get { return _type; }
      }

      public Guid WFStateId
      {
        get { return _wfStateId; }
      }
    }

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    /// <summary>
    /// Remembers all player workflow state instances ("currently playing" or "fullscreen content" states) from the current
    /// workflow navigation stack. We need that to update those states when the current player or primary player change.
    /// The topmost player workflow state instance from the navigation stack is at the end of this list.
    /// </summary>
    /// <remarks>
    /// Note that we can be in a CP state and in an FSC state at the same time.
    /// </remarks>
    protected List<PlayerWFStateInstance> _playerWfStateInstances = new List<PlayerWFStateInstance>(2);
    protected IList<PlayerContext> _playerContextsCache = null; // Index over slot index

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
           PlayerContextManagerMessaging.CHANNEL,
        });
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

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == WorkflowManagerMessaging.CHANNEL)
      {
        // Adjust our knowledge about the currently opened FSC/CP state if the user switches to one of them
        WorkflowManagerMessaging.MessageType messageType = (WorkflowManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case WorkflowManagerMessaging.MessageType.StatesPopped:
            ICollection<Guid> statesRemoved = new List<Guid>(
                ((IDictionary<Guid, NavigationContext>) message.MessageData[WorkflowManagerMessaging.CONTEXTS]).Keys);
            HandleStatesRemovedFromWorkflowStack(statesRemoved);
            break;
          case WorkflowManagerMessaging.MessageType.StatePushed:
            NavigationContext context = (NavigationContext) message.MessageData[WorkflowManagerMessaging.CONTEXT];
            Guid stateId = context.WorkflowState.StateId;
            HandleWorkflowStatePushed(stateId);
            break;
        }
      }
      else if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        // React to player changes
        PlayerManagerMessaging.MessageType messageType = (PlayerManagerMessaging.MessageType) message.MessageType;
        IPlayerSlotController psc;
        switch (messageType)
        {
          case PlayerManagerMessaging.MessageType.PlayerResumeState:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            IResumeState resumeState = (IResumeState) message.MessageData[PlayerManagerMessaging.KEY_RESUME_STATE];
            MediaItem mediaItem = (MediaItem) message.MessageData[PlayerManagerMessaging.KEY_MEDIAITEM];
            HandleResumeInfo(psc, mediaItem, resumeState);
            break;
          case PlayerManagerMessaging.MessageType.PlayerError:
          case PlayerManagerMessaging.MessageType.PlayerEnded:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            HandlePlayerEnded(psc);
            break;
          case PlayerManagerMessaging.MessageType.PlayerStopped:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            HandlePlayerStopped(psc);
            break;
          case PlayerManagerMessaging.MessageType.RequestNextItem:
            psc = (IPlayerSlotController) message.MessageData[PlayerManagerMessaging.PLAYER_SLOT_CONTROLLER];
            HandleRequestNextItem(psc);
            break;
        }
        CleanupPlayerContexts();
        CheckMediaWorkflowStates_NoLock(); // Primary player could have been changed or closed or CP player could have been closed
      }
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
      {
        // React to internal player context manager changes
        PlayerContextManagerMessaging.MessageType messageType = (PlayerContextManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerContextManagerMessaging.MessageType.UpdatePlayerRolesInternal:
            PlayerContext newCurrentPlayer = (PlayerContext) message.MessageData[PlayerContextManagerMessaging.NEW_CURRENT_PLAYER_CONTEXT];
            PlayerContext newAudioPlayer = (PlayerContext) message.MessageData[PlayerContextManagerMessaging.NEW_AUDIO_PLAYER_CONTEXT];
            HandleUpdatePlayerRoles(newCurrentPlayer, newAudioPlayer);
            break;
          // PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged not handled here
        }
      }
    }

    void OnPlayerSlotControllerClosed(IPlayerSlotController psc)
    {
      CleanupPlayerContexts();
    }

    protected void HandleStatesRemovedFromWorkflowStack(ICollection<Guid> statesRemoved)
    {
      lock (SyncObj)
      {
        // If one of our remembered player workflow states was removed from workflow navigation stack,
        // take it from our player workflow state cache. Our player workflow state cache is in the same order
        // as the workflow navigation stack, so if we tracked everything correctly, each removal of states should
        // remove states from the end of our cache.
        for (int i = 0; i < _playerWfStateInstances.Count; i++)
        {
          PlayerWFStateInstance wfStateInstance = _playerWfStateInstances[i];
          if (statesRemoved.Contains(wfStateInstance.WFStateId))
          {
            _playerWfStateInstances.RemoveRange(i, _playerWfStateInstances.Count - i);
            break;
          }
        }
      }
    }

    protected void HandleWorkflowStatePushed(Guid stateId)
    {
      Guid? potentialState = GetPotentialCPStateId();
      if (potentialState == stateId)
        lock(SyncObj)
          _playerWfStateInstances.Add(new PlayerWFStateInstance(PlayerWFStateType.CurrentlyPlaying, stateId));
      potentialState = GetPotentialFSCStateId();
      if (potentialState == stateId)
        lock (SyncObj)
          _playerWfStateInstances.Add(new PlayerWFStateInstance(PlayerWFStateType.FullscreenContent, stateId));
    }

    protected void HandleResumeInfo(IPlayerSlotController psc, MediaItem mediaItem, IResumeState resumeState)
    {
      // We can only handle resume info for valid MediaItemIds that are coming from MediaLibrary, not from local browsing.
      if (mediaItem == null)
        return;
      if (mediaItem.MediaItemId == Guid.Empty)
        return;

      string serialized = ResumeStateBase.Serialize(resumeState);

      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement.IsValidUser)
        userProfileDataManagement.UserProfileDataManagement.SetUserMediaItemData(userProfileDataManagement.CurrentUser.ProfileId, mediaItem.MediaItemId, PlayerContext.KEY_RESUME_STATE, serialized);

      if (!mediaItem.UserData.ContainsKey(PlayerContext.KEY_RESUME_STATE))
        mediaItem.UserData.Add(PlayerContext.KEY_RESUME_STATE, "");
      mediaItem.UserData[PlayerContext.KEY_RESUME_STATE] = serialized;

      int playPercentage = GetPlayPercentage(mediaItem, resumeState);
      NotifyPlayback(mediaItem, playPercentage);
    }

    protected static int GetPlayPercentage(MediaItem mediaItem, IResumeState resumeState)
    {
      int playPercentage = 100;
      PositionResumeState positionResume = resumeState as PositionResumeState;
      if (positionResume != null)
      {
        TimeSpan resumePosition = positionResume.ResumePosition;
        TimeSpan duration = TimeSpan.FromSeconds(0);
        IList<MediaItemAspect> aspects;
        if (mediaItem.Aspects.TryGetValue(VideoStreamAspect.ASPECT_ID, out aspects))
        {
          var aspect = aspects.First();
          int? part = (int?)aspect[VideoStreamAspect.ATTR_VIDEO_PART];
          int? partSet = (int?)aspect[VideoStreamAspect.ATTR_VIDEO_PART_SET];
          long? dur = null;
          if (!part.HasValue || part < 0)
          {
            dur = (long?)aspect[VideoStreamAspect.ATTR_DURATION];
          }
          else if (partSet.HasValue)
          {
            dur = aspects.Where(a => (int?)a[VideoStreamAspect.ATTR_VIDEO_PART_SET] == partSet &&
            aspect[VideoStreamAspect.ATTR_DURATION] != null).Sum(a => (long)a[VideoStreamAspect.ATTR_DURATION]);
          }
          if (dur.HasValue)
            duration = TimeSpan.FromSeconds(dur.Value);
        }
        else if (mediaItem.Aspects.TryGetValue(AudioAspect.ASPECT_ID, out aspects))
        {
          var aspect = aspects.First();
          long? dur = aspect == null ? null : (long?)aspect[AudioAspect.ATTR_DURATION];
          if (dur.HasValue)
            duration = TimeSpan.FromSeconds(dur.Value);
        }

        if (duration.TotalSeconds > 0)
          playPercentage = (int)(resumePosition.TotalSeconds * 100 / duration.TotalSeconds);
        else
          playPercentage = 0;
      }
      if (playPercentage > 100)
        playPercentage = 100;
      return playPercentage;
    }

    protected static void NotifyPlayback(MediaItem mediaItem, int playPercentage)
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PlayerManagerSettings settings = settingsManager.Load<PlayerManagerSettings>();
      bool watched = playPercentage >= settings.WatchedPlayPercentage;
      if (watched)
        playPercentage = 100;

      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      IContentDirectory cd = scm.ContentDirectory;
      // Server will update the PlayCount of MediaAspect in ML, this does not affect loaded items.
      if (cd != null)
        cd.NotifyPlayback(mediaItem.MediaItemId, watched);

      // Update loaded item also, so changes will be visible in GUI without reloading
      if (!mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_PERCENTAGE))
        mediaItem.UserData.Add(UserDataKeysKnown.KEY_PLAY_PERCENTAGE, "0");
      mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_PERCENTAGE] = playPercentage.ToString();

      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfileDataManagement.IsValidUser)
      {
        userProfileDataManagement.UserProfileDataManagement.SetUserMediaItemData(userProfileDataManagement.CurrentUser.ProfileId, mediaItem.MediaItemId,
          UserDataKeysKnown.KEY_PLAY_PERCENTAGE, playPercentage.ToString());
      }

      if (watched)
      {
        // Update loaded item also, so changes will be visible in GUI without reloading
        int currentPlayCount;
        if (MediaItemAspect.TryGetAttribute(mediaItem.Aspects, MediaAspect.ATTR_PLAYCOUNT, 0, out currentPlayCount))
        {
          MediaItemAspect.SetAttribute(mediaItem.Aspects, MediaAspect.ATTR_PLAYCOUNT, ++currentPlayCount);
        }

        if (!mediaItem.UserData.ContainsKey(UserDataKeysKnown.KEY_PLAY_COUNT))
          mediaItem.UserData.Add(UserDataKeysKnown.KEY_PLAY_COUNT, "0");
        currentPlayCount = Convert.ToInt32(mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_COUNT]);
        currentPlayCount++;
        mediaItem.UserData[UserDataKeysKnown.KEY_PLAY_COUNT] = currentPlayCount.ToString();

        if (userProfileDataManagement.IsValidUser)
        {
          userProfileDataManagement.UserProfileDataManagement.SetUserMediaItemData(userProfileDataManagement.CurrentUser.ProfileId, mediaItem.MediaItemId, 
            UserDataKeysKnown.KEY_PLAY_COUNT, currentPlayCount.ToString());
        }
      }
      ContentDirectoryMessaging.SendMediaItemChangedMessage(mediaItem, ContentDirectoryMessaging.MediaItemChangeType.Updated);
    }

    protected void HandlePlayerEnded(IPlayerSlotController psc)
    {
      IPlayerContext pc = PlayerContext.GetPlayerContext(psc);
      if (pc == null || !pc.IsActive)
        return;
      if (!pc.NextItem())
      {
        if (pc.CloseWhenFinished)
          pc.Close();
        else
          psc.Stop();
        CheckMediaWorkflowStates_Async();
      }
    }

    protected void HandlePlayerStopped(IPlayerSlotController psc)
    {
      IPlayerContext pc = PlayerContext.GetPlayerContext(psc);
      if (pc == null || !pc.IsActive)
        return;
      // We get the player message asynchronously, so we have to check the state of the slot again to ensure
      // we close the correct one
      if (pc.CloseWhenFinished && pc.CurrentPlayer == null)
        pc.Close();
      CheckMediaWorkflowStates_Async();
    }

    protected void HandleRequestNextItem(IPlayerSlotController psc)
    {
      PlayerContext pc = PlayerContext.GetPlayerContext(psc);
      if (pc == null || !pc.IsActive)
        return;
      pc.RequestNextItem_NoLock();
    }

    protected void HandleUpdatePlayerRoles(PlayerContext newCurrentPlayerContext, PlayerContext newAudioPlayerContext)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      CurrentPlayerContext = newCurrentPlayerContext;
      playerManager.AudioSlotController = newAudioPlayerContext.PlayerSlotController;
    }

    /// <summary>
    /// Schedules an asynchronous call to <see cref="CheckMediaWorkflowStates_NoLock"/> at the global thread pool.
    /// </summary>
    protected void CheckMediaWorkflowStates_Async()
    {
      ServiceRegistration.Get<IThreadPool>().Add(new DoWorkHandler(CheckMediaWorkflowStates_NoLock), "PlayerContextManager: CheckMediaWorkflowStates");
    }

    /// <summary>
    /// Gets the "currently playing" workflow state for the current player.
    /// </summary>
    /// <returns></returns>
    protected Guid? GetPotentialCPStateId()
    {
      IPlayerContext currentPC = CurrentPlayerContext;
      return currentPC == null ? new Guid?() : currentPC.CurrentlyPlayingWorkflowStateId;
    }

    /// <summary>
    /// Gets the "fullscreen content" workflow state for the primary player.
    /// </summary>
    /// <returns></returns>
    protected Guid? GetPotentialFSCStateId()
    {
      IPlayerContext primaryPC = PrimaryPlayerContext;
      return primaryPC == null ? new Guid?() : primaryPC.FullscreenContentWorkflowStateId;
    }

    /// <summary>
    /// Checks if our "currently playing" and "fullscreen content" states still fit to the
    /// appropriate players, i.e. if we are in a "currently playing" state and the current player context was
    /// changed, the workflow state will be adapted to match the new current player context's "currently playing" state.
    /// The same check will happen for the primary player context and the "fullscreen content" state.
    /// </summary>
    /// <remarks>
    /// This method must not be called when the player manager's lock is held.
    /// </remarks>
    protected void CheckMediaWorkflowStates_NoLock()
    {
      ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
      if (sss.CurrentState != SystemState.Running)
        // Only automatically change workflow states in running state
        return;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.StartBatchUpdateAsync();
      ILogger log = ServiceRegistration.Get<ILogger>();
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      try
      {
        for (int i = 0; i < _playerWfStateInstances.Count; i++)
        {
          // Find the first workflow state of our cached player workflow states which doesn't fit any more
          // and update to the new player workflow state of the same player workflow state type, if necessary.
          PlayerWFStateInstance wfStateInstance = _playerWfStateInstances[i];
          Guid? newStateId;
          string stateName;
          switch (wfStateInstance.WFStateType)
          {
            case PlayerWFStateType.CurrentlyPlaying:
              newStateId = GetPotentialCPStateId();
              stateName = "Currently Playing";
              break;
            case PlayerWFStateType.FullscreenContent:
              newStateId = GetPotentialFSCStateId();
              stateName = "Fullscreen Content";
              break;
            default:
              throw new NotImplementedException(string.Format("No handler for player workflow state type '{0}'",
                  wfStateInstance.WFStateType));
          }
          if (newStateId != wfStateInstance.WFStateId)
          {
            // Found the first player workflow state which doesn't fit any more
            log.Debug("PlayerContextManager: {0} Workflow State '{1}' doesn't fit any more to the current situation. Leaving workflow state.",
                stateName, wfStateInstance.WFStateId);
            lock (playerManager.SyncObj)
              // Remove all workflow states until the player workflow state which doesn't fit any more
              _playerWfStateInstances.RemoveRange(i, _playerWfStateInstances.Count - i);
            workflowManager.NavigatePopToStateAsync(wfStateInstance.WFStateId, true);
            if (newStateId.HasValue)
            {
              log.Debug("PlayerContextManager: Auto-switching to new {0} Workflow State '{1}'",
                  stateName, newStateId.Value);
              workflowManager.NavigatePushAsync(newStateId.Value);
            }
            break;
          }
        }
      }
      finally
      {
        workflowManager.EndBatchUpdateAsync();
      }
    }

    protected int FindIndexOfPlayerWFStateType(PlayerWFStateType type)
    {
      lock (SyncObj)
        for (int i = 0; i < _playerWfStateInstances.Count; i++)
        {
          PlayerWFStateInstance wfStateInstance = _playerWfStateInstances[i];
          if (wfStateInstance.WFStateType == type)
            return i;
        }
      return -1;
    }

    protected PlayerContext GetPlayerContextInternal(int slotIndex)
    {
      IList<PlayerContext> pcs = GetPlayerContexts();
      return pcs.Count <= slotIndex ? null : pcs[slotIndex];
    }

    protected void ClearPlayerContextsCache()
    {
      lock (SyncObj)
        _playerContextsCache = null;
    }

    protected void CleanupPlayerContexts()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        IList<PlayerContext> rawPlayerContexts = playerManager.PlayerSlotControllers.Select(PlayerContext.GetPlayerContext).Where(
                pc => pc != null && pc.IsActive).ToList();
        if (rawPlayerContexts.Count > 0)
        {
          int numPrimaryPlayerContexts = rawPlayerContexts.Where(pc => pc.IsPrimaryPlayerContext).Count();
          int numCurrentPlayerContexts = rawPlayerContexts.Where(pc => pc.IsCurrentPlayerContext).Count();
          if (numPrimaryPlayerContexts == 0)
            rawPlayerContexts[0].IsPrimaryPlayerContext = true;
          if (numCurrentPlayerContexts == 0)
            rawPlayerContexts[0].IsCurrentPlayerContext = true;
        }
        ClearPlayerContextsCache();
      }
    }

    /// <summary>
    /// Returns all active player contexts.
    /// </summary>
    /// <remarks>
    /// This method is very performant and uses an internal cache for retrieving the player contexts list.
    /// </remarks>
    /// <returns>List of all active player contexts in ascending slot index order.</returns>
    protected IList<PlayerContext> GetPlayerContexts()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        if (_playerContextsCache == null)
        {
          _playerContextsCache = playerManager.PlayerSlotControllers.Select(PlayerContext.GetPlayerContext).Where(
                pc => pc != null && pc.IsActive).ToList();
          // The primary player context flag in the player context is the master information. We must arrange our player contexts
          // cache in an order according to that flag, i.e. we must revert our list if the primary player is at the second position.
          if (_playerContextsCache.Count == 2 && _playerContextsCache[PlayerContextIndex.SECONDARY].IsPrimaryPlayerContext)
            _playerContextsCache = _playerContextsCache.Reverse().ToList();
        }
        return _playerContextsCache;
      }
    }

    protected IEnumerable<PlayerContext> GetPlayerContexts(Func<PlayerContext, int, bool> choiceDlgt)
    {
      int slotIndex = -1;
      return GetPlayerContexts().Where(pc => choiceDlgt(pc, ++slotIndex));
    }

    protected void ForEach(Action<PlayerContext, int> action)
    {
      int slotIndex = -1;
      foreach (PlayerContext pc in GetPlayerContexts())
        action(pc, ++slotIndex);
    }

    public int IndexOf(Func<PlayerContext, int, bool> func)
    {
      int slotIndex = -1;
      if (GetPlayerContexts().Any(pc => func(pc, ++slotIndex)))
        return slotIndex;
      return -1;
    }

    protected IOpenPlayerStrategy GetOpenPlayerStrategy()
    {
      ISettingsManager settingsManager = ServiceRegistration.Get<ISettingsManager>();
      PlayerContextManagerSettings settings = settingsManager.Load<PlayerContextManagerSettings>();
      string openPlayerStrategyTypeName = settings.OpenPlayerStrategyTypeName;
      try
      {
        if (!string.IsNullOrEmpty(openPlayerStrategyTypeName))
        {
          Type strategyType = Type.GetType(openPlayerStrategyTypeName);
          if (strategyType == null)
            return new Default();
          ConstructorInfo ci = strategyType.GetConstructor(new Type[] {});
          return (IOpenPlayerStrategy) ci.Invoke(new object[] {});
        }
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("PlayerContextManager: Unable to load open player strategy type '{0}'", e, openPlayerStrategyTypeName);
      }
      return new Default();
    }

    protected bool IsPlayerOfTypePresent<T>()
    {
      return GetPlayerContexts((pc, slotIndex) => pc.CurrentPlayer is T).Any();
    }

    protected void SetCurrentPlayerContext(Func<PlayerContext, int, bool> selector)
    {
        bool changed = false;
        IPlayerContext newCurrentPlayerContext = null;
        ForEach((pc, slotIndex) =>
          {
            bool newVal = selector(pc, slotIndex);
            if (pc.IsCurrentPlayerContext != newVal)
            {
              changed = true;
              pc.IsCurrentPlayerContext = newVal;
            }
          });
          if (!changed)
            return;
          CleanupPlayerContexts();
          CheckMediaWorkflowStates_Async();
          PlayerContextManagerMessaging.SendPlayerContextManagerMessage(PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged, newCurrentPlayerContext);
    }

    #region IDisposable implementation

    public void Dispose()
    {
      UnsubscribeFromMessages();
      ClearPlayerContextsCache();
    }

    #endregion

    #region IPlayerContextManager implementation

    // Returns the player manager's synchronization object; depending on if we already have a local reference to the
    // player manager, we will request the sync object's reference on the player manager itself or we use this property.
    public object SyncObj
    {
      get
      {
        IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
        return playerManager.SyncObj;
      }
    }

    public IList<IPlayerContext> PlayerContexts
    {
      get { return GetPlayerContexts().OfType<IPlayerContext>().ToList(); }
    }

    public bool IsAudioContextActive
    {
      get { return IsPlayerOfTypePresent<IAudioPlayer>(); }
    }

    public bool IsVideoContextActive
    {
      get { return IsPlayerOfTypePresent<IVideoPlayer>(); }
    }

    public bool IsPipActive
    {
      get
      {
        PlayerContext secondaryPC = GetPlayerContextInternal(PlayerContextIndex.SECONDARY);
        IPlayer secondaryPlayer = secondaryPC == null ? null : secondaryPC.CurrentPlayer;
        return secondaryPlayer is IImagePlayer || secondaryPlayer is IVideoPlayer;
      }
    }

    public bool IsCurrentlyPlayingWorkflowStateActive
    {
      get { return FindIndexOfPlayerWFStateType(PlayerWFStateType.CurrentlyPlaying) != -1; }
    }

    public bool IsFullscreenContentWorkflowStateActive
    {
      get { return FindIndexOfPlayerWFStateType(PlayerWFStateType.FullscreenContent) != -1; }
    }

    public IPlayerContext CurrentPlayerContext
    {
      get { return GetPlayerContexts((pc, index) => pc.IsCurrentPlayerContext).FirstOrDefault(); }
      set { SetCurrentPlayerContext((pc, slotIndex) => pc == value); }
    }

    public IPlayerContext PrimaryPlayerContext
    {
      get { return GetPlayerContexts().FirstOrDefault(pc => pc.IsPrimaryPlayerContext); }
    }

    public IPlayerContext SecondaryPlayerContext
    {
      get { return GetPlayerContexts().FirstOrDefault(pc => !pc.IsPrimaryPlayerContext); }
    }

    public int CurrentPlayerIndex
    {
      get
      {
        lock (SyncObj)
          return IndexOf((pc, index) => pc.IsCurrentPlayerContext);
      }
      set { SetCurrentPlayerContext((pc, slotIndex) => slotIndex == value); }
    }

    public int NumActivePlayerContexts
    {
      get { return GetPlayerContexts().Count(); }
    }

    public IPlayer this[int slotIndex]
    {
      get
      {
        lock (SyncObj)
        {
          PlayerContext pc = GetPlayerContextInternal(slotIndex);
          return pc == null ? null : pc.CurrentPlayer;
        }
      }
    }

    public void Shutdown()
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      ForEach((pc, slotIndex) =>
        {
          IPlayerSlotController psc = pc.Revoke();
          if (psc != null)
            playerManager.CloseSlot(psc);
        });
      UnsubscribeFromMessages();
    }

    public IPlayerContext OpenAudioPlayerContext(Guid mediaModuleId, string name, bool concurrentVideo, Guid currentlyPlayingWorkflowStateId,
        Guid fullscreenContentWorkflowStateId)
    {
      IOpenPlayerStrategy strategy = GetOpenPlayerStrategy();
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int currentPlayerSlotIndex;
        IPlayerSlotController slotController = strategy.PrepareAudioPlayerSlotController(playerManager, this, concurrentVideo, out currentPlayerSlotIndex);
        slotController.Closed += OnPlayerSlotControllerClosed;
        IPlayerContext result = new PlayerContext(slotController, mediaModuleId, name, AVType.Audio, currentlyPlayingWorkflowStateId, fullscreenContentWorkflowStateId);
        CleanupPlayerContexts();
        IList<IPlayerContext> playerContexts = PlayerContexts;
        int lastPlayerContextIndex = playerContexts.Count - 1;
        if (currentPlayerSlotIndex > lastPlayerContextIndex)
          currentPlayerSlotIndex = lastPlayerContextIndex;
        PlayerContextManagerMessaging.SendUpdatePlayerRolesMessage(playerContexts[currentPlayerSlotIndex], playerContexts[lastPlayerContextIndex]);
        return result;
      }
    }

    public IPlayerContext OpenVideoPlayerContext(Guid mediaModuleId, string name, PlayerContextConcurrencyMode concurrencyMode,
        Guid currentlyPlayingWorkflowStateId, Guid fullscreenContentWorkflowStateId)
    {
      IOpenPlayerStrategy strategy = GetOpenPlayerStrategy();
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      lock (playerManager.SyncObj)
      {
        int audioPlayerSlotIndex;
        int currentPlayerSlotIndex;
        bool makePrimaryPlayer;
        IPlayerSlotController slotController = strategy.PrepareVideoPlayerSlotController(playerManager, this, concurrencyMode,
            out makePrimaryPlayer, out audioPlayerSlotIndex, out currentPlayerSlotIndex);
        slotController.Closed += OnPlayerSlotControllerClosed;
        if (makePrimaryPlayer)
        {
          PlayerContext oldPrimaryPC = GetPlayerContextInternal(PlayerContextIndex.PRIMARY);
          if (oldPrimaryPC != null)
            oldPrimaryPC.IsPrimaryPlayerContext = false;
        }
        PlayerContext result = new PlayerContext(slotController, mediaModuleId, name, AVType.Video, currentlyPlayingWorkflowStateId, fullscreenContentWorkflowStateId);
        if (makePrimaryPlayer)
          result.IsPrimaryPlayerContext = true;
        CleanupPlayerContexts();
        IList<IPlayerContext> playerContexts = PlayerContexts;
        int lastPlayerContextIndex = playerContexts.Count - 1;
        if (currentPlayerSlotIndex > lastPlayerContextIndex)
          currentPlayerSlotIndex = lastPlayerContextIndex;
        if (audioPlayerSlotIndex > lastPlayerContextIndex)
          audioPlayerSlotIndex = lastPlayerContextIndex;
        PlayerContextManagerMessaging.SendUpdatePlayerRolesMessage(playerContexts[currentPlayerSlotIndex], playerContexts[audioPlayerSlotIndex]);
        return result;
      }
    }

    public IEnumerable<IPlayerContext> GetPlayerContextsByMediaModuleId(Guid mediaModuleId)
    {
      lock (SyncObj)
      {
        return GetPlayerContexts((pc, slotIndex) => pc.MediaModuleId == mediaModuleId).OfType<IPlayerContext>();
      }
    }

    public IEnumerable<IPlayerContext> GetPlayerContextsByAVType(AVType avType)
    {
      lock (SyncObj)
      {
        return GetPlayerContexts((pc, slotIndex) => pc.AVType == avType).OfType<IPlayerContext>();
      }
    }

    public void CloseAllPlayerContexts()
    {
      foreach (PlayerContext playerContext in GetPlayerContexts().Reverse())
        playerContext.Close();
    }

    public void ShowCurrentlyPlaying(bool asynchronously)
    {
      Guid currentlyPlayingStateId;
      lock (SyncObj)
      {
        if (IsCurrentlyPlayingWorkflowStateActive)
          return;
        IPlayerContext pc = CurrentPlayerContext;
        if (pc == null)
          return;
        currentlyPlayingStateId = pc.CurrentlyPlayingWorkflowStateId;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (asynchronously)
        workflowManager.NavigatePushAsync(currentlyPlayingStateId);
      else
        workflowManager.NavigatePush(currentlyPlayingStateId);
      // TODO: Would be good if we could wait for the CP screen to be shown. But to achieve that, we need the ScreenManager to send
      // a message when a screen change has completed.
    }

    public void ShowFullscreenContent(bool asynchronously)
    {
      Guid fullscreenContentStateId;
      lock (SyncObj)
      {
        if (IsFullscreenContentWorkflowStateActive)
          return;
        IPlayerContext pc = PrimaryPlayerContext;
        if (pc == null)
          return;
        fullscreenContentStateId = pc.FullscreenContentWorkflowStateId;
      }
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (asynchronously)
        workflowManager.NavigatePushAsync(fullscreenContentStateId);
      else
        workflowManager.NavigatePush(fullscreenContentStateId);
      // TODO: Would be good if we could wait for the FSC screen to be shown. But to achieve that, we need the ScreenManager to send
      // a message when a screen change has completed.
    }

    public AVType GetTypeOfMediaItem(MediaItem item)
    {
      // No locking necessary
      if (item.Aspects.ContainsKey(VideoAspect.Metadata.AspectId) ||
          item.Aspects.ContainsKey(ImageAspect.Metadata.AspectId))
        return AVType.Video;
      if (item.Aspects.ContainsKey(AudioAspect.Metadata.AspectId))
        return AVType.Audio;
      return AVType.None;
    }

    public IPlayerContext GetPlayerContext(PlayerChoice player)
    {
      lock (SyncObj)
      {
        switch (player)
        {
          case PlayerChoice.PrimaryPlayer:
            return GetPlayerContextInternal(PlayerContextIndex.PRIMARY);
          case PlayerChoice.SecondaryPlayer:
            return GetPlayerContextInternal(PlayerContextIndex.SECONDARY);
          case PlayerChoice.CurrentPlayer:
            return CurrentPlayerContext;
          case PlayerChoice.NotCurrentPlayer:
            return GetPlayerContexts((pc, slotIndex) => !pc.IsCurrentPlayerContext).FirstOrDefault();
          default:
            ServiceRegistration.Get<ILogger>().Warn("PlayerContextManager.GetPlayerContext: No handler implemented for PlayerChoice enum value '" + player + "'");
            return null;
        }
      }
    }

    public IPlayerContext GetPlayerContext(int slotIndex)
    {
      return GetPlayerContextInternal(slotIndex);
    }

    public ICollection<AudioStreamDescriptor> GetAvailableAudioStreams(out AudioStreamDescriptor currentAudioStream)
    {
      currentAudioStream = null;
      ICollection<AudioStreamDescriptor> result = new List<AudioStreamDescriptor>();
      foreach(IPlayerContext playerContext in GetPlayerContexts())
      {
        AudioStreamDescriptor current;
        CollectionUtils.AddAll(result, playerContext.GetAudioStreamDescriptors(out current));
        IPlayerSlotController psc = playerContext.PlayerSlotController;
        if (psc != null && psc.IsAudioSlot)
          currentAudioStream = current;
      }
      return result;
    }

    public bool SetAudioStream(AudioStreamDescriptor stream)
    {
      IPlayerManager playerManager = ServiceRegistration.Get<IPlayerManager>();
      IPlayerContext playerContext = stream.PlayerContext;
      lock (playerManager.SyncObj)
      {
        if (!playerContext.IsActive)
          return false;
        IPlayer player = playerContext.CurrentPlayer;
        if (player == null || player.Name != stream.PlayerName)
          return false;
        IVideoPlayer videoPlayer = player as IVideoPlayer;
        if (videoPlayer != null)
          videoPlayer.SetAudioStream(stream.AudioStreamName);
        playerManager.AudioSlotController = playerContext.PlayerSlotController;
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
      lock (SyncObj)
        CurrentPlayerIndex = 1 - CurrentPlayerIndex;
      CleanupPlayerContexts();
    }

    public void SwitchPipPlayers()
    {
      lock (SyncObj)
      {
        IList<PlayerContext> pcs = GetPlayerContexts();
        if (pcs.Count < 2)
          // We don't have enough active slots to switch
          return;
        PlayerContext first = pcs[0];
        PlayerContext second = pcs[1];
        first.IsPrimaryPlayerContext = !first.IsPrimaryPlayerContext;
        second.IsPrimaryPlayerContext = !second.IsPrimaryPlayerContext;
        ClearPlayerContextsCache();
        // Audio and Current player change their index automatically as the information is stored in the player context instance itself
        PlayerContextManagerMessaging.SendPlayerContextManagerMessage(PlayerContextManagerMessaging.MessageType.PlayerSlotsChanged);
      }
    }

    #endregion
  }
}
