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
using System.Net;
using System.Threading;
using System.Timers;
// Conflict between System.Timers.Timer and System.Threading.Timer
using SystemTimer = System.Timers.Timer;

using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.FileEventNotification;


namespace MediaPortal.Services.FileEventNotification
{

  /// <summary>
  /// This class wraps a FileSystemWatcher object. The class is not derived
  /// from FileSystemWatcher because most of the FileSystemWatcher methods 
  /// are not virtual. The class was designed to resemble FileSystemWatcher class
  /// as much as possible so that you can use DelayedFileSystemWatcher instead 
  /// of FileSystemWatcher objects. 
  /// DelayedFileSystemWatcher will capture all events from the FileSystemWatcher object.
  /// The captured events will be delayed by at least ConsolidationInterval milliseconds in order
  /// to be able to eliminate duplicate events. When duplicate events are found, the last event
  /// is droped and the first event is fired (the reverse is not recomended because it could
  /// cause some events not be fired at all since the last event will become the first event and
  /// it won't fire a if a new similar event arrives imediately afterwards).
  /// </summary>
  /// <remarks>
  ///  Based on code published by Adrian Hamza on
  ///  http://blogs.gotdotnet.com/ahamza/archive/2006/02/04/FileSystemWatcher_Duplicate_Events.aspx
  /// </remarks>
  internal class FileWatcher : IDisposable
  {

    #region Variables

    /// <summary>
    /// The watched path,
    /// should be the same as _filesystemWatcher.Path.
    /// </summary>
    private string _path;
    /// <summary>
    /// Listens to the file system change notifications and raises events when a directory, or file in a directory, changes.
    /// </summary>
    private FileSystemWatcher _fileSystemWatcher;
    /// <summary>
    /// All subscriptions for events comming from the current watcher.
    /// </summary>
    private IList<FileWatcherInfo> _subscriptions;
    /// <summary>
    /// All events received within one _serverTimer interval.
    /// </summary>
    private IList<FileWatchEvent> _events;
    /// <summary>
    /// Timer to periodically check if any events are received.
    /// </summary>
    private SystemTimer _notifyTimer;
    /// <summary>
    /// Timer to periodically check if the path is available.
    /// </summary>
    private SystemTimer _checkkTimer;
    /// <summary>
    /// Timer to periodically check if we can initialize the FileSystemWatcher.
    /// </summary>
    private SystemTimer _enableTimer;
    /// <summary>
    /// Time to wait between different checks for events.
    /// (milliseconds)
    /// </summary>
    private int _msConsolidationInterval;
    /// <summary>
    /// Time to wait between different verifications of the specified path.
    /// (milliseconds)
    /// </summary>
    private int _msCheckkInterval;
    /// <summary>
    /// Indicates whether the specified path needs to be periodically checked on its existance.
    /// </summary>
    private bool _isVolatile;
    /// <summary>
    /// Indicates whether the path is available.
    /// </summary>
    private bool _isPathAvailable;
    /// <summary>
    /// Indicates whether the current FileWatcher is disposed.
    /// </summary>
    private bool _isDisposed;
    /// <summary>
    /// Object used for synchronization of the _notifyTimer's events.
    /// </summary>
    private object _syncRoot;

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets or sets the watched path.
    /// </summary>
    /// <exception cref="NotSupportedDriveTypeException"></exception>
    public string Path
    {
      get { return _path; }
      set
      {
        // Check if the path's drive type is valid.
        if (!IsValidDriveType(value))
          throw new NotSupportedDriveTypeException("The drive type of \"" + value + "\" is not supported by the system.");
        // Check if the path is volatile.
        _isVolatile = IsVolatileLocation(value);
        // Lock the _syncRoot to make sure no events are processed during the change.
        lock (_syncRoot)
        {
          _path = value;
          _fileSystemWatcher.Path = value;
          _events.Clear();
        }
        if (_isVolatile)
        {
          // Volatile location, make sure we have the _checkTimer watching.
          if (_checkkTimer == null)
          {
            _checkkTimer = new SystemTimer(_msCheckkInterval);
            _checkkTimer.Elapsed += CheckTimer_Elapsed;
          }
          _checkkTimer.Enabled = _fileSystemWatcher.EnableRaisingEvents;
        }
        else if (_checkkTimer != null)
        {
          // Not a volatile location, _checkTimer isn't needed.
          _checkkTimer.Dispose();
          _checkkTimer = null;
        }
      }
    }

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

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the FileWatcher class, given the specified FileWatcherInfo.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// The Path property of the specified FileWatcherInfo is a null reference. 
    /// -or- 
    /// The Filter property of the specified FileWatcherInfo parameter is a null reference.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The Path property of the specified FileWatcherInfo is an empty string ("").
    /// -or-
    /// The path specified through the Path property of the specified FileWatcherInfo does not exist.
    /// </exception>
    /// <param name="fileWatchInfo">FileWatcherInfo containing the path and filter.</param>
    public FileWatcher(FileWatcherInfo fileWatchInfo)
    {
      if (fileWatchInfo.Filter == null)
        throw new ArgumentNullException("The Filter property of the"
          + " specified FileWatcherInfo parameter is a null reference.");
      _subscriptions = new List<FileWatcherInfo>();
      _path = fileWatchInfo.Path;
      if (IsPathAvailable(_path))
      {
        _fileSystemWatcher = new FileSystemWatcher(_path);
        Initialize();
        Add(fileWatchInfo);
      }
      // Else: we can't initialize yet, FileSystemWatcher would throw an Exception.
      else
      {
        _subscriptions.Add(fileWatchInfo);
        _enableTimer = new SystemTimer(2500);
        _enableTimer.Elapsed += EnableTimer_Elapsed;
        _enableTimer.Enabled = true;
      }
    }

