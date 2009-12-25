#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
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
using MediaPortal.Core.Commands;
using MediaPortal.Core.General;
using MediaPortal.Core.Localization;
using MediaPortal.Core.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;

namespace UiComponents.SkinBase.Models
{
  /// <summary>
  /// Model which attends the dialog workflow states "AttachToServer" and "DetachFromServer".
  /// </summary>
  public class ServerAttachmentModel : IWorkflowModel
  {
    public enum Mode
    {
      None,
      AttachToServer,
      DetachFromServer
    }

    #region Consts

    protected const string MODEL_ID_STR = "81A130E1-F417-47e4-AC9C-0B2E4912331F";

    protected const string ATTACH_TO_SERVER_DIALOG = "AttachToServerDialog";

    protected const string ATTACH_INFO_DIALOG_HEADER_RES = "[ServerConnection.AttachInfoDialogHeader]";
    protected const string ATTACH_INFO_DIALOG_TEXT_RES = "[ServerConnection.AttachInfoDialogText]";

    protected const string DETACH_CONFIRM_DIALOG_HEADER_RES = "[ServerConnection.DetachConfirmDialogHeader]";
    protected const string DETACH_CONFIRM_DIALOG_TEXT_RES = "[ServerConnection.DetachConfirmDialogText]";

    protected const string SERVER_FORMAT_TEXT_RES = "[ServerConnection.ServerFormatText]";

    protected const string UNKNOWN_SERVER_NAME_RES = "[ServerConnection.UnknownServerName]";
    protected const string UNKNOWN_SERVER_SYSTEM_RES = "[ServerConnection.UnknownServerSystem]";

    protected const string ATTACH_TO_SERVER_STATE_STR = "E834D0E0-BC35-4397-86F8-AC78C152E693";
    protected const string DETACH_FROM_SERVER_STATE_STR = "BAC42991-5AB6-471f-A185-673D2E3B1EBA";

    public const string SERVER_DESCRIPTOR_KEY = "ServerDescriptor";
    public const string NAME_KEY = "Name";
    public const string SERVER_NAME_KEY = "ServerName";
    public const string SYSTEM_KEY = "System";

    public const string AUTO_CLOSE_ON_NO_SERVER_KEY = "AutoCloseOnNoServer";

    protected static Guid MODEL_ID = new Guid(MODEL_ID_STR);

    /// <summary>
    /// In this state, the <see cref="ServerAttachmentModel"/> shows configuration dialogs to choose one of the home server
    /// which are present in the network.
    /// </summary>
    public static Guid ATTACH_TO_SERVER_STATE = new Guid(ATTACH_TO_SERVER_STATE_STR);

    /// <summary>
    /// In this state, the <see cref="ServerAttachmentModel"/> shows a dialog where it asks the user if he really
    /// wants to detach from the current home server.
    /// </summary>
    public static Guid DETACH_FROM_SERVER_STATE = new Guid(DETACH_FROM_SERVER_STATE_STR);

    #endregion

    #region Protected fields

    protected AsynchronousMessageQueue _messageQueue;
    protected object _syncObj = new object();
    protected ItemsList _availableServers;
    protected Property _singleServerProperty;
    protected Property _isNoServerAvailableProperty;
    protected Property _isMultipleServersAvailableProperty;
    protected Property _isSingleServerAvailableProperty;
    protected ServerDescriptor _singleAvailableServer;
    protected Guid? _attachInfoDialogHandle = null; // null = no dialog shown, Guid.Empty = don't leave WF, attach info dialog will be shown, some GUID = dialog with that id is open
    protected Guid _detachConfirmDialogHandle = Guid.Empty;
    protected Mode _mode;
    protected bool _autoCloseOnNoServer = false; // Automatically close the dialog if no more servers are available in the network
    
    #endregion

