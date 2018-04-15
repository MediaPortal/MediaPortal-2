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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge
{
  internal class MountingDataProxy : IDisposable
  {
    protected string _key;
    protected int _usageCount = 0;
    protected IFileSystemResourceAccessor _baseAccessor;
    protected string _rootDirectoryName;
    protected string _mountPath;
    protected object _syncObj = new object();

    /// <summary>
    /// Creates a new instance of this class which is based on the given <paramref name="baseAccessor"/>.
    /// </summary>
    /// <param name="key">Key which is used for this instance.</param>
    /// <param name="baseAccessor">Filesystem resource to mound.</param>
    public MountingDataProxy(string key, IFileSystemResourceAccessor baseAccessor)
    {
      _key = key;
      _baseAccessor = baseAccessor;
      if (!MountResource())
        throw new EnvironmentException("Cannot mount resource '{0}' to local file system", baseAccessor);
    }

    public void Dispose()
    {
      if (_baseAccessor == null)
        // Already disposed
        return;
      if (!UnmountResource())
        // The ownership was transferred to the resource mounting service, so if unmounting was succesful, we must not dispose our base accessor
        _baseAccessor.Dispose();
      _baseAccessor = null;
    }

    #region Protected methods

    protected bool MountResource()
    {
      IResourceMountingService resourceMountingService = ServiceRegistration.Get<IResourceMountingService>();
      _rootDirectoryName = Guid.NewGuid().ToString();
      _mountPath = resourceMountingService.CreateRootDirectory(_rootDirectoryName) == null ? null :
          resourceMountingService.AddResource(_rootDirectoryName, _baseAccessor);
      return _mountPath != null;
    }

    protected bool UnmountResource()
    {
      IResourceMountingService resourceMountingService = ServiceRegistration.Get<IResourceMountingService>();
      if (_mountPath == null)
        return false;
      resourceMountingService.RemoveResource(_rootDirectoryName, _baseAccessor);
      resourceMountingService.DisposeRootDirectory(_rootDirectoryName);
      return true;
    }

    protected void FireMountingDataOrphaned()
    {
      MountingDataOrphanedDlgt dlgt = MountingDataOrphaned;
      if (dlgt != null)
        dlgt(this);
    }

    #endregion

    #region Public members

    public delegate void MountingDataOrphanedDlgt(MountingDataProxy proxy);

    public MountingDataOrphanedDlgt MountingDataOrphaned;

    public string Key
    {
      get { return _key; }
    }

    public int UsageCount
    {
      get { return _usageCount; }
    }

    /// <summary>
    /// Returns a resource path which points to the transient local resource provided by this resource access bridge.
    /// The resource referred by this transient path is available until <see cref="UnmountResource"/> is called, which is called
    /// in the <see cref="Dispose"/> method.
    /// </summary>
    public ResourcePath TransientLocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, LocalFileSystemPath); }
    }

    public string LocalFileSystemPath
    {
      get { return _mountPath; }
    }

    public IFileSystemResourceAccessor ResourceAccessor
    {
      get { return _baseAccessor; }
    }

    public string MountPath
    {
      get { return _mountPath; }
    }

    public void IncUsage()
    {
      lock (_syncObj)
        _usageCount++;
    }

    public void DecUsage()
    {
      lock (_syncObj)
      {
        _usageCount--;
        if (_usageCount > 0)
          return;
      }
      // Outside the lock:
      FireMountingDataOrphaned();
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return "MountData; Root dir = '"  + _rootDirectoryName + "', mount path='" + _mountPath + "', base accessor='" + _baseAccessor + "'";
    }

    #endregion
  }
}