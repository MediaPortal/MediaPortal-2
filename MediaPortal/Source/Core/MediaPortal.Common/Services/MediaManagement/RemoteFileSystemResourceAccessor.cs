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
using System.Linq;
using MediaPortal.Common.MediaManagement.ResourceAccess;

namespace MediaPortal.Common.Services.MediaManagement
{
  public class RemoteFileSystemResourceAccessor : RemoteResourceAccessorBase, IFileSystemResourceAccessor
  {
    protected long? _sizeCache = null;
    protected DateTime? _lastChangedCache = null;

    protected RemoteFileSystemResourceAccessor(IResourceLocator resourceLocator, bool isFile,
        string resourcePathName, string resourceName, long size, DateTime lastChanged) :
        this(resourceLocator, isFile, resourcePathName, resourceName)
    {
      _lastChangedCache = lastChanged;
      _sizeCache = size;
    }

    protected RemoteFileSystemResourceAccessor(IResourceLocator resourceLocator, bool isFile,
        string resourcePathName, string resourceName) :
        base(resourceLocator, isFile, resourcePathName, resourceName) { }

    public static bool ConnectFileSystem(string nativeSystemId, ResourcePath nativeResourcePath,
        out IFileSystemResourceAccessor result)
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      result = null;
      bool isFileSystemResource;
      bool isFile;
      string resourcePathName;
      string resourceName;
      long size;
      DateTime lastChanged;
      if (!rris.GetResourceInformation(nativeSystemId, nativeResourcePath, out isFileSystemResource, out isFile,
          out resourcePathName, out resourceName, out lastChanged, out size) || !isFileSystemResource)
        return false;
      result = new RemoteFileSystemResourceAccessor(new ResourceLocator(nativeSystemId, nativeResourcePath), isFile,
          resourcePathName, resourceName, size, lastChanged);
      return true;
    }

    protected ICollection<IFileSystemResourceAccessor> WrapResourcePathsData(ICollection<ResourcePathMetadata> resourcesData)
    {
      string nativeSystemId = _resourceLocator.NativeSystemId;
      return new List<IFileSystemResourceAccessor>(resourcesData.Select(fileData => new RemoteFileSystemResourceAccessor(
          new ResourceLocator(nativeSystemId, fileData.ResourcePath), true, fileData.HumanReadablePath,
              fileData.ResourceName)).Cast<IFileSystemResourceAccessor>());
    }

    protected void FillCaches()
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      bool isFileSystemResource;
      bool isFile;
      string resourcePathName;
      string resourceName;
      DateTime lastChanged;
      long size;
      if (!rris.GetResourceInformation(_resourceLocator.NativeSystemId, _resourceLocator.NativeResourcePath,
          out isFileSystemResource, out isFile, out resourcePathName, out resourceName, out lastChanged, out size))
        throw new IOException(string.Format("Unable to get file information for '{0}'", _resourceLocator));
      _lastChangedCache = lastChanged;
      _sizeCache = isFile ? size : -1;
    }

    public override bool Exists
    {
      get
      {
        IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
        return rris.ResourceExists(_resourceLocator.NativeSystemId, _resourceLocator.NativeResourcePath);
      }
    }

    public override long Size
    {
      get
      {
        if (_sizeCache.HasValue)
          return _sizeCache.Value;
        FillCaches();
        return _sizeCache.Value;
      }
    }

    #region IFileSystemResourceAccessor implementation

    public bool IsDirectory
    {
      get { return !_isFile; }
    }

    public bool ResourceExists(string path)
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      string nativeSystemId = _resourceLocator.NativeSystemId;
      ResourcePath resourcePath = rris.ConcatenatePaths(nativeSystemId, _resourceLocator.NativeResourcePath, path);
      return rris.ResourceExists(nativeSystemId, resourcePath);
    }

    public IResourceAccessor GetResource(string path)
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      string nativeSystemId = _resourceLocator.NativeSystemId;
      ResourcePath resourcePath = rris.ConcatenatePaths(nativeSystemId, _resourceLocator.NativeResourcePath, path);
      IFileSystemResourceAccessor result;
      return ConnectFileSystem(nativeSystemId, resourcePath, out result) ? result : null;
    }

    public override DateTime LastChanged
    {
      get
      {
        if (_lastChangedCache.HasValue)
          return _lastChangedCache.Value;
        FillCaches();
        return _lastChangedCache.Value;
      }
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      ICollection<ResourcePathMetadata> filesData = rris.GetFiles(
          _resourceLocator.NativeSystemId, _resourceLocator.NativeResourcePath);
      return WrapResourcePathsData(filesData);
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      ICollection<ResourcePathMetadata> directoriesData = rris.GetChildDirectories(
          _resourceLocator.NativeSystemId, _resourceLocator.NativeResourcePath);
      return WrapResourcePathsData(directoriesData);
    }

    #endregion
  }
}