    public ServerAttachmentModel()
    {
      _singleServerProperty = new Property(typeof(string), string.Empty);
      _isNoServerAvailableProperty = new Property(typeof(bool), false);
      _isSingleServerAvailableProperty = new Property(typeof(bool), false);
      _isMultipleServersAvailableProperty = new Property(typeof(bool), false);
      _availableServers = new ItemsList();
      _messageQueue = new AsynchronousMessageQueue(this, new string[]
          {
            ServerConnectionMessaging.CHANNEL,
            DialogManagerMessaging.CHANNEL,
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
          case ServerConnectionMessaging.MessageType.AvailableServersChanged:
            ICollection<ServerDescriptor> availableServers = (ICollection<ServerDescriptor>)
                message.MessageData[ServerConnectionMessaging.AVAILABLE_SERVERS];
            SynchronizeAvailableServers();
            Mode mode;
            lock (_syncObj)
              mode = _mode;
            if (mode == Mode.AttachToServer)
            {
              if (_autoCloseOnNoServer && availableServers.Count == 0)
              {
                LeaveConfiguration();
                return;
              }
            }
            break;
        }
      }
      else if (message.ChannelName == DialogManagerMessaging.CHANNEL)
      {
        DialogManagerMessaging.MessageType messageType = (DialogManagerMessaging.MessageType) message.MessageType;
        if (messageType == DialogManagerMessaging.MessageType.DialogClosed)
        {
          Guid dialogHandle = (Guid) message.MessageData[DialogManagerMessaging.DIALOG_HANDLE];
          bool leaveConfiguration = false;
          bool doDetach = false;
          lock (_syncObj)
            if (_attachInfoDialogHandle == dialogHandle)
            {
              _attachInfoDialogHandle = null;
              leaveConfiguration = true;
            }
            else if (_detachConfirmDialogHandle == dialogHandle)
            {
              DialogResult dialogResult = (DialogResult) message.MessageData[DialogManagerMessaging.DIALOG_RESULT];
              _detachConfirmDialogHandle = Guid.Empty;
              if (dialogResult == DialogResult.Yes)
                doDetach = true;
              leaveConfiguration = true;
            }
          // Do the next two statements outside our lock
          if (doDetach)
            DoDetachFromHomeServer();
          if (leaveConfiguration)
            LeaveConfiguration();
        }
      }
    }

    protected void LeaveConfiguration()
    {
      if (_mode == Mode.None)
        return;
      IWorkflowManager workflowManager = ServiceScope.Get<IWorkflowManager>();
      workflowManager.NavigatePop(1);
    }

    protected void SynchronizeAvailableServers()
    {
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      IDictionary<string, ServerDescriptor> availableServers = new Dictionary<string, ServerDescriptor>();
      ICollection<ServerDescriptor> systemAvailableServers = scm.AvailableServers;
      if (systemAvailableServers != null) // AvailableServers can be null if in the meantime, a home server was attached
        foreach (ServerDescriptor sd in scm.AvailableServers)
          availableServers.Add(sd.MPBackendServerUUID, sd);
      IDictionary<string, ListItem> shownServers = new Dictionary<string, ListItem>();
      bool serversChanged = false;
      lock (_syncObj)
      {
        foreach (ListItem sdItem in _availableServers)
          shownServers.Add(((ServerDescriptor) sdItem.AdditionalProperties[SERVER_DESCRIPTOR_KEY]).MPBackendServerUUID, sdItem);
        foreach (string uuid in shownServers.Keys)
          if (!availableServers.ContainsKey(uuid))
          {
            _availableServers.Remove(shownServers[uuid]);
            serversChanged = true;
          }
        foreach (ServerDescriptor sd in availableServers.Values)
          if (!shownServers.ContainsKey(sd.MPBackendServerUUID))
          {
            ListItem serverItem = new ListItem();
            serverItem.SetLabel(NAME_KEY, LocalizationHelper.Translate(SERVER_FORMAT_TEXT_RES,
                sd.ServerName, sd.System.HostName));
            serverItem.SetLabel(SERVER_NAME_KEY, sd.ServerName);
            serverItem.SetLabel(SYSTEM_KEY, sd.System.HostName);
            serverItem.AdditionalProperties[SERVER_DESCRIPTOR_KEY] = sd;
            serverItem.Command = new MethodDelegateCommand(() => ChooseNewHomeServerAndClose(serverItem));
            _availableServers.Add(serverItem);
            serversChanged = true;
          }
      }
      if (serversChanged)
        // According to our locking strategy, do this outside the lock because this will trigger event handlers
        _availableServers.FireChange();
      IsNoServerAvailable = availableServers.Count == 0;
      IsSingleServerAvailable = availableServers.Count == 1;
      IsMultipleServersAvailable = availableServers.Count > 1;
      if (availableServers.Count == 1)
      {
        IEnumerator<ServerDescriptor> enumer = availableServers.Values.GetEnumerator();
        enumer.MoveNext();
        _singleAvailableServer = enumer.Current;
        SingleServer = LocalizationHelper.Translate(SERVER_FORMAT_TEXT_RES,
            _singleAvailableServer.ServerName, _singleAvailableServer.System.HostName);
      }
      else
        SingleServer = string.Empty;
    }

