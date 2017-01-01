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
using MediaPortal.Common;
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider;
using MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider.NeighborhoodBrowser;
using MediaPortal.Utilities;
using MediaPortal.Utilities.Exceptions;
using MediaPortal.Utilities.Network;

namespace MediaPortal.Extensions.ResourceProviders.NetworkNeighborhoodResourceProvider
{
  public class NetworkNeighborhoodResourceAccessor : ILocalFsResourceAccessor, IResourceChangeNotifier, IResourceDeletor
  {
    #region Protected fields

    protected NetworkNeighborhoodResourceProvider _parent;
    protected string _path;
    protected event PathChangeDelegate _changeDelegateProxy;
    protected ILocalFsResourceAccessor _underlayingResource = null; // Only set if the path points to a file system resource - not a server or root

    #endregion

    #region Ctor

    public NetworkNeighborhoodResourceAccessor(NetworkNeighborhoodResourceProvider parent, string path)
    {
      _parent = parent;
      _path = path;
      if (IsRootPath(path ) || IsServerPath(path))
        return;

      IResourceAccessor ra;
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
        if (LocalFsResourceProvider.Instance.TryCreateResourceAccessor("/" + path, out ra))
          _underlayingResource = (ILocalFsResourceAccessor)ra;
    }

    #endregion

    #region Protected methods

    protected ICollection<IFileSystemResourceAccessor> WrapLocalFsResourceAccessors(ICollection<IFileSystemResourceAccessor> localFsResourceAccessors)
    {
      ICollection<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
      if(localFsResourceAccessors != null && localFsResourceAccessors.Count > 0)
        CollectionUtils.AddAll(result, localFsResourceAccessors.Where(resourceAccessor => resourceAccessor != null && resourceAccessor.Path != null).
          Select(resourceAccessor => new NetworkNeighborhoodResourceAccessor(_parent, resourceAccessor.Path.Substring(1))));
      return result;
    }

    protected internal static bool IsRootPath(string providerPath)
    {
      return (providerPath == NetworkNeighborhoodResourceProvider.ROOT_PROVIDER_PATH);
    }

    protected internal static bool IsServerPath(string providerPath)
    {
      if (!providerPath.StartsWith("//"))
        return false;
      providerPath = StringUtils.RemoveSuffixIfPresent(providerPath.Substring(2), "/"); // Cut leading // and trailing /
      return !providerPath.Contains("/");
    }

    protected internal static bool IsSharePath(string providerPath)
    {
      if (!providerPath.StartsWith("//"))
        return false;
      providerPath = StringUtils.RemoveSuffixIfPresent(providerPath.Substring(2), "/"); // Cut leading // and trailing /
      return providerPath.IndexOf('/') == providerPath.LastIndexOf('/'); // Exactly one /
    }

    protected internal static string GetServerName(string providerPath)
    {
      return !IsServerPath(providerPath) ? null : providerPath.Substring(2);
    }

    protected internal static bool IsResource(string path)
    {
      if (IsRootPath(path))
        return true;
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(ResourcePath.BuildBaseProviderPath(NetworkNeighborhoodResourceProvider.NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID, path)))
        return IsServerPath(path) || LocalFsResourceProvider.Instance.IsResource("/" + path);
    }

    #endregion

    #region ILocalFsResourceAccessor implementation

    public void Dispose()
    {
      if (_underlayingResource != null)
        _underlayingResource.Dispose();
    }

    public IResourceProvider ParentProvider
    {
      get { return _parent; }
    }

    public bool Exists
    {
      get
      {
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
          return _underlayingResource == null ? IsServerPath(_path) : _underlayingResource.Exists;
      }
    }

    public bool IsDirectory
    {
      get
      {
        string dosPath = NetworkPath;
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
          return !string.IsNullOrEmpty(dosPath) && Directory.Exists(dosPath);
      }
    }

    public bool IsFile
    {
      get
      {
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
          return _underlayingResource != null && _underlayingResource.IsFile;
      }
    }

    public string Path
    {
      get { return _path; }
    }

    public string ResourceName
    {
      get
      {
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
          return GetServerName(_path) ?? (_underlayingResource == null ? string.Empty : _underlayingResource.ResourceName);
      }
    }

