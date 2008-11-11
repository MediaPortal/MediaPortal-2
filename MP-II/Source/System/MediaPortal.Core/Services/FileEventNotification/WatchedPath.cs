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
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Timers;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using SystemTimer = System.Timers.Timer;

namespace MediaPortal.Core.Services.FileEventNotification
{

  /// <summary>
  /// Represents a path that's being watched by a FileWatcher.
  /// WatchedPath performs periodic checks (polling) on a path's availability,
  /// and raises an event when the path's state changes.
  /// </summary>
  class WatchedPath : IDisposable
  {

    #region Constants

    /// <summary>
    /// The maximum interval between different polls.
    /// </summary>
    private const int PollInterval = 1500;

    #endregion

    #region Variables

    /// <summary>
    /// Indicates whether the path was available when last checked.
    /// </summary>
    private bool _available;
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
    /// The timer used to poll the path, to know if it is available.
    /// </summary>
    private SystemTimer _pollTimer;
    /// <summary>
    /// Object used for synchronization of the _pollTimer.
    /// </summary>
    private readonly object _syncPoll;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the watched path.
    /// </summary>
    public DirectoryInfo Path
    {
      get { return _path; }
      set
      {
        lock (_syncPoll)
        {
          _path = value;
          _serverAddress = null;
          _available = CheckPathAvailability();
        }
      }
    }

    /// <summary>
    /// Gets whether the path was available when last checked.
    /// </summary>
    public bool Available
    {
      get { return _available; }
    }

    /// <summary>
    /// Raised when the path's state changes.
    /// </summary>
    public event PathStateChangedEvent PathStateChangedEvent;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of WatchedPath.
    /// </summary>
    /// <param name="path">The path to watch.</param>
    public WatchedPath(string path)
      : this(path, false) {}

    /// <summary>
    /// Initializes a new instance of WatchedPath.
    /// </summary>
    /// <param name="path">The path to watch.</param>
    /// <param name="checkPathNow">True if the path's state should be synchronously checked during initialization.</param>
    public WatchedPath(string path, bool checkPathNow)
    {
      _path = new DirectoryInfo(path);
      _syncPoll = new object();
      if (checkPathNow)
        _available = CheckPathAvailability();
      _pollTimer = new SystemTimer(PollInterval);
      _pollTimer.Elapsed += PollTimer_Elapsed;
      _pollTimer.Enabled = true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns whether the watched path is available.
    /// </summary>
    /// <returns></returns>
    public bool IsPathAvailable()
    {
      Monitor.Enter(_syncPoll);
      bool callEvent;
      try
      {
        bool available = CheckPathAvailability();
        // We want to call the PathStateChangedEvent if the old state doesn't equal the current state
        callEvent = available != _available;
        _available = available;
      }
      finally
      {
        Monitor.Exit(_syncPoll);
      }
      if (callEvent && PathStateChangedEvent != null)
        PathStateChangedEvent(this, _available);
      return _available;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns whether the watched path is available at the current time.
    /// </summary>
    /// <remarks>
    /// We can't use DirectoryInfo.Exists or Directory.Exists(string),
    /// in some networking-related cases it's slow (hangs for 10 secs and more)
    /// and I (SMa) have one case where Directory.Exists threw a Win32Exception after a 40 second hang.
    /// ^ That's why there's a try-catch around <code>_path.Exists</code>.
    /// </remarks>
    /// <returns>True if the watched path is available, false otherwise.</returns>
    private bool CheckPathAvailability()
    {
      // We might save time by first checking if the path's server is available.
      if (!IsServerAvailable())
        return false;
      try
      {
        return _path.Exists;
      }
      catch (Exception e)
      {
        // Directory.Exists() might throw undocumented exceptions in .NET 2.0 (and maybe also later versions)
        ServiceScope.Get<ILogger>().Warn("FileEventNotifier encountered an exception for path \"{0}\"", e, _path.FullName);
        return false;
      }
    }

    /// <summary>
    /// Returns whether the path's server is available.
    /// </summary>
    /// <returns>True if the path's server is available, otherwise false.</returns>
    private bool IsServerAvailable()
    {
      if (!_path.FullName.StartsWith(@"\\"))
        return true;  // The path is not a UNC path, no way to know if its server is available.
      try
      {
        // Ping the host to see if its online.
        if (_serverAddress == null || !Ping(_serverAddress))
        {
          // There is no last known IP address for the server,
          // or the last known IP address doesn't reply the ICMP packet.
          // Get the servers hostname
          string host = _path.FullName.Substring(2); // Remove the "\\" in front of the hostname.
          int index = host.IndexOf(@"\"); // Find the next "\" to extract the hostname.
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
      return true;
    }

    /// <summary>
    /// Eventhandler for _pollTimer.
    /// Checks the path's availability and raises the PathStateChangedEvent if
    /// the availability doesn't match the last known state.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PollTimer_Elapsed(object sender, ElapsedEventArgs e)
    {
      if (!Monitor.TryEnter(_syncPoll))
        return; // Another thread is updating the path's availability.
      bool callEvent;
      try
      {
        bool available = CheckPathAvailability();
        callEvent = available != _available;
        _available = available;
      }
      finally
      {
        Monitor.Exit(_syncPoll);
      }
      if (callEvent && PathStateChangedEvent != null)
        PathStateChangedEvent(this, _available);
    }

    /// <summary>
    /// Returns whether a ping to the given IPAddress succeeded.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="NullReferenceException"></exception>
    /// <param name="ipAddress"></param>
    /// <returns></returns>
    private static bool Ping(IPAddress ipAddress)
    {
      bool pingAble = false;
      try
      {
        // ICMP message to an IP-address should be returned in < 5ms on any modern network, we'll timout at 20ms
        PingReply reply = new Ping().Send(ipAddress, 20);
        if (reply != null)
          pingAble = (reply.Status == IPStatus.Success);
      }
      catch (PingException) { }
      return pingAble;
    }

    #endregion

    #region IDisposable Members

    public void Dispose()
    {
      lock (_syncPoll)
      {
        if (_pollTimer != null)
        {
          _pollTimer.Dispose();
          _pollTimer = null;
        }
      }
    }

    #endregion

  }
}