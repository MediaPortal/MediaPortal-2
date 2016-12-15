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
using MediaPortal.Common;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Media.Actions
{
  public abstract class BaseTrackServerConnectionAction : IWorkflowContributor
  {
    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue = null;

    protected bool _isHomeServerConnected = false;

    #endregion

    void InitializeMessageQueue()
    {
      _messageQueue = new AsynchronousMessageQueue(this, GetMessageChannels());
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    /// <summary>
    /// Can be overridden by decendents to register additional message channels and to add additional message handlers to the queue.
    /// </summary>
    protected virtual IEnumerable<string> GetMessageChannels()
    {
      return new List<string> {ServerConnectionMessaging.CHANNEL};
    }

    void DisposeMessageQueue()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    protected virtual void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType = (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            Update();
            break;
        }
      }
    }

    protected virtual void Update()
    {
      IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
      _isHomeServerConnected = scm.IsHomeServerConnected;
    }

    protected void FireStateChanged()
    {
      ContributorStateChangeDelegate d = StateChanged;
      if (d != null) d();
    }

    #region IWorkflowContributor implementation

    public event ContributorStateChangeDelegate StateChanged;

    public abstract IResourceString DisplayTitle { get; }

    public virtual void Initialize()
    {
      InitializeMessageQueue();
      Update();
    }

    public virtual void Uninitialize()
    {
      DisposeMessageQueue();
    }

    public abstract bool IsActionVisible(NavigationContext context);

    public abstract bool IsActionEnabled(NavigationContext context);

    public abstract void Execute();

    #endregion
  }
}
