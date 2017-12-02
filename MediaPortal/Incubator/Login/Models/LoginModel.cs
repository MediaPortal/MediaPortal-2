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
using System.Linq;
using MediaPortal.Common;
using MediaPortal.Common.General;
using MediaPortal.Common.UserProfileDataManagement;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.Services.UserManagement;
using MediaPortal.UiComponents.Login.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.UiComponents.Login.General;
using MediaPortal.Common.Messaging;
using MediaPortal.Common.Runtime;
using MediaPortal.UI.Control.InputManager;
using MediaPortal.UI.Presentation.Players;
using MediaPortal.Common.Localization;
using MediaPortal.Common.Settings;
using MediaPortal.UI.ServerCommunication;

namespace MediaPortal.UiComponents.Login.Models
{
  /// <summary>
  /// viewmodel for handling logins
  /// </summary>
  public class LoginModel : BaseTimerControlledModel, IWorkflowModel, IDisposable
  {
    #region Consts

    public const string STR_MODEL_ID_LOGIN = "82582433-FD64-41bd-9059-7F662DBDA713";
    public static readonly Guid MODEL_ID_LOGIN = new Guid(STR_MODEL_ID_LOGIN);

    #endregion

    #region Private fields

    private ItemsList _loginUserList = null;
    private ItemsList _autoLoginUserList = null;
    private AbstractProperty _currentUserProperty;
    private AbstractProperty _userPasswordProperty;
    private AbstractProperty _isPasswordIncorrectProperty;
    private AbstractProperty _isUserLoggedInProperty;
    private AbstractProperty _enableUserLoginProperty;
    private Guid _passwordUser;
    private DateTime _lastActivity = DateTime.Now;
    private bool _firstLogin = true;

    #endregion

    #region Ctor

    /// <summary>
    /// constructor
    /// </summary>
    public LoginModel() : base(false, 2000)
    {
      _loginUserList = new ItemsList();
      _autoLoginUserList = new ItemsList();

      _currentUserProperty = new WProperty(typeof(UserProfile), null);
      _userPasswordProperty = new WProperty(typeof(string), string.Empty);
      _isPasswordIncorrectProperty = new WProperty(typeof(bool), false);
      _isUserLoggedInProperty = new WProperty(typeof(bool), false);
      _enableUserLoginProperty = new WProperty(typeof(bool), UserSettingStorage.UserLoginEnabled);

      _messageQueue = new AsynchronousMessageQueue(this, new[] { SystemMessaging.CHANNEL, ServerConnectionMessaging.CHANNEL });
      _messageQueue.MessageReceived += OnMessageReceived;
      _messageQueue.Start();
    }

    public override void Dispose()
    {
      base.Dispose();
      _loginUserList = null;
      _autoLoginUserList = null;
    }

    #endregion

    #region Public properties (Also accessed from the GUI)

    /// <summary>
    /// exposes the current user to the skin
    /// </summary>
    public AbstractProperty CurrentUserProperty
    {
      get { return _currentUserProperty; }
      set { _currentUserProperty = value; }
    }

    /// <summary>
    /// exposes the current user to the skin
    /// </summary>
    public UserProfile CurrentUser
    {
      get { return (UserProfile)_currentUserProperty.GetValue(); }
    }

    public AbstractProperty UserPasswordProperty
    {
      get { return _userPasswordProperty; }
      set { _userPasswordProperty = value; }
    }

    public string UserPassword
    {
      get { return (string)_userPasswordProperty.GetValue(); }
      set { _userPasswordProperty.SetValue(value); }
    }

    public AbstractProperty IsPasswordIncorrectProperty
    {
      get { return _isPasswordIncorrectProperty; }
    }

    public bool IsPasswordIncorrect
    {
      get { return (bool)_isPasswordIncorrectProperty.GetValue(); }
      set { _isPasswordIncorrectProperty.SetValue(value); }
    }

    public AbstractProperty IsUserLoggedInProperty
    {
      get { return _isUserLoggedInProperty; }
    }

    public bool IsUserLoggedIn
    {
      get { return (bool)_isUserLoggedInProperty.GetValue(); }
      set { _isUserLoggedInProperty.SetValue(value); }
    }

    public AbstractProperty EnableUserLoginProperty
    {
      get { return _enableUserLoginProperty; }
      set { _enableUserLoginProperty = value; }
    }

