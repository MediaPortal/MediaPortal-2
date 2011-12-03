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
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;

namespace MediaPortal.Common.Services.ResourceAccess.StreamedResourceToLocalFsAccessBridge
{
  /// <summary>
  /// Access bridge logic which maps a complex resource accessor to a local file resource.
  /// </summary>
  public class StreamedResourceToLocalFsAccessBridge : ILocalFsResourceAccessor
  {
    #region Protected fields

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
      _localFsPath = Path.Combine(_mountingDataProxy.LocalFileSystemPath, ToDosPath(StringUtils.RemovePrefixIfPresent(_path, "/")));
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

    static internal MountingDataProxy CreateMountingDataProxy(string key, IResourceAccessor baseResourceAccessor)
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
    /// <param name="baseResourceAccessor">Resource accessor which is used to provide the resource contents.</param>
    /// <param name="path">Relative path based on the given baseResourceAccessor.</param>
    /// <returns>Resource accessor which implements <see cref="ILocalFsResourceAccessor"/>.</returns>
    public static ILocalFsResourceAccessor GetLocalFsResourceAccessor(IResourceAccessor baseResourceAccessor, string path)
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

    public static ILocalFsResourceAccessor GetLocalFsResourceAccessor(IResourceAccessor baseResourceAccessor)
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
        if (!File.Exists(_localFsPath) && !Directory.Exists(_localFsPath))
          return DateTime.MinValue;
        return File.GetLastWriteTime(_localFsPath);
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
      string dosPath = Path.Combine(_localFsPath, ToDosPath(path));
      return File.Exists(dosPath) || Directory.Exists(dosPath);
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      IResourceAccessor ra = _mountingDataProxy.ResourceAccessor.Clone();
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

    public string ResourceName
    {
      get { return Path.GetFileName(_localFsPath); }
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