    /// <summary>
    /// Initializes a new instance of the FileWatcher class, given the specified collection of FileWatcherInfo.
    /// </summary>
    /// <exception cref="ArgumentNullException">
    /// The Path property of one of the specified FileWatcherInfo instances is a null reference. 
    /// -or- 
    /// The Filter property of one of the specified FileWatcherInfo instances is a null reference.
    /// -or-
    /// The specified fileWatchInfoCollection is a null reference.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// The Path property of one of the specified FileWatcherInfo instances is an empty string ("").
    /// -or-
    /// The path specified through the Path property of one of the specified FileWatcherInfo instances does not exist.
    /// -or-
    /// Not all paths specified through the Path property of the specified FileWatcherInfo instances, are equal.
    /// -or-
    /// The specified fileWatchInfoCollection is empty.
    /// </exception>
    /// <param name="fileWatchInfoCollection">Collection of FileWatcherInfo containing the path and filter.</param>
    public FileWatcher(IEnumerable<FileWatcherInfo> fileWatchInfoCollection)
    {
      if (fileWatchInfoCollection == null)
        throw new ArgumentNullException("The specified fileWatchInfoCollection "
          + "is a null reference.");
      IEnumerator<FileWatcherInfo> enumerator = fileWatchInfoCollection.GetEnumerator();
      if (!enumerator.MoveNext())
        throw new ArgumentException("The specified fileWatchInfoCollection is empty.");
      _path = enumerator.Current.Path;
      // The path must be accessible if we want to initialize the current FileWatcher.
      if (IsPathAvailable(_path))
      {
        // Initialize the FileSystemWatcher using the first instance of FileWatcherInfo.
        _fileSystemWatcher = new FileSystemWatcher(enumerator.Current.Path);
        _subscriptions = new List<FileWatcherInfo>();
        Initialize();
        do // We already did a MoveNext()
        {
          if (enumerator.Current.Path != _path)
            throw new ArgumentException(
              String.Format(
                "The current FileWatcher watches \"{0}\", while an item from the specified "
                + "fileWatchInfoCollection wants to watch \"{1}\"",
                _path, enumerator.Current.Path));
          Add(enumerator.Current);
        } while (enumerator.MoveNext());
      }
      // Else: we can't initialize yet, because of unavailable path
      else
      {
        foreach (FileWatcherInfo fileWatcherInfo in fileWatchInfoCollection)
        {
          // Check if all paths are equal,
          // we don't want to throw any exceptions while we try to initialize the current FileWatcher later on.
          // Especially not because that's going to happen in a different thread.
          if (fileWatcherInfo.Path != _path)
            throw new ArgumentException(
              String.Format("The current FileWatcher watches \"{0}\", while an item from the specified "
                            + "fileWatchInfoCollection wants to watch \"{1}\"",
                            _path, enumerator.Current.Path));
          _subscriptions.Add(fileWatcherInfo);
        }
        // Check if we can initialize, every 2 seconds
        _enableTimer = new SystemTimer(2000);
        _enableTimer.Elapsed += EnableTimer_Elapsed;
        _enableTimer.Enabled = true;
      }
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
    /// <param name="fileWatchInfo">The FileWatcherInfo to add.</param>
    public void Add(FileWatcherInfo fileWatchInfo)
    {
      if (fileWatchInfo.Path != _path)
        throw new ArgumentException(String.Format("Invalid path specified in the given fileWatchInfo. Contains \"{0}\", while \"{1}\" is excpected",
          fileWatchInfo.Path, _path));
      lock (_subscriptions)
      {
        // Don't add it twice
        if (!_subscriptions.Contains(fileWatchInfo))
        {
          _subscriptions.Add(fileWatchInfo);
          if (_fileSystemWatcher != null)
          {
            if (!_fileSystemWatcher.IncludeSubdirectories
                && fileWatchInfo.IncludeSubdirectories)
              _fileSystemWatcher.IncludeSubdirectories = true;
            // We have to enable the resources if this is ther first subscription.
            if (_subscriptions.Count == 1)
              EnableRaisingEvents();
          }
          // Else: another thread is trying to initialize the _fileSystemWatcher
        }
      }
    }

    /// <summary>
    /// Removes the given FileWatcherInfo from the current watch.
    /// The eventhandler won't be called anymore on a change in the current watch.
    /// </summary>
    /// <param name="fileWatchInfo">The FileWatcherInfo to remove.</param>
    /// <returns>True if the FileWatcherInfo is found and removed.</returns>
    public bool Remove(FileWatcherInfo fileWatchInfo)
    {
      bool removed;
      lock (_subscriptions)
        removed = _subscriptions.Remove(fileWatchInfo);
      if (removed)
      {
        if (_subscriptions.Count == 0)
          Dispose();  // No subscriptions left, release all resources.
        return true;
      }
      return false;
    }

    /// <summary>
    /// Returns a string representation of the current FileWatcher.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
      return String.Format("Path: {0}   Status: {1}",
                           // The watched path
                           _path,
                           // Status: Disposed, Enabled, or Disabled
                           (_isDisposed ? "Disposed" : (_fileSystemWatcher.EnableRaisingEvents ? "Enabled" : "Disabled")));
    }

