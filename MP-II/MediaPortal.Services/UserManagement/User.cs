#region Copyright (C) 2007 Team MediaPortal

/*
    Copyright (C) 2007 Team MediaPortal
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
using MediaPortal.Core.UserManagement;

namespace MediaPortal.Services.UserManagement
{
  /// <summary>
  /// User Class
  /// </summary>
  public class User : IUser
  {
    protected string _userName = "";
    protected string _image = "";
    protected string _password = "";
    protected bool _needsPassword = false;
    protected DateTime _lastLogin;
    protected List<IRole> _roles;

    /// <summary>
    /// constructor
    /// </summary>
    public User(string name, bool needsPassword, DateTime lastLogin, string image)
    {
      UserName = name;
      NeedsPassword = needsPassword;
      LastLogin = lastLogin;
      UserImage = image;
      _roles = new List<IRole>();
    }

    /// <summary>
    /// gets or sets the username
    /// </summary>
    public string UserName
    {
      get { return _userName; }
      set { _userName = value; }
    }

    /// <summary>
    /// gets or sets the password
    /// </summary>
    public string Password
    {
      get { return _password; }
      set { _password = value; }
    }

    /// <summary>
    /// returns true if a password is needed to login, false otherwise
    /// </summary>
    public bool NeedsPassword
    {
      get { return _needsPassword; }
      set { _needsPassword = value; }
    }

    /// <summary>
    /// gets or sets the last date and time of login
    /// </summary>
    public DateTime LastLogin
    {
      get { return _lastLogin; }
      set { _lastLogin = value; }
    }

    /// <summary>
    /// gets or sets the path to the user image
    /// </summary>
    public string UserImage
    {
      get { return _image; }
      set { _image = @"media\users\" + value; }
    }

    /// <summary>
    /// adds a role to this user
    /// </summary>
    /// <param name="role">the role to add</param>
    /// <returns>true if added, false otherwise</returns>
    public bool AddRole(IRole role)
    {
      _roles.Add(role);
      return true;
    }

    /// <summary>
    /// removes a role from this user
    /// </summary>
    /// <param name="role">the role to remove</param>
    /// <returns>true if removed, false otherwise</returns>
    public bool RemoveRole(IRole role)
    {
      return _roles.Remove(role);
    }

    /// <summary>
    /// gets the roles assigned to this user
    /// </summary>
    /// <returns>list of roles for this user</returns>
    public List<IRole> GetRoles()
    {
      return _roles;
    }

    /// <summary>
    /// checks if this user has permission on a IPermissionObject
    /// </summary>
    /// <param name="obj">the oject to check permission for</param>
    /// <returns>true if the user has permission to access the object, false otherwise</returns>
    public bool HasPermissionOn(IPermissionObject obj)
    {
      foreach (IRole role in GetRoles())
      {
        foreach (IPermission permission in role.GetPermissions())
        {
          if (permission.HasPermissionOn(obj))
          {
            return true;
          }
        }
      }
      return false;
    }
  }
}