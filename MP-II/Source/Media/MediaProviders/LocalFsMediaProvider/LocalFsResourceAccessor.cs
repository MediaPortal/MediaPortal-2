#region Copyright (C) 2007-2009 Team MediaPortal

/*
    Copyright (C) 2007-2009 Team MediaPortal
    http://www.team-mediaportal.com
 
    This file is part of MediaPortal II

    MediaPortal II is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal II is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal II.  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.Services.MediaManagement;
using MediaPortal.Utilities;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Media.MediaProviders.LocalFsMediaProvider
{
  public class LocalFsResourceAccessor : ResourceAccessorBase, ILocalFsResourceAccessor, IResourceChangeNotifier
  {
    protected LocalFsMediaProvider _provider;
    protected string _path;

    public LocalFsResourceAccessor(LocalFsMediaProvider provider, string path)
    {
      _provider = provider;
      _path = path;
    }

    protected ICollection<IFileSystemResourceAccessor> ConcatPaths(string rootPath,
        IEnumerable<string> namesWithPathPrefix, bool isDirectory)
    {
      rootPath = StringUtils.CheckSuffix(rootPath, "/");
      ICollection<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
      foreach (string file in namesWithPathPrefix)
        result.Add(new LocalFsResourceAccessor(_provider, rootPath + Path.GetFileName(file) + (isDirectory ? "/" : string.Empty)));
      return result;
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
        if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
          return DateTime.MinValue;
        return File.GetLastWriteTime(dosPath);
      }
    }

    public bool Exists(string path)
    {
      return _provider.IsResource(path);
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
      return !Directory.Exists(dosPath) ? null : ConcatPaths(_path, Directory.GetFiles(dosPath), false);
    }

    public ICollection<IFileSystemResourceAccessor> GetChildDirectories()
    {
      if (string.IsNullOrEmpty(_path))
        return null;
      if (_path == "/")
      {
        ICollection<IFileSystemResourceAccessor> result = new List<IFileSystemResourceAccessor>();
        foreach (string drive in Directory.GetLogicalDrives())
        {
          if (!new DriveInfo(drive).IsReady)
            continue;
          result.Add(new LocalFsResourceAccessor(_provider, "/" + FileUtils.RemoveTrailingPathDelimiter(drive) + "/"));
        }
        return result;
      }
      string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
      return !Directory.Exists(dosPath) ? null : ConcatPaths(_path, Directory.GetDirectories(dosPath), true);
    }

    public bool IsFile
    {
      get
      {
        if (string.IsNullOrEmpty(_path) || _path == "/")
          return false;
        string dosPath = LocalFsMediaProviderBase.ToDosPath(_path);
        return File.Exists(dosPath) || Directory.Exists(dosPath);
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
