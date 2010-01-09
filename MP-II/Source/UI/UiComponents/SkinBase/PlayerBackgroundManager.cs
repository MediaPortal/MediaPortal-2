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

using System.Collections.Generic;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using UiComponents.SkinBase.Models;

namespace UiComponents.SkinBase
{
  public class PlayerBackgroundManager : IBackgroundManager
  {
    public static string DEFAULT_BACKGROUND_SCREEN = "default-background";
    public static string VIDEO_BACKGROUND_SCREEN = "video-background";
    public static string PICTURE_BACKGROUND_SCREEN = "picture-background";

    protected ICollection<Key> _registeredKeyBindings = new List<Key>();
    protected object _syncObj = new object();
    protected AsynchronousMessageQueue _messageQueue = null;

    internal void DoInstall()
    {
      // Set initial background
      UpdateBackground();

      // Install message queue
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlayerManagerMessaging.CHANNEL,
           PlayerContextManagerMessaging.CHANNEL,
        });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    internal void DoUninstall()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
      {
        UpdateKeyBindings();
        UpdateBackground();
      }
      else if (message.ChannelName == PlayerContextManagerMessaging.CHANNEL)
      {
        PlayerContextManagerMessaging.MessageType messageType =
            (PlayerContextManagerMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case PlayerContextManagerMessaging.MessageType.CurrentPlayerChanged:
            UpdateKeyBindings();
            break;
        }
      }
    }

    /// <summary>
    /// Returns the player context for the current focused player.
    /// </summary>
    /// <returns>Player context for the current player or <c>null</c>, if there is no current player focus.</returns>
    protected static IPlayerContext GetCurrentPlayerContext()
    {
      IPlayerContextManager pcm = ServiceScope.Get<IPlayerContextManager>();
      int currentPlayerSlot = pcm.CurrentPlayerIndex;
      if (currentPlayerSlot == -1)
        currentPlayerSlot = PlayerManagerConsts.PRIMARY_SLOT;
      return pcm.GetPlayerContext(currentPlayerSlot);
    }

    /// <summary>
    /// Updates the globally registered key bindings depending on the current player. Will be called when the
    /// currently active player changes.
    /// </summary>
    protected void UpdateKeyBindings()
    {
      UnregisterKeyBindings();
      RegisterKeyBindings();
    }

    /// <summary>
    /// Registers key bindings for the currently active player, if there is a player active.
    /// </summary>
    protected void RegisterKeyBindings()
    {
      IPlayerContext currentPSC = GetCurrentPlayerContext();
      if (currentPSC == null)
        return;
      // TODO: Is there a ZoomMode/Change Aspect Ratio key in any input device (keyboard, IR, ...)? If yes,
      // we should register it here too
      lock (_syncObj)
      {
        // ------------------ Play controls ---------------------
        AddKeyBinding_NeedLock(
            Key.Play, () =>
              {
                PlayerModel.Play();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.Pause, () =>
              {
                PlayerModel.Pause();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.PlayPause, () =>
              {
                PlayerModel.TogglePause();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.Printable(' '), () =>
              {
                PlayerModel.TogglePause();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.Stop, () =>
              {
                PlayerModel.Stop();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.Rew, () =>
              {
                PlayerModel.SeekBackward();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.Fwd, () =>
              {
                PlayerModel.SeekForward();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.Previous, () =>
              {
                PlayerModel.Previous();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.Next, () =>
              {
                PlayerModel.Next();
                return true;
              });

        // ------------------------ Volume -----------------------
        AddKeyBinding_NeedLock(
            Key.Mute, () =>
              {
                PlayerModel.ToggleMute();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.VolumeUp, () =>
              {
                PlayerModel.VolumeUp();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.VolumeDown, () =>
              {
                PlayerModel.VolumeDown();
                return true;
              });

        // --------------------- Player management --------------------
        AddKeyBinding_NeedLock(
            Key.Blue, () =>
              {
                PlayerModel.ToggleCurrentPlayer();
                return true;
              });
        AddKeyBinding_NeedLock(
            Key.Printable('c'), () =>
              {
                PlayerModel.ToggleCurrentPlayer();
                return true;
              });

        // Register player specific key bindings
        // TODO: Register key bindings from current player
      }
    }

    protected void AddKeyBinding_NeedLock(Key key, ActionDlgt action)
    {
      _registeredKeyBindings.Add(key);
      IInputManager inputManager = ServiceScope.Get<IInputManager>();
      inputManager.AddKeyBinding(key, action);
    }

    /// <summary>
    /// Removes all key bindings which have been globally registered before.
    /// </summary>
    protected void UnregisterKeyBindings()
    {
      IInputManager inputManager = ServiceScope.Get<IInputManager>(false);
      if (inputManager == null)
        return;
      lock (_syncObj)
        foreach (Key key in _registeredKeyBindings)
          inputManager.RemoveKeyBinding(key);
    }

    protected static void UpdateBackground()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      string targetBackgroundScreen = GetTargetBackgroundScreen();
      if (screenManager.ActiveBackgroundScreenName != targetBackgroundScreen)
        screenManager.SetBackgroundLayer(targetBackgroundScreen);
    }

    protected static string GetTargetBackgroundScreen()
    {
      IPlayerManager playerManager = ServiceScope.Get<IPlayerManager>();
      if (playerManager.NumActiveSlots == 0)
        return DEFAULT_BACKGROUND_SCREEN;
      IPlayerSlotController pscPrimary = playerManager.GetPlayerSlotController(PlayerManagerConsts.PRIMARY_SLOT);
      if (pscPrimary.IsActive && pscPrimary.PlayerSlotState != PlayerSlotState.Stopped)
      {
        if (pscPrimary.CurrentPlayer is IVideoPlayer)
          return VIDEO_BACKGROUND_SCREEN;
        else if (pscPrimary.CurrentPlayer is IPicturePlayer)
          return PICTURE_BACKGROUND_SCREEN;
      }
      return DEFAULT_BACKGROUND_SCREEN;
    }

    #region IBackgroundManager implementation

    public void Install()
    {
      DoInstall();
    }

    public void Uninstall()
    {
      DoUninstall();
    }

    #endregion
  }
}