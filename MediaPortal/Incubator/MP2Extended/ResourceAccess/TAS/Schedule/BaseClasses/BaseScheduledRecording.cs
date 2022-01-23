#region Copyright (C) 2007-2020 Team MediaPortal

/*
    Copyright (C) 2007-2020 Team MediaPortal
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
using MediaPortal.Common;
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.Schedule.BaseClasses
{
  class BaseScheduledRecording
  {
    internal static WebScheduledRecording ScheduledRecording(ISchedule schedule)
    {
      IChannelAndGroupInfoAsync channelAndGroupInfo = ServiceRegistration.Get<ITvProvider>() as IChannelAndGroupInfoAsync;
      var channel = channelAndGroupInfo.GetChannelAsync(schedule.ChannelId).Result;
      string channelName = "";
      if (!channel.Success)
        channelName = channel.Result.Name;

      WebScheduledRecording webScheduledRecording = new WebScheduledRecording
        {
          Title = schedule.Name,
          ChannelId = schedule.ChannelId,
          EndTime = schedule.EndTime,
          StartTime = schedule.StartTime,
          ScheduleId = schedule.ScheduleId,
          //ProgramId = schedule,
          ChannelName = channelName
        };

      return webScheduledRecording;
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
