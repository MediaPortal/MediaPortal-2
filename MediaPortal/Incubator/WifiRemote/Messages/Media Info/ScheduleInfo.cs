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
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.WifiRemote.Messages.MediaInfo;

namespace MediaPortal.Plugins.WifiRemote
{
  internal class ScheduleInfo : IAdditionalMediaInfo
  {
    public string MediaType => "schedule";
    public string Id => ScheduleId.ToString();
    public int MpMediaType => (int)MpMediaTypes.Schedule;
    public int MpProviderId => (int)MpProviders.MPTvServer;

    /// <summary>
    /// ID of the schedule
    /// </summary>
    public int ScheduleId { get; set; }
    /// <summary>
    /// Schedule title
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Start time of this schedule
    /// </summary>
    public DateTime StartTime { get; set; }
    /// <summary>
    /// End time of this schedule
    /// </summary>
    public DateTime EndTime { get; set; }
    /// <summary>
    /// Schedule recording type
    /// </summary>
    public int RecordingType { get; set; }
    /// <summary>
    /// Id of channel
    /// </summary>
    public int ChannelId { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    public ScheduleInfo(ISchedule schedule)
    {
      ScheduleId = schedule.ScheduleId;
      ChannelId = schedule.ChannelId;
      Name = schedule.Name;
      StartTime = schedule.StartTime;
      RecordingType = (int)schedule.RecordingType;
      EndTime = schedule.EndTime;
    }
  }
}
