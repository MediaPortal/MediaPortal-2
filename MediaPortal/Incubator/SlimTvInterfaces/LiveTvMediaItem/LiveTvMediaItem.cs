using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem
{
  public class LiveTvMediaItem : MediaItem, IXmlSerializable
  {
    public const string SLOT_INDEX = "SlotIndex";
    public const string CHANNEL = "Channel";
    public const string CURRENT_PROGRAM = "CurrentProgram";
    public const string NEXT_PROGRAM = "NextProgram";
    public const string TUNING_TIME = "TuningTime";

    public const string MIME_TYPE_TV = "video/livetv";
    public const string MIME_TYPE_RADIO = "audio/radio";

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

    private IList<ITimeshiftContext> _timeshiftContexes = new List<ITimeshiftContext>();
    private Dictionary _additionalProperties = new Dictionary();

    XmlSchema IXmlSerializable.GetSchema()
    {
      return null;
    }

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
      try
      {
        // First read attributes, then check for empty start element
        if (!reader.MoveToAttribute("Id"))
          throw new ArgumentException("Id attribute not present");
        _id = new Guid(reader.Value);
        if (reader.IsEmptyElement)
          return;
      }
      finally
      {
        reader.ReadStartElement();
      }

      _additionalProperties = reader.DeserializeXml<Dictionary>();
      _timeshiftContexes = reader.DeserializeXml<List<ITimeshiftContext>>();


      while (reader.NodeType != XmlNodeType.EndElement)
      {
        MediaItemAspect mia = MediaItemAspect.Deserialize(reader);
        _aspects[mia.Metadata.AspectId] = mia;
      }
      reader.ReadEndElement(); // MI
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
      writer.WriteAttributeString("Id", _id.ToString("D"));
      // Additional properties
      _additionalProperties.SerializeXml(writer);
      // Timeshift contexes
      _timeshiftContexes.SerializeXml(writer);

      foreach (MediaItemAspect mia in _aspects.Values)
        mia.Serialize(writer);
    }

    public new void Serialize(XmlWriter writer)
    {
      writer.WriteStartElement("MI"); // MediaItem
      ((IXmlSerializable) this).WriteXml(writer);
      writer.WriteEndElement(); // MediaItem
    }

    public new static LiveTvMediaItem Deserialize(XmlReader reader)
    {
      LiveTvMediaItem result = new LiveTvMediaItem(Guid.Empty);
      ((IXmlSerializable) result).ReadXml(reader);
      return result;
    }
  }
}