    public string ResourcePathName
    {
      get { return LocalFileSystemPath; }
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(NetworkNeighborhoodResourceProvider.NETWORK_NEIGHBORHOOD_RESOURCE_PROVIDER_ID, _path); }
    }

    public DateTime LastChanged
    {
      get
      {
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
          return _underlayingResource == null ? new DateTime() : _underlayingResource.LastChanged;
      }
    }

    public long Size
    {
      get
      {
        using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
          return _underlayingResource == null ? -1 : _underlayingResource.Size;
      }
    }

    public void PrepareStreamAccess()
    {
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
        if (_underlayingResource != null)
          _underlayingResource.PrepareStreamAccess();
    }

    public Stream OpenRead()
    {
      if (_underlayingResource == null)
        return null;
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
        return _underlayingResource.OpenRead();
    }

    public async Task<Stream> OpenReadAsync()
    {
      if (_underlayingResource == null)
        return null;
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
        return await _underlayingResource.OpenReadAsync();
    }

    public Stream OpenWrite()
    {
      if (_underlayingResource == null)
        return null;
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
        return _underlayingResource.OpenWrite();
    }

    public IResourceAccessor Clone()
    {
      return new NetworkNeighborhoodResourceAccessor(_parent, _path);
    }

    public bool ResourceExists(string path)
    {
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
        return IsServerPath(path) || (_underlayingResource != null && _underlayingResource.ResourceExists(path));
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      IResourceAccessor ra;
      if (_parent.TryCreateResourceAccessor(ProviderPathHelper.Combine(_path, path), out ra))
        return (IFileSystemResourceAccessor)ra;
      return null;
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
      {
        if (_path == "/" || IsServerPath(_path))
          return new List<IFileSystemResourceAccessor>();
        return _underlayingResource == null ? null : WrapLocalFsResourceAccessors(_underlayingResource.GetFiles());
      }
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (IsRootPath(_path))
        return _parent.BrowserService.Hosts
          .Select(host => host.GetUncString()).Where(uncPathString => uncPathString != null)
          .Select(uncPathString => new NetworkNeighborhoodResourceAccessor(_parent, uncPathString.Replace('\\', '/')))
          .Cast<IFileSystemResourceAccessor>().ToList();
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
      {
        if (IsServerPath(_path))
          return SharesEnumerator.EnumerateShares(StringUtils.RemovePrefixIfPresent(_path, "//"))
            // Allow all filesystems, but exclude "Special" shares (IPC$, Admin$) and all other "hidden" shares (ending with "$" such as print$)
            .Where(share => share.IsFileSystem && !share.ShareType.HasFlag(ShareType.Special) && !share.UNCPath.EndsWith("$"))
            .Select(
              share =>
              {
                try { return new NetworkNeighborhoodResourceAccessor(_parent, share.UNCPath.Replace('\\', '/')); }
                catch (IllegalCallException) { return null; }
              }
            ).Where(share => share != null && share.Exists).Cast<IFileSystemResourceAccessor>().ToList(); // "share.Exists" considers the user's access rights.
        var childDirectories = _underlayingResource == null ? null : _underlayingResource.GetChildDirectories();
        return childDirectories == null ? null : WrapLocalFsResourceAccessors(childDirectories);
      }
    }

    public string LocalFileSystemPath
    {
      get { return _path.Replace('/', '\\'); }
    }

    public IDisposable EnsureLocalFileSystemAccess()
    {
      // Impersonation required
      return ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath);
    }

    /// <summary>
    /// Returns a UNC representation of the resource.
    /// </summary>
    public string NetworkPath
    {
      // Note: the ToDosPath method returns only one leading backslash
      get { return @"\" + LocalFsResourceProviderBase.ToDosPath(_path); }
    }

    #endregion

    #region IResourceChangeNotifier implementation

    protected IResourceAccessor WrapLocalFsResourceAccessor(IResourceAccessor localFsResourceAccessor)
    {
      return new NetworkNeighborhoodResourceAccessor(_parent, localFsResourceAccessor.Path.Substring(1));
    }

    protected void PathChangedProxy(IResourceAccessor resourceAccessor, IResourceAccessor oldResourceAccessor, MediaSourceChangeType changeType)
    {
      if (_changeDelegateProxy != null)
        _changeDelegateProxy(WrapLocalFsResourceAccessor(resourceAccessor), WrapLocalFsResourceAccessor(oldResourceAccessor), changeType);
    }

    public void RegisterChangeTracker(PathChangeDelegate changeDelegate, IEnumerable<string> fileNameFilters,
        IEnumerable<MediaSourceChangeType> changeTypes)
    {
      _changeDelegateProxy = changeDelegate;
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
      {
        if (_underlayingResource != null)
        {
          LocalFsResourceProvider lfsProvider = _underlayingResource.ParentProvider as LocalFsResourceProvider;
          string path = NetworkPath;
          if (!path.EndsWith(@"\")) path += @"\";
          lfsProvider.RegisterChangeTracker(PathChangedProxy, path, fileNameFilters, changeTypes);
        }
      }
    }

    public void UnregisterChangeTracker(PathChangeDelegate changeDelegate)
    {
      if (_underlayingResource != null)
      {
        _changeDelegateProxy = null;
        LocalFsResourceProvider lfsProvider = _underlayingResource.ParentProvider as LocalFsResourceProvider;
        lfsProvider.UnregisterChangeTracker(PathChangedProxy, LocalFileSystemPath);
      }
    }

    public void UnregisterAll(PathChangeDelegate changeDelegate)
    {
      if (_underlayingResource != null)
      {
        _changeDelegateProxy = null;
        LocalFsResourceProvider lfsProvider = _underlayingResource.ParentProvider as LocalFsResourceProvider;
        lfsProvider.UnregisterAll(PathChangedProxy);
      }
    }

    #endregion

    #region IResourceDeletor implementation

    public bool Delete()
    {
      using (ServiceRegistration.Get<IImpersonationService>().CheckImpersonationFor(CanonicalLocalResourcePath))
      {
        string dosPath = NetworkPath;
        try
        {
          if (IsDirectory)
          {
            Directory.Delete(dosPath);
            return true;
          }
          if (IsFile)
          {
            File.Delete(dosPath);
            return true;
          }
        }
        catch (Exception ex)
        {
          // There can be a wide range of exceptions because of read-only filesystems, access denied, file in use etc...
          ServiceRegistration.Get<ILogger>().Error("Error deleting resource '{0}'", ex, dosPath);
        }
        return false; // Non existing or exception
      }
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return LocalFileSystemPath;
    }

    #endregion
  }
}
