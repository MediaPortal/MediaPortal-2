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
          ChannelId = tvProgram.idChannel,
          ProgramId = tvProgram.idProgram,
          Title = tvProgram.title,
          Description = tvProgram.description,
          StartTime = tvProgram.startTime,
          EndTime = tvProgram.endTime,
          // TODO: Genre!
        };
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
      return new Channel { ChannelId = tvChannel.idChannel, Name = tvChannel.displayName };
    }

    public static IChannelGroup ToChannelGroup(this Mediaportal.TV.Server.TVDatabase.Entities.ChannelGroup tvGroup)
    {
      return new ChannelGroup { ChannelGroupId = tvGroup.idGroup, Name = tvGroup.groupName };
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