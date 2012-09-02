using System;
using System.Collections.Generic;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.SlimTv.UPnP.Items;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : ITvProvider, ITimeshiftControl, IProgramInfo, IChannelAndGroupInfo, IScheduleControl
  {
    public string Name
    {
      get { return "NativeTv Service"; }
    }

    public bool Init()
    {
      // TODO:
      return true;
    }

    public bool DeInit()
    {
      // TODO:
      return true;
    }

    public bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
    {
      // TODO:
      timeshiftMediaItem = CreateMediaItem(slotIndex, @"C:\temp\timeshift.ts", channel);
      return true;
    }

    public bool StopTimeshift(int slotIndex)
    {
      return true;
    }

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      LiveTvMediaItem tvStream = SlimTvMediaItemBuilder.CreateMediaItem(slotIndex, streamUrl, channel);
      if (tvStream != null)
      {
        // Add program infos to the LiveTvMediaItem
        IProgram currentProgram;
        if (GetCurrentProgram(channel, out currentProgram))
          tvStream.AdditionalProperties[LiveTvMediaItem.CURRENT_PROGRAM] = currentProgram;

        IProgram nextProgram;
        if (GetNextProgram(channel, out nextProgram))
          tvStream.AdditionalProperties[LiveTvMediaItem.NEXT_PROGRAM] = nextProgram;

        return tvStream;
      }
      return null;
    }

    public IChannel GetChannel(int slotIndex)
    {
      throw new NotImplementedException();
    }

    public bool GetCurrentProgram(IChannel channel, out IProgram program)
    {
      // TODO:
      program = new Program { ChannelId = channel.ChannelId, Title = "Program 1", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1) };
      return true;
    }

    public bool GetNextProgram(IChannel channel, out IProgram program)
    {
      // TODO:
      program = new Program { ChannelId = channel.ChannelId, Title = "Program 2", StartTime = DateTime.Now.AddHours(1), EndTime = DateTime.Now.AddHours(2) };
      return true;
    }

    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      // TODO:
      programs = new List<IProgram>
        {
          new Program { ChannelId = 1, Title = "Program 1", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(1)},
          new Program { ChannelId = 1, Title = "Program 2", StartTime = DateTime.Now, EndTime = DateTime.Now.AddHours(2)}
        };
      return true;
    }

    public bool GetProgramsForSchedule(ISchedule schedule, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetScheduledPrograms(IChannel channel, out IList<IProgram> programs)
    {
      throw new NotImplementedException();
    }

    public bool GetChannel(IProgram program, out IChannel channel)
    {
      throw new NotImplementedException();
    }

    public bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
      // TODO:
      groups = new List<IChannelGroup> { new ChannelGroup { ChannelGroupId = 1, Name = "Native group 1" } };
      return true;
    }

    public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
      // TODO:
      channels = new List<IChannel> { new Channel { ChannelId = 1, Name = "Native channel 1" } };
      return true;
    }

    public int SelectedChannelId { get; set; }

    public int SelectedChannelGroupId { get; set; }

    public bool CreateSchedule(IProgram program)
    {
      throw new NotImplementedException();
    }

    public bool RemoveSchedule(IProgram program)
    {
      throw new NotImplementedException();
    }

    public bool GetRecordingStatus(IProgram program, out RecordingStatus recordingStatus)
    {
      throw new NotImplementedException();
    }
  }
}
