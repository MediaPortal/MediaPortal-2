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

namespace MediaPortal.Plugins.SlimTv.Interfaces.Items
{
  /// <summary>
  /// ISchedule represents a schedule to record either single programs or series.
  /// </summary>
  public interface ISchedule
  {
    /// <summary>
    /// Gets the Schedule's ID.
    /// </summary>
    int ScheduleId { get; }
    /// <summary>
    /// Gets the optional ID of parent schedule.
    /// </summary>
    int? ParentScheduleId { get; }
    /// <summary>
    /// Gets or sets the Channel ID where this program should be recorded.
    /// </summary>
    int ChannelId { get; set; }
    /// <summary>
    /// Gets or sets the scheduled program name.
    /// </summary>
    string Name { get; set; }
    /// <summary>
    /// Gets or sets the scheduled start time.
    /// </summary>
    DateTime StartTime { get; }
    /// <summary>
    /// Gets or sets the scheduled end time.
    /// </summary>
    DateTime EndTime { get; }
    /// <summary>
    /// Gets an indicator if the schedule is a series.
    /// </summary>
    bool IsSeries { get; }
    /// <summary>
    /// Gets or sets the type of recording.
    /// </summary>
    ScheduleRecordingType RecordingType { get; set; }
    /// <summary>
    /// Gets or sets the schedule's priority.
    /// </summary>
    PriorityType Priority { get; set; }
    /// <summary>
    /// Gets or sets the pre-record interval.
    /// </summary>
    TimeSpan PreRecordInterval { get; set; }
    /// <summary>
    /// Gets or sets the post-record interval.
    /// </summary>
    TimeSpan PostRecordInterval { get; set; }
    /// <summary>
    /// Gets or sets the keep method.
    /// </summary>
    KeepMethodType KeepMethod { get; set; }
    /// <summary>
    /// Gets or sets the "keep until" date.
    /// </summary>
    DateTime? KeepDate { get; set; }
  }
}
