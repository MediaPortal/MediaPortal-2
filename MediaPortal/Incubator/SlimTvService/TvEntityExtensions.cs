using System.Linq;
using MediaPortal.Common.Utils;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.UPnP.Items;
using Mediaportal.TV.Server.TVControl.Interfaces.Services;
using Mediaportal.TV.Server.TVDatabase.TVBusinessLayer.Entities;

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
          // TODO: Genre!
        };

      // TODO: this is not yet working, no schedulings are visible
      // ProgramBLL programLogic = new ProgramBLL(tvProgram);
      // program.RecordingStatus = programLogic.IsRecording ? RecordingStatus.Recording : RecordingStatus.None;
      
      // Do time consuming calls only if needed
      if (includeRecordingStatus)
      {
        RecordingStatus recordingStatus;
        if (GetRecordingStatus(tvProgram, out recordingStatus))
          program.RecordingStatus = recordingStatus;
      }
      return program;
    }

    public static IChannel ToChannel(this Mediaportal.TV.Server.TVDatabase.Entities.Channel tvChannel)
    {
      return new Channel { ChannelId = tvChannel.IdChannel, Name = tvChannel.DisplayName };
    }

    public static IChannelGroup ToChannelGroup(this Mediaportal.TV.Server.TVDatabase.Entities.ChannelGroup tvGroup)
    {
      return new ChannelGroup { ChannelGroupId = tvGroup.IdGroup, Name = tvGroup.GroupName };
    }

    //TODO: this method slows down the whole program item loading a lot. Maybe we should load the recording status only on demand?
    public static bool GetRecordingStatus(Mediaportal.TV.Server.TVDatabase.Entities.Program tvProgram, out RecordingStatus recordingStatus)
    {
      recordingStatus = RecordingStatus.None;
      if (tvProgram == null)
        return false;
      IScheduleService scheduleService = GlobalServiceProvider.Get<IScheduleService>();
      if (scheduleService.ListAllSchedules().Any(s => new ScheduleBLL(s).IsRecordingProgram(tvProgram, true)))
        recordingStatus = RecordingStatus.Scheduled;
      return true;
    }
  }
}