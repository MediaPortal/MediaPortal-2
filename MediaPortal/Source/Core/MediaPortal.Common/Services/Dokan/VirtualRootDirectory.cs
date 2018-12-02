#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// Handle for a virtual root directory.
  /// </summary>
  public class VirtualRootDirectory : VirtualBaseDirectory
  {
    protected IDictionary<string, VirtualFileSystemResource> _children =
        new Dictionary<string, VirtualFileSystemResource>(StringComparer.InvariantCultureIgnoreCase);

    public VirtualRootDirectory(string name) : base(name, null) { }

    public override void Dispose()
    {
      foreach (VirtualFileSystemResource resource in _children.Values)
        resource.Dispose();
      base.Dispose();
    }

    public override IDictionary<string, VirtualFileSystemResource> ChildResources
    {
      get { return _children; }
    }

    public void AddResource(string name, VirtualFileSystemResource resource)
    {
      _children.Add(name, resource);
    }

    public void RemoveResource(string name)
    {
      _children.Remove(name);
    }

    public override string ToString()
    {
      return string.Format("Virtual root directory '{0}'", _name);
    }
  }
}