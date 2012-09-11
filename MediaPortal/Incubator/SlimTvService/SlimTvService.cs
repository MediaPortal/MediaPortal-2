using System;
using System.Collections.Generic;
using System.Configuration;
using MediaPortal.Common.MediaManagement;
using MediaPortal.Plugins.SlimTv.Interfaces;
using MediaPortal.Plugins.SlimTv.Interfaces.Items;
using MediaPortal.Plugins.SlimTv.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTv.Interfaces.ResourceProvider;
using MediaPortal.Plugins.SlimTv.UPnP.Items;
using Mediaportal.TV.Server.TVLibrary;

namespace MediaPortal.Plugins.SlimTv.Service
{
  public class SlimTvService : ITvProvider, ITimeshiftControl, IProgramInfo, IChannelAndGroupInfo, IScheduleControl
  {
    const int MAX_WAIT_MS = 2000;
    private TvServiceThread _tvServiceThread;

    public string Name
    {
      get { return "NativeTv Service"; }
    }

    public bool Init()
    {
      _tvServiceThread = new TvServiceThread(Environment.GetCommandLineArgs()[0]);
      _tvServiceThread.Start();
      return true;
    }

    public bool DeInit()
    {
      if (_tvServiceThread != null)
      {
        _tvServiceThread.Stop(MAX_WAIT_MS);
        _tvServiceThread = null;
      }
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
      channels = new List<IChannel> { new Channel { ChannelId = 1, Name = "Das Erste HD" } };
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
