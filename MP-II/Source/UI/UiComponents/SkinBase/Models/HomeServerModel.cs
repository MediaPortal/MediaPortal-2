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
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;

namespace UiComponents.SkinBase.Models
{
  /// <summary>
  /// Model which attends the workflow state "ShowHomeServer".
  /// </summary>
  public class HomeServerModel : IWorkflowModel
  {
    #region Consts

    protected const string MODEL_ID_STR = "854ABA9A-71A1-420b-A657-9641815F9C01";

    protected const string SHOW_HOMESERVER_SCREEN = "ShowHomeServer";
    protected const string NO_HOMESERVER_SCREEN = "NoHomeServer";

    protected const string SERVER_FORMAT_TEXT_RES = "[ServerConnection.ServerFormatText]";

    protected const string UNKNOWN_SERVER_NAME_RES = "[ServerConnection.UnknownServerName]";
    protected const string UNKNOWN_SERVER_SYSTEM_RES = "[ServerConnection.UnknownServerSystem]";

    protected const string CONFIGURE_HOME_SERVER_STATE_STR = "17214BAC-E79C-4e5e-9280-A01478B27579";

    public const string SERVER_DESCRIPTOR_KEY = "ServerDescriptor";
    public const string NAME_KEY = "Name";
    public const string SYSTEM_KEY = "System";

    protected static Guid MODEL_ID = new Guid(MODEL_ID_STR);

    /// <summary>
    /// In this state, the <see cref="HomeServerModel"/> shows a screen which displays the current home server.
    /// </summary>
    /// <remarks>
    /// When the connection state changes, the screen will automatically change to reflect the current connection state.
    /// </remarks>
    public static Guid CONFIGURE_HOME_SERVER_STATE = new Guid(CONFIGURE_HOME_SERVER_STATE_STR);

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();
    protected Property _homeServerProperty;
    protected Property _homeServerConnectedProperty;
    protected Property _isHomeServerAttachedProperty;

    #endregion

    public HomeServerModel()
    {
      _homeServerProperty = new Property(typeof(string), string.Empty);
      _homeServerConnectedProperty = new Property(typeof(bool), false);
      _isHomeServerAttachedProperty = new Property(typeof(bool), false);
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            ServerConnectionMessaging.CHANNEL
          });
      _messageQueue.MessageReceived += OnMessageReceived;
      // Message queue will be started in method EnterModelContext
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, QueueMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType) message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            SynchronizeHomeServer();
            break;
        }
      }
    }

    protected void SynchronizeHomeServer()
    {
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      if (string.IsNullOrEmpty(scm.HomeServerUUID))
      {
        IsHomeServerAttached = false;
        HomeServer = null;
        HomeServerConnected = false;
      }
      else
      {
        string serverName = scm.LastHomeServerName ?? LocalizationHelper.Translate(UNKNOWN_SERVER_NAME_RES);
        SystemName system = scm.LastHomeServerSystem;
        HomeServer = LocalizationHelper.Translate(SERVER_FORMAT_TEXT_RES, serverName,
            system == null ? LocalizationHelper.Translate(UNKNOWN_SERVER_SYSTEM_RES) : system.HostName);
        HomeServerConnected = scm.IsHomeServerConnected;
        IsHomeServerAttached = true;
      }
    }

    #region Public members to be called from the GUI

    public bool IsHomeServerAttached
    {
      get { return (bool) _isHomeServerAttachedProperty.GetValue(); }
      set { _isHomeServerAttachedProperty.SetValue(value); }
    }

    public Property IsHomeServerAttachedProperty
    {
      get { return _isHomeServerAttachedProperty; }
    }

    public string HomeServer
    {
      get { return (string) _homeServerProperty.GetValue(); }
      set { _homeServerProperty.SetValue(value); }
    }

    public Property HomeServerProperty
    {
      get { return _homeServerProperty; }
    }

    public bool HomeServerConnected
    {
      get { return (bool) _homeServerConnectedProperty.GetValue(); }
      set { _homeServerConnectedProperty.SetValue(value); }
    }

    public Property HomeServerConnectedProperty
    {
      get { return _homeServerConnectedProperty; }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      lock (_syncObj)
        _messageQueue.Start();
      SynchronizeHomeServer();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      lock (_syncObj)
        _messageQueue.Shutdown();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      // Nothing to do
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void ReActivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do
    }

    public void UpdateMenuActions(NavigationContext context, ICollection<WorkflowAction> actions)
    {
      // Nothing to do
    }

    #endregion
  }
}
