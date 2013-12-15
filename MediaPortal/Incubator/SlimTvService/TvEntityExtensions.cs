#region Copyright (C) 2007-2013 Team MediaPortal

/*
    Copyright (C) 2007-2013 Team MediaPortal
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
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
using Mediaportal.TV.Server.TVDatabase.Entities.Enums;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer.Entities;
using KeepMethodType = MediaPortal.Plugins.SlimTv.Interfaces.Items.KeepMethodType;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public static class TvEntityExtensions
  {
    public static IProgram ToProgram(this Mediaportal.TV.Server.TVDatabase.Entities.Program tvProgram, bool includeRecordingStatus = false)
    {
      if (tvProgram == null)
        return null;
      Program program = new Program
        {
          ChannelId = tvProgram.IdChannel,
          ProgramId = tvProgram.IdProgram,
          Title = tvProgram.Title,
          Description = tvProgram.Description,
          StartTime = tvProgram.StartTime,
          EndTime = tvProgram.EndTime,
        };

      ProgramBLL programLogic = new ProgramBLL(tvProgram);
      program.RecordingStatus = programLogic.IsRecording ? RecordingStatus.Recording : RecordingStatus.None;
      if (programLogic.IsRecordingOncePending)
        program.RecordingStatus |= RecordingStatus.Scheduled;
      if (programLogic.IsRecordingSeriesPending)
        program.RecordingStatus |= RecordingStatus.SeriesScheduled;

      return program;
    }

    public static IChannel ToChannel(this Mediaportal.TV.Server.TVDatabase.Entities.Channel tvChannel)
    {
      return new Channel { ChannelId = tvChannel.IdChannel, Name = tvChannel.DisplayName, MediaType = (MediaType)tvChannel.MediaType };
    }

    public static IChannelGroup ToChannelGroup(this Mediaportal.TV.Server.TVDatabase.Entities.ChannelGroup tvGroup)
    {
      return new ChannelGroup { ChannelGroupId = tvGroup.IdGroup, Name = tvGroup.GroupName };
    }

    public static ISchedule ToSchedule(this Mediaportal.TV.Server.TVDatabase.Entities.Schedule schedule)
    {
      return new Schedule
      {
        ChannelId = schedule.IdChannel,
        Name = schedule.ProgramName,
        KeepDate = schedule.KeepDate,
        KeepMethod = (KeepMethodType)schedule.KeepMethod,
        PreRecordInterval = TimeSpan.FromMinutes(schedule.PreRecordInterval),
        PostRecordInterval = TimeSpan.FromMinutes(schedule.PostRecordInterval),
        Priority = (PriorityType)schedule.Priority,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        ScheduleId = schedule.IdSchedule,
        ParentScheduleId = schedule.IdParentSchedule,
        IsSeries = schedule.Series,
      };
    }
  }
}