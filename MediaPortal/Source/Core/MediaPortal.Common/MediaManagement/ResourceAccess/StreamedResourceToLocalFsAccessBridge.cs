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
using System.IO;
using MediaPortal.Common.Services.MediaManagement;

namespace MediaPortal.Common.MediaManagement.ResourceAccess
{
  /// <summary>
  /// Access bridge logic which maps a complex resource accessor to a local file resource.
  /// </summary>
  /// <remarks>
  /// Typically, this class is instantiated by class <see cref="ResourceLocator"/> but it also can be used directly.
  /// </remarks>
  public class StreamedResourceToLocalFsAccessBridge : ILocalFsResourceAccessor
  {
    #region Protected fields

    protected IResourceAccessor _baseAccessor;
    protected string _rootDirectoryName;
    protected string _mountPath;

    #endregion

    #region Ctor & maintenance

    /// <summary>
    /// Creates a new instance of this class which is based on the given <paramref name="baseAccessor"/>.
    /// </summary>
    /// <param name="baseAccessor">Resource accessor denoting a file.</param>
    /// <exception cref="ArgumentException">If the given <paramref name="baseAccessor"/> doesn't denote a file
    /// resource (i.e. <c><see cref="IResourceAccessor.IsFile"/> == false</c>.</exception>
    public StreamedResourceToLocalFsAccessBridge(IResourceAccessor baseAccessor)
    {
      _baseAccessor = baseAccessor;
      MountResource();
    }

    public void Dispose()
    {
      UnmountResource();
      if (_baseAccessor != null)
      {
        _baseAccessor.Dispose();
        _baseAccessor = null;
      }
    }

    #endregion

    protected void MountResource()
    {
      IResourceMountingService resourceMountingService = ServiceRegistration.Get<IResourceMountingService>();
      _rootDirectoryName = Guid.NewGuid().ToString();
      _mountPath = resourceMountingService.CreateRootDirectory(_rootDirectoryName) == null ?
          null : resourceMountingService.AddResource(_rootDirectoryName, _baseAccessor);
    }

    protected void UnmountResource()
    {
      IResourceMountingService resourceMountingService = ServiceRegistration.Get<IResourceMountingService>();
      resourceMountingService.RemoveResource(_rootDirectoryName, _baseAccessor);
      resourceMountingService.DisposeRootDirectory(_rootDirectoryName);
    }

    public static ILocalFsResourceAccessor GetLocalFsResourceAccessor(IResourceAccessor baseResourceAccessor)
    {
      // Try to get an ILocalFsResourceAccessor
      ILocalFsResourceAccessor result = baseResourceAccessor as ILocalFsResourceAccessor;
      if (result != null)
        // Simple case: The media item is located in the local file system or the resource provider returns
        // an ILocalFsResourceAccessor from elsewhere - simply return it
        return result;
      // Set up a resource bridge mapping the remote or complex resource to a local file
      return new StreamedResourceToLocalFsAccessBridge(baseResourceAccessor);
    }

    #region IResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return null; }
    }

    public bool Exists
    {
      get { return _baseAccessor.Exists; }
    }

    public bool IsFile
    {
      get { return _baseAccessor.IsFile; }
    }

    public string ResourceName
    {
      get { return _baseAccessor.ResourceName; }
    }

    public string ResourcePathName
    {
      get { return _baseAccessor.ResourcePathName; }
    }

    public ResourcePath LocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, LocalFileSystemPath); }
    }

    public DateTime LastChanged
    {
      get { return _baseAccessor.LastChanged; }
    }

    public long Size
    {
      get { return _baseAccessor.Size; }
    }

    public bool ResourceExists(string path)
    {
      IFileSystemResourceAccessor fsra = _baseAccessor as IFileSystemResourceAccessor;
      return fsra == null ? false : fsra.ResourceExists(path);
    }

    public IResourceAccessor GetResource(string path)
    {
      IFileSystemResourceAccessor fsra = _baseAccessor as IFileSystemResourceAccessor;
      return fsra == null ? null : fsra.GetResource(path);
    }

    public void PrepareStreamAccess()
    {
      _baseAccessor.PrepareStreamAccess();
    }

    public Stream OpenRead()
    {
      // Using the stream on the base accessor doesn't cost so much resources than creating the bridge here
      return _baseAccessor.OpenRead();
    }

    public Stream OpenWrite()
    {
      // Using the stream on the base accessor doesn't cost so much resources than creating the bridge here
      return _baseAccessor.OpenWrite();
    }

    #endregion

    #region IFileSystemResourceAccessor implementation

    public bool IsDirectory
    {
      get
      {
        IFileSystemResourceAccessor fsra = _baseAccessor as IFileSystemResourceAccessor;
        return fsra == null ? false : fsra.IsDirectory;
      }
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      return new List<IFileSystemResourceAccessor>();
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      return new List<IFileSystemResourceAccessor>();
    }

    #endregion

    #region ILocalFsResourceAccessor implementation

    public string LocalFileSystemPath
    {
      get
      {
        if (_mountPath == null)
          return null;
        PrepareStreamAccess();
        return _mountPath;
      }
    }

    #endregion
  }
}