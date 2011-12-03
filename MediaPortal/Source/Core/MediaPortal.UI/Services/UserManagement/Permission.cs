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

using System.Collections.Generic;
using System.Linq;
using MediaPortal.UI.UserManagement;

namespace MediaPortal.UI.Services.UserManagement
{
  public class Permission : IPermission
  {
    protected string _name;
    protected IList<IPermissionObject> _objects;

    public Permission(string name)
    {
      _name = name;
      _objects = new List<IPermissionObject>();
    }

    public string Name
    {
      get { return _name; }
      set { _name = value; }
    }

    public bool AddPermissionObject(IPermissionObject item)
    {
      _objects.Add(item);
      return true;
    }

    public bool RemovePermissionObject(IPermissionObject item)
    {
      return _objects.Remove(item);
    }

    public virtual IList<IPermissionObject> GetPermissionObjects()
    {
      return _objects;
    }

    public virtual bool IncludesPermissionOn(IPermissionObject item)
    {
      return _objects.Any(obj => obj.IncludesObject(item));
    }

    public override int GetHashCode()
    {
      return _name == null ? 0 : _name.GetHashCode();
    }

    public override bool Equals(object o)
    {
      Permission other = o as Permission;
      return other != null && string.Compare(_name, other._name, false) == 0;
    }
  }
}
