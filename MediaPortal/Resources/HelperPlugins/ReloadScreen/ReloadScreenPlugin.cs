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

using MediaPortal.Core;
using MediaPortal.Core.Messaging;
using MediaPortal.Core.PluginManager;
using MediaPortal.Core.Runtime;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Screens;

namespace MediaPortal.Helpers.ReloadScreen
{
  public class ReloadScreenPlugin : IPluginStateTracker
  {
    #region Consts

    public static readonly Key REFRESH_KEY = Key.F4;

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;

    #endregion

    #region Implementation of IPluginStateTracker

    public void Activated(PluginRuntime pluginRuntime)
    {
      ISystemStateService sss = ServiceRegistration.Get<ISystemStateService>();
      if (sss.CurrentState == SystemState.Running)
        AddKeyAction();
      else
      {
        _messageQueue = new AsynchronousMessageQueue(typeof(ReloadScreenPlugin), new string[]
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
            AddKeyAction();
        }
      }
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    static void AddKeyAction()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.AddKeyBinding(REFRESH_KEY, RefreshScreenAction); // Use F4 because F5 is already used for media screen refresh
    }

    static void RefreshScreenAction()
    {
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      screenManager.Reload();
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      inputManager.RemoveKeyBinding(REFRESH_KEY);
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
