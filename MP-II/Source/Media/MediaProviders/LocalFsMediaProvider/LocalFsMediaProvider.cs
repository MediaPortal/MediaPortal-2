#region Copyright (C) 2007-2008 Team MediaPortal

/*
    Copyright (C) 2007-2008 Team MediaPortal
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
using MediaPortal.Core;
using MediaPortal.Core.FileEventNotification;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.MediaProviders;
using MediaPortal.Utilities.FileSystem;

namespace MediaPortal.Media.MediaProviders.LocalFsMediaProvider
{
  /// <summary>
  /// Media provider implementation for the local filesystem.
  /// </summary>
  public class LocalFsMediaProvider : LocalFsMediaProviderBase, IFileSystemMediaProvider, IMediaSourceChangeNotifier
  {
    protected class ChangeTrackerRegistrationKey
    {
      #region Protected fields

      protected string _path;
      protected PathChangeDelegate _pathChangeDelegate;

      #endregion

      public ChangeTrackerRegistrationKey(string path, PathChangeDelegate pathChangeDelegate)
      {
        _path = path;
        _pathChangeDelegate = pathChangeDelegate;
      }

      public string Path
      {
        get { return _path; }
      }

      public PathChangeDelegate PathChangeDelegate
      {
        get { return _pathChangeDelegate; }
      }

      public override int GetHashCode()
      {
        return _path.GetHashCode() + _pathChangeDelegate.GetHashCode();
      }

      public override bool Equals(object obj)
      {
        if (!(obj is ChangeTrackerRegistrationKey))
          return false;
        ChangeTrackerRegistrationKey other = (ChangeTrackerRegistrationKey) obj;
        return other.Path == Path && other.PathChangeDelegate == PathChangeDelegate;
      }
    }

    #region Public constants

    /// <summary>
    /// GUID string for the local filesystem media provider.
    /// </summary>
    public const string PROVIDER_ID_STR = "{E88E64A8-0233-4fdf-BA27-0B44C6A39AE9}";

    /// <summary>
    /// Local filesystem media provider GUID.
    /// </summary>
    public static Guid PROVIDER_ID = new Guid(PROVIDER_ID_STR);

    #endregion

    #region Protected fields

    protected MediaProviderMetadata _metadata;
    protected IDictionary<ChangeTrackerRegistrationKey, FileWatchInfo> _changeTrackers =
        new Dictionary<ChangeTrackerRegistrationKey, FileWatchInfo>();

    #endregion

    #region Ctor

    public LocalFsMediaProvider()
    {
      _metadata = new MediaProviderMetadata(PROVIDER_ID, "[LocalFsMediaProvider.Name]");
    }

    #endregion

    #region Protected methods

    protected void FileEventHandler(FileWatchInfo sender, IFileWatchEventArgs args)
    {
      IEnumerable<ChangeTrackerRegistrationKey> ctrks = GetAllChangeTrackerRegistrationsByPath(sender.Path);
      MediaSourceChangeType changeType = TranslateChangeType(args.ChangeType);
      foreach (ChangeTrackerRegistrationKey key in ctrks)
        key.PathChangeDelegate(this, args.OldPath, args.Path, changeType);
    }

    protected ICollection<ChangeTrackerRegistrationKey> GetAllChangeTrackerRegistrationsByPath(string path)
    {
      ICollection<ChangeTrackerRegistrationKey> result = new List<ChangeTrackerRegistrationKey>();
      foreach (ChangeTrackerRegistrationKey key in _changeTrackers.Keys)
        if (key.Path == path)
          result.Add(key);
      return result;
    }

    protected static MediaSourceChangeType TranslateChangeType(FileWatchChangeType changeType)
    {
      switch (changeType)
      {
        case FileWatchChangeType.Created:
          return MediaSourceChangeType.Created;
        case FileWatchChangeType.Deleted:
          return MediaSourceChangeType.Deleted;
        case FileWatchChangeType.Changed:
          return MediaSourceChangeType.Changed;
        case FileWatchChangeType.Renamed:
          return MediaSourceChangeType.Renamed;
        case FileWatchChangeType.All:
          return MediaSourceChangeType.All;
        case FileWatchChangeType.DirectoryDeleted:
          return MediaSourceChangeType.DirectoryDeleted;
        default:
          return MediaSourceChangeType.None;
      }
    }

    protected static ICollection<FileWatchChangeType> TranslateChangeTypes(IEnumerable<MediaSourceChangeType> changeTypes)
    {
      ICollection<FileWatchChangeType> result = new List<FileWatchChangeType>();
      foreach (MediaSourceChangeType changeType in changeTypes)
      {
        switch (changeType)
        {
          case MediaSourceChangeType.Created:
            result.Add(FileWatchChangeType.Created);
            break;
          case MediaSourceChangeType.Deleted:
            result.Add(FileWatchChangeType.Deleted);
            break;
          case MediaSourceChangeType.Changed:
            result.Add(FileWatchChangeType.Changed);
            break;
          case MediaSourceChangeType.Renamed:
            result.Add(FileWatchChangeType.Renamed);
            break;
          case MediaSourceChangeType.All:
            result.Add(FileWatchChangeType.All);
            break;
          case MediaSourceChangeType.DirectoryDeleted:
            result.Add(FileWatchChangeType.DirectoryDeleted);
            break;
          default:
            throw new ArgumentException(typeof(MediaSourceChangeType).Name+" '"+changeType+"' is not supported");
        }
      }
      return result;
    }

    #endregion

    #region IFileSystemMediaProvider implementation

    public MediaProviderMetadata Metadata
    {
      get { return _metadata; }
    }

    public Stream OpenRead(string path)
    {
      string dosPath = ToDosPath(path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return File.OpenRead(dosPath);
    }

    public Stream OpenWrite(string path)
    {
      string dosPath = ToDosPath(path);
      if (string.IsNullOrEmpty(dosPath) || !File.Exists(dosPath))
        return null;
      return File.OpenWrite(dosPath);
    }

    public bool IsDirectory(string path)
    {
      string dosPath = ToDosPath(path);
      if (string.IsNullOrEmpty(dosPath))
        return false;
      return Directory.Exists(dosPath);
    }

    public ICollection<string> GetFiles(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      if (path == "/")
        // No files at root level - there are only logical drives
        return new List<string>();
      string dosPath = ToDosPath(path);
      if (!Directory.Exists(dosPath))
        return null;
      return ConcatPaths(path, Directory.GetFiles(dosPath), false);
    }

    public ICollection<string> GetChildDirectories(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      if (path == "/")
      {
        ICollection<string> result = new List<string>();
        foreach (string drive in Directory.GetLogicalDrives())
        {
          if (!new DriveInfo(drive).IsReady)
            continue;
          result.Add("/" + FileUtils.RemoveTrailingPathDelimiter(drive) + "/");
        }
        return result;
      }
      string dosPath = ToDosPath(path);
      if (!Directory.Exists(dosPath))
        return null;
      return ConcatPaths(path, Directory.GetDirectories(dosPath), true);
    }

    public bool IsResource(string path)
    {
      if (string.IsNullOrEmpty(path) || path == "/")
        return false;
      string dosPath = ToDosPath(path);
      return File.Exists(dosPath) || Directory.Exists(dosPath);
    }

    public string GetResourceName(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      if (path == "/")
        return "/";
      if (!path.StartsWith("/"))
        return null;
      path = path.Substring(1);
      if (path.EndsWith(":/"))
      {
        DriveInfo di = new DriveInfo(path);
        return di.IsReady ? string.Format("{0} [{1}]", di.VolumeLabel, path) : path;
      }
      if (path.EndsWith("/"))
        path = path.Substring(0, path.Length-1);
      return Path.GetFileName(path);
    }

    public string GetResourcePath(string path)
    {
      if (string.IsNullOrEmpty(path))
        return null;
      if (path == "/")
        return "/";
      return ToDosPath(path);
    }

    #endregion

    #region IMediaSourceChangeNotifier implementation

    public void RegisterChangeTracker(PathChangeDelegate changeDelegate,
        string path, IEnumerable<string> fileNameFilters, IEnumerable<MediaSourceChangeType> changeTypes)
    {
      ICollection<FileWatchChangeType> fwiChangeTypes = TranslateChangeTypes(changeTypes);
      FileWatchInfo fwi = new FileWatchInfo(path, true, FileEventHandler, fileNameFilters, fwiChangeTypes);
      ChangeTrackerRegistrationKey ctrk = new ChangeTrackerRegistrationKey(path, changeDelegate);
      _changeTrackers[ctrk] = ServiceScope.Get<IFileEventNotifier>().Subscribe(fwi);
    }

    public void UnregisterChangeTracker(PathChangeDelegate changeDelegate, string path)
    {
      ChangeTrackerRegistrationKey ctrk = new ChangeTrackerRegistrationKey(path, changeDelegate);
      FileWatchInfo fwi;
      if (!_changeTrackers.TryGetValue(ctrk, out fwi))
        return;
      _changeTrackers.Remove(ctrk);
      ServiceScope.Get<IFileEventNotifier>().Unsubscribe(fwi);
    }

    public void UnregisterAll(PathChangeDelegate changeDelegate)
    {
      IEnumerable<ChangeTrackerRegistrationKey> oldKeys = new List<ChangeTrackerRegistrationKey>(
          _changeTrackers.Keys);
      foreach (ChangeTrackerRegistrationKey key in oldKeys)
        if (key.PathChangeDelegate.Equals(changeDelegate))
          _changeTrackers.Remove(key);
    }

    #endregion
  }
}
