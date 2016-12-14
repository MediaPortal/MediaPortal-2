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

using System.Collections.Generic;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Actions;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.SkinBase.Models;

namespace MediaPortal.UiComponents.SkinBase
{
  public class PlayerBackgroundManager : IBackgroundManager
  {
    protected ICollection<Key> _registeredKeyBindings = new List<Key>();
    protected object _syncObj = new object();
    protected AsynchronousMessageQueue _messageQueue = null;

    internal void DoInstall()
    {
      RegisterKeyBindings();

      // Set initial background
      UpdateBackground();

      // Install message queue
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           PlayerManagerMessaging.CHANNEL,
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

    static void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == PlayerManagerMessaging.CHANNEL)
        UpdateBackground();
    }

    /// <summary>
    /// Returns the player context for the current focused player.
    /// </summary>
    /// <returns>Player context for the current player or <c>null</c>, if there is no current player focus.</returns>
    protected static IPlayerContext GetCurrentPlayerContext()
    {
      IPlayerContextManager pcm = ServiceRegistration.Get<IPlayerContextManager>();
      return pcm.GetPlayerContext(PlayerChoice.CurrentPlayer);
    }

    /// <summary>
    /// Registers key bindings for the currently active player, if there is a player active.
    /// </summary>
    protected void RegisterKeyBindings()
    {
      // TODO: Is there a ZoomMode/Change Aspect Ratio key in any input device (keyboard, IR, ...)? If yes,
      // we should register it here too
      lock (_syncObj)
      {
        // ------------------ Play controls ---------------------
        AddKeyBinding_NeedLock(Key.Play, GeneralPlayerModel.Play);
        AddKeyBinding_NeedLock(Key.Pause, GeneralPlayerModel.Pause);
        AddKeyBinding_NeedLock(Key.PlayPause, GeneralPlayerModel.TogglePause);
        AddKeyBinding_NeedLock(Key.Printable(' '), GeneralPlayerModel.TogglePause);
        AddKeyBinding_NeedLock(Key.Stop, GeneralPlayerModel.Stop);
        AddKeyBinding_NeedLock(Key.Rew, GeneralPlayerModel.SeekBackward);
        AddKeyBinding_NeedLock(Key.Fwd, GeneralPlayerModel.SeekForward);
        AddKeyBinding_NeedLock(Key.Previous, GeneralPlayerModel.Previous);
        AddKeyBinding_NeedLock(Key.Next, GeneralPlayerModel.Next);

        // ------------------------ Volume -----------------------
        AddKeyBinding_NeedLock(Key.Mute, GeneralPlayerModel.ToggleMute);
        AddKeyBinding_NeedLock(Key.VolumeUp, GeneralPlayerModel.VolumeUp);
        AddKeyBinding_NeedLock(Key.VolumeDown, GeneralPlayerModel.VolumeDown);

        // --------------------- Player management --------------------
        AddKeyBinding_NeedLock(Key.Yellow, GeneralPlayerModel.SwitchPipPlayers);
        AddKeyBinding_NeedLock(Key.Blue, GeneralPlayerModel.ToggleCurrentPlayer);

        // Avoid registering of printable keys here. Otherwise this key is not available 
        // i.e. inside MediaNavigation to skip to the first item with this letter.

        // Don't register player specific key bindings here.
        // They should only be available in the FSC/CP states.
      }
    }

    protected void AddKeyBinding_NeedLock(Key key, VoidKeyActionDlgt action)
    {
      _registeredKeyBindings.Add(key);
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.AddKeyBinding(key, action);
    }

    /// <summary>
    /// Removes all key bindings which have been globally registered before.
    /// </summary>
    protected void UnregisterKeyBindings()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>(false);
      if (inputManager == null)
        return;
      lock (_syncObj)
        foreach (Key key in _registeredKeyBindings)
          inputManager.RemoveKeyBinding(key);
    }

    protected static void UpdateBackground()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      string targetBackgroundScreen = GetTargetBackgroundScreen();
      if (screenManager.ActiveBackgroundScreenName != targetBackgroundScreen)
        screenManager.SetBackgroundLayer(targetBackgroundScreen);
    }

    protected static string GetTargetBackgroundScreen()
    {
      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      if (playerContextManager.NumActivePlayerContexts == 0)
        return Consts.SCREEN_DEFAULT_BACKGROUND;
      IPlayerContext pcPrimary = playerContextManager.PrimaryPlayerContext;
      IPlayerSlotController pscPrimary = pcPrimary != null && pcPrimary.IsActive ? pcPrimary.PlayerSlotController : null;
      IPlayer pPrimary = pscPrimary == null ? null : pscPrimary.CurrentPlayer;
      if (pPrimary != null)
      {
        if (pPrimary is IVideoPlayer)
          return Consts.SCREEN_VIDEO_BACKGROUND;
        if (pPrimary is IImagePlayer)
          return Consts.SCREEN_IMAGE_BACKGROUND;
      }
      return Consts.SCREEN_DEFAULT_BACKGROUND;
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