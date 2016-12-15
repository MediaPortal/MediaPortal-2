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
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MPExtended.Services.TVAccessService.Interfaces;

namespace MediaPortal.Plugins.SlimTv.Providers.Items
{
  public class Schedule : ISchedule
  {
    public Schedule()
    { }

    public Schedule(WebScheduleBasic webSchedule, int serverIndex)
    {
      ServerIndex = serverIndex;
      ScheduleId = webSchedule.Id;
      ParentScheduleId = webSchedule.ParentScheduleId;
      ChannelId = webSchedule.ChannelId;
      Name = webSchedule.Title;
      StartTime = webSchedule.StartTime;
      EndTime = webSchedule.EndTime;
      RecordingType = (ScheduleRecordingType)webSchedule.ScheduleType;
      Priority = (PriorityType)webSchedule.Priority;
      PreRecordInterval = TimeSpan.FromMinutes(webSchedule.PreRecordInterval);
      PostRecordInterval = TimeSpan.FromMinutes(webSchedule.PostRecordInterval);
      KeepMethod = (KeepMethodType)webSchedule.KeepMethod;
      KeepDate = webSchedule.KeepDate;
    }
    public int ServerIndex { get; set; }
    public int ScheduleId { get; private set; }
    public int? ParentScheduleId { get; private set; }
    public int ChannelId { get; set; }
    public string Name { get; set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public bool IsSeries { get { return RecordingType != ScheduleRecordingType.Once; } }
    public ScheduleRecordingType RecordingType { get; set; }
    public PriorityType Priority { get; set; }
    public TimeSpan PreRecordInterval { get; set; }
    public TimeSpan PostRecordInterval { get; set; }
    public KeepMethodType KeepMethod { get; set; }
    public DateTime? KeepDate { get; set; }
  }
}
