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
using System.Threading.Tasks;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;

namespace MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge
{
  /// <summary>
  /// Access bridge logic which maps a complex resource accessor to a local file resource.
  /// </summary>
  public class StreamedResourceToLocalFsAccessBridge : ILocalFsResourceAccessor
  {
    #region Protected consts and fields

    protected const int ASYNC_STREAM_BUFFER_SIZE = 4096;

    protected static object _syncObj = new object();
    internal static IDictionary<string, MountingDataProxy> _activeMounts = new Dictionary<string, MountingDataProxy>();

    internal MountingDataProxy _mountingDataProxy;
    protected string _path;
    protected string _localFsPath;

    #endregion

    #region Ctor & maintenance

    /// <summary>
    /// Creates a new instance of this class which is based on the given <paramref name="mountingDataProxy"/>.
    /// </summary>
    /// <param name="mountingDataProxy">Mount this bridge is based on.</param>
    /// <param name="path">Path relative to the mount point.</param>
    internal StreamedResourceToLocalFsAccessBridge(MountingDataProxy mountingDataProxy, string path)
    {
      _mountingDataProxy = mountingDataProxy;
      _mountingDataProxy.IncUsage();
      _path = path;
      _localFsPath = System.IO.Path.Combine(_mountingDataProxy.LocalFileSystemPath, ToDosPath(StringUtils.RemovePrefixIfPresent(_path, "/")));
    }

    public void Dispose()
    {
      if (_mountingDataProxy == null)
        return;
      _mountingDataProxy.DecUsage();
      _mountingDataProxy = null;
    }

    #endregion

    #region Private, protected and internal members

    protected string ToDosPath(string providerPath)
    {
      return providerPath.Replace('/', '\\');
    }

    protected ICollection<IFileSystemResourceAccessor> CreateChildResourceAccessors(IEnumerable<string> namesWithPathPrefix, bool isDirectory)
    {
      string rootPath = StringUtils.CheckSuffix(_path, "/");
      return namesWithPathPrefix.Select(filePath => new StreamedResourceToLocalFsAccessBridge(_mountingDataProxy,
          rootPath + ProviderPathHelper.GetFileName(filePath) + (isDirectory ? "/" : string.Empty))).Cast<IFileSystemResourceAccessor>().ToList();
    }

    static void OnMountingDataOrphaned(MountingDataProxy proxy)
    {
      lock (_syncObj)
      {
        if (proxy.UsageCount > 0)
          // Double check if the proxy was reused when the lock was not set
          return;
        _activeMounts.Remove(proxy.Key);
        proxy.Dispose();
      }
    }

    static internal MountingDataProxy CreateMountingDataProxy(string key, IFileSystemResourceAccessor baseResourceAccessor)
    {
      MountingDataProxy result = new MountingDataProxy(key, baseResourceAccessor);
      result.MountingDataOrphaned += OnMountingDataOrphaned;
      return result;
    }

    #endregion

    /// <summary>
    /// Returns a resource accessor instance of interface <see cref="ILocalFsResourceAccessor"/>. This instance will return the
    /// given <paramref name="baseResourceAccessor"/>, casted to <see cref="ILocalFsResourceAccessor"/> if possible, or
    /// a new instance of <see cref="StreamedResourceToLocalFsAccessBridge"/> to provide the <see cref="ILocalFsResourceAccessor"/>
    /// instance.
    /// </summary>
    /// <remarks>
    /// The ownership of the given <paramref name="baseResourceAccessor"/> is transferred from the caller to the returned
    /// result value. That means, if this method succeeds, the caller must dispose the result value, it must not dispose
    /// the given <paramref name="baseResourceAccessor"/> any more.
    /// </remarks>
    /// <param name="baseResourceAccessor">Resource accessor which is used to provide the resource contents.</param>
    /// <param name="path">Relative path based on the given baseResourceAccessor.</param>
    /// <returns>Resource accessor which implements <see cref="ILocalFsResourceAccessor"/>.</returns>
    public static ILocalFsResourceAccessor GetLocalFsResourceAccessor(IFileSystemResourceAccessor baseResourceAccessor, string path)
    {
      // Try to get an ILocalFsResourceAccessor
      ILocalFsResourceAccessor result = baseResourceAccessor as ILocalFsResourceAccessor;
      if (result != null)
          // Simple case: The media item is located in the local file system or the resource provider returns
          // an ILocalFsResourceAccessor from elsewhere - simply return it
        return result;

      // Set up a resource bridge mapping the remote or complex resource to a local file or directory
      string key = baseResourceAccessor.CanonicalLocalResourcePath.Serialize();
      MountingDataProxy md;
      bool dispose = false;
      lock (_syncObj)
      {
        if (_activeMounts.TryGetValue(key, out md))
            // Base accessor not needed - we use our cached accessor
          dispose = true;
        else
          _activeMounts.Add(key, md = CreateMountingDataProxy(key, baseResourceAccessor));
      }
      if (dispose)
        baseResourceAccessor.Dispose();
      return new StreamedResourceToLocalFsAccessBridge(md, path);
    }

