#region Copyright (C) 2007-2015 Team MediaPortal

/*
    Copyright (C) 2007-2015 Team MediaPortal
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
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items
{
  public class VirtualCard : IVirtualCard
  {
    private static XmlSerializer _xmlSerializer;

    #region IVirtualCard Member

    public int BitRateMode { get; set; }
    public string ChannelName { get; set; }
    public string Device { get; set; }
    public bool Enabled { get; set; }
    public int GetTimeshiftStoppedReason { get; set; }
    public bool GrabTeletext { get; set; }
    public bool HasTeletext { get; set; }
    public int Id { get; set; }
    public int ChannelId { get; set; }
    public bool IsGrabbingEpg { get; set; }
    public bool IsRecording { get; set; }
    public bool IsScanning { get; set; }
    public bool IsScrambled { get; set; }
    public bool IsTimeShifting { get; set; }
    public bool IsTunerLocked { get; set; }
    public int MaxChannel { get; set; }
    public int MinChannel { get; set; }
    public string Name { get; set; }
    public int QualityType { get; set; }
    public string RecordingFileName { get; set; }
    public string RecordingFolder { get; set; }
    public int RecordingFormat { get; set; }
    public int RecordingScheduleId { get; set; }
    public DateTime RecordingStarted { get; set; }
    public string RemoteServer { get; set; }
    public string RTSPUrl { get; set; }
    public int SignalLevel { get; set; }
    public int SignalQuality { get; set; }
    public string TimeShiftFileName { get; set; }
    public string TimeShiftFolder { get; set; }
    public DateTime TimeShiftStarted { get; set; }
    public SlimTvCardType Type { get; set; }
    public IUser User { get; set; }

    #endregion

    /// <summary>
    /// Serializes this Channel instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a VirtualCard instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static VirtualCard Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as VirtualCard;
    }

    /// <summary>
    /// Deserializes a VirtualCard instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized user profile instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static VirtualCard Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as VirtualCard;
    }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(VirtualCard)));
    }
  }
}