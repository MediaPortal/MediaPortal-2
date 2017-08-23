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
using MediaPortal.Common.Logging;
using MediaPortal.Common.ResourceAccess;
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;
using MediaPortal.Utilities.Network;

namespace MediaPortal.Common.Services.ResourceAccess.LocalFsResourceProvider
{
  public class LocalFsResourceAccessor : ILocalFsResourceAccessor, IResourceChangeNotifier, IResourceDeletor
  {
    protected const int ASYNC_STREAM_BUFFER_SIZE = 4096;
    protected LocalFsResourceProvider _provider;
    protected string _path;

    public LocalFsResourceAccessor(LocalFsResourceProvider provider, string path)
    {
      _provider = provider;
      _path = path;
    }

    public void Dispose() { }

    protected ICollection<IFileSystemResourceAccessor> CreateChildResourceAccessors(IEnumerable<string> namesWithPathPrefix, bool isDirectory)
    {
      string rootPath = StringUtils.CheckSuffix(_path, "/");
      return namesWithPathPrefix.Select(filePath => new LocalFsResourceAccessor(
          _provider, rootPath + System.IO.Path.GetFileName(filePath) + (isDirectory ? "/" : string.Empty))).Cast<IFileSystemResourceAccessor>().ToList();
    }

    #region ILocalFsResourceAccessor implementation

    public IResourceProvider ParentProvider
    {
      get { return _provider; }
    }

    public string LocalFileSystemPath
    {
      get { return StringUtils.TrimToNull(LocalFsResourceProviderBase.ToDosPath(_path)); }
    }

    public IDisposable EnsureLocalFileSystemAccess()
    {
      // Nothing to do here; access is ensured as of the instantiation of this class
      return null;
    }

    public ResourcePath CanonicalLocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(LocalFsResourceProviderBase.LOCAL_FS_RESOURCE_PROVIDER_ID, _path); }
    }

    public DateTime LastChanged
    {
      get
      {
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        return LocalFsResourceProviderBase.GetSafeLastWriteTime(dosPath);
      }
    }

    public long Size
    {
      get
      {
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
          return -1;
        return new FileInfo(dosPath).Length;
      }
    }

    public bool ResourceExists(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      path = ProviderPathHelper.Combine(_path, path);
      return _provider.IsResource(path);
    }

    public IFileSystemResourceAccessor GetResource(string path)
    {
      IResourceAccessor result;
      if (_provider.TryCreateResourceAccessor(ProviderPathHelper.Combine(_path, path), out result))
        return (IFileSystemResourceAccessor) result;
      return null;
    }

    public void PrepareStreamAccess()
    {
      // Nothing to do
    }

    public Stream OpenRead()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return new FileStream(dosPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }

    public Task<Stream> OpenReadAsync()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      // In this implementation there is no preparational work to do. We therefore return a
      // completed Task; there is no need for any async operation.
      return Task.FromResult((Stream)new FileStream(dosPath, FileMode.Open, FileAccess.Read, FileShare.Read, ASYNC_STREAM_BUFFER_SIZE, true));
    }

    public Stream OpenWrite()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return File.OpenWrite(dosPath);
    }

    public IResourceAccessor Clone()
    {
      return new LocalFsResourceAccessor(_provider, _path);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      if (string.IsNullOrEmpty(_path))
        return null;
      if (_path == "/")
        // No files at root level - there are only logical drives
        return new List<IFileSystemResourceAccessor>();
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      return (!string.IsNullOrEmpty(dosPath) && Directory.Exists(dosPath)) ? CreateChildResourceAccessors(Directory.GetFiles(dosPath), false) : null;
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (string.IsNullOrEmpty(_path))
        return null;
      if (_path == "/")
        return Directory.GetLogicalDrives().Where(drive => new DriveInfo(drive).IsReady).Select<string, IFileSystemResourceAccessor>(
            drive => new LocalFsResourceAccessor(_provider, "/" + FileUtils.RemoveTrailingPathDelimiter(drive) + "/")).ToList();
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      return (!string.IsNullOrEmpty(dosPath) && Directory.Exists(dosPath)) ? CreateChildResourceAccessors(Directory.GetDirectories(dosPath), true) : null;
    }

    public bool IsFile
    {
      get
      {
        if (string.IsNullOrEmpty(_path) || _path == "/")
          return false;
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        return !string.IsNullOrEmpty(dosPath) && File.Exists(dosPath);
      }
    }

    public bool Exists
    {
      get
      {
        if (string.IsNullOrEmpty(_path) || _path == "/")
          return false;
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        return !string.IsNullOrEmpty(dosPath) && (File.Exists(dosPath) || Directory.Exists(dosPath));
      }
    }

    public bool IsDirectory
    {
      get
      {
        if (_path == "/")
          return true;
        string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
        return !string.IsNullOrEmpty(dosPath) && Directory.Exists(dosPath);
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
        if (string.IsNullOrEmpty(_path))
          return null;
        if (_path == "/")
          return "/";
        if (!_path.StartsWith("/"))
          return null;
        string path = _path.Substring(1);
        if (path.EndsWith(":/"))
        {
          DriveInfo di = new DriveInfo(path);
          if (!di.IsReady)
            return path;

          string driveName;
          if (di.DriveType != DriveType.Network || !SharesEnumerator.GetFormattedUNCPath(path, out driveName))
            driveName = di.VolumeLabel;
          return string.Format("[{0}] {1}", path, driveName);
        }
        path = LocalFsResourceProviderBase.ToDosPath(StringUtils.RemoveSuffixIfPresent(path, "/"));
        return string.IsNullOrEmpty(path) ? null : System.IO.Path.GetFileName(path);
      }
    }

    public string ResourcePathName
    {
      get
      {
        if (string.IsNullOrEmpty(_path))
          return null;
        return _path == "/" ? "/" : LocalFsResourceProviderBase.ToDosPath(_path);
      }
    }

    #endregion

    #region IResourceChangeNotifier implementation

    public void RegisterChangeTracker(PathChangeDelegate changeDelegate, IEnumerable<string> fileNameFilters,
        IEnumerable<MediaSourceChangeType> changeTypes)
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      if(!string.IsNullOrEmpty(dosPath) && (File.Exists(dosPath) || Directory.Exists(dosPath)))
        _provider.RegisterChangeTracker(changeDelegate, dosPath, fileNameFilters, changeTypes);
    }

    public void UnregisterChangeTracker(PathChangeDelegate changeDelegate)
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
      if (!string.IsNullOrEmpty(dosPath) && (File.Exists(dosPath) || Directory.Exists(dosPath)))
        _provider.UnregisterChangeTracker(changeDelegate, dosPath);
    }

    public void UnregisterAll(PathChangeDelegate changeDelegate)
    {
      _provider.UnregisterAll(changeDelegate);
    }

    #endregion

    #region IResourceDeletor members

    public bool Delete()
    {
      string dosPath = LocalFsResourceProviderBase.ToDosPath(_path);
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

    #endregion

    #region Base overrides

    public override string ToString()
    {
      return ResourcePathName;
    }

    #endregion
  }
}
