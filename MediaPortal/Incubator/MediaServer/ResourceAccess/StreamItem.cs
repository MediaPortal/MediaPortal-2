#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Extensions.MediaServer.DLNA;
using MediaPortal.Extensions.TranscodingService.Interfaces.Transcoding;

namespace MediaPortal.Extensions.MediaServer.ResourceAccess
{
  public class StreamItem
  {
    private SemaphoreSlim _busyLock = new SemaphoreSlim(1, 1);

    public StreamItem(string clientIp)
    {
      ClientIp = clientIp;
    }

    /// <summary>
    /// Gets or sets the requested MediaItem
    /// </summary>
    public Guid RequestedMediaItem { get; set; }

    /// <summary>
    /// Gets or sets the title of the requested item
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Gets or sets the transcode object used to setup transcoding
    /// </summary>
    public DlnaMediaItem TranscoderObject { get; set; }

    /// <summary>
    /// Gets a lock for indicating that files are in use
    /// </summary>
    public SemaphoreSlim BusyLock { get { return _busyLock; } }

    /// <summary>
    /// Gets whether the stream is live
    /// </summary>
    public bool IsLive
    {
      get => LiveChannelId > 0;
    }

    /// <summary>
    /// Gets or sets the live Channel ID
    /// </summary>
    public int LiveChannelId { get; set; }

    /// <summary>
    /// Gets or sets the time when the stream was started
    /// </summary>
    public DateTime StartTimeUtc { get; }

    /// <summary>
    /// Gets or sets the IP of the Client, which started the stream
    /// </summary>
    public string ClientIp { get; }

    /// <summary>
    /// Gets or sets whether a stream is currently in progress
    /// </summary>
    public bool IsActive
    {
      get
      {
        if (StreamContext == null)
          return false;
        if (StreamContext is TranscodeContext context)
          return context.InUse;
        return false;
      }
    }

    /// <summary>
    /// Gets or sets the stream context currently streaming
    /// </summary>
    public StreamContext StreamContext { get; set; }

    /// <summary>
    /// Constructor, sets for example the start time
    /// </summary>
    public StreamItem()
    {
      StartTimeUtc = DateTime.UtcNow;
    }
  }
}
