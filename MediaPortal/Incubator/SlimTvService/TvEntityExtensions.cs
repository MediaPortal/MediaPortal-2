using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.UPnP.Items;
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

      if (includeRecordingStatus)
      {
        ProgramBLL programLogic = new ProgramBLL(tvProgram);
        program.RecordingStatus = programLogic.IsRecording ? RecordingStatus.Recording : RecordingStatus.None;
        if (programLogic.IsRecordingOncePending)
          program.RecordingStatus |= RecordingStatus.Scheduled;
        if (programLogic.IsRecordingSeriesPending)
          program.RecordingStatus |= RecordingStatus.SeriesScheduled;
      }
      return program;
    }

    public static IChannel ToChannel(this Mediaportal.TV.Server.TVDatabase.Entities.Channel tvChannel)
    {
      return new Channel { ChannelId = tvChannel.IdChannel, Name = tvChannel.DisplayName, MediaType = (MediaType)tvChannel.MediaType};
    }

    public static IChannelGroup ToChannelGroup(this Mediaportal.TV.Server.TVDatabase.Entities.ChannelGroup tvGroup)
    {
      return new ChannelGroup { ChannelGroupId = tvGroup.IdGroup, Name = tvGroup.GroupName };
    }
  }
}