using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem
{
  public class LiveTvMediaItem: MediaItem
  {
    public const string SLOT_INDEX = "SlotIndex";
    public const string CHANNEL = "Channel";
    public const string CURRENT_PROGRAM = "CurrentProgram";
    public const string NEXT_PROGRAM = "NextProgram";
    public const string TUNING_TIME = "TuningTime";

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

    /// <summary>
    /// Gets a list of timeshift contexes.
    /// </summary>
    public IList<ITimeshiftContext> TimeshiftContexes
    {
      get { return _timeshiftContexes; }
    }

    private readonly IList<ITimeshiftContext> _timeshiftContexes = new List<ITimeshiftContext>();
    private readonly Dictionary<string, object> _additionalProperties = new Dictionary<string, object>();

    public static LiveTvMediaItem Deserialize(XmlReader reader)
    {
      LiveTvMediaItem result = new LiveTvMediaItem(Guid.Empty);
      ((IXmlSerializable) result).ReadXml(reader);
      return result;
    }
  }
}
