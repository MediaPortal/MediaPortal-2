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
using MediaPortal.Plugins.MP2Extended.TAS;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule.BaseClasses
{
  class BaseScheduleBasic
  {
    internal static WebScheduleBasic ScheduleBasic(ISchedule schedule)
    {
       WebScheduleBasic webScheduleBasic = new WebScheduleBasic
        {
          Title = schedule.Name,
          Id = schedule.ScheduleId,
          ChannelId = schedule.ChannelId,
          EndTime = schedule.EndTime,
          StartTime = schedule.StartTime,
          PostRecordInterval = Convert.ToInt32(schedule.PostRecordInterval.TotalMinutes),
          PreRecordInterval = Convert.ToInt32(schedule.PostRecordInterval.TotalMinutes),
          ScheduleType = ConvertTo<WebScheduleType>(schedule.RecordingType),
          Priority = (int)schedule.Priority,
          KeepMethod = ConvertTo<WebScheduleKeepMethod>(schedule.KeepMethod),
        };
      if (schedule.ParentScheduleId != null)
        webScheduleBasic.ParentScheduleId = schedule.ParentScheduleId.Value;
      if (schedule.KeepDate != null)
        webScheduleBasic.KeepDate = schedule.KeepDate.Value;

      return webScheduleBasic;
    }

    public static T ConvertTo<T>(object value)
      where T : struct,IConvertible
    {
      var sourceType = value.GetType();
      if (!sourceType.IsEnum)
        throw new ArgumentException("Source type is not enum");
      if (!typeof(T).IsEnum)
        throw new ArgumentException("Destination type is not enum");
      return (T)Enum.Parse(typeof(T), value.ToString());
    }
  }
}
