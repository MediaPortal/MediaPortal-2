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
using MediaPortal.Common;
using MediaPortal.Common.Commands;
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Logging;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.Utilities;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;

namespace MediaPortal.UiComponents.SkinBase.Models
{
  /// <summary>
  /// Provides a workflow model to attend the complex configuration process for server and client shares
  /// in the MP2 configuration.
  /// </summary>
  public class UserConfigModel : IWorkflowModel, IDisposable
  {
    #region Consts

    public const string STR_MODEL_ID_USERCONFIG = "9B20B421-DF2E-42B6-AFF2-7EB6B60B601D";
    public static readonly Guid MODEL_ID_USERCONFIG = new Guid(STR_MODEL_ID_USERCONFIG);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected bool _updatingProperties = false;
    protected ItemsList _sharesList = null;
    protected ItemsList _userList = null;
    protected UserProxy _userProxy = null; // Encapsulates state and communication of user configuration
    protected AbstractProperty _isHomeServerConnectedProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public UserConfigModel()
    {
      _isHomeServerConnectedProperty = new WProperty(typeof(bool), false);
    }

    public void Dispose()
    {
      UserProxy = null;
      _sharesList = null;
      _userList = null;
    }

    #endregion

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           ServerConnectionMessaging.CHANNEL,
           ContentDirectoryMessaging.CHANNEL,
           SharesMessaging.CHANNEL,
        });
      messageQueue.MessageReceived += OnMessageReceived;
      messageQueue.Start();
      lock (_syncObj)
        _messageQueue = messageQueue;
    }

    void UnsubscribeFromMessages()
    {
      AsynchronousMessageQueue messageQueue;
      lock (_syncObj)
      {
        messageQueue = _messageQueue;
        _messageQueue = null;
      }
      if (messageQueue == null)
        return;
      messageQueue.Shutdown();
    }

    void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType =
            (ServerConnectionMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerAttached:
          case ServerConnectionMessaging.MessageType.HomeServerDetached:
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
            UpdateProperties_NoLock();
            UpdateUserLists_NoLock(false);
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            UpdateProperties_NoLock();
            UpdateUserLists_NoLock(false);
            break;
        }
      }
      else if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ContentDirectoryMessaging.MessageType.RegisteredSharesChanged:
            UpdateProperties_NoLock();
            UpdateUserLists_NoLock(false);
            break;
        }
      }
      else if (message.ChannelName == SharesMessaging.CHANNEL)
      {
        SharesMessaging.MessageType messageType = (SharesMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SharesMessaging.MessageType.ShareAdded:
          case SharesMessaging.MessageType.ShareRemoved:
            UpdateProperties_NoLock();
            UpdateUserLists_NoLock(false);
            break;
        }
      }
    }

    #region Public properties (Also accessed from the GUI)

    public UserProxy UserProxy
    {
      get { return _userProxy; }
      private set
      {
        lock (_syncObj)
        {
          if (_userProxy != null)
            _userProxy.Dispose();
          _userProxy = value;
        }
      }
    }

    public ItemsList SharesList
    {
      get { return _sharesList; }
    }

    public ItemsList UserList
    {
      get { return _userList; }
    }

    public AbstractProperty IsHomeServerConnectedProperty
    {
      get { return _isHomeServerConnectedProperty; }
    }

    /// <summary>
    /// <c>true</c> if a home server is attached and it is currently connected.
    /// </summary>
    public bool IsHomeServerConnected
    {
      get { return (bool)_isHomeServerConnectedProperty.GetValue(); }
      set { _isHomeServerConnectedProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    public void RemoveSelectedUserAndFinish()
    {
      try
      {
        NavigateBackToOverview();
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    public void FinishUserConfiguration()
    {
      try
      {

      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    public void EditCurrentUser()
    {
      try
      {
        _userProxy.EditMode = UserProxy.UserEditMode.EditUser;
        IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
        //workflowManager.NavigatePush(Consts.WF_STATE_ID_SHARE_EDIT_CHOOSE_RESOURCE_PROVIDER);
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    protected void ErrorEditShare(Exception exc)
    {
      ServiceRegistration.Get<ILogger>().Warn("UserConfigModel: Problem adding/editing user", exc);
      IScreenManager screenManager = ServiceRegistration.Get<IScreenManager>();
      //screenManager.ShowScreen(Consts.SCREEN_SHARES_CONFIG_PROBLEM);
    }

    public void NavigateBackToOverview()
    {
      UserProxy = null;
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      //workflowManager.NavigatePopToState(Consts.WF_STATE_ID_SHARES_OVERVIEW, false);
    }

    #endregion

    #region Private and protected methods

    protected ICollection<Share> GetSelectedShares(ItemsList sharesItemsList)
    {
      lock (_syncObj)
        // Fill the result inside this method to make it possible to lock other threads out while looking at the shares list
        return new List<Share>(sharesItemsList.Where(
            shareItem => shareItem.Selected).Select(
            shareItem => (Share)shareItem.AdditionalProperties[Consts.KEY_SHARE]));
    }

    protected internal void UpdateUserLists_NoLock(bool create)
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
        if (create)
          _userList = new ItemsList();
      }
      try
      {
        IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
        if (userManagement.UserProfileDataManagement == null)
          return;
        // add users to expose them
        var users = userManagement.UserProfileDataManagement.GetProfiles();
        foreach (UserProfile user in users.Where(u => u != null))
        {
          ListItem item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, user.Name);
          _userList.Add(item);
        }
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    protected void UpdateProperties_NoLock()
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
      }
      try
      {
        IServerConnectionManager serverConnectionManager = ServiceRegistration.Get<IServerConnectionManager>();
        IsHomeServerConnected = serverConnectionManager.IsHomeServerConnected;
        SystemName homeServerSystem = serverConnectionManager.LastHomeServerSystem;
        lock (_syncObj)
        {
          IsHomeServerConnected = homeServerSystem != null;
        }
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    /// <summary>
    /// Prepares the internal data of this model to match the specified new
    /// <paramref name="workflowState"/>. This method will be called in result of a
    /// forward state navigation as well as for a backward navigation.
    /// </summary>
    /// <param name="workflowState">The workflow state to prepare.</param>
    /// <param name="push">Set to <c>true</c>, if the given <paramref name="workflowState"/> has been pushed onto
    /// the workflow navigation stack. Else, set to <c>false</c>.</param>
    protected void PrepareState(Guid workflowState, bool push)
    {
      try
      {
        if (workflowState == Consts.WF_STATE_ID_USERS_OVERVIEW)
        {
          UpdateUserLists_NoLock(true);
        }
        else if (!push)
        {
          return;
        }
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ErrorEditShare(e);
      }
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        UserProxy = null;
        _sharesList = null;
        _userList = null;
      }
    }

    protected void DisconnectedError()
    {
      // Called when a remote call crashes because the server was disconnected. We don't do anything here because
      // we automatically move to the overview state in the OnMessageReceived method when the server disconnects.
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_USERCONFIG; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      SubscribeToMessages();
      ClearData();
      UpdateProperties_NoLock();
      PrepareState(newContext.WorkflowState.StateId, true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UnsubscribeFromMessages();
      ClearData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
      PrepareState(newContext.WorkflowState.StateId, push);
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      PrepareState(newContext.WorkflowState.StateId, false);
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Not used yet, currently we don't show any menu during the shares configuration process.
      // Perhaps we'll add menu actions later for different convenience procedures.
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}