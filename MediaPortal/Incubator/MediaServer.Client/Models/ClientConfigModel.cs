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
using MediaPortal.Common.Exceptions;
using MediaPortal.Common.General;
using MediaPortal.Common.Logging;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.Common.UserProfileDataManagement;
using System.Threading.Tasks;
using MediaPortal.Common.UserManagement;
using MediaPortal.Extensions.MediaServer.Client.General;
using MediaPortal.Extensions.MediaServer.Interfaces.Settings;
using MediaPortal.Plugins.ServerSettings;
using MediaPortal.Common.Localization;

namespace MediaPortal.Extensions.MediaServer.Client.Models
{
  /// <summary>
  /// Provides a workflow model to attend the complex configuration process for server and client shares
  /// in the MP2 configuration.
  /// </summary>
  public class ClientConfigModel : IWorkflowModel, IDisposable
  {
    #region Consts

    public const string STR_MODEL_ID_DLNA_CLIENTCONFIG = "A5E439BD-6A88-4B1B-9032-0C6F1CE9DD30";
    public static readonly Guid MODEL_ID_DLNA_CLIENTCONFIG = new Guid(STR_MODEL_ID_DLNA_CLIENTCONFIG);

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected bool _updatingProperties = false;
    protected ProfileLink _selectedClient = null;
    protected ItemsList _clientList = null;
    protected ItemsList _profileList = null;
    protected ItemsList _userList = null;
    protected AbstractProperty _isHomeServerConnectedProperty;
    protected AbstractProperty _isClientSelectedProperty;
    protected AbstractProperty _selectedUserInfoProperty;
    protected AbstractProperty _selectedProfileInfoProperty;
    protected AbstractProperty _selectedClientNameProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    #endregion

    #region Ctor

    public ClientConfigModel()
    {
      _isHomeServerConnectedProperty = new WProperty(typeof(bool), false);
      _selectedUserInfoProperty = new WProperty(typeof(string), string.Empty);
      _selectedProfileInfoProperty = new WProperty(typeof(string), string.Empty);
      _selectedClientNameProperty = new WProperty(typeof(string), string.Empty);
      _isClientSelectedProperty = new WProperty(typeof(bool), false);
    }

    public void Dispose()
    {
      _selectedClient = null;
      _clientList = null;
      _userList = null;
      _profileList = null;
    }

    #endregion

    void SubscribeToMessages()
    {
      AsynchronousMessageQueue messageQueue = new AsynchronousMessageQueue(this, new string[]
        {
           ServerConnectionMessaging.CHANNEL,
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
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            _ = UpdateUserLists_NoLock(false);
            UpdateClientLists_NoLock(false);
            break;
        }
      }
    }

    #region Public properties (Also accessed from the GUI)

    public ItemsList ClientList
    {
      get
      {
        lock (_syncObj)
          return _clientList;
      }
    }

    public ItemsList UserList
    {
      get
      {
        lock (_syncObj)
          return _userList;
      }
    }

    public ItemsList ProfileList
    {
      get
      {
        lock (_syncObj)
          return _profileList;
      }
    }

    public AbstractProperty IsHomeServerConnectedProperty
    {
      get { return _isHomeServerConnectedProperty; }
    }

    public bool IsHomeServerConnected
    {
      get { return (bool)_isHomeServerConnectedProperty.GetValue(); }
      set { _isHomeServerConnectedProperty.SetValue(value); }
    }

    public AbstractProperty SelectedClientNameProperty
    {
      get { return _selectedClientNameProperty; }
    }

    public string SelectedClientName
    {
      get { return (string)SelectedClientNameProperty.GetValue(); }
      set { SelectedClientNameProperty.SetValue(value); }
    }

    public AbstractProperty SelectedUserInfoProperty
    {
      get { return _selectedUserInfoProperty; }
    }

    public string SelectedUserInfo
    {
      get { return (string)SelectedUserInfoProperty.GetValue(); }
      set { SelectedUserInfoProperty.SetValue(value); }
    }

    public AbstractProperty SelectedProfileInfoProperty
    {
      get { return _selectedProfileInfoProperty; }
    }

    public string SelectedProfileInfo
    {
      get { return (string)SelectedProfileInfoProperty.GetValue(); }
      set { SelectedProfileInfoProperty.SetValue(value); }
    }

    public AbstractProperty IsClientSelectedProperty
    {
      get { return _isClientSelectedProperty; }
    }

    public bool IsClientSelected
    {
      get { return (bool)IsClientSelectedProperty.GetValue(); }
      set { IsClientSelectedProperty.SetValue(value); }
    }

    #endregion

    #region Public methods

    public void OpenSelectUserDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectClientUser");
    }

