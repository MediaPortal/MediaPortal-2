#region Copyright (C) 2007-2018 Team MediaPortal

/*
    Copyright (C) 2007-2018 Team MediaPortal
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
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items
{
  public class Schedule : ISchedule
  {
    private static XmlSerializer _xmlSerializer;

    #region ISchedule Member

    public int ScheduleId { get; set; }
    public int? ParentScheduleId { get; set; }
    public int ChannelId { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ScheduleRecordingType RecordingType { get; set; }
    public PriorityType Priority { get; set; }
    public TimeSpan PreRecordInterval { get; set; }
    public TimeSpan PostRecordInterval { get; set; }
    public KeepMethodType KeepMethod { get; set; }
    public DateTime? KeepDate { get; set; }
    public bool IsSeries { get { return RecordingType != ScheduleRecordingType.Once; } }

    #endregion

    /// <summary>
    /// Serializes this Schedule instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a Schedule instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized Schedule instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static Schedule Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as Schedule;
    }

    /// <summary>
    /// Deserializes a Schedule instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized Schedule instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static Schedule Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as Schedule;
    }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(Schedule)));
    }
  }
}