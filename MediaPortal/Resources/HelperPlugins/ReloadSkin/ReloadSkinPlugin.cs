#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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

using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Runtime;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.Helpers.ReloadSkin
{
  public class ReloadSkinPlugin : IPluginStateTracker
  {
    #region Consts

    // F5 is already used for media screen refresh
    public static readonly Key RELOAD_SCREEN_KEY = Key.F3;
    public static readonly Key RELOAD_THEME_KEY = Key.F4;

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();

    #endregion

    protected void DropMessageQueue()
    {
      lock (_syncObj)
      {
        if (_messageQueue != null)
          _messageQueue.Terminate();
        _messageQueue = null;
      }
    }

    #region Implementation of IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
      if (sss.CurrentState == SystemState.Running)
        AddKeyActions();
      else
      {
        _messageQueue = new AsynchronousMessageQueue(typeof(ReloadSkinPlugin), new string[]
          {
              SystemMessaging.CHANNEL
          });
        _messageQueue.MessageReceived += OnMessageReceived;
        _messageQueue.Start();
      }
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType) message.MessageType;
        if (messageType == SystemMessaging.MessageType.SystemStateChanged)
        {
          SystemState newState = (SystemState) message.MessageData[SystemMessaging.NEW_STATE];
          if (newState == SystemState.Running)
          {
            AddKeyActions();
            DropMessageQueue();
          }
        }
      }
    }

    static void AddKeyActions()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.AddKeyBinding(RELOAD_SCREEN_KEY, ReloadScreenAction);
      inputManager.AddKeyBinding(RELOAD_THEME_KEY, ReloadThemeAction);
    }

    static void ReloadScreenAction()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.Reload();
    }

    static void ReloadThemeAction()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.ReloadSkinAndTheme();
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.RemoveKeyBinding(RELOAD_SCREEN_KEY);
      inputManager.RemoveKeyBinding(RELOAD_THEME_KEY);
      DropMessageQueue();
    }

    public void Continue()
    {
      // Nothing to do
    }

    public void Shutdown()
    {
      // Nothing to do
    }

    #endregion
  }
}
