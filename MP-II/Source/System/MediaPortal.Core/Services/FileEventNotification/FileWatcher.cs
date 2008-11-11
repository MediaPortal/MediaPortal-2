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
using System.Threading;
using System.Timers;
// Conflict between System.Timers.Timer and System.Threading.Timer
using SystemTimer = System.Timers.Timer;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.FileEventNotification;

namespace MediaPortal.Core.Services.FileEventNotification
{

  /// <summary>
  /// This class wraps a FileSystemWatcher object.
  /// </summary>
  /// <remarks> 
  /// FileWatcher is not derived from FileSystemWatcher because most of
  /// the FileSystemWatcher methods are not virtual.
  /// FileWatcher will capture all events from the FileSystemWatcher object.
  /// The captured events will be delayed by at least 1000 milliseconds in order
  /// to be able to eliminate duplicate events. When duplicate events are found,
  /// the last event is droped and the first event is fired.
  /// FileWatcher can be used to watch a path which isn't always available, the watch
  /// will be initialized as soon as the path becomes available. When the path is lost
  /// (ie the USB drive gets detached), FileWatcher will wait for the path to become
  /// available again and will restore the watch.
  /// </remarks>
  internal class FileWatcher : IDisposable
  {

    #region Constants

    /// <summary>
    /// Time to wait between different checks for events.
    /// (milliseconds)
    /// </summary>
    private const int ConsolidationInterval = 1000;

    #endregion

    #region Variables

    /// <summary>
    /// The path to watch.
    /// </summary>
    private readonly WatchedPath _watchedPath;
    /// <summary>
    /// The FileSystemWatcher used to watch the path.
    /// </summary>
    private FileSystemWatcher _watcher;
    /// <summary>
    /// Indicates whether we're watching the path.
    /// </summary>
    private bool _watching;
    /// <summary>
    /// Indicates whether the current FileWatcher is disposed.
    /// </summary>
    private bool _isDisposed;
    /// <summary>
    /// All subscriptions for events comming from the current watcher.
    /// </summary>
    private readonly IList<FileWatcherInfo> _subscriptions;
    /// <summary>
    /// All events received within one _serverTimer interval.
    /// </summary>
    private readonly IList<FileWatchEvent> _events;
    /// <summary>
    /// Timer to periodically check if any events are received.
    /// </summary>
    private SystemTimer _notifyTimer;
    /// <summary>
    /// Object used for synchronization of the _notifyTimer.
    /// </summary>
    private readonly object _syncNotify;

    #endregion

    #region Properties

    /// <summary>
    /// Gets whether the current FileWatcher is disposed.
    /// </summary>
    public bool IsDisposed
    {
      get { return _isDisposed; }
    }

    /// <summary>
    /// Gets the subscriptions of the current FileWatcher.
    /// </summary>
    public IList<FileWatcherInfo> Subscriptions
    {
      get { return _subscriptions; }
    }

    #endregion

    #region Public Events

    /// <summary>
    /// Raised when the current FileWatcher is disposed.
    /// </summary>
    public event EventHandler Disposed;

    #endregion

    #region Constructor/Destructor

    /// <summary>
    /// Initializes a new instance of the FileWatcher class, given the path to watch.
    /// </summary>
    /// <exception cref="NotSupportedDriveTypeException">
    /// The drive type of the specified path is not supported by the system.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// The parameter \"path\" is a null reference.
    /// </exception>
    /// <param name="path">The path to watch.</param>
    public FileWatcher(string path)
    {
      if (path == null)
        throw new ArgumentNullException("path", "The parameter \"path\" is a null reference.");
      _syncNotify = new object();
      if (!IsValidDriveType(path))
        throw new NotSupportedDriveTypeException("The drive type of \"" + path + "\" is not supported by the system.");
      _subscriptions = new List<FileWatcherInfo>();
      _events = new List<FileWatchEvent>();
      // Initialize the Component to watch the path's availability.
      _watchedPath = new WatchedPath(path, false);
      _watchedPath.PathStateChangedEvent += WatchedPath_PathStateChangedEvent;
    }

