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

namespace MediaPortal.UiComponents.Login.Models
{
  /// <summary>
  /// viewmodel for handling logins
  /// </summary>
  public class LoginModel : IWorkflowModel, IDisposable
  {
    private ItemsList _usersExposed = null;
    private AbstractProperty _currentUser;
    private AbstractProperty _userPassword;

    public const string KEY_PROFILE_ID = "ProfileId";
    public const string KEY_HAS_PASSWORD = "Password";
    public const string STR_MODEL_ID_LOGIN = "82582433-FD64-41bd-9059-7F662DBDA713";
    public static readonly Guid MODEL_ID_LOGIN = new Guid(STR_MODEL_ID_LOGIN);

    /// <summary>
    /// constructor
    /// </summary>
    public LoginModel()
    {
      _currentUser = new WProperty(typeof(UserProfile), null);
      _userPassword = new WProperty(typeof(string), string.Empty);

      RefreshUserList();
      SetCurrentUser();
    }

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
        item.SetLabel("HasImage", user.Image != null ? "true" : "false");
        item.SetLabel("HasPassword", !string.IsNullOrEmpty(user.Password) ? "true" : "false");
        item.SetLabel("LastLogin", user.LastLogin.HasValue ? user.LastLogin.Value.ToString("G") : "");
        _usersExposed.Add(item);
      }
      // tell the skin that something might have changed
      _usersExposed.FireChange();
    }

    /// <summary>
    /// selects a user
    /// </summary>
    /// <param name="item"></param>
    public void SelectUser(ListItem item)
    {
      UserPassword = "";
      if ((bool)item.AdditionalProperties[KEY_HAS_PASSWORD])
      {
        ServiceRegistration.Get<IScreenManager>().ShowDialog("DialogEnterPassword",
          (string name, System.Guid id) =>
          {
            Guid profileId = (Guid)item.AdditionalProperties[KEY_PROFILE_ID];
            LoginUser(profileId, UserPassword);
          });
      }
      else
      {
        Guid profileId = (Guid)item.AdditionalProperties[KEY_PROFILE_ID];
        LoginUser(profileId, UserPassword);
      }
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

    public void Dispose()
    {
      _usersExposed = null;
    }

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
  }
}
