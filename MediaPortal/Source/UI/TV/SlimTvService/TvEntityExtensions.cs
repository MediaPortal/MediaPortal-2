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
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
#if TVE3
#else
using MediaPortal.Common.Utils;
using Mediaportal.TV.Server.TVDatabase.Entities;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer.Entities;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
#endif
using Channel = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Channel;
using ChannelGroup = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.ChannelGroup;
using KeepMethodType = MediaPortal.Plugins.SlimTv.Interfaces.Items.KeepMethodType;
using Program = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Program;
using Schedule = MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items.Schedule;
using ScheduleRecordingType = MediaPortal.Plugins.SlimTv.Interfaces.ScheduleRecordingType;
using System.Linq;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public static class TvEntityExtensions
  {
#if TVE3
    public static IProgram ToProgram(this TvDatabase.Program tvProgram, bool includeRecordingStatus = false)
    {
      if (tvProgram == null)
        return null;
      Program program = new Program
      {
        ChannelId = tvProgram.IdChannel,
        ProgramId = tvProgram.IdProgram,
        Title = tvProgram.Title,
        Description = tvProgram.Description,
        Genre = tvProgram.Genre,
        StartTime = tvProgram.StartTime,
        EndTime = tvProgram.EndTime,
        OriginalAirDate = tvProgram.OriginalAirDate,
        Classification = tvProgram.Classification,
        ParentalRating = tvProgram.ParentalRating,
        StarRating = tvProgram.StarRating,
        SeasonNumber = tvProgram.SeriesNum,
        EpisodeNumber = tvProgram.EpisodeNum,
        EpisodeNumberDetailed = tvProgram.EpisodeNumber,
        EpisodePart = tvProgram.EpisodePart,
        EpisodeTitle = tvProgram.EpisodeName,
      };

      program.RecordingStatus = tvProgram.IsRecording ? RecordingStatus.Recording : RecordingStatus.None;
      if (tvProgram.IsRecordingOncePending || tvProgram.IsRecordingOnce)
        program.RecordingStatus |= RecordingStatus.Scheduled;
      if (tvProgram.IsRecordingSeriesPending || tvProgram.IsRecordingSeries)
        program.RecordingStatus |= RecordingStatus.SeriesScheduled;
      if (tvProgram.IsRecordingOnce)
        program.RecordingStatus |= RecordingStatus.RecordingOnce;
      if (tvProgram.IsRecordingSeries)
        program.RecordingStatus |= RecordingStatus.RecordingSeries;
      if (tvProgram.IsRecordingManual)
        program.RecordingStatus |= RecordingStatus.RecordingManual;
      program.HasConflict = tvProgram.HasConflict;

      return program;
    }

    public static IChannel ToChannel(this TvDatabase.Channel tvChannel)
    {
      if (tvChannel == null)
        return null;
      return new Channel
      {
        ChannelId = tvChannel.IdChannel,
        ChannelNumber = tvChannel.ChannelNumber,
        Name = tvChannel.DisplayName,
        MediaType = tvChannel.IsTv ? MediaType.TV : MediaType.Radio,
        EpgHasGaps = tvChannel.EpgHasGaps,
        ExternalId = tvChannel.ExternalId,
        GrapEpg = tvChannel.GrabEpg,
        LastGrabTime = tvChannel.LastGrabTime,
        TimesWatched = tvChannel.TimesWatched,
        TotalTimeWatched = tvChannel.TotalTimeWatched,
        VisibleInGuide = tvChannel.VisibleInGuide,
        GroupNames = tvChannel.GroupNames.ToList()
      };
    }

    public static IChannelGroup ToChannelGroup(this TvDatabase.ChannelGroup tvGroup)
    {
      if (tvGroup == null)
        return null;
      return new ChannelGroup { ChannelGroupId = tvGroup.IdGroup, Name = tvGroup.GroupName, MediaType = MediaType.TV, SortOrder = tvGroup.SortOrder};
    }

    public static IChannelGroup ToChannelGroup(this TvDatabase.RadioChannelGroup radioGroup)
    {
      if (radioGroup == null)
        return null;
      // Note: this temporary workaround uses negative group ids to be able to separate them later. This can be removed once there is a 
      // dedicated radio group interface (if required).
      return new ChannelGroup { ChannelGroupId = -radioGroup.IdGroup, Name = radioGroup.GroupName, MediaType = MediaType.Radio, SortOrder = radioGroup.SortOrder};
    }

    public static ISchedule ToSchedule(this TvDatabase.Schedule schedule)
    {
      if (schedule == null)
        return null;
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
        RecordingType = (ScheduleRecordingType)schedule.ScheduleType
      };
    }
