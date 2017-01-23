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

using System.Linq;
using System.ServiceProcess;
using MediaPortal.Backend.ClientCommunication;
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.Common.SystemResolver;

namespace MediaPortal.Server
{
  public partial class WindowsService : ServiceBase
  {
    protected ApplicationLauncher _launcher = null;
    protected AsynchronousMessageQueue _messageQueue = null;
    protected SuspendLevel _suspendLevel = SuspendLevel.None;
    public WindowsService()
    {
      InitializeComponent();
      ServiceName = "MP2 Server Service";
      CanStop = true;
      CanPauseAndContinue = false;
      CanHandlePowerEvent = true;
      AutoLog = false;
    }

    protected override void OnStart(string[] args)
    {
      if (_launcher != null)
        return;
      _launcher = new ApplicationLauncher(null);
      _launcher.Start();
      SubscribeToMessages();
      UpdatePowerState();
    }

    protected override void OnStop()
    {
      UnsubscribeFromMessages();
      if (_launcher == null)
        return;
      _launcher.Stop();
    }

    protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
    {
      if (powerStatus == PowerBroadcastStatus.QuerySuspend)
      {
        // Deny suspend if required.
        if (_suspendLevel > SuspendLevel.None)
          return false;
      }
      return base.OnPowerEvent(powerStatus);
    }

    #region Message and power state handling

    protected void SubscribeToMessages()
    {
      _messageQueue = new AsynchronousMessageQueue("WindowsService", new[] { ClientManagerMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    protected void UnsubscribeFromMessages()
    {
      if (_messageQueue == null)
        return;
      _messageQueue.Shutdown();
      _messageQueue = null;
    }

    protected void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName != ClientManagerMessaging.CHANNEL)
        return;

      ClientManagerMessaging.MessageType messageType = (ClientManagerMessaging.MessageType) message.MessageType;
      switch (messageType)
      {
        case ClientManagerMessaging.MessageType.ClientOnline:
        case ClientManagerMessaging.MessageType.ClientOffline:
          UpdatePowerState();
          break;
      }
    }

    protected void UpdatePowerState()
    {
      IClientManager clientManager = ServiceRegistration.Get<IClientManager>(false);
      if (clientManager == null)
        return;

      int countRemoteSystems = clientManager.ConnectedClients.
        Select(clientConnection => clientConnection.Descriptor.MPFrontendServerUUID).
        Count(mpFrontendServerUUID => !ServiceRegistration.Get<ISystemResolver>().GetSystemNameForSystemId(mpFrontendServerUUID).IsLocalSystem());

      // Avoid suspend as long as remote clients are connected. A local client prevents suspend using its own logic.
      // Set a continous state for current thread (which is the AMQ thread, not the "MainThread").
      _suspendLevel = countRemoteSystems > 0 ? SuspendLevel.AvoidSuspend : SuspendLevel.None;
      ServiceRegistration.Get<ILogger>().Debug("UpdatePowerState: Setting continuous suspend level to {0}", _suspendLevel);
      ServiceRegistration.Get<ISystemStateService>().SetCurrentSuspendLevel(_suspendLevel, true);
    }

    #endregion
  }
}
