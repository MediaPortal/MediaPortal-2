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
using MediaPortal.Plugins.MP2Extended.TAS.Tv;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;

namespace MediaPortal.Plugins.MP2Extended.ResourceAccess.TAS.EPG.BaseClasses
{
  class BaseProgramDetailed
  {
    internal WebProgramDetailed ProgramDetailed(IProgram program)
    {
      if (program == null)
        return new WebProgramDetailed();

      IProgramRecordingStatus recordingStatus = program as IProgramRecordingStatus;
      IProgramSeries programSeries = program as IProgramSeries;
      WebProgramBasic webProgramBasic = new BaseProgramBasic().ProgramBasic(program);

      WebProgramDetailed webProgramDetailed = new WebProgramDetailed
      {
        // From Basic
        Description = webProgramBasic.Description,
        ChannelId = webProgramBasic.ChannelId,
        StartTime = webProgramBasic.StartTime,
        EndTime = webProgramBasic.EndTime,
        Title = webProgramBasic.Title,
        Id = webProgramBasic.Id,
        DurationInMinutes = webProgramBasic.DurationInMinutes,
        Classification = program.Classification,
        OriginalAirDate = program.OriginalAirDate ?? DateTime.Now,
        ParentalRating = program.ParentalRating,
        StarRating = program.StarRating,
        IsScheduled = webProgramBasic.IsScheduled,

        Genre = program.Genre,

        IsRecording = recordingStatus != null && recordingStatus.RecordingStatus != RecordingStatus.None,
        IsRecordingSeriesPending = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.SeriesScheduled,
        IsRecordingOncePending = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.Scheduled,
        IsRecordingSeries = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.RecordingSeries,
        IsRecordingManual = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.RecordingManual,
        IsRecordingOnce = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.RecordingOnce,
        HasConflict = recordingStatus != null && recordingStatus.HasConflict,
        SeriesNum = programSeries.SeasonNumber,
        EpisodeNum = programSeries.EpisodeNumber,
        EpisodeName = programSeries.EpisodeTitle,
        EpisodeNumber = programSeries.EpisodeNumberDetailed,
        EpisodePart = programSeries.EpisodePart,
      };

      //webProgramDetailed.IsPartialRecordingSeriesPending;

      return webProgramDetailed;
    }
  }
}
