#region Copyright (C) 2007-2021 Team MediaPortal

/*
    Copyright (C) 2007-2021 Team MediaPortal
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
using System.IO;
using System.Linq;
using MediaPortal.Common.Logging;
using MediaPortal.Common.PathManager;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.Dokan;
using MediaPortal.Common.SystemResolver;

namespace MediaPortal.Common.Services.ResourceAccess
{
  /// <summary>
  /// <see cref="IResourceMountingService"/> backed by the Dokan user-mode file system.
  /// </summary>
  /// <remarks>
  /// Lives in <c>MediaPortal.Common.Windows</c> because Dokan is a Windows file system driver: the
  /// binding in <c>Services\Dokan</c> talks to <c>dokan2.dll</c> through P/Invoke and fills Win32
  /// structures such as <c>WIN32_FIND_DATA</c>. The interface stayed in <c>MediaPortal.Common</c>, so
  /// the code that mounts resources does not know which of the two implementations it got.
  ///
  /// This used to be called <c>ResourceMountingService</c> and was registered unconditionally by
  /// <see cref="MediaPortal.Common.ApplicationCore"/>. The name says which backend it is now, because
  /// <c>MediaPortal.Common</c> ships a second, portable implementation for platforms without a
  /// mount-capable back end.
  /// </remarks>
  public class DokanResourceMountingService : IResourceMountingService, IDisposable
  {
    /// <summary>
    /// Registers this implementation as the process-wide <see cref="IResourceMountingService"/>,
    /// replacing the portable one that <see cref="ApplicationCore.RegisterCoreServices"/> installed.
    /// </summary>
    /// <remarks>
    /// Called by the Windows hosts during startup, next to <c>GdiPlusImageTransformer.Register()</c>.
    /// <c>MediaPortal.Common</c> cannot call this itself without referencing a platform assembly, which
    /// would put the dependency right back where it was.
    /// </remarks>
    public static void Register()
    {
      ServiceRegistration.Set<IResourceMountingService>(new DokanResourceMountingService());
    }

    protected object _syncObj = new object();
    protected Dokan.Dokan _dokanExecutor = null;

    #region IDisposable implementation

    public void Dispose()
    {
      Shutdown(); // Make sure we're shut down
    }

    #endregion

    #region IResourceMountingService implementation

    public ResourcePath RootPath
    {
      get
      {
        lock (_syncObj)
          return _dokanExecutor == null ? null : LocalFsResourceProviderBase.ToResourcePath(_dokanExecutor.MountPoint);
      }
    }

    public ICollection<string> RootDirectories
    {
      get
      {
        lock (_syncObj)
          return new List<string>(_dokanExecutor.RootDirectory.ChildResources.Keys);
      }
    }

    public void Startup()
    {
      IPathManager pathManager = ServiceRegistration.Get<IPathManager>();
      string mountPoint = pathManager.GetPath("<REMOTERESOURCES>");

      if (!Directory.Exists(mountPoint))
      {
        Directory.CreateDirectory(mountPoint);
      }

      _dokanExecutor = Dokan.Dokan.Install(mountPoint);     

      if (_dokanExecutor == null)
      {
        ServiceRegistration.Get<ILogger>().Warn("DokanResourceMountingService: Due to problems in DOKAN, resources cannot be mounted into the local filesystem");
      }
      else
      {
        // We share the same synchronization object to avoid multithreading issues between the two classes
        _syncObj = _dokanExecutor.SyncObj;
      } 
    }

    public void Shutdown()
    {
      if (_dokanExecutor == null)
        return;
      _dokanExecutor.Dispose();
      _dokanExecutor = null;
    }

    public void Restart()
    {
      ServiceRegistration.Get<ILogger>().Info("DokanResourceMountingService: Restarting service");
      lock (_syncObj)
      {
        Shutdown();
        Startup();
      }
    }

    public bool IsVirtualResource(ResourcePath rp)
    {
      if (rp == null)
        return false;
      int numPathSegments = rp.Count();
      if (numPathSegments == 0)
        return false;
      ResourcePath firstRPSegment = new ResourcePath(new ProviderPathSegment[] { rp[0] });
      String pathRoot = Path.GetPathRoot(LocalFsResourceProviderBase.ToDosPath(firstRPSegment));
      if (string.IsNullOrEmpty(pathRoot) || pathRoot.Length < 2)
        return false;
      string fullPath = LocalFsResourceProviderBase.ToDosPath(rp);
      if (!string.IsNullOrEmpty(fullPath) && fullPath.Equals(_dokanExecutor.MountPoint, StringComparison.InvariantCultureIgnoreCase))
        return true;
      return false;
    }

    public string CreateRootDirectory(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        ResourcePath rootPath = RootPath;
        if (rootPath == null)
          return null;
        _dokanExecutor.RootDirectory.AddResource(rootDirectoryName, new VirtualRootDirectory(rootDirectoryName));
        return ResourcePathHelper.Combine(rootPath.Serialize(), rootDirectoryName);
      }
    }

    public void DisposeRootDirectory(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        if (_dokanExecutor == null)
          return;
        VirtualRootDirectory rootDirectory = _dokanExecutor.GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return;
        _dokanExecutor.RootDirectory.RemoveResource(rootDirectoryName);
        rootDirectory.Dispose();
      }
    }

    public ICollection<IFileSystemResourceAccessor> GetResources(string rootDirectoryName)
    {
      lock (_syncObj)
      {
        if (_dokanExecutor == null)
          return null;
        VirtualRootDirectory rootDirectory = _dokanExecutor.GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return null;
        return rootDirectory.ChildResources.Values.Select(resource => resource.ResourceAccessor).ToList();
      }
    }

    public string AddResource(string rootDirectoryName, IFileSystemResourceAccessor resourceAccessor)
    {
      lock (_syncObj)
      {
        if (_dokanExecutor == null)
          return null;
        VirtualRootDirectory rootDirectory = _dokanExecutor.GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return null;
        string resourceName = resourceAccessor.ResourceName;
        rootDirectory.AddResource(resourceName, !resourceAccessor.IsFile ?
            (VirtualFileSystemResource) new VirtualDirectory(resourceName, resourceAccessor) :
            new VirtualFile(resourceName, resourceAccessor));
        string mountPoint = _dokanExecutor.MountPoint;
        return Path.Combine(mountPoint, rootDirectoryName + "\\" + resourceName);
      }
    }

    public void RemoveResource(string rootDirectoryName, IFileSystemResourceAccessor resourceAccessor)
    {
      lock (_syncObj)
      {
        if (_dokanExecutor == null)
          return;
        VirtualRootDirectory rootDirectory = _dokanExecutor.GetRootDirectory(rootDirectoryName);
        if (rootDirectory == null)
          return;
        string resourceName = resourceAccessor.ResourceName;
        VirtualFileSystemResource toRemove;
        if (!rootDirectory.ChildResources.TryGetValue(resourceName, out toRemove))
          return;
        rootDirectory.ChildResources.Remove(resourceName);
        toRemove.Dispose();
      }
    }

    #endregion
  }
}
