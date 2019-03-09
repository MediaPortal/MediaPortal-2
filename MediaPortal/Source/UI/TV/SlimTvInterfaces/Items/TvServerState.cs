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

using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  /// <summary>
  /// Class used to notify connected clients of the current state of the TV server.
  /// This class must be serializable by the XmlSerializer.
  /// </summary>
  public class TvServerState : IXmlSerializable
  {
    #region Internal classes

    [KnownType(typeof(Schedule))]
    public class ScheduleList : List<ISchedule>
    {
      public ScheduleList() { }
      public ScheduleList(IEnumerable<ISchedule> collection)
        : base(collection)
      { }
    }

    #endregion

    public static readonly Guid STATE_ID = new Guid("2A58935C-3363-4FA1-B48D-1EF0E81F830D");

    protected bool _isRecording = false;
    protected ScheduleList _currentlyRecordingSchedules = new ScheduleList();

    public bool IsRecording
    {
      get { return _isRecording; }
      set { _isRecording = value; }
    }

    public IList<ISchedule> CurrentlyRecordingSchedules
    {
      get { return _currentlyRecordingSchedules; }
      set { _currentlyRecordingSchedules = new ScheduleList(value); }
    }

    #region XML serialization

    XmlSchema IXmlSerializable.GetSchema()
    {
      return null;
    }

    void IXmlSerializable.ReadXml(XmlReader reader)
    {
      reader.ReadStartElement();
      _isRecording = reader.DeserializeXml<bool>();
      _currentlyRecordingSchedules = reader.DeserializeXml<ScheduleList>();
      reader.ReadEndElement();
    }

    void IXmlSerializable.WriteXml(XmlWriter writer)
    {
      _isRecording.SerializeXml(writer);
      _currentlyRecordingSchedules.SerializeXml(writer);
    }

    #endregion
  }
}