    protected void ShowAttachToServerDialog()
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      screenManager.ShowDialog(ATTACH_TO_SERVER_DIALOG, dialogName =>
          {
            if (_attachInfoDialogHandle == null)
              LeaveConfiguration();
          });
    }

    /// <summary>
    /// Shows an info dialog that the server with the given <see cref="sd"/> was attached.
    /// </summary>
    /// <param name="sd">Descriptor of the server whose information should be shown.</param>
    protected void ShowAttachInformationDialogAndClose(ServerDescriptor sd)
    {
      IScreenManager screenManager = ServiceScope.Get<IScreenManager>();
      IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
      _attachInfoDialogHandle = Guid.Empty; // Set this to value != null here to make the attachment dialog's close handler know we are not finished in our WF-state
      screenManager.CloseDialog();
      string header = LocalizationHelper.Translate(ATTACH_INFO_DIALOG_HEADER_RES);
      string text = LocalizationHelper.Translate(ATTACH_INFO_DIALOG_TEXT_RES, sd.ServerName, sd.System.HostName);
      Guid handle = dialogManager.ShowDialog(header, text, DialogType.OkDialog, false, DialogButtonType.Ok);
      lock (_syncObj)
        _attachInfoDialogHandle = handle;
    }

    protected void AttachInformationDialogCont()
    {
      LeaveConfiguration();
    }

    protected void ShowDetachConfirmationDialog()
    {
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      IDialogManager dialogManager = ServiceScope.Get<IDialogManager>();
      string header = LocalizationHelper.Translate(DETACH_CONFIRM_DIALOG_HEADER_RES);
      string serverName = scm.LastHomeServerName ?? LocalizationHelper.Translate(UNKNOWN_SERVER_NAME_RES);
      SystemName system = scm.LastHomeServerSystem;
      string text = LocalizationHelper.Translate(DETACH_CONFIRM_DIALOG_TEXT_RES,
          serverName, system == null ? LocalizationHelper.Translate(UNKNOWN_SERVER_SYSTEM_RES) : system.HostName);
      Guid handle = dialogManager.ShowDialog(header, text, DialogType.YesNoDialog, false, DialogButtonType.No);
      lock (_syncObj)
        _detachConfirmDialogHandle = handle;
    }

    protected void DoDetachFromHomeServer()
    {
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      scm.DetachFromHomeServer();
    }

    #region Public members to be called from the GUI

    /// <summary>
    /// Gets a list of available MediaPortal servers to be offered to connect to.
    /// </summary>
    public ItemsList AvailableServers
    {
      get
      {
        lock (_syncObj)
          return _availableServers;
      }
    }