    public bool EnableUserLogin
    {
      get { return (bool)_enableUserLoginProperty.GetValue(); }
    }

    /// <summary>
    /// exposes the users to the skin
    /// </summary>
    public ItemsList Users
    {
      get { return _loginUserList; }
    }

    public ItemsList AutoLoginUsers
    {
      get { return _autoLoginUserList; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// selects a user
    /// </summary>
    /// <param name="item"></param>
    public void SelectUser(UserProxy item)
    {
      UserPassword = "";
      IsPasswordIncorrect = false;
      _passwordUser = item.Id;
      if (!string.IsNullOrEmpty(item.Password))
      {
        ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogEnterPassword",
          (string name, Guid id) =>
          {
            _ = LoginUser(_passwordUser, UserPassword);
          });
      }
      else
      {
        _ = LoginUser(_passwordUser, UserPassword);
      }
    }

    private void OnAutoLoginUserSelectionChanged(AbstractProperty property, object oldValue)
    {
      UserProxy userProfile = null;
      lock (_syncObj)
      {
        userProfile = (UserProxy)_autoLoginUserList.Where(i => i.Selected).FirstOrDefault();
      }
      SelectAutoUser(userProfile);
    }

    public void SelectAutoUser(UserProxy item)
    {
      if (item == null)
        return;

      UserPassword = "";
      IsPasswordIncorrect = false;
      _passwordUser = item.Id;
      if (!string.IsNullOrEmpty(item.Password) && UserSettingStorage.AutoLoginUser != item.Id)
      {
        ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogEnterPassword",
          (string name, Guid id) =>
          {
            _ = SetAutoLoginUser(_passwordUser, UserPassword);
          });
      }
      else
      {
        _ = SetAutoLoginUser(_passwordUser, UserPassword);
      }
    }

    public void ConfirmPassword()
    {
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement.UserProfileDataManagement == null)
        return;

      var result = userManagement.UserProfileDataManagement.GetProfileAsync(_passwordUser).Result;
      if (!result.Success)
        return;
      UserProfile userProfile = result.Result;
      if (Utils.VerifyPassword(UserPassword, userProfile.Password))
      {
        IsPasswordIncorrect = false;
        ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
      }
      else
      {
        IsPasswordIncorrect = true;
      }
    }

    public void LogoutUser()
    {
      //Logout user and return to home screen
      if (IsUserLoggedIn)
      {
        SetCurrentUser(null).Wait();
        ShowHomeScreen(true); //Force home screen and clear history
        if (UserSettingStorage.UserLoginScreenEnabled)
          ShowLoginScreen();
      }
    }

    #endregion

    #region Private and protected methods

