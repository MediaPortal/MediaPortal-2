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
using System.Threading;
using System.Threading.Tasks;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;
using MediaPortal.Plugins.MP2Extended.Utils;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream
{
  internal class StreamItem
  {
    private const int DEFAULT_TIMEOUT = 5 * 60; // 5 minutes

    private int _idleTimeout;
    private readonly SemaphoreSlim _busyLock = new SemaphoreSlim(1);
    private readonly object _requestLock = new object();
    private DateTime _requestTime = DateTime.MinValue;
    private long _requestSegment = 0;

    /// <summary>
    /// Gets or sets the requested MediaItem
    /// </summary>
    internal MediaItem RequestedMediaItem { get; set; }

    /// <summary>
    /// Gets or sets the title of the requested item
    /// </summary>
    internal string Title { get; set; }

    /// <summary>
    /// Gets or sets a description of the Client
    /// </summary>
    internal string ClientDescription { get; set; }

    /// <summary>
    /// Gets or sets the Idle timeout in seconds.
    /// If the timeout is set to -1 the default timeout is used
    /// </summary>
    internal int IdleTimeout
    {
      get
      {
        if (_idleTimeout == -1)
          return DEFAULT_TIMEOUT;
        return _idleTimeout;
      }
      set { _idleTimeout = value; }
    }

    /// <summary>
    /// Gets or sets the time of the last activity on this <see cref="StreamItem"/>.
    /// Used to determine whether to stop the stream if idle for longer than <see cref="IdleTimeout"/>.
    /// </summary>
    internal DateTime LastActivityTime { get; set; }

    /// <summary>
    /// Gets or sets the profile which is used for streaming
    /// </summary>
    internal EndPointProfile Profile { get; set; }

    /// <summary>
    /// Gets or sets the transcode object used to setup transcoding
    /// </summary>
    internal ProfileMediaItem TranscoderObject { get; set; }

    /// <summary>
    /// Gets or sets the position from which the streaming should start
    /// </summary>
    internal long StartPosition { get; set; }

    /// <summary>
    /// Gets a lock for indicating that files are in use
    /// </summary>
    internal SemaphoreSlim BusyLock { get { return _busyLock; } }

    /// <summary>
    /// Waits to enter the busy lock and returns an IDisposable that will
    /// update the <see cref="LastActivityTime"/> and release the lock
    /// when disposed.
    /// </summary>
    /// <returns><see cref="IDisposable"/> that releases the lock when disposed.</returns>
    internal Task<IDisposable> RequestBusyLockAsync()
    {
      return RequestBusyLockAsync(CancellationToken.None);
    }

    /// <summary>
    /// Waits to enter the busy lock and returns an IDisposable that will
    /// update the <see cref="LastActivityTime"/> and release the lock
    /// when disposed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to use to cancel waiting.</param>
    /// <returns><see cref="IDisposable"/> that releases the lock when disposed.</returns>
    internal async Task<IDisposable> RequestBusyLockAsync(CancellationToken cancellationToken)
    {
      await _busyLock.WaitAsync(cancellationToken);
      try
      {
        SemaphoreReleaser releaser = new SemaphoreReleaser(_busyLock);
        // Set the last activity time to just before the busy lock is released
        releaser.Releasing += (s, e) => LastActivityTime = DateTime.Now;
        return releaser;
      }
      catch
      {
        // Shouldn't ever happen, but just in case,
        // if there's an error release here
        _busyLock.Release();
        throw;
      }
    }

    /// <summary>
    /// Gets or sets the type of stream item
    /// </summary>
    internal WebMediaType ItemType { get; set; }

    /// <summary>
    /// Gets whether the stream is live
    /// </summary>
    internal bool IsLive
    {
      get
      {
        if (ItemType == Common.WebMediaType.TV || ItemType == Common.WebMediaType.Radio)
        {
          return true;
        }
        return false;
      }
    }

    /// <summary>
    /// Gets or sets the live Channel ID
    /// </summary>
    internal int LiveChannelId { get; set; }

    /// <summary>
    /// Gets or sets the time when the stream was started
    /// </summary>
    internal DateTime StartTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the IP of the Client, which started the stream
    /// </summary>
    internal string ClientIp { get; set; }

    /// <summary>
    /// Gets or sets whether a stream is currently in progress
    /// </summary>
    internal bool IsActive
    {
      get
      {
        if (StreamContext is TranscodeContext context)
          return context.InUse;
        if (StreamContext != null)
          return true;
        return false;
      }
    }

    /// <summary>
    /// Gets or sets the transcoding context used by this stream
    /// </summary>
    internal StreamContext StreamContext { get; set; }

    internal async Task<bool> RequestSegmentAsync(long segment)
    {
      lock (_requestLock)
      {
        if (_requestTime < DateTime.Now)
        {
          _requestTime = DateTime.Now;
          _requestSegment = segment;
        }
      }

      //Allow multiple requests to die out so only the last request is used
      await Task.Delay(2000);
      if (segment == _requestSegment)
        return true;

      return false;
    }

    /// <summary>
    /// Constructor, sets for example the start time
    /// </summary>
    internal StreamItem()
    {
      StartTimeUtc = DateTime.UtcNow;
    }
  }
}
