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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Common.UserProfileDataManagement
{
  #region Helper class 

  [DebuggerDisplay("[{CountUsed}] - {Name}")]
  public class NameCount
  {
    [XmlAttribute("n")]
    public string Name { get; set; }
    [XmlAttribute("c")]
    public int CountUsed { get; set; }
  }

  [DebuggerDisplay("[{TimeUsed}] - {Name}")]
  public class NameTime
  {
    [XmlAttribute("n")]
    public string Name { get; set; }
    [XmlAttribute("t")]
    public DateTime TimeUsed { get; set; }
  }

  #endregion

  [XmlRoot("ul")]
  [Serializable]
  public class UsageStatisticsList : List<UsageStatistics>
  {

    private static readonly XmlSerializer _serializer = new XmlSerializer(typeof(UsageStatisticsList));

    public static UsageStatisticsList Deserialize(string serializedStatistics)
    {
      using (TextReader reader = new StringReader(serializedStatistics))
        return (UsageStatisticsList)_serializer.Deserialize(reader);
    }

    public static string Serialize(UsageStatisticsList statistics, int maxEntries = UsageStatistics.MAX_STORED_ENTRIES)
    {
      XmlWriterSettings settings = new XmlWriterSettings { OmitXmlDeclaration = true };
      XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
      ns.Add("", "");
      StringBuilder sb = new StringBuilder();
      using (TextWriter sw = new StringWriter(sb))
      using (XmlWriter xw = XmlWriter.Create(sw, settings))
        _serializer.Serialize(xw, statistics.LimitEntries(), ns);
      return sb.ToString();
    }

    public UsageStatisticsList LimitEntries(int maxEntries = UsageStatistics.MAX_STORED_ENTRIES)
    {
      var limited = new UsageStatisticsList();
      limited.AddRange(this.Select(s => s.LimitEntries(maxEntries)));
      return limited;
    }

    public void SetUsed(string scope, string usedItem)
    {
      var stat = this.FirstOrDefault(s => s.Scope == scope);
      if (stat == null)
      {
        stat = new UsageStatistics { Scope = scope };
        Add(stat);
      }
      stat.SetUsed(usedItem);
    }

    public static string SetUsed(string serialized, string scope, string usedItem)
    {
      var stats = !string.IsNullOrEmpty(serialized) ? Deserialize(serialized) : new UsageStatisticsList();
      stats.SetUsed(scope, usedItem);
      return Serialize(stats);
    }
  }

  [XmlRoot("u")]
  [Serializable]
  public class UsageStatistics
  {
    public const int MAX_STORED_ENTRIES = 20;
    public const int MAX_RETURNED_ENTRIES = 6;

    [XmlAttribute("s")]
    public string Scope { get; set; }
    [XmlElement("t")]
    public List<NameCount> TopUsed { get; set; } = new List<NameCount>();
    [XmlElement("l")]
    public List<NameTime> LastUsed { get; set; } = new List<NameTime>();

    public UsageStatistics LimitEntries(int maxEntries = MAX_STORED_ENTRIES)
    {
      return new UsageStatistics
      {
        Scope = Scope,
        TopUsed = TopUsed.OrderByDescending(e => e.CountUsed).Take(maxEntries).ToList(),
        LastUsed = LastUsed.OrderByDescending(e => e.TimeUsed).Take(maxEntries).ToList()
      };
    }

    public void SetUsed(string usedItem)
    {
      // Part 1: stats by usage counter
      var nameCounts = TopUsed?.ToList() ?? new List<NameCount>();
      var existingEntry = nameCounts.FirstOrDefault(e => e.Name == usedItem);
      if (existingEntry != null)
      {
        existingEntry.CountUsed++;
      }
      else
      {
        // Limit list of stored items
        if (nameCounts.Count >= MAX_STORED_ENTRIES)
          nameCounts.Remove(nameCounts.OrderByDescending(e => e.CountUsed).Last());

        nameCounts.Add(new NameCount { CountUsed = 1, Name = usedItem });
      }
      TopUsed = nameCounts;

      // Part 2: stats by usage counter
      var nameTime = LastUsed?.ToList() ?? new List<NameTime>();
      var existingTimeEntry = nameTime.FirstOrDefault(e => e.Name == usedItem);
      if (existingEntry != null)
        existingTimeEntry.TimeUsed = DateTime.Now;
      else
        nameTime.Insert(0, new NameTime { TimeUsed = DateTime.Now, Name = usedItem });

      LastUsed = nameTime;
    }
  }
}
