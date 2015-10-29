using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

      WebProgramDetailed webProgramDetailed = new WebProgramDetailed
      {
        Genre = program.Genre,
        Description = program.Description,
        ChannelId = program.ChannelId,
        StartTime = program.StartTime,
        EndTime = program.EndTime,
        Title = program.Title,
        Id = program.ProgramId,
        DurationInMinutes = Convert.ToInt32(program.EndTime.Subtract(program.StartTime).TotalMinutes),
        Classification = program.Classification,
        OriginalAirDate = program.OriginalAirDate ?? DateTime.Now,
        ParentalRating = program.ParentalRating,
        StarRating = program.StarRating,

        IsRecording = recordingStatus != null && recordingStatus.RecordingStatus != RecordingStatus.None,
        IsRecordingSeriesPending = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.SeriesScheduled,
        IsRecordingOncePending = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.Scheduled,
        IsRecordingSeries = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.RecordingSeries,
        IsRecordingManual = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.RecordingManual,
        IsRecordingOnce = recordingStatus != null && recordingStatus.RecordingStatus == RecordingStatus.RecordingOnce,
        IsScheduled = recordingStatus.IsScheduled,
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
