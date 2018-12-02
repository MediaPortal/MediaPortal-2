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
using System.Xml;
using System.Xml.Serialization;

namespace MediaPortal.Common.TaskScheduler
{
  /// <summary>
  /// Specifies the ocurrence of a scheduled <see cref="Task"/>.
  /// </summary>
  public enum Occurrence
  {
    /// <summary>
    /// Only run the task once.
    /// </summary>
    Once,

    /// <summary>
    /// Repeatedly Run the task.
    /// </summary>
    Repeat,

    /// <summary>
    /// Run the task every time when the program starts.
    /// </summary>
    EveryStartUp,

    /// <summary>
    /// Run the task every time when the system wakes up from standby.
    /// </summary>
    EveryWakeUp
  }

  /// <summary>
  /// Specifies the type of schedule for a scheduled <see cref="Task"/>.
  /// </summary>
  public enum ScheduleType
  {
    TimeBased,
    IntervalBased
  }

  /// <summary>
  /// The Schedule struct represents the schedule from a particular Task. Schedules are either time-based or
  /// interval-based, depending on the setting of the property Type which is a value from enum ScheduleType. Depending on this
  /// type, either the tuple Minute, Hour and Day or the Interval variable must be defined. The task scheduler will act on
  /// these depending on the schedule type. Interval is of type TimeSpan. For the tuple the following conditions must be met:
  /// 
  /// - Minute must be between -1 and 59, where -1 means "any minute" and 0-59 means an exact value
  /// - Hour must be between -1 and 23, where -1 means "any hour" and 0-23 means an exact value
  /// - Day must be between -1 and 6, where -1 means "any day" and 0-6 means Sunday-Saturday (according to the WeekDay enum).
  /// </summary>
  [Serializable]
  public struct Schedule
  {
    private int _minute;
    private int _hour;
    private int _day;
    private TimeSpan _interval;
    private ScheduleType _type;

    public int Minute
    {
      get { return _minute; }
      set
      {
        if (value < -1 || value > 59)
          throw new ArgumentOutOfRangeException("Minute", "must be between -1 and 59");
        _minute = value;
        _type = ScheduleType.TimeBased;
      }
    }

    public int Hour
    {
      get { return _hour; }
      set
      {
        if (value < -1 || value > 23)
          throw new ArgumentOutOfRangeException("Hour", "must be between -1 and 23");
        _hour = value;
        _type = ScheduleType.TimeBased;
      }
    }

    public int Day
    {
      get { return _day; }
      set
      {
        if (value < -1 || value > 6)
          throw new ArgumentOutOfRangeException("Day", "must be between -1 and 6");
        _day = value;
        _type = ScheduleType.TimeBased;
      }
    }

    [XmlIgnore]
    public TimeSpan Interval
    {
      get { return _interval; }
      set
      {
        _interval = value;
        _type = ScheduleType.IntervalBased;
      }
    }

    [XmlElement("Interval", DataType = "duration")]
    public string TimeSpanInterval
    {
      get { return XmlConvert.ToString(_interval); }
      set { Interval = XmlConvert.ToTimeSpan(value); }
    }

    public ScheduleType Type
    {
      get { return _type; }
      set
      {
        if (value == ScheduleType.TimeBased)
          _interval = TimeSpan.MinValue;
        else
        {
          _minute = -1;
          _hour = -1;
          _day = -1;
        }
        _type = value;
      }
    }
  }

  [Serializable]
  public class Task : ICloneable
  {
    #region Private fields

    private Guid _taskID = Guid.Empty;
    private Occurrence _occurrence = Occurrence.Once;
    private Schedule _schedule = new Schedule();
    private DateTime _lastRun = DateTime.MinValue;
    private DateTime _nextRun = DateTime.MinValue;
    private DateTime _expires = DateTime.MaxValue;
    private bool _wakeup = false;
    private bool _forceRun = false;
    private bool _needUpdate = true;
    private string _owner = String.Empty;

    #endregion

    #region Ctor