    #endregion

    #region IDisposable Members

    /// <summary>
    /// Releases the resources used by the current FileWatcher.
    /// </summary>
    public void Dispose()
    {
      _isDisposed = true;
      ServiceScope.Get<ILogger>().Debug("Disposing FileWatcher for \"{0}\"", _path);
      if (_fileSystemWatcher != null)
        _fileSystemWatcher.Dispose();
      if (_notifyTimer != null)
        _notifyTimer.Dispose();
      if (_checkkTimer != null)
        _checkkTimer.Dispose();
      if (_enableTimer != null)
        _enableTimer.Dispose();
      if (Disposed != null)
        Disposed(this, new EventArgs());
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Helps the constructors,
    /// by initializing all local variables, except for _fileSystemWatcher.
    /// </summary>
    /// <exception cref="NotSupportedDriveTypeException"></exception>
    private void Initialize()
    {
      // Set the timer intervals.
      _msConsolidationInterval = 1000;
      _msCheckkInterval = 2000;
      // Do some basic checks regarding the path.
      _isVolatile = IsVolatileLocation(_path);
      _isPathAvailable = IsPathAvailable(_path);
      // Object used to synchronize.
      if (_syncRoot == null)
        _syncRoot = new object();
      // List of received events.
      if (_events == null)
        _events = new List<FileWatchEvent>();
      // List of subscriptions, to send events to.
      if (_subscriptions == null)
        _subscriptions = new List<FileWatcherInfo>();
      // Configure _fileSystemWatcher.
      _fileSystemWatcher.Filter = "*";
      _fileSystemWatcher.Changed += FileSystemEventHandler;
      _fileSystemWatcher.Created += FileSystemEventHandler;
      _fileSystemWatcher.Deleted += FileSystemEventHandler;
      _fileSystemWatcher.Renamed += FileSystemEventHandler;
      _fileSystemWatcher.Error += ErrorEventHandler;
      // Initialize the timer to periodically check if any events have been received.
      _notifyTimer = new SystemTimer(_msConsolidationInterval);
      _notifyTimer.Elapsed += NotifyTimer_Elapsed;
      _notifyTimer.AutoReset = true;
      _notifyTimer.Enabled = _fileSystemWatcher.EnableRaisingEvents;
      if (_isVolatile)
      {
        // Initialize the timer to periodically check if the path is still available.
        _checkkTimer = new SystemTimer(_msCheckkInterval);
        _checkkTimer.Elapsed += CheckTimer_Elapsed;
        _checkkTimer.Enabled = _fileSystemWatcher.EnableRaisingEvents;
      }
    }

    /// <summary>
    /// Creates a new instance of the FileSystemWatcher, which is based on the current one.
    /// The calling thread is blocked untill the Watch is fully reinitialized.
    /// </summary>
    private void ReInitializeWatcher()
    {
      // No need to reinitialize if the current FileWatcher is disposed.
      if (_isDisposed)
        return;
      // Disable the timers, to make sure no events are processed during reinitialization.
      if (_notifyTimer != null)
        _notifyTimer.Enabled = false;
      if (_checkkTimer != null)
        _checkkTimer.Enabled = false;
      // Create a new instance of the internal FileSystemWatcher.
      _fileSystemWatcher = new FileSystemWatcher(_path, _fileSystemWatcher.Filter);
      // Keep trying to enable the FileSystemWatcher, and block the calling thread 'till we're done.
      bool enabled = _fileSystemWatcher.EnableRaisingEvents;
      while (!enabled)
      {
        if (IsPathAvailable(_path))
        {
          try
          {
            _fileSystemWatcher.EnableRaisingEvents = true;
            enabled = true;
          }
          catch (FileNotFoundException)
          {
            // IsPathAvailable isn't always correct for UNC paths,
            // it appears like Directory.Exists() isn't updated quick enough.
            // We'll have to try again later.
            Thread.Sleep(500);
          }
        }
        else
        {
          Thread.Sleep(1500);
        }
      }
      // Enable the timers again.
      if (_notifyTimer != null)
        _notifyTimer.Enabled = true;
      if (_checkkTimer != null)
        _checkkTimer.Enabled = true;
    }

    /// <summary>
    /// Notifies subscribers that the watch is disabled,
    /// and disables the _notifyTimer.
    /// </summary>
    private void NotifyDisabledWatch()
    {
      // Disable the timer responsible for sending events.
      _notifyTimer.Enabled = false;
      // Notify subscriptions that the watch is lost.
      Queue<FileWatchEvent> report = new Queue<FileWatchEvent>(1);
      report.Enqueue(new FileWatchEvent(FileWatchChangeType.Disabled, _path));
      RaiseEvents(report);
    }

    /// <summary>
    /// Restores a previously lost watch by removing all items
    /// which don't need to be autorestored.
    /// And by then reinitializing the watcher.
    /// Finally all subscribers are notified about the new status.
    /// </summary>
    private void ReEnableWatch()
    {
      // Remove all items which don't need to be restored.
      for (int i = _subscriptions.Count - 1; i > -1; i--)
      {
        if (!_subscriptions[i].AutoRestore)
          _subscriptions.RemoveAt(i);
      }
      // Dispose this if no subscriptions left.
      if (_subscriptions.Count == 0)
      {
        Dispose();
      }
      // Else: reinitialize the watcher.
      else
      {
        ReInitializeWatcher();
        // Notify subscribers that the watch is enabled.
        Queue<FileWatchEvent> _report = new Queue<FileWatchEvent>(1);
        _report.Enqueue(new FileWatchEvent(FileWatchChangeType.Enabled, _path));
        RaiseEvents(_report);
      }
    }

    /// <summary>
    /// Enables the current resources,
    /// meaning the current FileWatcher starts watching.
    /// </summary>
    private void EnableRaisingEvents()
    {
      if (IsPathAvailable(_path))
      {
        _fileSystemWatcher.EnableRaisingEvents = true;
        if (_subscriptions.Count != 0)
        {
          _notifyTimer.Enabled = true;
          if (_checkkTimer != null)
            _checkkTimer.Enabled = true;
        }
      }
      // Else: periodically check if the path is available to watch
      else
      {
        if (_checkkTimer == null)
        {
          _checkkTimer = new SystemTimer(_msCheckkInterval);
          _checkkTimer.Elapsed += CheckTimer_Elapsed;
        }
        _checkkTimer.Enabled = true;
      }
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
        throw new ArgumentNullException("The parameter \"eventQueue\" is a null reference.");
      while (eventQueue.Count > 0)
      {
        FileWatchEvent watchEvent = eventQueue.Dequeue();
        lock (_subscriptions)
        {
          foreach (FileWatcherInfo info in _subscriptions)
          {
            IFileWatchEventArgs args = new FileWatchEventArgs(watchEvent);
            if (info.MayRaiseEventFor(args))
              new Thread(RaiseEvent).Start(new EventData(info, args));
          }
        }
      }
    }

    /// <summary>
    /// Raises an event for the given EventData.
    /// </summary>
    /// <param name="eventData">An instance of EventData.</param>
    private void RaiseEvent(object eventData)
    {
      // Should we give the current Thread a name?
      EventData data = (EventData)eventData;
      data.Info.EventHandler(data.Info, data.Args);
    }

    /// <summary>
    /// Determines whether the specified path links to a potential volatile location.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool IsVolatileLocation(string path)
    {
      if (path == null)
        throw new ArgumentNullException("The \"path\" parameter is a null reference.");
      if (path.StartsWith(@"\\"))
        return true;
      DriveInfo driveInfo = new DriveInfo(path);
      switch (driveInfo.DriveType)
      {
        case DriveType.Fixed:
          // A fixed drive should never "disappear"
          return false;
        case DriveType.NoRootDirectory:
          // Should never be returned, if it does get returned: fix it.
          throw new NotSupportedException(String.Format("Illegal return value while trying to get the drive type for \"{0}\"", path));
        default: // Volatile DriveTypes: Unknown, Removable, Network, CDRom, and Ram
          return true;
      }
    }

    /// <summary>
    /// Returns whether the drive's type is valid.
    /// A valid drive type should be watchable by the FileSystemWatcher.
    /// </summary>
    /// <exception cref="ArgumentNullException">The "path" parameter is a null reference</exception>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool IsValidDriveType(string path)
    {
      if (path == null)
        throw new ArgumentNullException("The \"path\" parameter is a null reference.");
      if (path.StartsWith(@"\\"))
        return true;
      try
      {
        return new DriveInfo(path).DriveFormat.StartsWith("NTFS");
      }
      catch (ArgumentException)
      {
        return false;
      }
    }

    /// <summary>
    /// Returns whether the given path is available.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns></returns>
    private bool IsPathAvailable(string path)
    {
      // Is it a UNC path? If so, we'll need to take a different approach to optimize performance.
      if (path.StartsWith(@"\\"))
      {
        // Get the servers hostname
        string host = path.Substring(2);
        int index = host.IndexOf('\\');
        if (index != -1)
          host = host.Substring(0, index);
        try
        {
          // Try to resolve the UNC path by DNS.
          // If server isn't active: we loose about 2 seconds
          // If we would do an immediate Directory.Exist: we loose up to 10 seconds.
          Dns.GetHostEntry(host);
        }
        catch (System.Net.Sockets.SocketException)
        {
          // Can't resolve the DNS, meaning the server is not available.
          return false;
        }
        bool exists = Directory.Exists(path);
        // Resolving an unexisting UNC path may take up to 10 seconds,
        // we need to log what causes the slacking performance.
        if (!exists)
          ServiceScope.Get<ILogger>().Debug("FileEventNotifier tries to connect to \"{0}\", but it appears like "
            + "the specified path doesn't exist. This may result in a noticeable performance drop.", path);
        return exists;
      }
      // Else: it's no UNC path, using Directory.Exist should be save and fast.
      return Directory.Exists(path);
    }

    #endregion

    #region Private EventHandlers

    /// <summary>
    /// Handles all events comming from the _fileSystemWatcher.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FileSystemEventHandler(object sender, FileSystemEventArgs e)
    {
      lock (_events)
        _events.Add(new FileWatchEvent(e));
    }

    /// <summary>
    /// Handles errors from the _fileSystemWatcher.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ErrorEventHandler(object sender, ErrorEventArgs e)
    {
      ServiceScope.Get<ILogger>().Warn("A FileSystemWatcher error has occurred: {0}", e.GetException().Message);
      // We need to create a new instance of the FileSystemWatcher because the old one is now corrupted.
      if (IsPathAvailable(_path))
      {
        ReInitializeWatcher();
      }
      else if (_checkkTimer == null)
      {
        _checkkTimer = new SystemTimer(_msCheckkInterval);
        _checkkTimer.Elapsed += CheckTimer_Elapsed;
        _checkkTimer.Enabled = true;
      }
      else
      {
        _checkkTimer.Enabled = true;
      }
    }

    /// <summary>
    /// Filters all received events, and raises the waiting ones.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NotifyTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      Thread.CurrentThread.Name = "FileWatcher - Raising Events";
      // We don't fire the events inside the lock. We will queue them here until the code exits the locks.
      Queue<FileWatchEvent> eventsToBeFired = null;
      if (Monitor.TryEnter(_syncRoot))
      {
        // Only one thread at a time is processing the events                
        try
        {
          eventsToBeFired = new Queue<FileWatchEvent>();
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
          Monitor.Exit(_syncRoot);
        }
      }
      // else - this timer event was skipped, processing will happen during the next timer event
      // Now fire all the events if any events are in eventsToBeFired
      if (eventsToBeFired != null)
        RaiseEvents(eventsToBeFired);
    }