    public string SingleServer
    {
      get { return (string) _singleServerProperty.GetValue(); }
      set { _singleServerProperty.SetValue(value); }
    }

    public Property SingleServerProperty
    {
      get { return _singleServerProperty; }
    }

    public bool IsNoServerAvailable
    {
      get { return (bool) _isNoServerAvailableProperty.GetValue(); }
      set { _isNoServerAvailableProperty.SetValue(value); }
    }

    public Property IsNoServerAvailableProperty
    {
      get { return _isNoServerAvailableProperty; }
    }

    public bool IsSingleServerAvailable
    {
      get { return (bool) _isSingleServerAvailableProperty.GetValue(); }
      set { _isSingleServerAvailableProperty.SetValue(value); }
    }

    public Property IsSingleServerAvailableProperty
    {
      get { return _isSingleServerAvailableProperty; }
    }

    public bool IsMultipleServersAvailable
    {
      get { return (bool) _isMultipleServersAvailableProperty.GetValue(); }
      set { _isMultipleServersAvailableProperty.SetValue(value); }
    }

    public Property IsMultipleServersAvailableProperty
    {
      get { return _isMultipleServersAvailableProperty; }
    }

    /// <summary>
    /// Called from the skin to connect to the <see cref="SingleServer"/>.
    /// </summary>
    public void ConnectToSingleServerAndClose()
    {
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      scm.SetNewHomeServer(_singleAvailableServer.MPBackendServerUUID);
      ShowAttachInformationDialogAndClose(_singleAvailableServer);
    }

    /// <summary>
    /// Called from the skin to connect to one of the available servers.
    /// </summary>
    /// <param name="availableServerItem">One of the items in the <see cref="AvailableServers"/> collection</param>
    public void ChooseNewHomeServerAndClose(ListItem availableServerItem)
    {
      ServerDescriptor sd = (ServerDescriptor) availableServerItem.AdditionalProperties[SERVER_DESCRIPTOR_KEY];
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      scm.SetNewHomeServer(sd.MPBackendServerUUID);
      ShowAttachInformationDialogAndClose(sd);
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      lock (_syncObj)
        if (_mode != Mode.None)
          return false; // We are already active
      _detachConfirmDialogHandle = Guid.Empty;
      _attachInfoDialogHandle = null;
      IServerConnectionManager scm = ServiceScope.Get<IServerConnectionManager>();
      if (newContext.WorkflowState.StateId == ATTACH_TO_SERVER_STATE)
      {
        // We are always able to enter this state
      }
      else if (newContext.WorkflowState.StateId == DETACH_FROM_SERVER_STATE)
      {
        if (string.IsNullOrEmpty(scm.HomeServerSystemId))
          return false;
      }
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _messageQueue.Start();
      if (newContext.WorkflowState.StateId == ATTACH_TO_SERVER_STATE)
      {
        lock (_syncObj)
          _mode = Mode.AttachToServer;
        object o = newContext.GetContextVariable(AUTO_CLOSE_ON_NO_SERVER_KEY, false);
        if (o != null)
          _autoCloseOnNoServer = (bool) o;
      }
      else if (newContext.WorkflowState.StateId == DETACH_FROM_SERVER_STATE)
      {
        lock (_syncObj)
          _mode = Mode.DetachFromServer;
      }
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      lock (_syncObj)
        _mode = Mode.None;
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

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Nothing to do
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      Mode mode;
      lock (_syncObj)
        mode = _mode;
      switch (mode)
      {
        case Mode.AttachToServer:
          SynchronizeAvailableServers();
          ShowAttachToServerDialog();
          break;
        case Mode.DetachFromServer:
          ShowDetachConfirmationDialog();
          break;
        default:
          return ScreenUpdateMode.AutoWorkflowManager; // Error case
      }
      return ScreenUpdateMode.ManualWorkflowModel;
    }

    #endregion
  }
}
