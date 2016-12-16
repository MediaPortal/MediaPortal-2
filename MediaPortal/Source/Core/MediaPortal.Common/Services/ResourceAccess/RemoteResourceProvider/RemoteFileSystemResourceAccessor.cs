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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities.Exceptions;

namespace MediaPortal.Common.Services.ResourceAccess.RemoteResourceProvider
{
  public class RemoteFileSystemResourceAccessor : IFileSystemResourceAccessor
  {
    protected object _syncObj = new object();
    protected string _nativeSystemId;
    protected ResourcePath _nativeResourcePath;
    protected bool _isFile;
    protected string _resourcePathName;
    protected string _resourceName;
    protected Stream _underlayingStream = null; // Lazy initialized
    protected long? _sizeCache = null;
    protected DateTime? _lastChangedCache = null;

    protected RemoteFileSystemResourceAccessor(string nativeSystemId, ResourcePath nativeResourcePath, bool isFile,
        string resourcePathName, string resourceName)
    {
      _nativeSystemId = nativeSystemId;
      _nativeResourcePath = nativeResourcePath;
      _isFile = isFile;
      _resourcePathName = resourcePathName;
      _resourceName = resourceName;
    }

    public void Dispose()
    {
      lock (_syncObj)
        if (_underlayingStream != null)
        {
          _underlayingStream.Dispose();
          _underlayingStream = null;
        }
    }

    public string NativeSystemId
    {
      get { return _nativeSystemId; }
    }

    public ResourcePath NativeResourcePath
    {
      get { return _nativeResourcePath; }
    }

    protected RemoteFileSystemResourceAccessor(string nativeSystemId, ResourcePath nativeResourcePath, bool isFile,
        string resourcePathName, string resourceName, long size, DateTime lastChanged) :
            this(nativeSystemId, nativeResourcePath, isFile, resourcePathName, resourceName)
    {
      _lastChangedCache = lastChanged;
      _sizeCache = size;
    }

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

    protected ICollection<IFileSystemResourceAccessor> WrapResourcePathsData(ICollection<ResourcePathMetadata> resourcesData, bool files)
    {
      return new List<IFileSystemResourceAccessor>(resourcesData.Select(fileData => new RemoteFileSystemResourceAccessor(
          _nativeSystemId, fileData.ResourcePath, files, fileData.HumanReadablePath,
          fileData.ResourceName)));
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

    #region IFileSystemResourceAccessor implementation

    public bool Exists
    {
      get
      {
        IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
        return rris.ResourceExists(_nativeSystemId, _nativeResourcePath);
      }
    }

    public long Size
    {
      get
      {
        if (_sizeCache.HasValue)
          return _sizeCache.Value;
        FillCaches();
        return _sizeCache.Value;
      }
    }

    public bool IsFile
    {
      get { return _isFile; }
    }

    public bool IsDirectory
    {
      get { return !_isFile; }
    }

    public IResourceProvider ParentProvider
    {
      get { return null; }
    }

    public string Path
    {
      get { return RemoteResourceProvider.BuildProviderPath(_nativeSystemId, _nativeResourcePath); }
    }

    public string ResourceName
    {
      get { return _resourceName; }
    }

    public string ResourcePathName
    {
      get { return _resourcePathName; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        return ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID,
            RemoteResourceProvider.BuildProviderPath(_nativeSystemId, _nativeResourcePath));
      }
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

    public DateTime LastChanged
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
      return WrapResourcePathsData(filesData, true);
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      ICollection<ResourcePathMetadata> directoriesData = rris.GetChildDirectories(_nativeSystemId, _nativeResourcePath);
      return WrapResourcePathsData(directoriesData, false);
    }

    public void PrepareStreamAccess()
    {
      if (!_isFile || _underlayingStream != null)
        return;
      IRemoteResourceInformationService rris = ServiceRegistration.Get<IRemoteResourceInformationService>();
      string resourceURL;
      IPAddress localIpAddress;
      if (!rris.GetFileHttpUrl(_nativeSystemId, _nativeResourcePath, out resourceURL, out localIpAddress))
        return;
      lock (_syncObj)
        _underlayingStream = new CachedMultiSegmentHttpStream(resourceURL, localIpAddress, Size);
    }

    public Stream OpenRead()
    {
      if (!_isFile)
        throw new IllegalCallException("Only files can provide stream access");
      PrepareStreamAccess();
      return new SynchronizedMasterStreamClient(_underlayingStream, _syncObj);
    }

    public async Task<Stream> OpenReadAsync()
    {
      if (!_isFile)
        throw new IllegalCallException("Only files can provide stream access");
      // ToDo: Implement PrepareStreamAccess in an async way and implement the async virtual methods of SynchronizedMasterStreamClient
      await Task.Run(() => PrepareStreamAccess());
      return new SynchronizedMasterStreamClient(_underlayingStream, _syncObj);
    }

    public Stream OpenWrite()
    {
      if (!_isFile)
        throw new IllegalCallException("Only files can provide stream access");
      return null;
    }

    public IResourceAccessor Clone()
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