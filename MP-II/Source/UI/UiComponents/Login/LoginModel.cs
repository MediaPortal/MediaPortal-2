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
using System.Windows.Forms;
using MediaPortal.Core;
using MediaPortal.Core.General;
using MediaPortal.UI.Presentation.DataObjects;
using MediaPortal.UI.UserManagement;

namespace UiComponents.Login
{
  /// <summary>
  /// viewmodel for handling logins
  /// </summary>
  public class LoginModel
  {
    private ItemsList _usersExposed = new ItemsList();
    private AbstractProperty _currentUser;

    /// <summary>
    /// constructor
    /// </summary>
    public LoginModel()
    {
      _currentUser = new WProperty(null);
      LoadUsers();
    }

    /// <summary>
    /// will load the users from somewhere
    /// </summary>
    private void LoadUsers()
    {
      IUserService userService = ServiceScope.Get<IUserService>();
      // add a few dummy users, later this should be more flexible and handled by a login manager / user account control
      IUser u1 = userService.AddUser(SystemInformation.ComputerName.ToLower());
      u1.NeedsPassword = true;
      u1.LastLogin = new DateTime(2007, 10, 25, 12, 20, 30);
      u1.UserImage = SystemInformation.ComputerName.ToLower() + ".jpg";
      IUser u2 = userService.AddUser(SystemInformation.UserName.ToLower());
      u2.NeedsPassword = false;
      u2.LastLogin = new DateTime(2007, 10, 26, 10, 30, 13);
      u2.UserImage = SystemInformation.UserName.ToLower() + ".jpg";
      RefreshUserList();
    }

    /// <summary>
    /// this will turn the _users list into the _usersExposed list
    /// </summary>
    private void RefreshUserList()
    {
      IList<IUser> users = ServiceScope.Get<IUserService>().GetUsers();
      // clear the exposed users list
      Users.Clear();
      // add users to expose them
      foreach (IUser user in users)
      {
        if (user == null)
        {
          continue;
        }
        ListItem buff = new ListItem();
        buff.SetLabel("UserName", user.UserName);
        buff.SetLabel("UserImage", user.UserImage);
        if (user.NeedsPassword)
        {
          buff.SetLabel("NeedsPassword", "true");
        }
        else
        {
          buff.SetLabel("NeedsPassword", "false");
        }
        buff.SetLabel("LastLogin", user.LastLogin.ToString("G"));
        Users.Add(buff);
      }
      // tell the skin that something might have changed
      Users.FireChange();
    }

    /// <summary>
    /// selects a user
    /// </summary>
    /// <param name="item"></param>
    public void SelectUser(ListItem item)
    {
      IList<IUser> users = ServiceScope.Get<IUserService>().GetUsers();

      foreach (IUser user in users)
      {
        if (user.UserName == item.Labels["UserName"].Evaluate())
        {
          CurrentUser = user;
          return;
        }
      }
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
    public IUser CurrentUser
    {
      get { return (IUser) _currentUser.GetValue(); }
      set { _currentUser.SetValue(value); }
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