    public void OpenSelectProfileDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectClientProfile");
    }

    public void OpenConfirmDeleteDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogDeleteClientConfirm");
    }


    public void DeleteClient()
    {
      try
      {
        ListItem item = _clientList.FirstOrDefault(i => i.Selected);
        if (item == null)
          return;

        int oldItemIndex = _clientList.IndexOf(item) - 1;
        ProfileLink client = (ProfileLink)item.AdditionalProperties[Consts.KEY_CLIENT];

        item.SelectedProperty.Detach(OnClientItemSelectionChanged);
        lock (_syncObj)
          _clientList.Remove(item);

        // Set focus to first in list
        if (oldItemIndex > 0 && oldItemIndex < _clientList.Count)
          _clientList[oldItemIndex].Selected = true;
        else
        {
          var firstItem = _clientList.FirstOrDefault();
          if (firstItem != null)
            firstItem.Selected = true;
        }

        _clientList.FireChange();
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ClientConfigModel: Problems deleting client", e);
      }
    }

    public void SaveClients()
    {
      try
      {
        ProfileLinkSettings settings = new ProfileLinkSettings();
        foreach (var item in _clientList)
        {
          var client = (ProfileLink)item.AdditionalProperties[Consts.KEY_CLIENT];
          if (client.DefaultUserProfile == Guid.Empty.ToString())
            client.DefaultUserProfile = null;
          settings.Links.Add(client);
        }

        IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
        serverSettings.Save(settings);
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ClientConfigModel: Problems saving clients", e);
      }
    }

    #endregion

    #region Private and protected methods

    private void SetClient(ProfileLink client)
    {
      try
      {
        _selectedClient = client;
        IsClientSelected = client != null;
        SelectedClientName = client?.ClientName ?? "";
        SelectedUserInfo = _userList.FirstOrDefault(p => ((UserProfile)p.AdditionalProperties[Consts.KEY_USER]).ProfileId.ToString() == client?.DefaultUserProfile)?.Labels[Consts.KEY_NAME].Evaluate() ?? LocalizationHelper.Translate(Consts.RES_NOUSER) ?? "";
        SelectedProfileInfo = _profileList.FirstOrDefault(p => p.AdditionalProperties[Consts.KEY_PROFILE].ToString() == client?.Profile)?.Labels[Consts.KEY_NAME].Evaluate() ?? client?.Profile ?? "";
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("ClientConfigModel: Error selecting client", e);
      }
    }

    public void SelectProfile(ListItem item)
    {
      string profile = (string)item.AdditionalProperties[Consts.KEY_PROFILE];
      if (_selectedClient != null)
      {
        _selectedClient.Profile = profile;
        SetClient(_selectedClient);
      }
    }

    public void SelectUser(ListItem item)
    {
      UserProfile user = (UserProfile)item.AdditionalProperties[Consts.KEY_USER];
      if (_selectedClient != null)
      {
        _selectedClient.DefaultUserProfile = user.ProfileId.ToString();
        SetClient(_selectedClient);
      }
    }

    protected internal void UpdateClientLists_NoLock(bool create, string selectedClientName = null)
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
        if (create)
        {
          _clientList = new ItemsList();
          _profileList = new ItemsList();
        }
      }
      try
      {
        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        if (scm == null || scm.ContentDirectory == null)
          return;

        SystemName homeServerSystem = scm.LastHomeServerSystem;
        IsHomeServerConnected = homeServerSystem != null;

        IServerSettingsClient serverSettings = ServiceRegistration.Get<IServerSettingsClient>();
        ProfileLinkSettings settings = serverSettings.Load<ProfileLinkSettings>();
        ListItem item = null;

        _clientList.Clear();
        foreach (ProfileLink client in settings.Links)
        {
          item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, client.ClientName);
          item.AdditionalProperties[Consts.KEY_CLIENT] = client;
          item.SelectedProperty.Attach(OnClientItemSelectionChanged);
          lock (_syncObj)
            _clientList.Add(item);
        }
        _clientList.FireChange();

        _profileList.Clear();
        item = new ListItem();
        item.SetLabel(Consts.KEY_NAME, LocalizationHelper.Translate(Consts.RES_AUTO));
        item.AdditionalProperties[Consts.KEY_PROFILE] = "Auto";
        lock (_syncObj)
          _profileList.Add(item);
        foreach (var profile in ProfileLinkSettings.Profiles)
        {
          item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, profile.Value);
          item.AdditionalProperties[Consts.KEY_PROFILE] = profile.Key;
          lock (_syncObj)
            _profileList.Add(item);
        }
        _profileList.FireChange();
      }
      catch (NotConnectedException)
      {
        throw;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ClientConfigModel: Problems updating clients", e);
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    protected internal async Task UpdateUserLists_NoLock(bool create, Guid? selectedUserId = null)
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
        if (userManagement == null || userManagement.UserProfileDataManagement == null)
          return;

        // add users to expose them
        var users = await userManagement.UserProfileDataManagement.GetProfilesAsync();
        _userList.Clear();
        ListItem item = new ListItem();
        item.SetLabel(Consts.KEY_NAME, LocalizationHelper.Translate(Consts.RES_NOUSER));
        item.AdditionalProperties[Consts.KEY_USER] = new UserProfile(Guid.Empty, LocalizationHelper.Translate(Consts.RES_NOUSER));
        lock(_syncObj)
            _userList.Add(item);
        foreach (UserProfile user in users)
        {
          item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, user.Name);
          item.AdditionalProperties[Consts.KEY_USER] = user;
          lock (_syncObj)
            _userList.Add(item);
        }
        _userList.FireChange();
      }
      catch (NotConnectedException)
      {
        throw;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("ClientConfigModel: Problems updating users", e);
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    private void OnClientItemSelectionChanged(AbstractProperty property, object oldValue)
    {
      // Only handle the event if new item got selected. The unselected event can be ignored.
      if (!(bool)property.GetValue())
        return;

      ProfileLink client = null;
      lock (_syncObj)
      {
        client = _clientList.Where(i => i.Selected).Select(i => (ProfileLink)i.AdditionalProperties[Consts.KEY_CLIENT]).FirstOrDefault();
      }
      SetClient(client);
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _selectedClient = null;
        _userList = null;
        _userList = null;
        _profileList = null;
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
      get { return MODEL_ID_DLNA_CLIENTCONFIG; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      SubscribeToMessages();
      ClearData();
      _ = UpdateUserLists_NoLock(true);
      UpdateClientLists_NoLock(true);
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      UnsubscribeFromMessages();
      ClearData();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {

    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
      // Nothing to do here
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {

    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
      // Perhaps we'll add menu actions later for different convenience procedures.
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
