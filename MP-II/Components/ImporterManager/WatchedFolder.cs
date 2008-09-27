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
using System.IO;
using System.Collections.Generic;

namespace Components.Services.ImporterManager
{
  public class WatchedFolder : IDisposable
  {
    #region Private fields

    string _folder;
    DelayedFileSystemWatcher _watcher;     // Watching the files
    DelayedFileSystemWatcher _watcherDir;  // Watching the directory
    DateTime _lastWatchedEvent;
    List<FileChangeEvent> _changesDetected;

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchedFolder"/> class.
    /// </summary>
    /// <param name="folder">The path of the folder to watch.</param>
    public WatchedFolder(string folder)
    {
      _folder = folder;
      _lastWatchedEvent = DateTime.MinValue;
      _changesDetected = new List<FileChangeEvent>();
      // Create the watchers. 
      // We need 2 type of watchers, for files and for directories
      // Reason is that we don't know if the event occured on a file or directory.
      // For a Create / Change / Rename we could figure that out using FileInfo or DirectoryInfo,
      // but when something gets deleted, we don't know if it is a File or directory
      _watcher = new DelayedFileSystemWatcher(_folder);
      _watcher.Created += _watcher_Created;
      _watcher.Changed += _watcher_Changed;
      _watcher.Deleted += _watcher_Deleted;
      _watcher.Renamed += _watcher_Renamed;
      _watcher.IncludeSubdirectories = true;
      _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Attributes;
      _watcher.EnableRaisingEvents = true;

      // Directory Watcher
      _watcherDir = new DelayedFileSystemWatcher(_folder);
      // For directories, we're only interested in Delete, Create and Rename.
      _watcherDir.Created += _watcher_Created;
      _watcherDir.Deleted += _watcher_DirectoryDeleted;
      _watcherDir.Renamed += _watcher_Renamed;
      _watcherDir.IncludeSubdirectories = true;
      _watcherDir.NotifyFilter = NotifyFilters.DirectoryName;
      _watcherDir.EnableRaisingEvents = true;
    }


    public IList<FileChangeEvent> Changes
    {
      get
      {
        if (_changesDetected.Count == 0) return null;
        TimeSpan ts = DateTime.Now - _lastWatchedEvent;
        if (ts.TotalSeconds >= 1)
        {
          List<FileChangeEvent> changes = _changesDetected;
          _changesDetected = new List<FileChangeEvent>();
          return changes;
        }
        return null;
      }
    }

    /// <summary>
    /// Handles the Deleted event of the _watcher control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.IO.FileSystemEventArgs"/> instance containing the event data.</param>
    void _watcher_Deleted(object sender, FileSystemEventArgs e)
    {
      Add(FileChangeEvent.FileChangeType.Deleted, e.FullPath);
    }

    /// <summary>
    /// Handles the Deleted Directory event of the _watcherDir control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.IO.FileSystemEventArgs"/> instance containing the event data.</param>
    void _watcher_DirectoryDeleted(object sender, FileSystemEventArgs e)
    {
      Add(FileChangeEvent.FileChangeType.DirectoryDeleted, e.FullPath);
    }

    /// <summary>
    /// Handles the Created event of the _watcher control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.IO.FileSystemEventArgs"/> instance containing the event data.</param>
    void _watcher_Created(object sender, FileSystemEventArgs e)
    {
      Add(FileChangeEvent.FileChangeType.Created, e.FullPath);
    }

    /// <summary>
    /// Handles the Change event of the _watcher control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.IO.FileSystemEventArgs"/> instance containing the event data.</param>
    void _watcher_Changed(object sender, FileSystemEventArgs e)
    {
      FileInfo fi = new FileInfo(e.FullPath);
      // Was the change on a file? Ignore change events on directories
      if (fi.Exists)
      {
        Add(FileChangeEvent.FileChangeType.Changed, e.FullPath);
      }
    }

    /// <summary>
    /// Handles the Renamed event of the _watcher control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="System.IO.RenamedEventArgs"/> instance containing the event data.</param>
    void _watcher_Renamed(object sender, RenamedEventArgs e)
    {
      Add(FileChangeEvent.FileChangeType.Renamed, e.FullPath, e.OldFullPath);
    }

    /// <summary>
    /// Gets the folder which is watched by this instance.
    /// </summary>
    public string Folder
    {
      get { return _folder; }
    }

    void Add(FileChangeEvent.FileChangeType type, string fileName)
    {
      Add(type, fileName, null);
    }

    void Add(FileChangeEvent.FileChangeType type, string fileName, string oldFileName)
    {
      FileChangeEvent change = new FileChangeEvent();
      change.Type = type;
      change.FullPath = fileName;
      change.OldFullPath = oldFileName;
      _changesDetected.Add(change);
      _lastWatchedEvent = DateTime.Now;
    }

    #region IDisposable Members

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    public void Dispose()
    {
      if (_watcher == null)
        return;
      _watcher.EnableRaisingEvents = false;
      _watcher.Dispose();
      _watcher = null;
    }

    #endregion
  }
}
