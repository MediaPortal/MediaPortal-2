#region Copyright (C) 2007-2017 Team MediaPortal

/*
    Copyright (C) 2007-2017 Team MediaPortal
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

    public const string MIME_TYPE_TV = "slimtv/livetv";
    public const string MIME_TYPE_RADIO = "slimtv/radio";
    public const string MIME_TYPE_WTVREC = "slimtv/wtv";

    public LiveTvMediaItem(Guid mediaItemId)
      : base(mediaItemId)
    {}
    public LiveTvMediaItem(Guid mediaItemId, IDictionary<Guid, IList<MediaItemAspect>> aspects)
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
        _aspects[mia.Metadata.AspectId] = new []{ mia };
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

      foreach (IList<MediaItemAspect> list in _aspects.Values)
        foreach (MediaItemAspect mia in list)
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
