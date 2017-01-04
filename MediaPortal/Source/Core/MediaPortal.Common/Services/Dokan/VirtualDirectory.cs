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
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.Dokan
{
  /// <summary>
  /// Handle for a virtual directory.
  /// </summary>
  public class VirtualDirectory : VirtualBaseDirectory
  {
    protected IDictionary<string, VirtualFileSystemResource> _children = null; // Lazily initialized

// ReSharper disable SuggestBaseTypeForParameter
    public VirtualDirectory(string name, IFileSystemResourceAccessor resourceAccessor) : base(name, resourceAccessor) { }
// ReSharper restore SuggestBaseTypeForParameter

    public override void Dispose()
    {
      if (_children != null)
        foreach (VirtualFileSystemResource resource in _children.Values)
          resource.Dispose();
      _children = null;
      base.Dispose();
    }

    public IFileSystemResourceAccessor Directory
    {
      get { return _resourceAccessor; }
    }

    public override IDictionary<string, VirtualFileSystemResource> ChildResources
    {
      get
      {
        if (_children == null)
        {
          _children = new Dictionary<string, VirtualFileSystemResource>(StringComparer.InvariantCultureIgnoreCase);
          try
          {
            ICollection<IFileSystemResourceAccessor> entries = Directory.GetChildDirectories();
            if (entries != null)
              foreach (IFileSystemResourceAccessor childDirectoryAccessor in entries)
                _children[childDirectoryAccessor.ResourceName] = new VirtualDirectory(
                    childDirectoryAccessor.ResourceName, childDirectoryAccessor);
            entries = Directory.GetFiles();
            if (entries != null)
              foreach (IFileSystemResourceAccessor fileAccessor in entries)
                _children[fileAccessor.ResourceName] = new VirtualFile(fileAccessor.ResourceName, fileAccessor);
          }
          catch (Exception e)
          {
            ServiceRegistration.Get<ILogger>().Warn("Dokan virtual directory: Error collecting child resources of directory '{0}'", e, _name);
          }
        }
        return _children;
      }
    }

    public override string ToString()
    {
      return string.Format("Virtual directory '{0}'", _name);
    }
  }
}