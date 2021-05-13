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
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items
{
  public class ScheduleRule : IScheduleRule
  {
    private static XmlSerializer _xmlSerializer;

    private IList<IScheduleRuleTarget> _targets = new List<IScheduleRuleTarget>();

    #region IScheduleRule Member

    public int RuleId { get; set; }
    public string Name { get; set; }
    public bool Active { get; set; }
    [XmlIgnore]
    public IList<IScheduleRuleTarget> Targets
    {
      get => _targets;
      set => _targets = value;
    }
    public int? ChannelGroupId { get; set; }
    public int? ChannelId { get; set; }
    public bool IsSeries { get; set; }
    public string SeriesName { get; set; }
    public string SeasonNumber { get; set; }
    public string EpisodeNumber { get; set; }
    public string EpisodeTitle { get; set; }
    public string EpisodeInfoFallback { get; set; }
    public RuleEpisodeInfoFallback EpisodeInfoFallbackType { get; set; }
    public DateTime? StartFromTime { get; set; }
    public DateTime? StartToTime { get; set; }
    public DayOfWeek? StartOnOrAfterDay { get; set; }
    public DayOfWeek? StartOnOrBeforeDay { get; set; }
    public PriorityType Priority { get; set; }
    public RuleRecordingType RecordingType { get; set; }
    public TimeSpan PreRecordInterval { get; set; }
    public TimeSpan PostRecordInterval { get; set; }
    public KeepMethodType KeepMethod { get; set; }
    public DateTime? KeepDate { get; set; }

    #endregion

    public List<ScheduleRuleTarget> XML_Targets
    {
      get
      {
        var list = new List<ScheduleRuleTarget>();
        foreach (var target in _targets)
        {
          list.Add(new ScheduleRuleTarget
          {
            SearchText = target.SearchText,
            SearchMatch = target.SearchMatch,
            SearchTarget = target.SearchTarget
          });
        }
        return list;
      }
      set
      {
        _targets.Clear();
        foreach (var target in value)
          _targets.Add(target);
      }
    }

    /// <summary>
    /// Serializes this Schedule rule instance to the given <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">Writer to write the XML serialization to.</param>
    public void Serialize(XmlWriter writer)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      xs.Serialize(writer, this);
    }

    /// <summary>
    /// Deserializes a Schedule rule instance from a given XML fragment.
    /// </summary>
    /// <param name="str">XML fragment containing a serialized Schedule rule instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static ScheduleRule Deserialize(string str)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      using (StringReader reader = new StringReader(str))
        return xs.Deserialize(reader) as ScheduleRule;
    }

    /// <summary>
    /// Deserializes a Schedule rule instance from a given <paramref name="reader"/>.
    /// </summary>
    /// <param name="reader">XML reader containing a serialized Schedule rule instance.</param>
    /// <returns>Deserialized instance.</returns>
    public static ScheduleRule Deserialize(XmlReader reader)
    {
      XmlSerializer xs = GetOrCreateXMLSerializer();
      return xs.Deserialize(reader) as ScheduleRule;
    }

    protected static XmlSerializer GetOrCreateXMLSerializer()
    {
      return _xmlSerializer ?? (_xmlSerializer = new XmlSerializer(typeof(ScheduleRule)));
    }
  }
}