    /// <summary>
    /// Checks if the location is available,
    /// and raises events if the status has changed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void CheckTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      // Disable the timer to avoid simultaneous checks.
      _checkkTimer.Enabled = false;
      // Set the current threads name for logging purpose.
      Thread.CurrentThread.Name = "FileWatcher - CheckPath";
      // Check if the watched path still exists.
      bool pathAvailable = IsPathAvailable(_path);
      // Is the set state still up to date?
      if (_isPathAvailable != pathAvailable)
      {
        _isPathAvailable = pathAvailable;
        if (pathAvailable)
          // Path was unavailable and is now available again, we'll have to restore.
          ReEnableWatch();
        else
          // Path was available, and is now unavailable, we'll have to handle the lost watch.
          NotifyDisabledWatch();
      }
      // Maybe we missed the path going offline and then quickly going online again,
      // this means that the FileSystemWatcher stopped watching without us noticing it.
      else if (!_fileSystemWatcher.EnableRaisingEvents)
      {
        NotifyDisabledWatch();  // Send a notification that the path is offline
        Thread.Sleep(100);  // Give subscribers a chance to alter their data (they are notified in different threads)
        ReEnableWatch();     // Now get it to watch again
      }
      // Enable the timer and wait for the next check
      if (!_isDisposed)
        _checkkTimer.Enabled = true;
    }

