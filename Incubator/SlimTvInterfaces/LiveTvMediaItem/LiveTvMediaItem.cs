using System;
using System.Collections.Generic;
using MediaPortal.Core.MediaManagement;

namespace MediaPortal.Plugins.SlimTvClient.Interfaces.LiveTvMediaItem
{
  public class LiveTvMediaItem: MediaItem
  {
    public const string CHANNEL = "Channel";
    public const string CURRENT_PROGRAM = "CurrentProgram";
    public const string NEXT_PROGRAM = "NextProgram";

    public LiveTvMediaItem(Guid mediaItemId)
      : base(mediaItemId)
    {}
    public LiveTvMediaItem(Guid mediaItemId, IDictionary<Guid, MediaItemAspect> aspects)
      : base(mediaItemId, aspects)
    { }

    /// <summary>
    /// Gets a dictionary of additional properties. They are used to store dynamic information that gets not added to MediaLibrary.
    /// </summary>
    public IDictionary<string, object> AdditionalProperties
    { 
      get { return _additionalProperties; } 
    }

    private readonly Dictionary<string, object> _additionalProperties = new Dictionary<string, object>();

  }
}
