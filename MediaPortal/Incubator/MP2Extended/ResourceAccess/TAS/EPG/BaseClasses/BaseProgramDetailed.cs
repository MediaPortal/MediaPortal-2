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
