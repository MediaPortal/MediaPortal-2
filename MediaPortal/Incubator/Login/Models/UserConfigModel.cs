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
using MediaPortal.Common.MediaManagement;
using MediaPortal.Common.Messaging;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UI.Presentation.Workflow;
using MediaPortal.UI.ServerCommunication;
using MediaPortal.UI.Shares;
using MediaPortal.UiComponents.Login.General;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.Common.SystemCommunication;
using MediaPortal.Common.Localization;
using System.IO;
using MediaPortal.Utilities.Graphics;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.UI.Presentation.Utilities;

namespace MediaPortal.UiComponents.Login.Models
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
    public static int MAX_IMAGE_WIDTH = 128;
    public static int MAX_IMAGE_HEIGHT = 128;

    #endregion

    #region Protected fields

    protected object _syncObj = new object();
    protected bool _updatingProperties = false;
    protected string _imagePath = null;
    protected Dictionary<UserProfileType, string> _profileTypes = new Dictionary<UserProfileType, string>();
    protected PathBrowserCloseWatcher _pathBrowserCloseWatcher = null;
    protected ItemsList _serverSharesList = null;
    protected ItemsList _localSharesList = null;
    protected ItemsList _userList = null;
    protected ItemsList _profileList = null;
    protected UserProxy _userProxy = null; // Encapsulates state and communication of user configuration
    protected AbstractProperty _isHomeServerConnectedProperty;
    protected AbstractProperty _showLocalSharesProperty;
    protected AbstractProperty _isLocalHomeServerProperty;
    protected AbstractProperty _anyShareAvailableProperty;
    protected AbstractProperty _selectShareInfoProperty;
    protected AbstractProperty _profileTypeNameProperty;
    protected AbstractProperty _isUserSelectedProperty;
    protected AbstractProperty _isSystemUserSelectedProperty;
    protected AsynchronousMessageQueue _messageQueue = null;

    protected readonly static string[] DEFAULT_IMAGE_FILE_EXTENSIONS = new string[]
      {
          ".jpg",
          ".jpeg",
          ".png",
          ".bmp",
          ".gif",
          ".tga",
          ".tiff",
          ".tif",
      };

    #endregion

    #region Ctor

    public UserConfigModel()
    {
      _isHomeServerConnectedProperty = new WProperty(typeof(bool), false);
      _showLocalSharesProperty = new WProperty(typeof(bool), false);
      _isLocalHomeServerProperty = new WProperty(typeof(bool), false);
      _anyShareAvailableProperty = new WProperty(typeof(bool), false);
      _selectShareInfoProperty = new WProperty(typeof(string), string.Empty);
      _profileTypeNameProperty = new WProperty(typeof(string), string.Empty);
      _isUserSelectedProperty = new WProperty(typeof(bool), false);
      _isSystemUserSelectedProperty = new WProperty(typeof(bool), false);

      _profileTypes.Add(UserProfileType.ClientProfile, LocalizationHelper.Translate(Consts.RES_CLIENT_PROFILE_TEXT));
      _profileTypes.Add(UserProfileType.UserProfile, LocalizationHelper.Translate(Consts.RES_USER_PROFILE_TEXT));
      _profileTypes.Add(UserProfileType.AdminProfile, LocalizationHelper.Translate(Consts.RES_ADMIN_PROFILE_TEXT));

      _profileList = new ItemsList();
      ListItem item = null;
      foreach (var profile in _profileTypes.Where(p => p.Key != UserProfileType.ClientProfile))
      {
        item = new ListItem();
        item.SetLabel(Consts.KEY_NAME, profile.Value);
        item.AdditionalProperties[Consts.KEY_PROFILE_TYPE] = profile.Key;
        _profileList.Add(item);
      }

      UserProxy = new UserProxy();
      UserProxy.ProfileTypeProperty.Attach(OnProfileTypeChanged);
      ProfileTypeName = _profileTypes.FirstOrDefault(i => i.Key == UserProxy.ProfileType).Value;
    }

    public void Dispose()
    {
      UserProxy = null;
      _serverSharesList = null;
      _localSharesList = null;
      _userList = null;
      _profileList = null;
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
            _ = UpdateUserLists_NoLock(false);
            _ = UpdateShareLists_NoLock(false);
            break;
          case ServerConnectionMessaging.MessageType.HomeServerDisconnected:
            _ = UpdateUserLists_NoLock(false);
            _ = UpdateShareLists_NoLock(false);
            break;
        }
      }
      else if (message.ChannelName == ContentDirectoryMessaging.CHANNEL)
      {
        ContentDirectoryMessaging.MessageType messageType = (ContentDirectoryMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ContentDirectoryMessaging.MessageType.RegisteredSharesChanged:
            _ = UpdateShareLists_NoLock(false);
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
            _ = UpdateShareLists_NoLock(false);
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

    public ItemsList ServerSharesList
    {
      get
      {
        lock (_syncObj)
          return _serverSharesList;
      }
    }

    public ItemsList LocalSharesList
    {
      get
      {
        lock (_syncObj)
          return _localSharesList;
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

    public ItemsList ProfileTypeList
    {
      get
      {
        lock(_syncObj)
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

    public AbstractProperty IsLocalHomeServerProperty
    {
      get { return _isLocalHomeServerProperty; }
    }

    public bool IsLocalHomeServer
    {
      get { return (bool)_isLocalHomeServerProperty.GetValue(); }
      set { _isLocalHomeServerProperty.SetValue(value); }
    }

    public AbstractProperty ShowLocalSharesProperty
    {
      get { return _showLocalSharesProperty; }
    }

    public bool ShowLocalShares
    {
      get { return (bool)_showLocalSharesProperty.GetValue(); }
      set { _showLocalSharesProperty.SetValue(value); }
    }

    public AbstractProperty AnyShareAvailableProperty
    {
      get { return _anyShareAvailableProperty; }
    }

    public bool AnyShareAvailable
    {
      get { return (bool)_anyShareAvailableProperty.GetValue(); }
      set { _anyShareAvailableProperty.SetValue(value); }
    }

    public AbstractProperty SelectedSharesInfoProperty
    {
      get { return _selectShareInfoProperty; }
    }

    public string SelectedSharesInfo
    {
      get { return (string)_selectShareInfoProperty.GetValue(); }
      set { _selectShareInfoProperty.SetValue(value); }
    }

    public AbstractProperty ProfileTypeNameProperty
    {
      get { return _profileTypeNameProperty; }
    }

    public string ProfileTypeName
    {
      get { return (string)_profileTypeNameProperty.GetValue(); }
      set { _profileTypeNameProperty.SetValue(value); }
    }

    public AbstractProperty IsUserSelectedProperty
    {
      get { return _isUserSelectedProperty; }
    }

    public bool IsUserSelected
    {
      get { return (bool)_isUserSelectedProperty.GetValue(); }
      set { _isUserSelectedProperty.SetValue(value); }
    }

    public AbstractProperty IsSystemUserSelectedProperty
    {
      get { return _isSystemUserSelectedProperty; }
    }

    public bool IsSystemUserSelected
    {
      get { return (bool)_isSystemUserSelectedProperty.GetValue(); }
      set { _isSystemUserSelectedProperty.SetValue(value); }
    }

    public string ImagePath
    {
      get { return _imagePath; }
      set
      {
        _imagePath = null;
        if (File.Exists(value))
        {
          using (FileStream stream = new FileStream(value, FileMode.Open))
          using (MemoryStream resized = (MemoryStream)ImageUtilities.ResizeImage(stream, ImageFormat.Png, MAX_IMAGE_WIDTH, MAX_IMAGE_HEIGHT))
          {
            if (resized != null)
            {
              UserProxy.Image = resized.ToArray();
              _imagePath = value;
            }
          }
        }
      }
    }

    #endregion

    #region Public methods

    public void OpenChooseProfileTypeDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogChooseProfileType");
    }

    public void OpenConfirmDeleteDialog()
    {
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogDeleteConfirm");
    }

    public void OpenSelectSharesDialog()
    {
      foreach (ListItem item in _serverSharesList)
        item.Selected = UserProxy.SelectedShares.Contains(((Share)item.AdditionalProperties[Consts.KEY_SHARE]).ShareId);
      foreach (ListItem item in _localSharesList)
        item.Selected = UserProxy.SelectedShares.Contains(((Share)item.AdditionalProperties[Consts.KEY_SHARE]).ShareId);
      ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogSelectShares",
        (string name, System.Guid id) =>
        {
          UserProxy.SelectedShares.Clear();
          foreach (ListItem item in _serverSharesList.Where(i => i.Selected))
            UserProxy.SelectedShares.Add(((Share)item.AdditionalProperties[Consts.KEY_SHARE]).ShareId);
          foreach (ListItem item in _localSharesList.Where(i => i.Selected))
            UserProxy.SelectedShares.Add(((Share)item.AdditionalProperties[Consts.KEY_SHARE]).ShareId);
          SetSelectedShares();
        });
    }

    public void OpenSelectUserImageDialog()
    {
      string imageFilename = _imagePath;
      string initialPath = string.IsNullOrEmpty(imageFilename) ? null : DosPathHelper.GetDirectory(imageFilename);
      Guid dialogHandle = ServiceRegistration.Get<IPathBrowser>().ShowPathBrowser(Consts.RES_SELECT_USER_IMAGE, true, false,
          string.IsNullOrEmpty(initialPath) ? null : LocalFsResourceProviderBase.ToResourcePath(initialPath),
          path =>
          {
            string choosenPath = LocalFsResourceProviderBase.ToDosPath(path.LastPathSegment.Path);
            if (string.IsNullOrEmpty(choosenPath))
              return false;

            return IsValidImage(choosenPath);
          });

      if (_pathBrowserCloseWatcher != null)
        _pathBrowserCloseWatcher.Dispose();

      _pathBrowserCloseWatcher = new PathBrowserCloseWatcher(this, dialogHandle, choosenPath =>
      {
        ImagePath = LocalFsResourceProviderBase.ToDosPath(choosenPath);
      }, null);
    }

    public void AddUser()
    {
      try
      {
        UserProfile user = new UserProfile(Guid.Empty, LocalizationHelper.Translate(Consts.RES_NEW_USER_TEXT), UserProfileType.UserProfile);
        user.LastLogin = DateTime.Now;

        ListItem item = new ListItem();
        item.SetLabel(Consts.KEY_NAME, user.Name);
        item.AdditionalProperties[Consts.KEY_USER] = user;
        item.SelectedProperty.Attach(OnUserItemSelectionChanged);
        item.Selected = true;

        lock (_syncObj)
          _userList.Add(item);

        SetUser(user);

        _userList.FireChange();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserConfigModel: Problems adding user", e);
      }
    }

    public void CopyUser()
    {
      try
      {
        int shareCount = 0;
        string hash = UserProxy.Password;
        if (UserProxy.IsPasswordChanged)
          hash = Utils.HashPassword(UserProxy.Password);
        UserProfile user = new UserProfile(Guid.Empty, LocalizationHelper.Translate(Consts.RES_NEW_USER_TEXT), UserProxy.ProfileType, hash, DateTime.Now, UserProxy.Image);
        user.AddAdditionalData(UserDataKeysKnown.KEY_ALLOWED_AGE, UserProxy.AllowedAge.ToString());
        foreach (var shareId in UserProxy.SelectedShares)
          user.AddAdditionalData(UserDataKeysKnown.KEY_ALLOWED_SHARE, ++shareCount, shareId.ToString());
        user.AddAdditionalData(UserDataKeysKnown.KEY_ALLOW_ALL_AGES, UserProxy.RestrictAges ? "0" : "1");
        user.AddAdditionalData(UserDataKeysKnown.KEY_ALLOW_ALL_SHARES, UserProxy.RestrictShares ? "0" : "1");
        user.AddAdditionalData(UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT, UserProxy.IncludeParentGuidedContent ? "1" : "0");
        user.AddAdditionalData(UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT, UserProxy.IncludeUnratedContent ? "1" : "0");

        ListItem item = new ListItem();
        item.SetLabel(Consts.KEY_NAME, user.Name);
        item.AdditionalProperties[Consts.KEY_USER] = user;
        item.SelectedProperty.Attach(OnUserItemSelectionChanged);
        item.Selected = true;

        lock (_syncObj)
          _userList.Add(item);

        SetUser(user);

        _userList.FireChange();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserConfigModel: Problems adding user", e);
      }
    }

    public async Task DeleteUser()
    {
      try
      {
        ListItem item = _userList.FirstOrDefault(i => i.Selected);
        if (item == null)
          return;

        UserProfile user = (UserProfile)item.AdditionalProperties[Consts.KEY_USER];

        item.SelectedProperty.Detach(OnUserItemSelectionChanged);
        lock (_syncObj)
          _userList.Remove(item);

        if (user.ProfileId != Guid.Empty)
        {
          IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
          if (userManagement != null && userManagement.UserProfileDataManagement != null)
          {
            if (!await userManagement.UserProfileDataManagement.DeleteProfileAsync(user.ProfileId))
            {
              ServiceRegistration.Get<ILogger>().Warn("UserConfigModel: Problems deleting user '{0}' (name '{1}')", user.ProfileId, user.Name);
            }
          }
        }

        _userList.FireChange();
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserConfigModel: Problems deleting user", e);
      }
    }

    public async Task SaveUser()
    {
      try
      {
        if (UserProxy.IsUserValid)
        {
          int shareCount = 0;
          bool success = true;
          string hash = UserProxy.Password;
          bool wasCreated = false;
          if(UserProxy.IsPasswordChanged)
            hash = Utils.HashPassword(UserProxy.Password);
          if (UserProxy.ProfileType == UserProfileType.ClientProfile)
            hash = ""; //Client profiles can't have passwords
          IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
          if (userManagement != null && userManagement.UserProfileDataManagement != null)
          {
            if (UserProxy.Id == Guid.Empty)
            {
              UserProxy.Id = await userManagement.UserProfileDataManagement.CreateProfileAsync(UserProxy.Name, UserProxy.ProfileType, hash);
              wasCreated = true;
            }
            else
            {
              success = await userManagement.UserProfileDataManagement.UpdateProfileAsync(UserProxy.Id, UserProxy.Name, UserProxy.ProfileType, hash);
            }
            if (UserProxy.Id == Guid.Empty)
            {
              ServiceRegistration.Get<ILogger>().Error("UserConfigModel: Problems saving user '{0}'", UserProxy.Name);
              return;
            }

            if (UserProxy.Image != null)
              success &= await userManagement.UserProfileDataManagement.SetProfileImageAsync(UserProxy.Id, UserProxy.Image);
            success &= await userManagement.UserProfileDataManagement.SetUserAdditionalDataAsync(UserProxy.Id, UserDataKeysKnown.KEY_ALLOWED_AGE, UserProxy.AllowedAge.ToString());
            success &= await userManagement.UserProfileDataManagement.ClearUserAdditionalDataKeyAsync(UserProxy.Id, UserDataKeysKnown.KEY_ALLOWED_SHARE);
            foreach (var shareId in UserProxy.SelectedShares)
              success &= await userManagement.UserProfileDataManagement.SetUserAdditionalDataAsync(UserProxy.Id, UserDataKeysKnown.KEY_ALLOWED_SHARE, shareId.ToString(), ++shareCount);
            success &= await userManagement.UserProfileDataManagement.SetUserAdditionalDataAsync(UserProxy.Id, UserDataKeysKnown.KEY_ALLOW_ALL_AGES, UserProxy.RestrictAges ? "0" : "1");
            success &= await userManagement.UserProfileDataManagement.SetUserAdditionalDataAsync(UserProxy.Id, UserDataKeysKnown.KEY_ALLOW_ALL_SHARES, UserProxy.RestrictShares ? "0" : "1");
            success &= await userManagement.UserProfileDataManagement.SetUserAdditionalDataAsync(UserProxy.Id, UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT, UserProxy.IncludeParentGuidedContent ? "1" : "0");
            success &= await userManagement.UserProfileDataManagement.SetUserAdditionalDataAsync(UserProxy.Id, UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT, UserProxy.IncludeUnratedContent ? "1" : "0");

            if (!success)
            {
              ServiceRegistration.Get<ILogger>().Error("UserConfigModel: Problems saving setup for user '{0}'", UserProxy.Name);
              return;
            }
          }

          ListItem item = _userList.FirstOrDefault(i => i.Selected);
          if (item == null)
            return;

          shareCount = 0;
          UserProfile user = new UserProfile(UserProxy.Id, UserProxy.Name, UserProxy.ProfileType, hash, UserProxy.LastLogin, UserProxy.Image);
          if (wasCreated)
            user.LastLogin = DateTime.Now;
          user.AddAdditionalData(UserDataKeysKnown.KEY_ALLOWED_AGE, UserProxy.AllowedAge.ToString());
          foreach (var shareId in UserProxy.SelectedShares)
            user.AddAdditionalData(UserDataKeysKnown.KEY_ALLOWED_SHARE, ++shareCount, shareId.ToString());
          user.AddAdditionalData(UserDataKeysKnown.KEY_ALLOW_ALL_AGES, UserProxy.RestrictAges ? "0" : "1");
          user.AddAdditionalData(UserDataKeysKnown.KEY_ALLOW_ALL_SHARES, UserProxy.RestrictShares ? "0" : "1");
          user.AddAdditionalData(UserDataKeysKnown.KEY_INCLUDE_PARENT_GUIDED_CONTENT, UserProxy.IncludeParentGuidedContent ? "1" : "0");
          user.AddAdditionalData(UserDataKeysKnown.KEY_INCLUDE_UNRATED_CONTENT, UserProxy.IncludeUnratedContent ? "1" : "0");

          // Update current logged in user if the same
          if(userManagement.CurrentUser.ProfileId == user.ProfileId)
            userManagement.CurrentUser = user;

          item.SetLabel(Consts.KEY_NAME, user.Name);
          item.AdditionalProperties[Consts.KEY_USER] = user;
          _userList.FireChange();

          SetUser(user);
        }
      }
      catch (NotConnectedException)
      {
        DisconnectedError();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserConfigModel: Problems saving user", e);
      }
    }

    public void SelectProfileType(ListItem item)
    {
      UserProfileType profileType = (UserProfileType)item.AdditionalProperties[Consts.KEY_PROFILE_TYPE];
      UserProxy.ProfileType = profileType;
    }

    #endregion

    #region Private and protected methods

    public bool IsValidImage(string choosenPath)
    {
      return DEFAULT_IMAGE_FILE_EXTENSIONS.Any(e => String.Compare(e, Path.GetExtension(choosenPath), StringComparison.OrdinalIgnoreCase) == 0);
    }

    private void SetUser(UserProfile userProfile)
    {
      try
      {
        if (userProfile != null && UserProxy != null)
        {
          UserProxy.SetUserProfile(userProfile, _localSharesList, _serverSharesList);
          foreach (var item in _profileList)
          {
            item.Selected = (UserProfileType)item.AdditionalProperties[Consts.KEY_PROFILE_TYPE] == UserProxy.ProfileType;
          }
        }
        else
        {
          UserProxy?.Clear();
        }

        IsUserSelected = userProfile != null;
        IsSystemUserSelected = userProfile?.ProfileType == UserProfileType.ClientProfile;

        SetSelectedShares();
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("UserConfigModel: Error selecting user", e);
      }
    }

    private void SetSelectedShares()
    {
      if (UserProxy != null)
        SelectedSharesInfo = string.Format("{0}: {1}", LocalizationHelper.Translate(Consts.RES_SHARES_TEXT), UserProxy.SelectedShares.Count);
    }

    protected internal async Task UpdateUserLists_NoLock(bool create)
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
        foreach (UserProfile user in users)
        {
          ListItem item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, user.Name);
          item.AdditionalProperties[Consts.KEY_USER] = user;
          item.SelectedProperty.Attach(OnUserItemSelectionChanged);
          lock (_syncObj)
            _userList.Add(item);
        }
        if (_userList.Count > 0)
        {
          _userList[0].Selected = true;
          SetUser((UserProfile)_userList[0].AdditionalProperties[Consts.KEY_USER]);
        }
      }
      catch (NotConnectedException)
      {
        throw;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Warn("Problems updating users", e);
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    private void OnUserItemSelectionChanged(AbstractProperty property, object oldValue)
    {
      UserProfile userProfile = null;
      lock (_syncObj)
      {
        userProfile = _userList.Where(i => i.Selected).Select(i => (UserProfile)i.AdditionalProperties[Consts.KEY_USER]).FirstOrDefault();
      }
      SetUser(userProfile);
    }

    private void OnProfileTypeChanged(AbstractProperty property, object oldValue)
    {
      ProfileTypeName = _profileTypes.FirstOrDefault(i => i.Key == UserProxy.ProfileType).Value;
    }

    protected internal async Task UpdateShareLists_NoLock(bool create)
    {
      lock (_syncObj)
      {
        if (_updatingProperties)
          return;
        _updatingProperties = true;
        if (create)
        {
          _serverSharesList = new ItemsList();
          _localSharesList = new ItemsList();
        }
      }
      try
      {
        ILocalSharesManagement sharesManagement = ServiceRegistration.Get<ILocalSharesManagement>();
        var shares = sharesManagement.Shares.Values;
        _localSharesList.Clear();
        foreach (Share share in shares)
        {
          ListItem item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, share.Name);
          item.AdditionalProperties[Consts.KEY_SHARE] = share;
          if (UserProxy != null)
            item.Selected = UserProxy.SelectedShares.Contains(share.ShareId);
          lock (_syncObj)
            _localSharesList.Add(item);
        }

        IServerConnectionManager scm = ServiceRegistration.Get<IServerConnectionManager>();
        if (scm == null || scm.ContentDirectory == null)
          return;

        // add users to expose them
        shares = await scm.ContentDirectory.GetSharesAsync(scm.HomeServerSystemId, SharesFilter.All);
        _serverSharesList.Clear();
        foreach (Share share in shares)
        {
          ListItem item = new ListItem();
          item.SetLabel(Consts.KEY_NAME, share.Name);
          item.AdditionalProperties[Consts.KEY_SHARE] = share;
          if (UserProxy != null)
            item.Selected = UserProxy.SelectedShares.Contains(share.ShareId);
          lock (_syncObj)
            _serverSharesList.Add(item);
        }
        SystemName homeServerSystem = scm.LastHomeServerSystem;
        IsLocalHomeServer = homeServerSystem != null && homeServerSystem.IsLocalSystem();
        IsHomeServerConnected = homeServerSystem != null;
        ShowLocalShares = !IsLocalHomeServer || _localSharesList.Count > 0;
        AnyShareAvailable = _serverSharesList.Count > 0 || _localSharesList.Count > 0;
      }
      catch (NotConnectedException)
      {
        throw;
      }
      catch (Exception e)
      {
        ServiceRegistration.Get<ILogger>().Error("Problems updating shares", e);
      }
      finally
      {
        lock (_syncObj)
          _updatingProperties = false;
      }
    }

    protected void ClearData()
    {
      lock (_syncObj)
      {
        _userList = null;
        _localSharesList = null;
        _serverSharesList = null;
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
      _ = UpdateShareLists_NoLock(true);
      _ = UpdateUserLists_NoLock(true);
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
