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
using System.Net.NetworkInformation;
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

    #region Variables

    /// <summary>
    /// The path to watch.
    /// </summary>
    private DirectoryInfo _path;
    /// <summary>
    /// The IP address of the server containing the path.
    /// (used if the path a UNC)
    /// </summary>
    private IPAddress _serverAddress;
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
    private IList<FileWatcherInfo> _subscriptions;
    /// <summary>
    /// All events received within one _serverTimer interval.
    /// </summary>
    private IList<FileWatchEvent> _events;
    /// <summary>
    /// The timer used to poll the path, to know if it is available.
    /// </summary>
    private SystemTimer _pollTimer;
    /// <summary>
    /// Timer to periodically check if any events are received.
    /// </summary>
    private SystemTimer _notifyTimer;
    /// <summary>
    /// Time to wait between different checks for events.
    /// (milliseconds)
    /// </summary>
    private int _msConsolidationInterval;
    /// <summary>
    /// Time to wait between the different polls.
    /// (in milliseconds)
    /// </summary>
    private int _msPollInterval;
    /// <summary>
    /// Object used for synchronization of the _notifyTimer.
    /// </summary>
    private object _syncNotify;
    /// <summary>
    /// Object used for synchronization of the _pollTimer.
    /// </summary>
    private object _syncPoll;

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

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the FileWatcher class, given the path to watch.
    /// </summary>
    /// <exception cref="NotSupportedDriveTypeException">
    /// The drive type of the specified path is not supported by the system.
    /// </exception>
    /// <exception cref="NullReferenceException">
    /// The parameter \"path\" is a null reference.
    /// </exception>
    /// <param name="path">The path to watch.</param>
    public FileWatcher(string path)
    {
      if (path == null)
        throw new NullReferenceException("The parameter \"path\" is a null reference.");
      _syncNotify = new object();
      _syncPoll = new object();
      _msConsolidationInterval = 1000;
      _msPollInterval = 1500;
      _path = new DirectoryInfo(path);
      if (!IsValidDriveType(_path.Root.FullName))
        throw new NotSupportedDriveTypeException("The drive type of \"" + _path.Root.FullName + "\" is not supported by the system.");
      _subscriptions = new List<FileWatcherInfo>();
      _events = new List<FileWatchEvent>();
      // Initialize the Component to watch the path's availability.
      InitializePollTimer();
      // _pollTimer is responsible to initialize the service.
    }

    #endregion

    #region Destructor

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
      if (fileWatcherInfo.Path != _path.FullName)
        throw new InvalidFileWatchInfoException("The specified path does not equal the watched path.");
      lock (_subscriptions)
      {
        if (!_subscriptions.Contains(fileWatcherInfo))
        {
          _subscriptions.Add(fileWatcherInfo);
          if (fileWatcherInfo.EventHandler != null)
          {
            FileWatchEventArgs eventArgs =
              new FileWatchEventArgs(_watching ? FileWatchChangeType.Enabled : FileWatchChangeType.Disabled,
                                     fileWatcherInfo.Path);
            RaiseEvent(new EventData(fileWatcherInfo, eventArgs));
          }
        }
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
      _isDisposed = true;
      if (_notifyTimer != null)
        _notifyTimer.Dispose();
      if (_pollTimer != null)
        _pollTimer.Dispose();
      if (_watcher != null)
        _watcher.Dispose();
      if (_events != null)
        _events.Clear();
      _watching = false;
      RaiseEvent(FileWatchChangeType.Disposed);
      if (Disposed != null)
        Dispose();
    }

    #endregion

    #region Initializers

    /// <summary>
    /// Initializes a FileSystemWatcher for the specified path.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <param name="path">Path to watch.</param>
    /// <returns></returns>
    private FileSystemWatcher InitializeWatcher(string path)
    {
      FileSystemWatcher watcher = new FileSystemWatcher(path);
      watcher.Changed += FileSystemEventHandler;
      watcher.Created += FileSystemEventHandler;
      watcher.Deleted += FileSystemEventHandler;
      watcher.Renamed += FileSystemEventHandler;
      watcher.Error += ErrorEventHandler;
      watcher.Disposed += Watcher_Disposed;
      return watcher;
    }

    /// <summary>
    /// Initializes the watch service which will watch the path itself,
    /// so we'll know if/when the path is created and deleted.
    /// </summary>
    private void InitializePollTimer()
    {
      if (_pollTimer == null)
      {
        _pollTimer = new SystemTimer(_msPollInterval);
        _pollTimer.Elapsed += PollTimer_Elapsed;
        _pollTimer.Enabled = true;
      }
      else if (!_pollTimer.Enabled)
      {
        _pollTimer.Enabled = true;
      }
    }

    /// <summary>
    /// Tries to initialize the service.
    /// </summary>
    /// <returns></returns>
    private bool TryInitializeService()
    {
      if (!_watching && IsPathAvailable(_path.FullName))
      {
        try
        {
          _watcher = InitializeWatcher(_path.FullName);
          _watcher.IncludeSubdirectories = true;
          _watcher.EnableRaisingEvents = true;
          _watching = true;
          _notifyTimer = new SystemTimer(_msConsolidationInterval);
          _notifyTimer.Elapsed += NotifyTimer_Elapsed;
          _notifyTimer.Enabled = true;
        }
        catch (Exception)
        {
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
        {
          RaiseEvent(_path.FullName, FileWatchChangeType.Enabled);
          return true;
        }
      }
      return false;
    }

    #endregion

    #region Enable/Disable

    /// <summary>
    /// Enables the watch service.
    /// </summary>
    private void EnableWatch()
    {
      InitializePollTimer();
      if (_watcher == null)
      {
        TryInitializeService();
        return;
      }
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
      RaiseEvent(_path.FullName, FileWatchChangeType.Enabled);
    }

    /// <summary>
    /// Disables the watch service.
    /// </summary>
    private void DisableWatch()
    {
      if (_watcher != null)
      {
        _notifyTimer.Enabled = false;
        _watching = false;
        _watcher.Dispose();
        _watcher = null;
        RaiseEvent(_path.FullName, FileWatchChangeType.Disabled);
      }
    }

    #endregion

    #region PathChecks

    /// <summary>
    /// Returns whether the drive's type is valid.
    /// A valid drive type should be watchable by the FileSystemWatcher.
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool IsValidDriveType(string path)
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
    /// Returns whether the given path is available.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns></returns>
    private bool IsPathAvailable(string path)
    {
      // Is it a UNC path? If so, we'll need to take a different approach to optimize performance.
      if (path.StartsWith(@"\\"))
      {
        // We save a lot of time by pinging the server first.
        try
        {
          // Ping the host to see if its online.
          if (_serverAddress == null || !Ping(_serverAddress))
          {
            // Get the servers hostname
            string host = path.Substring(2); // Remove the "\\" in front of the hostname.
            int index = host.IndexOf(@"\");  // Find the next "\" to extract the hostname.
            if (index != -1)
              host = host.Substring(0, index);
            // Try solve the UNC path by DNS.
            IPHostEntry hostEntry = Dns.GetHostEntry(host);
            bool pingAble = false;
            foreach (IPAddress address in hostEntry.AddressList)
            {
              if (Ping(address))
              {
                _serverAddress = address; // Save the address for faster lookup later.
                pingAble = true;
                break;
              }
            }
            if (!pingAble)
              return false;
          }
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
      // Else: it's no UNC path, using DirectoryInfo.Exist should be save and fast.
      return Directory.Exists(path);
    }

    /// <summary>
    /// Returns whether a ping to the given IPAddress succeeded.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="NullReferenceException"></exception>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    private bool Ping(IPAddress ipAddress)
    {
      bool pingAble;
      try
      {
        // ICMP message should be returned in < 5ms on any modern network, we'll timout at 25ms
        pingAble = (new Ping().Send(ipAddress, 25).Status == IPStatus.Success);
      }
      catch (PingException)
      {
        pingAble = false;
      }
      return pingAble;
    }

    #endregion

    #region EventRaisers

    /// <summary>
    /// Notifies all subscriptions about the given change.
    /// </summary>
    /// <param name="changeType"></param>
    /// <param name="path"></param>
    private void RaiseEvent(string path, FileWatchChangeType changeType)
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
        throw new ArgumentNullException("The parameter \"eventQueue\" is a null reference.");
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

    /// <summary>
    /// Raises an event for the given EventData.
    /// </summary>
    /// <param name="eventData">An instance of EventData.</param>
    private void RaiseEvent(object eventData)
    {
      // Should we give the current Thread a name?
      EventData data = (EventData)eventData;
      if (data.Info.EventHandler != null)
        data.Info.EventHandler(data.Info, data.Args);
    }

    #endregion

    #region FileSystemWatcher EventHandlers

    /// <summary>
    /// Handles all events incomming from the _fileSystemWatcher.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FileSystemEventHandler(object sender, FileSystemEventArgs e)
    {
      FileSystemWatcher fileSystemWatcher = (FileSystemWatcher) sender;
      // Received an event for the watched path?
      if (fileSystemWatcher.Path == _path.FullName && _watching)
      {
        lock (_events)
          _events.Add(new FileWatchEvent(e));
      }
      // Received an event for the parent path, regarding the watched path?
      else if (e.FullPath + "\\" == _path.FullName)
      {
        if ((e.ChangeType & WatcherChangeTypes.Deleted) == WatcherChangeTypes.Deleted)
        {
          DisableWatch();
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
      Monitor.Enter(_syncPoll);
      _watching = false;
      try
      {
        DisableWatch();
      }
      finally
      {
        Monitor.Exit(_syncPoll);
      }
    }

    /// <summary>
    /// Handles the Disposed event for the FileSystemWatchers.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Watcher_Disposed(object sender, EventArgs e)
    {
      _watching = false;
    }

    #endregion

    #region Timer EventHandlers

    /// <summary>
    /// Filters all received events, and raises the waiting ones.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NotifyTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      // Set the current threads name for logging purpose.
      if (Thread.CurrentThread.Name == null)
        Thread.CurrentThread.Name = "FileEventNotifier";
      // We don't fire the events inside the lock. We will queue them here until the code exits the locks.
      Queue<FileWatchEvent> eventsToBeFired = null;
      if (Monitor.TryEnter(_syncNotify))
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
          Monitor.Exit(_syncNotify);
        }
      }
      // else - this timer event was skipped, processing will happen during the next timer event
      // Now fire all the events if any events are in eventsToBeFired
      if (eventsToBeFired != null)
        RaiseEvents(eventsToBeFired);
    }

    /// <summary>
    /// Polls if the location is available,
    /// and handles changes in status.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PollTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      _watching = (_watcher != null && _watcher.EnableRaisingEvents);
      if (Monitor.TryEnter(_syncPoll))
      {
        // IsPathAvailable might take some time,
        // we do this inside of the lock to make sure we don't have dozens of checks at the same time.
        bool pathAvailable = IsPathAvailable(_path.FullName);
        try
        {
          if (!_watching && pathAvailable)
            EnableWatch();
          else if (_watching && !pathAvailable)
            DisableWatch();
        }
        finally
        {
          Monitor.Exit(_syncPoll);
        }
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
