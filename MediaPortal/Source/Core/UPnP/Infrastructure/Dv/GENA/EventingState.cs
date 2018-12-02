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
using System.Linq;
using MediaPortal.Utilities;
using UPnP.Infrastructure.Dv.DeviceTree;

namespace UPnP.Infrastructure.Dv.GENA
{
  /// <summary>
  /// Stores additional data for a UPnP state variable, which is necessary to moderate its change events.
  /// </summary>
  public class ModerationData
  {
    public DateTime LastEventTime = DateTime.MinValue;
    public object LastValue = double.MinValue;
  }

  /// <summary>
  /// Stores eventing data for all state variables of a UPnP service for unicast event subscriptions and for multicast events.
  /// Holds the <see cref="EventKey"/> and a list of scheduled event notifications.
  /// </summary>
  public class EventingState
  {
    protected uint _eventKey = 0;
    protected IDictionary<DvStateVariable, ModerationData> _moderationData =
        new Dictionary<DvStateVariable, ModerationData>();
    protected SortedList<DateTime, HashSet<DvStateVariable>> _scheduledEventNotifications =
        new SortedList<DateTime, HashSet<DvStateVariable>>();

    /// <summary>
    /// Returns the current event sequence number. The sequence number is <code>0</code> for the initial
    /// event; in this case, no "normal" change events must be sent for any variable. After the event sequence
    /// number was used, <see cref="IncEventKey"/> must be called to increment the sequence number.
    /// </summary>
    public uint EventKey
    {
      get { return _eventKey; }
    }

    /// <summary>
    /// Increments the event sequence number.
    /// </summary>
    public void IncEventKey()
    {
      _eventKey++;
      if (_eventKey == 0)
        // See (DevArch), 4.1.1 (event key must be wrapped to 1)
        _eventKey = 1;
    }

    /// <summary>
    /// Checks if the specified <paramref name="variable"/> is moderated, calculates the next eventing
    /// time for the variable and adapts internal moderation data.
    /// </summary>
    /// <remarks>
    /// The specified <paramref name="variable"/> will be added to the pending event notifications.
    /// </remarks>
    /// <param name="variable">Variable which was changed.</param>
    public void ModerateChangeEvent(DvStateVariable variable)
    {
      ModerationData md;
      bool wasCreated = false;
      if (!_moderationData.TryGetValue(variable, out md))
      {
        wasCreated = true;
        _moderationData[variable] = md = new ModerationData();
      }
      DateTime now = DateTime.Now;
      DateTime scheduleTime;
      if (variable.ModeratedMaximumRate.HasValue)
      {
        if (wasCreated || md.LastEventTime + variable.ModeratedMaximumRate < now)
          scheduleTime = now;
        else
          scheduleTime = now + variable.ModeratedMaximumRate.Value;
      }
      else if (variable.ModeratedMinimumDelta != 0)
      {
        if (!(variable.DataType is DvStandardDataType))
          scheduleTime = now;
        else
        {
          if (!wasCreated &&
              Math.Abs(((DvStandardDataType) variable.DataType).GetNumericDelta(md.LastValue, variable.Value)) < variable.ModeratedMinimumDelta)
            return;
          scheduleTime = now;
        }
      }
      else
        scheduleTime = now;

      HashSet<DvStateVariable> scheduledVariables;
      if (_scheduledEventNotifications.TryGetValue(scheduleTime, out scheduledVariables) && scheduledVariables != null)
        scheduledVariables.Add(variable);
      else
        _scheduledEventNotifications[scheduleTime] = new HashSet<DvStateVariable> {variable};
    }

    /// <summary>
    /// Schedules a change event for the specified variables at the given <paramref name="scheduleTime"/>.
    /// </summary>
    /// <param name="variables">The variables which will be scheduled.</param>
    /// <param name="scheduleTime">Time when the event will be scheduled.</param>
    public void ScheduleEventNotification(IEnumerable<DvStateVariable> variables, DateTime scheduleTime)
    {
      HashSet<DvStateVariable> scheduledVariables;
      if (_scheduledEventNotifications.TryGetValue(scheduleTime, out scheduledVariables) && scheduledVariables != null)
        CollectionUtils.AddAll(scheduledVariables, variables);
      else
        _scheduledEventNotifications[scheduleTime] = new HashSet<DvStateVariable>(variables);
    }

    public TimeSpan? GetNextScheduleTimeSpan()
    {
      KeyValuePair<DateTime, HashSet<DvStateVariable>>? kvp = GetFirstScheduledEventNotification();
      if (kvp == null)
        return null;
      TimeSpan result = kvp.Value.Key - DateTime.Now;
      return result < TimeSpan.Zero ? TimeSpan.Zero : result;
    }

    protected KeyValuePair<DateTime, HashSet<DvStateVariable>>? GetFirstScheduledEventNotification()
    {
      IEnumerator<KeyValuePair<DateTime, HashSet<DvStateVariable>>> enumer = _scheduledEventNotifications.GetEnumerator();
      if (enumer.MoveNext())
        return enumer.Current;
      return null;
    }

    public ICollection<DvStateVariable> GetDueEvents()
    {
      DateTime now = DateTime.Now;
      ICollection<DvStateVariable> result = null;
      // Continue stepping through the (sorted) list of pending event notifications and collect all
      // variables to event until we find an entry which is scheduled in the future
      KeyValuePair<DateTime, HashSet<DvStateVariable>>? kvp;
      while ((kvp = GetFirstScheduledEventNotification()).HasValue)
      {
        if (kvp.Value.Key <= now)
        {
          if (result == null)
            result = new HashSet<DvStateVariable>();
          CollectionUtils.AddAll(result, kvp.Value.Value);
          _scheduledEventNotifications.RemoveAt(0);
        }
        else
          break;
      }
      return result;
    }

    /// <summary>
    /// Updates internal data structures that are necessary for event moderation.
    /// This method needs to be called when an event message for the given <paramref name="variable"/> is sent.
    /// </summary>
    public void UpdateModerationData(DvStateVariable variable)
    {
      ModerationData md;
      if (!_moderationData.TryGetValue(variable, out md))
        _moderationData[variable] = md = new ModerationData();
      DateTime now = DateTime.Now;
      md.LastEventTime = now;
      md.LastValue = variable.Value;
    }
  }
}