    protected override void Update()
    {
      if (IsUserLoggedIn && UserSettingStorage.AutoLogoutEnabled && CheckIfIdle())
      {
        // Logout inactive user
        LogoutUser();
        return;
      }

      // Update login mode
      if (EnableUserLogin != UserSettingStorage.UserLoginEnabled)
        EnableUserLoginProperty.SetValue(UserSettingStorage.UserLoginEnabled);

      // Client login retry
      if (CurrentUser == UserManagement.UNKNOWN_USER)
        SetCurrentUser().Wait();

      // Update user
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement?.CurrentUser?.Name != CurrentUser?.Name)
      {
        CurrentUserProperty.SetValue(userManagement.CurrentUser);
        CurrentUserProperty.Fire(null);
      }
    }

    private void ShowHomeScreen(bool force)
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      if (force || workflowManager.CurrentNavigationContext.WorkflowState.StateId != Consts.WF_STATE_ID_HOME_SCREEN)
      {
        workflowManager.NavigatePopToState(Consts.WF_STATE_ID_HOME_SCREEN, false);
      }
    }

    private void ShowLoginScreen()
    {
      IWorkflowManager workflowManager = ServiceRegistration.Get<IWorkflowManager>();
      workflowManager.NavigatePush(Consts.WF_STATE_ID_LOGIN_SCREEN, new NavigationContextConfig());
    }

    private bool CheckIfIdle()
    {
      TimeSpan idleTimeout = TimeSpan.FromMinutes(UserSettingStorage.AutoLogoutIdleTimeoutInMin);
      IInputManager inputManager = ServiceRegistration.Get<IInputManager>();
      if((_lastActivity - inputManager.LastMouseUsageTime) < idleTimeout ||
              (_lastActivity - inputManager.LastInputTime) < idleTimeout)
      {
        _lastActivity = DateTime.Now;
        return false;
      }

      IPlayerContextManager playerContextManager = ServiceRegistration.Get<IPlayerContextManager>();
      IPlayer primaryPlayer = playerContextManager[PlayerContextIndex.PRIMARY];
      IMediaPlaybackControl mbc = primaryPlayer as IMediaPlaybackControl;
      if (((primaryPlayer is IVideoPlayer || primaryPlayer is IImagePlayer) && (mbc != null)) ||
          playerContextManager.IsFullscreenContentWorkflowStateActive)
      {
        _lastActivity = DateTime.Now;
        return false;
      }

      return true;
    }

    private void OnMessageReceived(AsynchronousMessageQueue queue, SystemMessage message)
    {
      if (message.ChannelName == SystemMessaging.CHANNEL)
      {
        SystemMessaging.MessageType messageType = (SystemMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case SystemMessaging.MessageType.SystemStateChanged:
            SystemState newState = (SystemState)message.MessageData[SystemMessaging.NEW_STATE];
            if(newState == SystemState.Running)
            {
              StartTimer();
              if (UserSettingStorage.AutoLoginUser == Guid.Empty && UserSettingStorage.UserLoginScreenEnabled && UserSettingStorage.UserLoginEnabled)
              {
                ShowLoginScreen();
              }
            }
            else if (newState == SystemState.Suspending || newState == SystemState.Hibernating)
            {
              LogoutUser();
            }
            else if (newState == SystemState.ShuttingDown)
            {
              StopTimer();
            }
            break;
        }
      }
      else if(message.ChannelName == ServerConnectionMessaging.CHANNEL)
      {
        ServerConnectionMessaging.MessageType messageType = (ServerConnectionMessaging.MessageType)message.MessageType;
        switch (messageType)
        {
          case ServerConnectionMessaging.MessageType.HomeServerConnected:
            _ = SetCurrentUser();

            _ = RefreshUserList();
            break;
        }
      }
    }

    private async Task SetCurrentUser(UserProfile userProfile = null)
    {
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfile == null)
      {
        if (UserSettingStorage.AutoLoginUser != Guid.Empty && _firstLogin)
        {
          if (userProfileDataManagement.UserProfileDataManagement != null)
          {
            var result = await userProfileDataManagement.UserProfileDataManagement.GetProfileAsync(UserSettingStorage.AutoLoginUser);
            if (result.Success)
            {
              userProfile = userProfileDataManagement.CurrentUser = result.Result;
              _firstLogin = false;
            }
          }
        }
        if(userProfile == null)
        {
          // Init with system default
          userProfileDataManagement.CurrentUser = null;
          userProfile = userProfileDataManagement.CurrentUser;
        }
      }
      else
      {
        userProfileDataManagement.CurrentUser = userProfile;
      }
      CurrentUserProperty.SetValue(userProfile);

      if (userProfile != UserManagement.UNKNOWN_USER)
      {
        if (userProfileDataManagement.UserProfileDataManagement != null)
          await userProfileDataManagement.UserProfileDataManagement.LoginProfileAsync(userProfile.ProfileId);
        _lastActivity = DateTime.Now;
        IsUserLoggedIn = !userProfile.Name.Equals(System.Windows.Forms.SystemInformation.ComputerName, StringComparison.InvariantCultureIgnoreCase) ||
          userProfile.ProfileType != UserProfileType.ClientProfile;
      }
      else
      {
        IsUserLoggedIn = false;
      }
    }

    /// <summary>
    /// this will turn the _users list into the _usersExposed list
    /// </summary>
    private async Task RefreshUserList()
    {
      // clear the exposed users list
      _loginUserList.Clear();
      _autoLoginUserList.Clear();

      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement.UserProfileDataManagement == null)
        return;

      UserProfile defaultProfile = new UserProfile(Guid.Empty, 
        LocalizationHelper.Translate(Consts.RES_SYSTEM_DEFAULT_TEXT) + " (" + System.Windows.Forms.SystemInformation.ComputerName + ")", 
        UserProfileType.ClientProfile);
      UserProxy proxy = new UserProxy();
      proxy.SetLabel(Consts.KEY_NAME, defaultProfile.Name);
      proxy.SetUserProfile(defaultProfile);
      proxy.Selected = true;
      proxy.SelectedProperty.Attach(OnAutoLoginUserSelectionChanged);
      _autoLoginUserList.Add(proxy);

      // add users to expose them
      var users = await userManagement.UserProfileDataManagement.GetProfilesAsync();
      foreach (UserProfile user in users)
      {
        if (user.ProfileType != UserProfileType.ClientProfile)
        {
          proxy = new UserProxy();
          proxy.SetLabel(Consts.KEY_NAME, user.Name);
          proxy.SetUserProfile(user);
          _loginUserList.Add(proxy);
        }

        if (!user.Name.Equals(System.Windows.Forms.SystemInformation.ComputerName, StringComparison.InvariantCultureIgnoreCase))
        {
          proxy = new UserProxy();
          proxy.SetLabel(Consts.KEY_NAME, user.Name);
          proxy.SetUserProfile(user);
          if (UserSettingStorage.AutoLoginUser == user.ProfileId)
            proxy.Selected = true;
          proxy.SelectedProperty.Attach(OnAutoLoginUserSelectionChanged);
          _autoLoginUserList.Add(proxy);
        }
      }

      // tell the skin that something might have changed
      _loginUserList.FireChange();
      _autoLoginUserList.FireChange();
    }

    private async Task LoginUser(Guid profileId, string password)
    {
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement.UserProfileDataManagement == null)
        return;
      var result = await userManagement.UserProfileDataManagement.GetProfileAsync(profileId);
      if (!result.Success)
        return;
      UserProfile userProfile = result.Result;

      if (string.IsNullOrEmpty(userProfile.Password) || Utils.VerifyPassword(password, userProfile.Password))
      {
        await SetCurrentUser(userProfile);
        await userManagement.UserProfileDataManagement.LoginProfileAsync(profileId);
        ShowHomeScreen(false);
      }
    }

    private async Task SetAutoLoginUser(Guid profileId, string password)
    {
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      UserProfile userProfile = null;
      UserProxy listUser;
      if (profileId != Guid.Empty && userManagement.UserProfileDataManagement != null)
      {
        var result = await userManagement.UserProfileDataManagement.GetProfileAsync(profileId);
        if (result.Success && Utils.VerifyPassword(password, result.Result.Password))
          userProfile = result.Result;
      }
      if (userProfile != null)
      {
        ISettingsManager localSettings = ServiceRegistration.Get<ISettingsManager>();
        UserSettings settings = localSettings.Load<UserSettings>();
        settings.AutoLoginUser = userProfile.ProfileId;
        localSettings.Save(settings);
        UserSettingStorage.AutoLoginUser = userProfile.ProfileId;

        listUser = (UserProxy)_autoLoginUserList.FirstOrDefault(u => ((UserProxy)u).Id == userProfile.ProfileId);
        if (listUser != null)
        {
          listUser.Selected = true;
          return;
        }
      }
      //Try to select current selected user
      listUser = (UserProxy)_autoLoginUserList.FirstOrDefault(u => ((UserProxy)u).Id == UserSettingStorage.AutoLoginUser);
      if (listUser != null)
      {
        listUser.Selected = true;
        return;
      }
      //Try to select default user
      if (_autoLoginUserList.Count > 0)
        _autoLoginUserList[0].Selected = true;
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_LOGIN; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      if (!UserSettingStorage.UserLoginEnabled)
        return false;
      if (oldContext?.WorkflowState?.StateId == Consts.WF_STATE_ID_HOME_SCREEN)
        return true;
      if (oldContext?.WorkflowState?.Name.Contains("/Users") ?? false)
        return true;

      return false;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _ = RefreshUserList();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      _loginUserList.Clear();
      _autoLoginUserList.Clear();
    }

    public void ChangeModelContext(NavigationContext oldContext, NavigationContext newContext, bool push)
    {
    }

    public void Deactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void Reactivate(NavigationContext oldContext, NavigationContext newContext)
    {
    }

    public void UpdateMenuActions(NavigationContext context, IDictionary<Guid, WorkflowAction> actions)
    {
    }

    public ScreenUpdateMode UpdateScreen(NavigationContext context, ref string screen)
    {
      return ScreenUpdateMode.AutoWorkflowManager;
    }

    #endregion
  }
}
