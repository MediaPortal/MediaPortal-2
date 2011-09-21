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
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Extensions.MediaProviders.LocalFsMediaProvider
{
  public class LocalFsResourceAccessor : ILocalFsResourceAccessor, IResourceChangeNotifier
  {
    protected LocalFsMediaProvider _provider;
    protected string _path;

    public LocalFsResourceAccessor(LocalFsMediaProvider provider, string path)
    {
      _provider = provider;
      _path = path;
    }

    public void Dispose() { }

    protected ICollection<IFileSystemResourceAccessor> ConcatPaths(string rootPath,
        IEnumerable<string> namesWithPathPrefix, bool isDirectory)
    {
      rootPath = StringUtils.CheckSuffix(rootPath, "/");
      return namesWithPathPrefix.Select(file => new LocalFsResourceAccessor(_provider, rootPath + Path.GetFileName(file) + (isDirectory ? "/" : string.Empty))).Cast<IFileSystemResourceAccessor>().ToList();
    }

    protected string ExpandPath(string path)
    {
      return path.StartsWith("/") ? path : StringUtils.CheckSuffix(_path, "/") + StringUtils.RemovePrefixIfPresent(path, "/");
    }

    #region ILocalFsResourceAccessor implementation

    public IMediaProvider ParentProvider
    {
      get { return _provider; }
    }

    public string LocalFileSystemPath
    {
      get { return LocalFsMediaProviderBase.ToDosPath(_path); }
    }

    public ResourcePath LocalResourcePath
    {
      get { return ResourcePath.BuildBaseProviderPath(LocalFsMediaProviderBase.LOCAL_FS_MEDIA_PROVIDER_ID, _path); }
    }

    public DateTime LastChanged
    {
      get
      {
        string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
        if (string.IsNullOrEmpty(dosPath))
          return DateTime.MinValue;
        if (!File.Exists(dosPath) && !Directory.Exists(dosPath))
          return DateTime.MinValue;
        return File.GetLastWriteTime(dosPath);
      }
    }

    public long Size
    {
      get
      {
        string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
        if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
          return -1;
        return new FileInfo(dosPath).Length;
      }
    }

    public bool ResourceExists(string path)
    {
      if (string.IsNullOrEmpty(path))
        return false;
      path = ExpandPath(path);
      return _provider.IsResource(path);
    }

    public IResourceAccessor GetResource(string path)
    {
      return _provider.CreateResourceAccessor(ExpandPath(path));
    }

    public void PrepareStreamAccess()
    {
      // Nothing to do
    }

    public Stream OpenRead()
    {
      string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return File.OpenRead(dosPath);
    }

    public Stream OpenWrite()
    {
      string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return File.OpenWrite(dosPath);
    }

    public ICollection<IFileSystemResourceAccessor> GetFiles()
    {
      if (string.IsNullOrEmpty(_path))
        return null;
      if (_path == "/")
        // No files at root level - there are only logical drives
        return new List<IFileSystemResourceAccessor>();
      string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
      return (!string.IsNullOrEmpty(dosPath) && Directory.Exists(dosPath)) ? ConcatPaths(_path, Directory.GetFiles(dosPath), false) : null;
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (string.IsNullOrEmpty(_path))
        return null;
      if (_path == "/")
        return Directory.GetLogicalDrives().Where(drive => new DriveInfo(drive).IsReady).Select<string, IFileSystemResourceAccessor>(
            drive => new LocalFsResourceAccessor(_provider, "/" + FileUtils.RemoveTrailingPathDelimiter(drive) + "/")).ToList();
      string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
      return (!string.IsNullOrEmpty(dosPath) && Directory.Exists(dosPath)) ? ConcatPaths(_path, Directory.GetDirectories(dosPath), true) : null;
    }

    public bool IsFile
    {
      get
      {
        if (string.IsNullOrEmpty(_path) || _path == "/")
          return false;
        string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
        return !string.IsNullOrEmpty(dosPath) && File.Exists(dosPath);
      }
    }

    public bool Exists
    {
      get {
        if (string.IsNullOrEmpty(_path) || _path == "/")
          return false;
        string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
        return !string.IsNullOrEmpty(dosPath) && (File.Exists(dosPath) || Directory.Exists(dosPath));
      }
    }

    public bool IsDirectory
    {
      get
      {
        if (_path == "/")
          return true;
        string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
        return !string.IsNullOrEmpty(dosPath) && Directory.Exists(dosPath);
      }
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
          return di.IsReady ? string.Format("[{0}] {1}", path, di.VolumeLabel) : path;
        }
        path = StringUtils.RemoveSuffixIfPresent(path, "/");
        return Path.GetFileName(path);
      }
    }

    public string ResourcePathName
    {
      get
      {
        if (string.IsNullOrEmpty(_path))
          return null;
        return _path == "/" ? "/" : LocalFsMediaProviderBase.ToDosPath(_path);
      }
    }

    #endregion

    #region IResourceChangeNotifier implementation

    public void RegisterChangeTracker(PathChangeDelegate changeDelegate, IEnumerable<string> fileNameFilters,
        IEnumerable<MediaSourceChangeType> changeTypes)
    {
      _provider.RegisterChangeTracker(changeDelegate, _path, fileNameFilters, changeTypes);
    }

    public void UnregisterChangeTracker(PathChangeDelegate changeDelegate)
    {
      _provider.UnregisterAll(changeDelegate);
    }

    public void UnregisterAll(PathChangeDelegate changeDelegate)
    {
      _provider.UnregisterAll(changeDelegate);
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