    /// <summary>
    /// Destructor, disposes all used components.
    /// </summary>
    ~FileWatcher()
    {
      // Make sure to have released all resources.
      Dispose();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a new FileWatcherInfo to the current watch.
    /// The eventhandler in the specified FileWatcherInfo will be called on a change in the current watch.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The given fileWatchInfo contains a different path than the one being watched.
    /// </exception>
    /// <param name="fileWatcherInfo">The FileWatcherInfo to add.</param>
    public void Add(FileWatcherInfo fileWatcherInfo)
    {
      if (fileWatcherInfo.Path != _watchedPath.Path.FullName)
        throw new InvalidFileWatchInfoException("The specified path does not equal the watched path.");
      lock (_subscriptions)
      {
        if (_subscriptions.Contains(fileWatcherInfo))
          return; // The given subscription already exists
        _subscriptions.Add(fileWatcherInfo);
        if (fileWatcherInfo.EventHandler == null)
          return; // No event to call
        FileWatchEventArgs eventArgs =
          new FileWatchEventArgs(_watching ? FileWatchChangeType.Enabled : FileWatchChangeType.Disabled,
                                 fileWatcherInfo.Path);
        RaiseEvent(new EventData(fileWatcherInfo, eventArgs));
      }
    }

    /// <summary>
    /// Removes the given FileWatcherInfo from the current watch.
    /// The eventhandler won't be called anymore on a change in the current watch.
    /// </summary>
    /// <param name="fileWatcherInfo">The FileWatcherInfo to remove.</param>
    /// <returns>True if the FileWatcherInfo is found and removed.</returns>
    public bool Remove(FileWatcherInfo fileWatcherInfo)
    {
      lock (_subscriptions)
        return _subscriptions.Remove(fileWatcherInfo);
    }

    #endregion

    #region IDisposable Members

    /// <summary>
    /// Releases the resources used by the current FileWatcher.
    /// </summary>
    public void Dispose()
    {
      if (_isDisposed)
        return;
      _isDisposed = true;
      string path = _watchedPath.Path.FullName;
      if (_notifyTimer != null)
        _notifyTimer.Dispose();
      if (_watchedPath != null)
        _watchedPath.Dispose();
      if (_watcher != null)
        _watcher.Dispose();
      if (_events != null)
        _events.Clear();
      _watching = false;
      RaiseSingleEvent(path, FileWatchChangeType.Disposed);
      if (Disposed != null)
        Disposed(this, new EventArgs());
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns an initialized FileSystemWatcher for the specified path.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <param name="path">Path to initialize FileSystemWatcher for.</param>
    /// <returns></returns>
    private FileSystemWatcher InitializeFileSystemWatcher(string path)
    {
      FileSystemWatcher watcher = new FileSystemWatcher(path);
      watcher.Changed += FileSystemEventHandler;
      watcher.Created += FileSystemEventHandler;
      watcher.Deleted += FileSystemEventHandler;
      watcher.Renamed += FileSystemEventHandler;
      watcher.Error += ErrorEventHandler;
      watcher.Disposed += FileSystemWatcher_Disposed;
      return watcher;
    }

    /// <summary>
    /// Tries to initialize the service.
    /// </summary>
    /// <returns></returns>
    private void TryInitializeService()
    {
      if (_watching || !_watchedPath.Available)
        return;
      try
      {
        _watcher = InitializeFileSystemWatcher(_watchedPath.Path.FullName);
        _watcher.IncludeSubdirectories = true;
        _watcher.EnableRaisingEvents = true;
        _watching = true;
        _notifyTimer = new SystemTimer(ConsolidationInterval);
        _notifyTimer.Elapsed += NotifyTimer_Elapsed;
        _notifyTimer.Enabled = true;
      }
      catch (Exception)
      {
        // If something went wrong: dispose both the watcher and the notifytimer.
        _watching = false;
        if (_watcher != null)
        {
          _watcher.Dispose();
          _watcher = null;
        }
        if (_notifyTimer != null)
        {
          _notifyTimer.Dispose();
          _notifyTimer = null;
        }
      }
      if (_watcher != null)
        RaiseSingleEvent(_watchedPath.Path.FullName, FileWatchChangeType.Enabled);
    }

    /// <summary>
    /// Enables the watch service.
    /// </summary>
    private void EnableWatch()
    {
      if (_watcher == null)
      {
        TryInitializeService();
        return;
      }
      if (!_watchedPath.Available)
        return;
      try
      {
        _watcher.EnableRaisingEvents = true;
      }
      catch (FileNotFoundException)
      {
        return;
      }
      _watching = true;
      _notifyTimer.Enabled = true;
      RaiseSingleEvent(_watchedPath.Path.FullName, FileWatchChangeType.Enabled);
    }

    /// <summary>
    /// Disables the watch service.
    /// </summary>
    private void DisableWatch()
    {
      if (_watcher == null)
        return;
      _notifyTimer.Enabled = false;
      _watching = false;
      _watcher.Dispose();
      _watcher = null;
      RaiseSingleEvent(_watchedPath.Path.FullName, FileWatchChangeType.Disabled);
    }

    /// <summary>
    /// Notifies all subscriptions about the given change.
    /// </summary>
    /// <param name="changeType"></param>
    /// <param name="path"></param>
    private void RaiseSingleEvent(string path, FileWatchChangeType changeType)
    {
      Queue<FileWatchEvent> _report = new Queue<FileWatchEvent>(1);
      _report.Enqueue(new FileWatchEvent(changeType, path));
      RaiseEvents(_report);
    }

    /// <summary>
    /// Raises all queued events.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// The parameter "eventQueue" is a null reference.
    /// </exception>
    /// <param name="eventQueue"></param>
    private void RaiseEvents(Queue<FileWatchEvent> eventQueue)
    {
      if (eventQueue == null)
        throw new ArgumentNullException("eventQueue", "The parameter \"eventQueue\" is a null reference.");
      while (eventQueue.Count > 0)
      {
        FileWatchEvent watchEvent = eventQueue.Dequeue();
        IFileWatchEventArgs args = new FileWatchEventArgs(watchEvent);
        lock (_subscriptions)
        {
          foreach (FileWatcherInfo info in _subscriptions)
          {
            if (info.MayRaiseEventFor(args))
              new Thread(RaiseEvent).Start(new EventData(info, args));
          }
        }
      }
    }

    #endregion

    #region EventHandlers

    /// <summary>
    /// Filters all received events, and raises the waiting ones.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NotifyTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (!Monitor.TryEnter(_syncNotify))
        return; // This timer event was skipped, processing will happen during the next timer event
      // Set the current threads name for logging purpose.
      if (Thread.CurrentThread.Name == null)
        Thread.CurrentThread.Name = "FileEventNotifier";
      // Only one thread at a time is processing the events.
      // We don't fire the events inside the lock. We will queue them here until the code exits the lock.
      Queue<FileWatchEvent> eventsToBeFired = new Queue<FileWatchEvent>();
      try
      {
        // Lock the collection while processing the events
        lock (_events)
        {
          for (int i = 0; i < _events.Count; i++)
          {
            if (_events[i].Delayed)
            {
              // This event has been delayed already so we can fire it
              // We just need to remove any duplicates
              for (int j = i + 1; j < _events.Count; j++)
              {
                if (_events[i].IsDuplicate(_events[j]))
                {
                  // Removing later duplicates
                  _events.RemoveAt(j);
                  j--; // Don't skip next event
                }
              }
              // Is the current event still delayed?
              // FileWatchEvent.IsDuplicate() could have changed the state.
              if (_events[i].Delayed)
              {
                // Add the event to the list of events to be fired
                eventsToBeFired.Enqueue(_events[i]);
                // Remove it from the current list
                _events.RemoveAt(i);
                i--; // Don't skip next event
              }
            }
            else
            {
              // This event was not delayed yet, so we will delay processing
              // this event for at least one timer interval
              _events[i].Delayed = true;
            }
          }
        }
      }
      finally
      {
        Monitor.Exit(_syncNotify);
      }
      RaiseEvents(eventsToBeFired);
    }

    /// <summary>
    /// Handles changes of the state of the watched path.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="pathAvailable"></param>
    private void WatchedPath_PathStateChangedEvent(WatchedPath sender, bool pathAvailable)
    {
      if (pathAvailable && !_watching)
        EnableWatch();
      else if (!pathAvailable && _watching)
        DisableWatch();
    }

    /// <summary>
    /// Handles all events raised by _fileSystemWatcher.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FileSystemEventHandler(object sender, FileSystemEventArgs e)
    {
      FileSystemWatcher fileSystemWatcher = (FileSystemWatcher)sender;
      // Received an event for the watched path?
      if (fileSystemWatcher.Path == _watchedPath.Path.FullName && _watching)
      {
        lock (_events)
          _events.Add(new FileWatchEvent(e));
      }
      // Received an event for the parent path, regarding the watched path?
      else if (e.FullPath + "\\" == _watchedPath.Path.FullName)
      {
        if ((e.ChangeType & WatcherChangeTypes.Deleted) == WatcherChangeTypes.Deleted)
        {
          DisableWatch();
          lock (_events)
            _events.Clear();
        }
        else if ((e.ChangeType & WatcherChangeTypes.Created) == WatcherChangeTypes.Created
            || (e.ChangeType & WatcherChangeTypes.Renamed) == WatcherChangeTypes.Renamed)
        {
          EnableWatch();
        }
      }
      // Else: discart the event
    }

    /// <summary>
    /// Handles errors from the FileSystemWatchers.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ErrorEventHandler(object sender, ErrorEventArgs e)
    {
      if (sender == _watcher)
        DisableWatch();
    }

    /// <summary>
    /// Handles the Disposed event for the FileSystemWatchers.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FileSystemWatcher_Disposed(object sender, EventArgs e)
    {
      if (sender == _watcher)
        _watching = false;
      else if (sender is FileSystemWatcher)
        // The sender is another FileSystemWatcher, we don't care about its state.
        ((FileSystemWatcher)sender).Disposed -= FileSystemWatcher_Disposed;
    }

    #endregion

    #region Static Methods

    /// <summary>
    /// Returns whether the drive's type is valid.
    /// A valid drive type should be watchable by the FileSystemWatcher.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private static bool IsValidDriveType(string path)
    {
      if (path.StartsWith(@"\\"))
        return true;
      try
      {
        return new DriveInfo(path).DriveFormat == "NTFS";
      }
      catch (ArgumentException)
      {
        return false;
      }
      catch (IOException)
      {
        return false;
      }
    }

    /// <summary>
    /// Raises an event for the given EventData.
    /// </summary>
    /// <param name="eventData">An instance of EventData.</param>
    private static void RaiseEvent(object eventData)
    {
      // Should we give the current Thread a name?
      EventData data = (EventData)eventData;
      if (data.Info.EventHandler != null)
      {
        try
        {
          data.Info.EventHandler(data.Info, data.Args);
        }
        // Suppress exceptions,
        // else the whole core could crash if a plugin throws unhandled exceptions from within the eventhandler.
        catch (Exception e)
        {
          ServiceScope.Get<ILogger>().Error("Unhandled FileWatchEvent for path \"{0}\".", e, data.Args.Path);
        }
      }
    }

    #endregion

  }
}