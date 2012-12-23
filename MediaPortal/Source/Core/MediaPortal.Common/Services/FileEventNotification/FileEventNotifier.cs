#region Copyright (C) 2007-2012 Team MediaPortal

/*
    Copyright (C) 2007-2012 Team MediaPortal
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
using System.Linq;
using System.Threading;
using MediaPortal.Common.FileEventNotification;

namespace MediaPortal.Common.Services.FileEventNotification
{

  /// <summary>
  /// <see cref="FileEventNotifier"/> is an advanced implementation of <see cref="System.IO.FileSystemWatcher"/>.
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
      var watcher = (FileWatcher)sender;
      if (!watcher.IsDisposed) return;  // Just make sure it's disposed
      lock (_watchers)      // Don't let other threads add a new item to the disposed watcher
      {
        // Find and remove the disposed watcher
        foreach (KeyValuePair<string, FileWatcher> pair in _watchers)
        {
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

    // ToDo: subscribe to parent path if available and adjust filter
    // eg if D:\ is watched, D:\TEMP can be added there
    // Set a SubscribedPath property for FileWatcherInfo.
    // Also change the way FileWatcherInfo applies filters! Must inspect the path!
    private FileWatcherInfo Subscribe(FileWatcherInfo fileWatcherInfo)
    {
      lock (_watchers)
      {
        bool foundValidWatcher = false;  // Indicates whether we found a useful FileWatcher during the foreach.
        foreach (var item in _watchers)
        {
          // Does the current FileWatcher watch the specified path?
          if (item.Key == fileWatcherInfo.Path)
          {
            foundValidWatcher = !item.Value.IsDisposed;
            if (foundValidWatcher)
              // The watcher is not disposed, add a subscription.
              _watchers[fileWatcherInfo.Path].AddSubscription(fileWatcherInfo);
            else
              // No need to keep this disposed watcher referenced.
              _watchers.Remove(item);
            break;
          }
        }
        if (!foundValidWatcher)
        {
          // Existing non-disposed watcher is not found, create a new one.
          var watcher = new FileWatcher(fileWatcherInfo.Path);
          _watchers.Add(fileWatcherInfo.Path, watcher);
          watcher.Disposed += FileWatcher_Disposed;
          watcher.Watching = true;
          watcher.AddSubscription(fileWatcherInfo);
        }
      }
      return fileWatcherInfo;
    }

    #endregion

    #region IFileEventNotifier Members

    public FileWatchInfo Subscribe(string path, bool includeSubDirectories, FileEventHandler eventHandler)
    {
      return Subscribe(path, includeSubDirectories, eventHandler, new string[0]);
    }

    public FileWatchInfo Subscribe(string path, bool includeSubDirectories, FileEventHandler eventHandler,
        IEnumerable<string> filter)
    {
      return Subscribe(path, includeSubDirectories, eventHandler, filter, new FileWatchChangeType[0]);
    }

    public FileWatchInfo Subscribe(string path, bool includeSubDirectories, FileEventHandler eventHandler,
        IEnumerable<string> filter, IEnumerable<FileWatchChangeType> changeTypes)
    {
      var id = GetFirstFreeId();
      var fileWatcherInfo = new FileWatcherInfo(id, path, includeSubDirectories, eventHandler,
                                                filter, changeTypes);
      return Subscribe(fileWatcherInfo);
    }

    // ToDo: unsubscribe from parent path if available
    // Use a SubscribedPath property in FileWatcherInfo.
    // FileWatcher needs to optimize on a Remove.
    // eg when the watcher watches for "D:\" and "D:\TEMP",
    //    -> optimize to "D:\TEMP" when "D:\" is removed.
    public bool Unsubscribe(FileWatchInfo fileWatchInfo)
    {
      var fileWatcherInfo = fileWatchInfo as FileWatcherInfo;
      if (fileWatcherInfo == null || fileWatcherInfo.Id == -1)
        throw new InvalidFileWatchInfoException(String.Format(
          "The specified FileWatcherInfo for path \"{0}\" can't be unsubscribed because it's not a subscribed item of the service.",
          fileWatchInfo.Path));
      // Free the assigned ID.
      lock (_freeId)
      {
        _freeId.Enqueue(fileWatcherInfo.Id);
        fileWatcherInfo.Id = -1;
      }
      lock (_watchers)
      {
        if (_watchers.ContainsKey(fileWatcherInfo.Path)) // Must contain key to be able to remove
        {
          var watcher = _watchers[fileWatcherInfo.Path];
          if (watcher.RemoveSubscription(fileWatcherInfo))
          {
            bool isLastItem = !watcher.Subscriptions.Any();
            if (isLastItem) watcher.Dispose();
            return true;
          }
        }
      }
      return false;
    }

    #endregion

  }
}