    /// <summary>
    /// Parameterless Constructor for object deserialisation
    /// </summary>
    public Task()
    {
    }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// This schedule will occur every every minute, every hour, every day.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="occurrence">specifies when the task's schedule should occur.</param>
    public Task(string owner, Occurrence occurrence) : this(owner, -1, occurrence) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    public Task(string owner, int minute) : this(owner, minute, Occurrence.Once) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    /// <param name="occurrence">specifies when the task's schedule should occur.</param>
    public Task(string owner, int minute, Occurrence occurrence) : this(owner, minute, -1, occurrence) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    /// <param name="hour">specifies at which hour this task should run (-1 = every hour).</param>
    public Task(string owner, int minute, int hour) : this(owner, minute, hour, Occurrence.Once) { }

    /// <summary>
    /// Creates a new task for the task scheduler with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    /// <param name="hour">specifies at which hour this task should run (-1 = every hour).</param>
    /// <param name="occurrence">specifies when the task's schedule should occur.</param>
    public Task(string owner, int minute, int hour, Occurrence occurrence) : this(owner, minute, hour, -1, occurrence) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    /// <param name="hour">specifies at which hour this task should run (-1 = every hour).</param>
    /// <param name="day">specifies at which day of the week this task should run (-1 = every day).</param>
    public Task(string owner, int minute, int hour, int day) : this(owner, minute, hour, day, Occurrence.Once) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    /// <param name="hour">specifies at which hour this task should run (-1 = every hour).</param>
    /// <param name="day">specifies at which day of the week this task should run (-1 = every day).</param>
    /// <param name="occurrence">specifies when the task's schedule should occur.</param>
    public Task(string owner, int minute, int hour, int day, Occurrence occurrence) :
        this(owner, minute, hour, day, occurrence, DateTime.MaxValue) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    /// <param name="hour">specifies at which hour this task should run (-1 = every hour).</param>
    /// <param name="day">specifies at which day of the week this task should run (-1 = every day).</param>
    /// <param name="occurrence">specifies when the task's schedule should occur.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    public Task(string owner, int minute, int hour, int day, Occurrence occurrence, DateTime expires) :
        this(owner, minute, hour, day, occurrence, expires, true) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    /// <param name="hour">specifies at which hour this task should run (-1 = every hour).</param>
    /// <param name="day">specifies at which day of the week this task should run (-1 = every day).</param>
    /// <param name="occurrence">specifies when the task's schedule should occur.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    /// <param name="forceRun">specifies whether a schedule should be triggered forcefully in case system was down when schedule was due (true).</param>
    public Task(string owner, int minute, int hour, int day, Occurrence occurrence, DateTime expires, bool forceRun) :
        this(owner, minute, hour, day, occurrence, expires, forceRun, false) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with a time-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="minute">specifies at which minute this task should run (-1 = every minute).</param>
    /// <param name="hour">specifies at which hour this task should run (-1 = every hour).</param>
    /// <param name="day">specifies at which day of the week this task should run (-1 = every day).</param>
    /// <param name="occurrence">specifies when the task's schedule should occur.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    /// <param name="forceRun">specifies whether a schedule should be triggered forcefully in case system was down when schedule was due (true).</param>
    /// <param name="wakeup">specifies whether the system should be woken up from standby for this task's schedule (false).</param>
    public Task(string owner, int minute, int hour, int day, Occurrence occurrence, DateTime expires, bool forceRun, bool wakeup)
    {
      _owner = owner;
      _schedule.Minute = minute;
      _schedule.Hour = hour;
      _schedule.Day = day;
      _schedule.Type = ScheduleType.TimeBased;
      _occurrence = occurrence;
      _expires = expires;
      _forceRun = forceRun;
      if (wakeup && (occurrence == Occurrence.EveryStartUp || occurrence == Occurrence.EveryWakeUp))
        throw new ArgumentException("wakeup setting cannot be used together with Occurrence " + _occurrence);
      _wakeup = wakeup;
    }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with an interval-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="interval">specifies the interval of this task's schedule.</param>
    public Task(string owner, TimeSpan interval) : this(owner, interval, DateTime.MaxValue) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with an interval-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="interval">specifies the interval of this task's schedule.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    public Task(string owner, TimeSpan interval, DateTime expires) : this(owner, interval, expires, true) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with an interval-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="interval">specifies the interval of this task's schedule.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    /// <param name="forceRun">specifies whether a schedule should be triggered forcefully in case system was down when schedule was due (true).</param>
    public Task(string owner, TimeSpan interval, DateTime expires, bool forceRun) :
        this(owner, interval, expires, forceRun, false) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with an interval-based <see cref="Schedule"/>.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="interval">specifies the interval of this task's schedule.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    /// <param name="forceRun">specifies whether a schedule should be triggered forcefully in case system was down when schedule was due (true).</param>
    /// <param name="wakeup">specifies whether the system should be woken up from standby for this task's schedule (false).</param>
    public Task(string owner, TimeSpan interval, DateTime expires, bool forceRun, bool wakeup)
    {
      _owner = owner;
      _schedule.Interval = interval;
      _schedule.Type = ScheduleType.IntervalBased;
      _occurrence = Occurrence.Repeat;
      _expires = expires;
      _forceRun = forceRun;
      _wakeup = wakeup;
    }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with given <see cref="Schedule"/> as schedule.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="schedule">specifies the schedule of this task.</param>
    /// <param name="occurrence">specifies the occurrence of this task.</param>
    public Task(string owner, Schedule schedule, Occurrence occurrence) : this(owner, schedule, occurrence, DateTime.MaxValue) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with given <see cref="Schedule"/> as schedule.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="schedule">specifies the schedule of this task.</param>
    /// <param name="occurrence">specifies the occurrence of this task.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    public Task(string owner, Schedule schedule, Occurrence occurrence, DateTime expires) :
        this(owner, schedule, occurrence, expires, true) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with given <see cref="Schedule"/> as schedule.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="schedule">specifies the schedule of this task.</param>
    /// <param name="occurrence">specifies the occurrence of this task.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    /// <param name="forceRun">specifies whether a schedule should be triggered forcefully in case system was down when schedule was due (true).</param>
    public Task(string owner, Schedule schedule, Occurrence occurrence, DateTime expires, bool forceRun) :
        this(owner, schedule, occurrence, expires, forceRun, false) { }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> with given <see cref="Schedule"/> as schedule.
    /// </summary>
    /// <param name="owner">specifies the owner of this task.</param>
    /// <param name="schedule">specifies the schedule of this task.</param>
    /// <param name="occurrence">specifies the occurrence of this task.</param>
    /// <param name="expires">specifies when the task's schedule should expire.</param>
    /// <param name="forceRun">specifies whether a schedule should be triggered forcefully in case system was down when schedule was due (true).</param>
    /// <param name="wakeup">specifies whether the system should be woken up from standby for this task's schedule (false).</param>
    public Task(string owner, Schedule schedule, Occurrence occurrence, DateTime expires, bool forceRun, bool wakeup)
    {
      _owner = owner;
      _schedule = schedule;
      _occurrence = occurrence;
      _expires = expires;
      _forceRun = forceRun;
      _wakeup = wakeup;
    }