    /// <summary>
    /// Provides delayed initialization of the current FileWatcher.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void EnableTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      // Disable the timer, checking the path might take some time.
      _enableTimer.Enabled = false;
      // Set the current threads name for logging purpose.
      Thread.CurrentThread.Name = "FileWatcher - EnableAttempt";
      if (_subscriptions.Count == 0)
      {
        // No need to initialize, no subscriptions.
        Dispose();
      }
      else if (IsPathAvailable(_subscriptions[0].Path))
      {
        // Don't need the timer anymore.
        _enableTimer.Dispose();
        _enableTimer = null;
        // Now we can initialize all resources.
        _fileSystemWatcher = new FileSystemWatcher(_subscriptions[0].Path);
        Initialize();
        // Add all waiting subscriptions with the Add() method.
        lock (_subscriptions)
        {
          IList<FileWatcherInfo> subscriptions = _subscriptions;
          _subscriptions = new List<FileWatcherInfo>(subscriptions.Count);
          foreach (FileWatcherInfo subscription in subscriptions)
            Add(subscription);
        }
        // Notify subscriptions of the enabled watch.
        Queue<FileWatchEvent> events = new Queue<FileWatchEvent>(1);
        events.Enqueue(new FileWatchEvent(FileWatchChangeType.Enabled, _path));
        RaiseEvents(events);
      }
      else
      {
        // We'll have to check again later.
        _enableTimer.Enabled = true;
      }
    }

    #endregion

    #region Private Structs

    /// <summary>
    /// EventData contains all data needed to raise an event.
    /// Contains the affected FileWatcherInfo and the FileWatchEventArgs
    /// </summary>
    private struct EventData
    {

      #region Variables

      private FileWatchInfo _fileWatchInfo;
      private IFileWatchEventArgs _fileWatchEventArgs;

      #endregion

      #region Properties

      /// <summary>
      /// Gets the FileWatcherInfo to report to.
      /// </summary>
      public FileWatchInfo Info
      {
        get { return _fileWatchInfo; }
      }

      /// <summary>
      /// Gets the extra arguments regarding the event.
      /// </summary>
      public IFileWatchEventArgs Args
      {
        get { return _fileWatchEventArgs; }
      }

      #endregion

      #region Constructors

      /// <summary>
      /// Initializes a new instance of EventData which holds an instance of FileWatcherInfo
      /// and an instance of FileWatchEventArgs.
      /// </summary>
      /// <param name="fileWatchInfo"></param>
      /// <param name="fileWatchEventArgs"></param>
      public EventData(FileWatchInfo fileWatchInfo, IFileWatchEventArgs fileWatchEventArgs)
      {
        _fileWatchInfo = fileWatchInfo;
        _fileWatchEventArgs = fileWatchEventArgs;
      }

      #endregion

    }

    #endregion

  }
}
