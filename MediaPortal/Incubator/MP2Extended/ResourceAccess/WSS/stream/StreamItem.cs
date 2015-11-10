using System;
using MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.Profiles;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.WSS.stream
{
  internal class StreamItem
  {
    private const int DEFAULT_TIMEOUT = 5 * 60; // 5 minutes

    private int _idleTimeout;
    
    /// <summary>
    /// Gets or sets the GUID of the requeste Media Item
    /// </summary>
    internal Guid ItemId { get; set; }

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

    internal ProfileMediaItem TranscoderObject { get; set; }

    /// <summary>
    /// Gets or sets the position from which the streaming should start
    /// </summary>
    internal long StartPosition { get; set; }

    /// <summary>
    /// Gets or sets the audio stream to use for streaming
    /// </summary>
    internal int AudioStream { get; set; }

    /// <summary>
    /// Gets or sets the subtitle stream to use for streaming
    /// </summary>
    internal int SubtitleStream { get; set; }

    /// <summary>
    /// Gets or sets the time when the stream was started
    /// </summary>
    internal DateTime StartTime { get; set; }

    /// <summary>
    /// Gets or sets the IP of the Client, which started the stream
    /// </summary>
    internal string ClientIp { get; set; }

    /// <summary>
    /// Constructor, sets for example the start time
    /// </summary>
    internal StreamItem()
    {
      StartTime = DateTime.Now;
    }
  }
}
