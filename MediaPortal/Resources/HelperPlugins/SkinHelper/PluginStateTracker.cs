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

using MediaPortal.Common;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.PluginManager;
using MediaPortal.Common.Runtime;

namespace MediaPortal.Helpers.SkinHelper
{
  public class PluginStateTracker : IPluginStateTracker
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();

    protected ReloadSkinActions _reloadSkinActions = null;
    protected LoadSkinThemeActions _loadSkinThemeActions = null;

    #endregion

    public PluginStateTracker()
    {
      _reloadSkinActions = new ReloadSkinActions();
      _loadSkinThemeActions = new LoadSkinThemeActions();
    }

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
      {
        _reloadSkinActions.RegisterKeyActions();
        _loadSkinThemeActions.RegisterKeyActions();
      }
      else
      {
        _messageQueue = new AsynchronousMessageQueue(typeof(ReloadSkinActions), new string[]
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
            _reloadSkinActions.RegisterKeyActions();
            _loadSkinThemeActions.RegisterKeyActions();
            DropMessageQueue();
          }
        }
      }
    }

    public bool RequestEnd()
    {
      return true;
    }

    public void Stop()
    {
      DropMessageQueue();
      _reloadSkinActions.UnregisterKeyActions();
      _loadSkinThemeActions.UnregisterKeyActions();
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
