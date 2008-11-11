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
using System.Threading;
using MediaPortal.Core.FileEventNotification;

namespace MediaPortal.Core.Services.FileEventNotification
{

  /// <summary>
  /// FileEventNotifier is an advanced implementation of System.IO.FileSystemWatcher.
  /// </summary>
  /// <remarks>
  /// Features
  /// <ul>
  ///   <li>Watch files/directories</li>
  ///   <li>Autorestore on path lost/retrieved</li>
  ///   <li>Events on path lost/retrieved</li>
  ///   <li>One event per change</li>
  ///   <li>Advanced filtering</li>
  ///   <li>No events when filesize is increasing</li>
  ///   <li>Internal error handling</li>
  /// </ul>
  /// Limitations
  /// <ul>
  ///   <li>NTFS Only</li>
  /// </ul>
  /// See the wiki for detailed information.
  /// </remarks>
  public class FileEventNotifier : IFileEventNotifier
  {

    #region Variables

    /// <summary>
    /// The last assigned ID.
    /// </summary>
    private int _lastId;
    /// <summary>
    /// All free ID's.
    /// </summary>
    private readonly Queue<int> _freeId;
    /// <summary>
    /// All active FileWatchers, with the watched path as the key.
    /// </summary>
    private readonly IDictionary<string, FileWatcher> _watchers;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of FileEventNotifier,
    /// wich is the default implementation of IFileEventNotifier.
    /// A service to watch paths with.
    /// </summary>
    public FileEventNotifier()
    {
      _lastId = -1;
      _freeId = new Queue<int>();
      _watchers = new Dictionary<string, FileWatcher>();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets the first free ID,
    /// increments _lastId if a new id is created.
    /// </summary>
    /// <returns></returns>
    private int GetFirstFreeId()
    {
      int id;
      lock (_freeId)
      {
        if (_freeId.Count == 0)
        {
          Interlocked.Increment(ref _lastId);
          id = _lastId;
        }
        else
        {
          id = _freeId.Dequeue();
        }
      }
      return id;
    }

    /// <summary>
    /// Removes the sender, which must be a FileWatcher, from _watchers.
    /// </summary>
    /// <param name="sender">The disposed FileWatcher.</param>
    /// <param name="e">Extra arguments. (not used)</param>
    private void FileWatcher_Disposed(object sender, EventArgs e)
    {
      FileWatcher watcher = (FileWatcher)sender;
      if (!watcher.IsDisposed) return;  // Just make sure it's disposed
      lock (_watchers)      // Don't let other threads add a new item to the disposed watcher
      {
        // Find and remove the disposed watcher
        foreach (KeyValuePair<string, FileWatcher> pair in _watchers)
        {
          // The Value must equal the object.
          // No IEquetable implemented in FileWatcher.
          if (pair.Value.Equals(watcher))
          {
            _watchers.Remove(pair);
            break;
          }
        }
      }
      lock (_freeId)
      {
        // Free the ID's
        foreach (FileWatcherInfo subscription in watcher.Subscriptions)
        {
          _freeId.Enqueue(subscription.Id);
          subscription.Id = -1;
        }
      }
    }

    #endregion

    #region IFileEventNotifier Members

    // ToDo: subscribe to parent path if available and adjust filter
    // eg if D:\ is watched, D:\TEMP can be added there
    // Set the internal SubscribedPath property.
    // Also change the way FileWatcherInfo applies filters! Must inspect the path!
    public FileWatchInfo Subscribe(FileWatchInfo fileWatchInfo)
    {
      FileWatcherInfo fileWatcherInfo = new FileWatcherInfo(fileWatchInfo);
      fileWatcherInfo.Id = GetFirstFreeId();
      lock (_watchers)
      {
        bool foundWatcher = false;  // Indicate whether we found the FileWatcher during the foreach
        foreach (KeyValuePair<string, FileWatcher> pair in _watchers)
        {
          // Does the current FileWatcher watch the specified path? If not, continue.
          if (pair.Key != fileWatcherInfo.Path) continue;
          // Make sure the selected watcher is not disposed.
          if (!pair.Value.IsDisposed)
          {
            _watchers[fileWatcherInfo.Path].Add(fileWatcherInfo);
            foundWatcher = true;
            break;
          }
          // Else, No need to keep it referenced.
          _watchers.Remove(pair);
        }
        if (!foundWatcher)
        {
          // Existing watcher is not found, create a new one.
          FileWatcher watcher = new FileWatcher(fileWatcherInfo.Path);
          watcher.Disposed += FileWatcher_Disposed;
          watcher.Add(fileWatcherInfo);
          _watchers.Add(fileWatcherInfo.Path, watcher);
        }
      }
      return fileWatcherInfo;
    }

    // ToDo: unsubscribe from parent path if available
    // Use the internal SubscribedPath property.
    // FileWatcher needs to optimize on a Remove.
    // eg when the watcher watches for "D:\" and "D:\TEMP",
    //    -> optimize to "D:\TEMP" when "D:\" is removed.
    public bool Unsubscribe(FileWatchInfo fileWatchInfo)
    {
      if (!(fileWatchInfo is FileWatcherInfo)
        || ((FileWatcherInfo) fileWatchInfo).Id == -1)
        throw new InvalidFileWatchInfoException(
          String.Format(
            "The specified FileWatcherInfo for path \"{0}\" can't be unsubscribed because it's not a subscribed item of the service.",
            fileWatchInfo.Path));
      FileWatcherInfo fileWatcherInfo = (FileWatcherInfo) fileWatchInfo;
      lock (_freeId)
      {
        _freeId.Enqueue(fileWatcherInfo.Id);
        fileWatcherInfo.Id = -1;
      }
      lock (_watchers)
      {
        if (_watchers.ContainsKey(fileWatcherInfo.Path)) // Must contain key to be able to remove
        {
          FileWatcher watcher = _watchers[fileWatcherInfo.Path];
          if (watcher.Remove(fileWatcherInfo))
          {
            if (watcher.Subscriptions.Count == 0)
              watcher.Dispose();
            return true;
          }
        }
      }
      return false;
    }

    #endregion

  }
}