    /// <summary>
    /// Convenience method for <see cref="GetLocalFsResourceAccessor(IFileSystemResourceAccessor,string)"/> for the root path (<c>"/"</c>).
    /// </summary>
    /// <param name="baseResourceAccessor">Resource accessor which is used to provide the resource contents.</param>
    /// <returns>Resource accessor which implements <see cref="ILocalFsResourceAccessor"/>.</returns>
    public static ILocalFsResourceAccessor GetLocalFsResourceAccessor(IFileSystemResourceAccessor baseResourceAccessor)
    {
      return GetLocalFsResourceAccessor(baseResourceAccessor, "/");
    }

    #region ILocalFsResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return null; }
    }

    public string LocalFileSystemPath
    {
      get { return _localFsPath; }
    }

    public IDisposable EnsureLocalFileSystemAccess()
    {
      // Nothing to do here; access is ensured as of the instantiation of this class
      return null;
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get
      {
        return ResourcePath.Deserialize(StringUtils.CheckSuffix(_mountingDataProxy.ResourceAccessor.CanonicalLocalResourcePath.Serialize(), "/") + StringUtils.RemovePrefixIfPresent(_path, "/"));
      }
    }

    public DateTime LastChanged
    {
      get
      {
        return LocalFsResourceProviderBase.GetSafeLastWriteTime(_localFsPath);
      }
    }

    public long Size
    {
      get
      {
        if (string.IsNullOrEmpty(_localFsPath) || !File.Exists(_localFsPath))
          return -1;
        return new FileInfo(_localFsPath).Length;
      }
    }

    public bool ResourceExists(string path)
    {
      string dosPath = System.IO.Path.Combine(_localFsPath, ToDosPath(path));
      return File.Exists(dosPath) || Directory.Exists(dosPath);
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      IFileSystemResourceAccessor ra = (IFileSystemResourceAccessor) _mountingDataProxy.ResourceAccessor.Clone();
      try
      {
        return GetLocalFsResourceAccessor(ra, ProviderPathHelper.Combine(_path, path));
      }
      catch
      {
        ra.Dispose();
        throw;
      }
    }

    public void PrepareStreamAccess()
    {
      // No way to prepare stream access in the underlaying resource accessor because we only have the root accessor present
    }

    public Stream OpenRead()
    {
      if (string.IsNullOrEmpty(_localFsPath) || !File.Exists(_localFsPath))
        return null;
      return File.OpenRead(_localFsPath);
    }

    public Task<Stream> OpenReadAsync()
    {
      if (string.IsNullOrEmpty(_localFsPath) || !File.Exists(_localFsPath))
        return null;
      // In this implementation there is no preparational work to do. We therefore return a
      // completed Task; there is no need for any async operation.
      return Task.FromResult((Stream)new FileStream(_localFsPath, FileMode.Open, FileAccess.Read, FileShare.Read, ASYNC_STREAM_BUFFER_SIZE, true));
    }

    public Stream OpenWrite()
    {
      if (string.IsNullOrEmpty(_localFsPath) || !File.Exists(_localFsPath))
        return null;
      return File.OpenWrite(_localFsPath);
    }

    public IResourceAccessor Clone()
    {
      return new StreamedResourceToLocalFsAccessBridge(_mountingDataProxy, _path);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      return Directory.Exists(_localFsPath) ? CreateChildResourceAccessors(Directory.GetFiles(_localFsPath), false) : null;
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      return Directory.Exists(_localFsPath) ? CreateChildResourceAccessors(Directory.GetDirectories(_localFsPath), true) : null;
    }

    public bool IsFile
    {
      get { return File.Exists(_localFsPath); }
    }

    public bool Exists
    {
      get { return ResourceExists(string.Empty); }
    }

    public bool IsDirectory
    {
      get { return Directory.Exists(_localFsPath); }
    }

    public string Path
    {
      get { return _path; }
    }

    public string ResourceName
    {
      get { return System.IO.Path.GetFileName(_localFsPath); }
    }

    public string ResourcePathName
    {
      get { return _localFsPath; }
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return "Streamed resource to local FS access bridge resource accessor; MountingData = '"  + _mountingDataProxy + "'";
    }

    #endregion
  }
}
