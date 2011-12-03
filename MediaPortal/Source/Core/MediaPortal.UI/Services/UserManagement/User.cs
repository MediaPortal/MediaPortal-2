#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using MediaPortal.UI.UserManagement;

namespace MediaPortal.UI.Services.UserManagement
{
  public class User : IUser
  {
    protected string _userName;
    protected DateTime _lastLogin;
    protected string _image = string.Empty;
    protected string _password = string.Empty;
    protected bool _needsPassword = false;
    protected List<IRole> _roles = new List<IRole>();

    public User(string name)
    {
      _userName = name;
      _lastLogin = new DateTime();
    }

    public string UserName
    {
      get { return _userName; }
      set { _userName = value; }
    }

    public string Password
    {
      get { return _password; }
      set { _password = value; }
    }

    public bool NeedsPassword
    {
      get { return _needsPassword; }
      set { _needsPassword = value; }
    }

    public DateTime LastLogin
    {
      get { return _lastLogin; }
      set { _lastLogin = value; }
    }

    public string UserImage
    {
      get { return _image; }
      set { _image = @"media\users\" + value; }
    }

    public bool AddRole(IRole role)
    {
      _roles.Add(role);
      return true;
    }

    public bool RemoveRole(IRole role)
    {
      return _roles.Remove(role);
    }

    public IList<IRole> GetRoles()
    {
      return _roles;
    }

    public bool HasPermissionOn(IPermissionObject obj)
    {
      return GetRoles().SelectMany(role => role.GetPermissions()).Any(permission => permission.IncludesPermissionOn(obj));
    }

    public override int GetHashCode()
    {
      return _userName == null ? 0 : _userName.GetHashCode();
    }

    public override bool Equals(object o)
    {
      User other = o as User;
      return other != null && string.Compare(_userName, other._userName, false) == 0;
    }
  }
}