    /// <summary>
    /// Creates a new task for the <see cref="TaskScheduler"/> based on the given task.
    /// Used for the <see cref="ICloneable"/> implementation.
    /// </summary>
    /// <param name="task">The new task should be based upon.</param>
    public Task(Task task)
    {
      _taskID = task.ID;
      _owner = task.Owner;
      _schedule.Minute = task.Schedule.Minute;
      _schedule.Hour = task.Schedule.Hour;
      _schedule.Day = task.Schedule.Day;
      _schedule.Interval = task.Schedule.Interval;
      _schedule.Type = task.Schedule.Type;
      _expires = task.Expires;
      _forceRun = task.ForceRun;
      _wakeup = task.WakeupSystem;
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Indicates whether or not a schedule is expired.
    /// </summary>
    public bool IsExpired(DateTime now)
    {
      return (_expires <= _nextRun || _expires < now);
    }

    /// <summary>
    /// Indicates whether or not this schedule is due now.
    /// </summary>
    /// <returns><c>true</c> if this task is due, else <c>false</c>.</returns>
    public bool IsDue(DateTime now)
    {
      if (NextRun > now)
        return false;
      if (NextRun == now)
        return true;
      if (_forceRun && NextRun < now)
        return true;
      return false;
    }

    #endregion

    #region Private methods

    /// <summary>
    /// Resets the state (LastRun) of this task. Also sets an internal flag that the "NextRun" flag needs updating.
    /// </summary>
    private void Reset()
    {
      _lastRun = DateTime.MinValue;
      _needUpdate = true;
    }

    /// <summary>
    /// Retrieves the next schedule DateTime based on the internal structure of the Task (i.e., Occurrence type,
    /// ScheduleType and configured interval or minutes/hours/days). Used internally to update the NextRun property.
    /// </summary>
    /// <returns>Next scheduled date and time of this task. If this task is configured to be started at
    /// <see cref="TaskScheduler.Occurrence.EveryStartUp"/> or <see cref="TaskScheduler.Occurrence.EveryStartUp"/>,
    /// <see cref="DateTime.MaxValue"/> is returned.</returns>
    private DateTime GetNextScheduleDateTime()
    {
      switch (_occurrence)
      {
        case Occurrence.Once:
        case Occurrence.Repeat:
          if (_schedule.Type == ScheduleType.IntervalBased)
          {
            // interval-based schedule; determine next schedule based on last run + interval
            if (_lastRun == DateTime.MinValue)
            {
              DateTime now = DateTime.Now;
              return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).Add(_schedule.Interval);
            }
            return _lastRun.Add(_schedule.Interval);
          }
          // time-based schedule; determine next schedule based on last run + given schedule
          DateTime nowDate = DateTime.Now;
          DateTime nextDate = CalculateNextTimeBasedSchedule();
          // check if next schedule is not in the past
          if (nextDate < nowDate)
          {
            // it's in the past, so reset it and recalculate schedule
            Reset();
            nextDate = CalculateNextTimeBasedSchedule();
          }
          return nextDate;
//        case Occurrence.EveryStartUp: // Default behavior
//        case Occurrence.EveryWakeUp: // Default behavior
        default:
          return DateTime.MaxValue;
      }
    }