#else
    public static IProgram ToProgram(this Mediaportal.TV.Server.TVDatabase.Entities.Program tvProgram, bool includeRecordingStatus = false)
    {
      if (tvProgram == null)
        return null;
      Program program = new Program
        {
          ChannelId = tvProgram.ChannelId,
          ProgramId = tvProgram.ProgramId,
          Title = tvProgram.Title,
          Description = tvProgram.Description,
          Genre = tvProgram.ProgramCategory?.Category,
          StartTime = tvProgram.StartTime,
          EndTime = tvProgram.EndTime,
          OriginalAirDate = tvProgram.OriginalAirDate,
          Classification = tvProgram.Classification,
          ParentalRating = tvProgram.ParentalRating,
          StarRating = tvProgram.StarRating,
          SeasonNumber = tvProgram.SeriesNum,
          EpisodeNumber = tvProgram.EpisodeNum,
          EpisodeNumberDetailed = tvProgram.EpisodeNum,  // TVE3.5 doesn't have Episode.Number?
          EpisodePart = tvProgram.EpisodePart,
          EpisodeTitle = tvProgram.EpisodeName,
        };

      ProgramBLL programLogic = new ProgramBLL(tvProgram);
      program.RecordingStatus = programLogic.IsRecording ? RecordingStatus.Recording : RecordingStatus.None;
      if (programLogic.IsRecordingOncePending || programLogic.IsRecordingOnce)
        program.RecordingStatus |= RecordingStatus.Scheduled;
      if (programLogic.IsRecordingSeriesPending || programLogic.IsRecordingSeries)
        program.RecordingStatus |= RecordingStatus.SeriesScheduled;
      if (programLogic.IsRecordingOnce)
        program.RecordingStatus |= RecordingStatus.RecordingOnce;
      if (programLogic.IsRecordingSeries)
        program.RecordingStatus |= RecordingStatus.RecordingSeries;
      if (programLogic.IsRecordingManual)
        program.RecordingStatus |= RecordingStatus.RecordingManual;
      program.HasConflict = programLogic.HasConflict;

      return program;
    }

    public static IChannel ToChannel(this Mediaportal.TV.Server.TVDatabase.Entities.Channel tvChannel)
    {
      return tvChannel == null ? null : new Channel
      {
        ChannelId = tvChannel.ChannelId,
        ChannelNumber = tvChannel.ChannelNumber,
        Name = tvChannel.DisplayName,
        MediaType = (MediaType)tvChannel.MediaType,
        EpgHasGaps = tvChannel.EpgHasGaps,
        ExternalId = tvChannel.ExternalId,
        GrapEpg = tvChannel.GrabEpg,
        LastGrabTime = tvChannel.LastGrabTime,
        TimesWatched = tvChannel.TimesWatched,
        TotalTimeWatched = tvChannel.TotalTimeWatched,
        VisibleInGuide = tvChannel.VisibleInGuide,
        GroupNames = tvChannel.GroupMaps?.Select(group => group.ChannelGroup?.GroupName).ToList()
      };
    }

    public static IChannelGroup ToChannelGroup(this Mediaportal.TV.Server.TVDatabase.Entities.ChannelGroup radioGroup)
    {
      return new ChannelGroup { ChannelGroupId = radioGroup.ChannelGroupId, Name = radioGroup.GroupName, MediaType = radioGroup.MediaType == 0 ? MediaType.TV : MediaType.Radio, SortOrder = radioGroup.SortOrder};
    }

    public static ISchedule ToSchedule(this Mediaportal.TV.Server.TVDatabase.Entities.Schedule schedule)
    {
      return new Schedule
      {
        ChannelId = schedule.ChannelId,
        Name = schedule.ProgramName,
        KeepDate = schedule.KeepDate,
        KeepMethod = (KeepMethodType)schedule.KeepMethod,
        PreRecordInterval = TimeSpan.FromMinutes(schedule.PreRecordInterval),
        PostRecordInterval = TimeSpan.FromMinutes(schedule.PostRecordInterval),
        Priority = (PriorityType)schedule.Priority,
        StartTime = schedule.StartTime,
        EndTime = schedule.EndTime,
        ScheduleId = schedule.ScheduleId,
        ParentScheduleId = schedule.ParentScheduleId,
        RecordingType = (ScheduleRecordingType)schedule.ScheduleType
      };
    }
#endif

    //public static IProgram[] ToPrograms(this NowAndNext nowAndNext)
    //{
    //  if (nowAndNext == null)
    //    return null;

    //  IProgram[] programs = new IProgram[2]; // 0: now; 1: next
    //  programs[0] = new Program
    //  {
    //    ChannelId = nowAndNext.ChannelId,
    //    ProgramId = nowAndNext.IdProgramNow,
    //    Title = nowAndNext.TitleNow,
    //    Description = nowAndNext.DescriptionNow,
    //    StartTime = nowAndNext.StartTimeNow,
    //    EndTime = nowAndNext.EndTimeNow
    //  };
    //  programs[1] = new Program
    //  {
    //    ChannelId = nowAndNext.ChannelId,
    //    ProgramId = nowAndNext.IdProgramNext,
    //    Title = nowAndNext.TitleNext,
    //    Description = nowAndNext.DescriptionNext,
    //    StartTime = nowAndNext.StartTimeNow,
    //    EndTime = nowAndNext.StartTimeNext
    //  };
    //  return programs;
    //}

    // Morpheus_xx, 2014-01-03: this helper method could be used to filter programs that are CanceledSchedules, because the actual Program.State does not reflect this situation
    // Using this extension works, but causes quite a big overhead. TVE35 should handle this situation internally.
    //public static IEnumerable<Mediaportal.TV.Server.TVDatabase.Entities.Program> ProcessCanceledSchedules(this IEnumerable<Mediaportal.TV.Server.TVDatabase.Entities.Program> programs)
    //{
    //  IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
    //  if (scheduleService == null)
    //    yield break;

    //  var allSchedules = ScheduleManagement.ListAllSchedules(ScheduleIncludeRelationEnum.CanceledSchedule);

    //  foreach (Mediaportal.TV.Server.TVDatabase.Entities.Program program in programs)
    //  {
    //    ProgramState state = (ProgramState)program.State;
    //    if (state.HasFlag(ProgramState.RecordSeriesPending))
    //    {
    //      foreach (Mediaportal.TV.Server.TVDatabase.Entities.Schedule schedule in allSchedules)
    //      {
    //        ScheduleBLL scheduleBll = new ScheduleBLL(schedule);
    //        if (scheduleBll.IsSerieIsCanceled(program.StartTime, program.ChannelId))
    //          program.State = 0;
    //      }
    //    }
    //    yield return program;
    //  }
    //}

  }
}
