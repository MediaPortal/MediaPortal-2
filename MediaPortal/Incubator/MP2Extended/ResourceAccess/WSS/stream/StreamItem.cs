using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text;
using MediaPortal.Plugins.MP2Extended.Common;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;
using MediaPortal.Plugins.Transcoding.Service.Transcoders.Base;
using MediaPortal.Common.MediaManagement;
using System.Threading;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream
{
  internal class StreamItem
  {
    private const int DEFAULT_TIMEOUT = 5 * 60; // 5 minutes

    private int _idleTimeout;
    private object _busyLock = new object();
    private DateTime _requestTime = DateTime.MinValue;
    private long _requestSegment = 0;
    private object _requestLock = new object();
    
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
    /// If the tuimeout is set to -1 the default timeout is used
    /// </summary>
    internal int IdleTimeout {
      get
      {
        if (_idleTimeout == -1)
          return DEFAULT_TIMEOUT;
        return _idleTimeout;
      }
      set { _idleTimeout = value; } }

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
    internal object BusyLock { get { return _busyLock; } }

    /// <summary>
    /// Gets or sets the type of stream item
    /// </summary>
    internal WebMediaType ItemType { get; set; }

    /// <summary>
    /// Gets or sets the time when the stream was started
    /// </summary>
    internal DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the IP of the Client, which started the stream
    /// </summary>
    internal string ClientIp { get; set; }

    /// <summary>
    /// Gets or sets whether a stream is currently in progress
    /// </summary>
    internal bool IsActive { get; set; }

    /// <summary>
    /// Gets or sets the transcoding context used by this stream
    /// </summary>
    internal TranscodeContext StreamContext { get; set; }

    internal bool RequestSegment(long Segment)
    {
      lock (_requestLock)
      {
        if(_requestTime < DateTime.Now)
        {
          _requestTime = DateTime.Now;
          _requestSegment = Segment;
        }
      }
      //Allow multiple requests to die out so only the last request is used
      Thread.Sleep(2000);
      if (Segment == _requestSegment) return true;
      return false;
    }

    /// <summary>
    /// Constructor, sets for example the start time
    /// </summary>
    internal StreamItem()
    {
      StartTime = DateTime.Now;
    }
  }
}
