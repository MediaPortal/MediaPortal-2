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
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.SlimTv.Interfaces
{
  [Flags]
  public enum RecordingStatus
  {
    None,
    Scheduled,
    SeriesScheduled,
    RuleScheduled,
    Recording
  }

  public enum ScheduleRecordingType
  {
    Once,
    Daily,
    Weekly,
    EveryTimeOnThisChannel,
    EveryTimeOnEveryChannel,
    Weekends,
    WorkingDays,
    WeeklyEveryTimeOnThisChannel
  }

  public interface IScheduleControl
  {
    /// <summary>
    /// Creates a single or extended series schedule.
    /// </summary>
    /// <param name="program">Program to record.</param>
    /// <param name="recordingType">Schedule recording type.</param>
    /// <param name="schedule">Returns the schedule instance.</param>
    /// <returns><c>true</c> if successful.</returns>
    bool CreateSchedule(IProgram program, ScheduleRecordingType recordingType, out ISchedule schedule);

    /// <summary>
    /// Creates a single or extended series schedule.
    /// </summary>
    /// <param name="channel">Channel to record.</param>
    /// <param name="from">Recording time from.</param>
    /// <param name="to">Recording time to.</param>
    /// <param name="schedule">Returns the schedule instance.</param>
    /// <returns><c>true</c> if successful.</returns>
    bool CreateScheduleByTime(IChannel channel, DateTime from, DateTime to, out ISchedule schedule);

    /// <summary>
    /// Deletes a schedule for the given <paramref name="program"/>. If the <paramref name="recordingType"/> is set to <see cref="ScheduleRecordingType.Once"/>,
    /// only the actual program schedule will be removed. If any other series type is used, the full schedule will be removed (including all single schedules).
    /// </summary>
    /// <param name="program">Program to cancel.</param>
    /// <param name="recordingType">Schedule recording type.</param>
    /// <returns><c>true</c> if successful.</returns>
    bool RemoveScheduleForProgram(IProgram program, ScheduleRecordingType recordingType); // ISchedule schedule ?
    
    /// <summary>
    /// Deletes a given <paramref name="schedule"/>.
    /// </summary>
    /// <param name="schedule">Schedule to delete.</param>
    /// <returns><c>true</c> if successful.</returns>
    bool RemoveSchedule(ISchedule schedule);

    /// <summary>
    /// Gets the <paramref name="recordingStatus"/> for the given <paramref name="program"/>.
    /// </summary>
    /// <param name="program">Program.</param>
    /// <param name="recordingStatus">Recording/Scheduling status.</param>
    /// <returns><c>true</c> if successful.</returns>
    bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus);

    /// <summary>
    /// Gets the file or stream name of currently running recording of the given <paramref name="program"/>.
    /// </summary>
    /// <param name="program">Program.</param>
    /// <param name="fileOrStream">Returns the filename or stream url.</param>
    /// <returns><c>true</c> if successful.</returns>
    bool GetRecordingFileOrStream(IProgram program, out string fileOrStream);

    /// <summary>
    /// Tries to get a list of programs for the given <paramref name="schedule"/>.
    /// </summary>
    /// <param name="schedule">Schedule</param>
    /// <param name="programs">Returns programs</param>
    /// <returns><c>true</c> if at least one program could be found</returns>
    bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs);

    //bool GetSchedules(IChannel channel, out IList<ISchedule> schedules);
    /// <summary>
    /// Gets the list of all available schedules.
    /// </summary>
    /// <param name="schedules">Returns schedules</param>
    /// <returns><c>true</c> if at least one schedule could be found</returns>
    bool GetSchedules(out IList<ISchedule> schedules);

    //bool AddRule(IScheduleRule rule);
    //bool RemoveRule(IScheduleRule rule);
    //bool GetRules(out IList<IScheduleRule> rules);
    //TODO
  }
}