    /// <summary>
    /// Calculates the NextRun property for time-based task schedules. Used internally by the method
    /// GetNextScheduleDateTime().
    /// </summary>
    /// <returns>Next task schedule date and time</returns>
    private DateTime CalculateNextTimeBasedSchedule()
    {
      // Up to 8 different time-based schedules are possible
      DateTime nextDate;
      DateTime now = DateTime.Now;
      int min = _schedule.Minute;
      int hour = _schedule.Hour;
      int day = _schedule.Day;
      if (min == -1 && hour == -1 && day == -1)
      {
        // Run every minute, every hour, every day (same as a TimeSpan of one minute)
        if (_lastRun == DateTime.MinValue)
          return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
        return _lastRun.AddMinutes(1);
      }
      if (hour == -1 && day == -1)
      {
        // Run at xx minute, every hour, every day
        if (_lastRun == DateTime.MinValue)
        {
          nextDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, min, 0);
          if (now.Minute < min)
            return nextDate;
          return nextDate.AddHours(1);
        }
        return _lastRun.AddHours(1);
      }
      if (min == -1 && hour == -1)
      {
        // Run every minute, every hour, on day x
        int nowDay = (int) now.DayOfWeek;
        if (_lastRun == DateTime.MinValue)
        {
          nextDate = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
          if (nowDay == day)
          {
            if ((int) now.AddMinutes(1).DayOfWeek != nowDay)
              nextDate = nextDate.AddDays(7);
            else
              nextDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
          }
          else if (nowDay < day)
            nextDate = nextDate.AddDays(day - nowDay);
          else
            nextDate = nextDate.AddDays(7 - (nowDay - day));
        }
        else
        {
          nextDate = _lastRun.AddMinutes(1);
          if ((int)nextDate.DayOfWeek != day)
          {
            nextDate = new DateTime(_lastRun.Year, _lastRun.Month, _lastRun.Day, 0, 0, 0);
            if (nowDay == day)
              nextDate = nextDate.AddDays(7);
            else if (nowDay < day)
              nextDate = nextDate.AddDays(day - nowDay);
            else
              nextDate = nextDate.AddDays(7 - (nowDay - day));
          }
        }
        return nextDate;
      }
      if (min == -1 && day == -1)
      {
        // Run every minute, at hour xx, every day
        if (_lastRun == DateTime.MinValue)
        {
          if (now.Hour == hour && now.Minute < 59)
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
          if (now.Hour < hour)
            return new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
          nextDate = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
          return nextDate.AddDays(1);
        }
        if (_lastRun.Minute < 59)
          return _lastRun.AddMinutes(1);
        nextDate = new DateTime(_lastRun.Year, _lastRun.Month, _lastRun.Day, _lastRun.Hour, 0, 0);
        return nextDate.AddDays(1);
      }
      if (day == -1)
      {
        // Run at xx minute, at hour xx, every day
        if (_lastRun == DateTime.MinValue)
        {
          if ((now.Hour <= hour && now.Minute <= min) || (now.Hour < hour))
            return new DateTime(now.Year, now.Month, now.Day, hour, min, 0);
          nextDate = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);
          return nextDate.AddDays(1);
        }
        return _lastRun.AddDays(1);
      }
      if (hour == -1)
      {
        // Run at xx minute, every hour, on day x
        int nowDay = (int)now.DayOfWeek;
        if (_lastRun == DateTime.MinValue)
        {
          if (nowDay == day)
          {
            nextDate = new DateTime(now.Year, now.Month, now.Day, now.Hour, min, 0);
            if (now.Minute < min && now.Hour < 23)
              return nextDate;
            if (now.Minute > min && now.Hour < 23)
              return nextDate.AddHours(1);
          }
          nextDate = new DateTime(now.Year, now.Month, now.Day, 0, min, 0);
          if (nowDay == day)
            return nextDate.AddDays(7);
          if (nowDay < day)
            return nextDate.AddDays(day - nowDay);
          return nextDate.AddDays(7 - (nowDay - day));
        }
        if ((int)_lastRun.DayOfWeek == day && _lastRun.Hour < 23)
          return _lastRun.AddHours(1);
        nextDate = new DateTime(_lastRun.Year, _lastRun.Month, _lastRun.Day, 0, min, 0);
        if (nowDay == day)
          return nextDate.AddDays(7);
        if (nowDay < day)
          return nextDate.AddDays(day - nowDay);
        return nextDate.AddDays(7 - (nowDay - day));
      }
      if (min == -1)
      {
        // Run every minute, at hour xx, on day x
        int nowDay = (int)now.DayOfWeek;
        if (_lastRun == DateTime.MinValue)
        {
          if (nowDay == day && now.Hour == hour && now.Minute < 59)
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0).AddMinutes(1);
          nextDate = new DateTime(now.Year, now.Month, now.Day, hour, 0, 0);
          if (nowDay == day)
            return nextDate.AddDays(7);
          if (nowDay < day)
            return nextDate.AddDays(day - nowDay);
          return nextDate.AddDays(7 - (nowDay - day));
        }
        if ((int)_lastRun.DayOfWeek == day && _lastRun.Hour == hour && _lastRun.Minute < 59)
          return _lastRun.AddMinutes(1);
        nextDate = new DateTime(_lastRun.Year, _lastRun.Month, _lastRun.Day, _lastRun.Hour, 0, 0);
        if (nowDay == day)
          return _lastRun.AddDays(7);
        if (nowDay < day)
          return nextDate.AddDays(day - nowDay);
        return nextDate.AddDays(7 - (nowDay - day));
      }
      else
      {
        // Run at xx minute, at hour xx, on day x
        int nowDay = (int)now.DayOfWeek;
        if (_lastRun == DateTime.MinValue)
        {
          nextDate = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);
          if (nowDay == day && now.Hour <= hour && now.Minute < min)
            return nextDate;
          if (nowDay == day)
            return nextDate.AddDays(7);
          if (nowDay < day)
            return nextDate.AddDays(day - nowDay);
          return nextDate.AddDays(7 - (nowDay - day));
        }
        nextDate = new DateTime(now.Year, now.Month, now.Day, hour, min, 0);
        if (nowDay == day)
          return nextDate.AddDays(7);
        if (nowDay < day)
          return nextDate.AddDays(day - nowDay);
        return nextDate.AddDays(7 - (nowDay - day));
      }
    }

    #endregion

    #region Properties

    /// <summary>
    /// Provides access to the internal Schedule object which holds the schedule of this task. You can easily change 
    /// the schedule rom a task through this property.
    /// </summary>
    public Schedule Schedule
    {
      get { return _schedule; }
      set { _schedule = value; }
    }

    /// <summary>
    /// Indicates whether or not a scheduled task should be fired when a schedule is already (way) past its due time.
    /// Setting this to true makes sure a schedule is always run (for example when the program was restarted)
    /// </summary>
    public bool ForceRun
    {
      get { return _forceRun; }
      set { _forceRun = value; }
    }

    /// <summary>
    /// Indicates whether or not the system should be woken up from standby to perform this task.
    /// Throws an ArgumentException when trying to set this on tasks which are scheduled to run during Startup/Wakeup.
    /// </summary>
    public bool WakeupSystem
    {
      get { return _wakeup; }
      set
      {
        if (value)
          if (_occurrence == Occurrence.EveryWakeUp || _occurrence == Occurrence.EveryStartUp)
            throw new ArgumentException("WakeupSystem setting cannot be used together with Occurrence " + _occurrence);
        _wakeup = value;
      }
    }

    /// <summary>
    /// Indicates when the schedule was due last time. This property should be set only by the task scheduler itself,
    /// not by a schedule owner / source.
    /// </summary>
    public DateTime LastRun
    {
      get { return _lastRun; }
      set
      {
        _lastRun = value;
        _needUpdate = true;
      }
    }

    /// <summary>
    /// Indicates when the schedule is due next time.
    /// </summary>
    public DateTime NextRun
    {
      get
      {
        if (_needUpdate)
        {
          _nextRun = GetNextScheduleDateTime();
          _needUpdate = false;
        }
        return _nextRun;
      }
      set { _nextRun = value; }
    }

    /// <summary>
    /// Indicates when this Task will expire. DateTime.MaxValue means it will never expire. 
    /// Throws ArgumentOutOfRangeException when the value is set lower or equal to the current time.
    /// </summary>
    public DateTime Expires
    {
      get { return _expires; }
      set { _expires = value; }
    }

    /// <summary>
    /// Unique task identifier. This property should be set only by the task scheduler itself. A value of 0 indicates
    /// that the Task is not yet submitted to the task scheduler.
    /// </summary>
    public Guid ID
    {
      get { return _taskID; }
      set { _taskID = value; }
    }

    /// <summary>
    /// Provides the owner o this task.
    /// </summary>
    public string Owner
    {
      get { return _owner; }
      set { _owner = value; }
    }

    /// <summary>
    /// Specifies the occurrence type for this task. Either one of the following:
    /// </summary>
    public Occurrence Occurrence
    {
      get { return _occurrence; }
      set { _occurrence = value; }
    }

    #endregion

    #region ICloneable implementation

    public virtual object Clone()
    {
      return new Task(this);
    }

    #endregion

    #region Base overrides

    public override string ToString()
    {
      if (_schedule.Type == ScheduleType.IntervalBased)
        return String.Format("Task: {0}, Owner: {1}, Occurrence: {2}, Type: interval, Interval: {3}, LastRun: {4}, NextRun: {5}, Expires: {6}, Wakeup: {7}, Force: {8}",
          _taskID, _owner, _occurrence, _schedule.Interval, _lastRun, NextRun, _expires, _wakeup, _forceRun);
      return String.Format("Task: {0}, Owner: {1}, Occurrence: {2}, Type: time-based: D:{3}-H:{4}-M:{5}, LastRun: {6}, NextRun: {7}, Expires: {8}, Wakeup: {9}, Force: {10}",
        _taskID, _owner, _occurrence, _schedule.Day, _schedule.Hour, _schedule.Minute, _lastRun, NextRun, _expires, _wakeup, _forceRun);
    }

    #endregion
  }
}