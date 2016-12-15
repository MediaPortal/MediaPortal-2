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
  /// Handle for a virtual resource.
  /// </summary>
  /// <remarks>
  /// Multithreading safety is ensured by locking on the mounting service's synchronization object.
  /// </remarks>
  public abstract class VirtualFileSystemResource
  {
    protected string _name;

    protected IFileSystemResourceAccessor _resourceAccessor;
    protected ICollection<FileHandle> _fileHandles = new HashSet<FileHandle>();
    protected DateTime _creationTime;

    protected VirtualFileSystemResource(string name, IFileSystemResourceAccessor resourceAccessor)
    {
      _name = name;
      _resourceAccessor = resourceAccessor;
      _creationTime = DateTime.Now;
    }

    public virtual void Dispose()
    {
      foreach (FileHandle handle in _fileHandles)
        handle.Cleanup();
      if (_resourceAccessor != null)
        try
        {
          _resourceAccessor.Dispose();
        }
        catch (Exception e)
        {
          ServiceRegistration.Get<ILogger>().Warn("Dokan virtual filesystem resource: Error disposing resource accessor '{0}'", e, _resourceAccessor);
        }
      _resourceAccessor = null;
    }

    public IFileSystemResourceAccessor ResourceAccessor
    {
      get { return _resourceAccessor; }
    }

    public DateTime CreationTime
    {
      get { return _creationTime; }
    }

    public void AddFileHandle(FileHandle handle)
    {
      _fileHandles.Add(handle);
    }

    public void RemoveFileHandle(FileHandle handle)
    {
      _fileHandles.Remove(handle);
    }
  }
}