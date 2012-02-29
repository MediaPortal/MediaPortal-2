#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using MediaPortal.Common.ResourceAccess;

namespace MediaPortal.Common.Services.ResourceAccess.RemoteResourceProvider
{
  public class RemoteFileSystemResourceAccessor : RemoteResourceAccessorBase, IFileSystemResourceAccessor
  {
    protected long? _sizeCache = null;
    protected DateTime? _lastChangedCache = null;

    protected RemoteFileSystemResourceAccessor(string nativeSystemId, ResourcePath nativeResourcePath, bool isFile,
        string resourcePathName, string resourceName, long size, DateTime lastChanged) :
            this(nativeSystemId, nativeResourcePath, isFile, resourcePathName, resourceName)
    {
      _lastChangedCache = lastChanged;
      _sizeCache = size;
    }

    protected RemoteFileSystemResourceAccessor(string nativeSystemId, ResourcePath nativeResourcePath, bool isFile,
        string resourcePathName, string resourceName) :
            base(nativeSystemId, nativeResourcePath, isFile, resourcePathName, resourceName) { }

    public static bool ConnectFileSystem(string nativeSystemId, ResourcePath nativeResourcePath, out IFileSystemResourceAccessor result)
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
      result = new RemoteFileSystemResourceAccessor(nativeSystemId, nativeResourcePath, isFile,
          resourcePathName, resourceName, size, lastChanged);
      return true;
    }

    protected ICollection<IFileSystemResourceAccessor> WrapResourcePathsData(ICollection<ResourcePathMetadata> resourcesData)
    {
      return new List<IFileSystemResourceAccessor>(resourcesData.Select(fileData => new RemoteFileSystemResourceAccessor(
          _nativeSystemId, fileData.ResourcePath, true, fileData.HumanReadablePath,
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
      if (!rris.GetResourceInformation(_nativeSystemId, _nativeResourcePath,
          out isFileSystemResource, out isFile, out resourcePathName, out resourceName, out lastChanged, out size))
        throw new IOException(string.Format("Unable to get file information for resource '{0}' at system '{1}'", _nativeResourcePath, _nativeSystemId));
      _lastChangedCache = lastChanged;
      _sizeCache = isFile ? size : -1;
    }

    public override bool Exists
    {
      get
      {
        IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
        return rris.ResourceExists(_nativeSystemId, _nativeResourcePath);
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
      ResourcePath resourcePath = rris.ConcatenatePaths(_nativeSystemId, _nativeResourcePath, path);
      return rris.ResourceExists(_nativeSystemId, resourcePath);
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      ResourcePath resourcePath = rris.ConcatenatePaths(_nativeSystemId, _nativeResourcePath, path);
      IFileSystemResourceAccessor result;
      return ConnectFileSystem(_nativeSystemId, resourcePath, out result) ? result : null;
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
      ICollection<ResourcePathMetadata> filesData = rris.GetFiles(_nativeSystemId, _nativeResourcePath);
      return WrapResourcePathsData(filesData);
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      ICollection<ResourcePathMetadata> directoriesData = rris.GetChildDirectories(_nativeSystemId, _nativeResourcePath);
      return WrapResourcePathsData(directoriesData);
    }

    public override IResourceAccessor Clone()
    {
      RemoteFileSystemResourceAccessor result = new RemoteFileSystemResourceAccessor(_nativeSystemId, _nativeResourcePath,
          _isFile, _resourcePathName, _resourceName)
        {
            _sizeCache = _sizeCache,
            _lastChangedCache = _lastChangedCache
        };
      return result;
    }

    #endregion
  }
}