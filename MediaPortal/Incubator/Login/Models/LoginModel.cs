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
using MediaPortal.UiComponents.SkinBase.General;
using MediaPortal.UiComponents.Login.Settings;
using MediaPortal.UI.Presentation.Models;
using MediaPortal.UI.Presentation.Workflow;
using System.Collections.Generic;
using MediaPortal.UI.Presentation.Screens;
using MediaPortal.Utilities.Events;

namespace MediaPortal.UiComponents.Login.Models
{
  /// <summary>
  /// viewmodel for handling logins
  /// </summary>
  public class LoginModel : IWorkflowModel, IDisposable
  {
    #region Consts

    public const string KEY_PROFILE_ID = "ProfileId";
    public const string KEY_HAS_PASSWORD = "Password";
    public const string STR_MODEL_ID_LOGIN = "82582433-FD64-41bd-9059-7F662DBDA713";
    public static readonly Guid MODEL_ID_LOGIN = new Guid(STR_MODEL_ID_LOGIN);

    #endregion

    #region Private fields

    private ItemsList _usersExposed = null;
    private AbstractProperty _currentUser;
    private AbstractProperty _userPassword;
    private AbstractProperty _isPasswordIncorrect;
    private DelayedEvent _loginTimer = null;
    private Guid _passwordUser;

    #endregion

    #region Ctor

    /// <summary>
    /// constructor
    /// </summary>
    public LoginModel()
    {
      _currentUser = new WProperty(typeof(UserProfile), null);
      _userPassword = new WProperty(typeof(string), string.Empty);
      _isPasswordIncorrect = new WProperty(typeof(bool), false);

      _loginTimer = new DelayedEvent(1000);
      _loginTimer.OnEventHandler += DelayedLogin;

      RefreshUserList();
      SetCurrentUser();
    }

    public void Dispose()
    {
      _usersExposed = null;
    }

    #endregion

    #region Public properties (Also accessed from the GUI)

    /// <summary>
    /// exposes the current user to the skin
    /// </summary>
    public AbstractProperty CurrentUserProperty
    {
      get { return _currentUser; }
      set { _currentUser = value; }
    }

    /// <summary>
    /// exposes the current user to the skin
    /// </summary>
    public UserProfile CurrentUser
    {
      get { return (UserProfile)_currentUser.GetValue(); }
    }

    public AbstractProperty UserPasswordProperty
    {
      get { return _userPassword; }
      set { _userPassword = value; }
    }

    public string UserPassword
    {
      get { return (string)_userPassword.GetValue(); }
      set { _userPassword.SetValue(value); }
    }

    public AbstractProperty IsPasswordIncorrectProperty
    {
      get { return _isPasswordIncorrect; }
    }

    public bool IsPasswordIncorrect
    {
      get { return (bool)_isPasswordIncorrect.GetValue(); }
      set { _isPasswordIncorrect.SetValue(value); }
    }

    public bool EnableUserLogin
    {
      get { return UserSettingWatcher.UserLoginEnabled; }
    }

    /// <summary>
    /// exposes the users to the skin
    /// </summary>
    public ItemsList Users
    {
      get { return _usersExposed; }
    }

    #endregion

    #region Public methods

    /// <summary>
    /// selects a user
    /// </summary>
    /// <param name="item"></param>
    public void SelectUser(ListItem item)
    {
      UserPassword = "";
      IsPasswordIncorrect = false;
      _passwordUser = (Guid)item.AdditionalProperties[KEY_PROFILE_ID];
      if ((bool)item.AdditionalProperties[KEY_HAS_PASSWORD])
      {
        ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogEnterPassword",
          (string name, System.Guid id) =>
          {
            LoginUser(_passwordUser, UserPassword);
          });
      }
      else
      {
        LoginUser(_passwordUser, UserPassword);
      }
    }

    public void ConfirmPassword()
    {
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      UserProfile userProfile;
      if (userManagement.UserProfileDataManagement == null)
        return;
      if (!userManagement.UserProfileDataManagement.GetProfile(_passwordUser, out userProfile))
        return;
      if (General.Utils.VerifyPassword(UserPassword, userProfile.Password))
      {
        IsPasswordIncorrect = false;
        ServiceRegistration.Get<IScreenManager>().CloseTopmostDialog();
      }
      else
      {
        IsPasswordIncorrect = true;
      }
    }

    #endregion

    #region Private and protected methods

    private void SetCurrentUser(UserProfile userProfile = null)
    {
      IUserManagement userProfileDataManagement = ServiceRegistration.Get<IUserManagement>();
      if (userProfile == null)
      {
        // Init with system default
        userProfileDataManagement.CurrentUser = null;
        userProfile = userProfileDataManagement.CurrentUser;
      }
      else
      {
        userProfileDataManagement.CurrentUser = userProfile;
      }
      CurrentUserProperty.SetValue(userProfile);

      if (userProfile == UserManagement.UNKNOWN_USER)
      {
        //Schedule retry of login
        _loginTimer.EnqueueEvent(null, EventArgs.Empty);
      }
    }

    private void DelayedLogin(object sender, EventArgs e)
    {
      SetCurrentUser(null);
    }

    /// <summary>
    /// this will turn the _users list into the _usersExposed list
    /// </summary>
    private void RefreshUserList()
    {
      // clear the exposed users list
      _usersExposed = new ItemsList();

      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      if (userManagement.UserProfileDataManagement == null)
        return;
      // add users to expose them
      var users = userManagement.UserProfileDataManagement.GetProfiles();
      foreach (UserProfile user in users.Where(u => u.ProfileType != UserProfile.CLIENT_PROFILE))
      {
        ListItem item = new ListItem();
        item.SetLabel(Consts.KEY_NAME, user.Name);

        item.AdditionalProperties[KEY_PROFILE_ID] = user.ProfileId;
        item.AdditionalProperties[KEY_HAS_PASSWORD] = !string.IsNullOrEmpty(user.Password);
        item.SetLabel("HasPassword", !string.IsNullOrEmpty(user.Password) ? "true" : "false");
        item.SetLabel("LastLogin", user.LastLogin.HasValue ? user.LastLogin.Value.ToString("G") : "");
        _usersExposed.Add(item);
      }
      // tell the skin that something might have changed
      _usersExposed.FireChange();
    }

    private void LoginUser(Guid profileId, string password)
    {
      IUserManagement userManagement = ServiceRegistration.Get<IUserManagement>();
      UserProfile userProfile;
      if (userManagement.UserProfileDataManagement == null)
        return;
      if (!userManagement.UserProfileDataManagement.GetProfile(profileId, out userProfile))
        return;
      if (General.Utils.VerifyPassword(password, userProfile.Password))
      {
        SetCurrentUser(userProfile);
        userManagement.UserProfileDataManagement.LoginProfile(profileId);
      }
    }

    #endregion

    #region IWorkflowModel implementation

    public Guid ModelId
    {
      get { return MODEL_ID_LOGIN; }
    }

    public bool CanEnterState(NavigationContext oldContext, NavigationContext newContext)
    {
      return true;
    }

    public void EnterModelContext(NavigationContext oldContext, NavigationContext newContext)
    {
      RefreshUserList();
    }

    public void ExitModelContext(NavigationContext oldContext, NavigationContext newContext)
    {

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
