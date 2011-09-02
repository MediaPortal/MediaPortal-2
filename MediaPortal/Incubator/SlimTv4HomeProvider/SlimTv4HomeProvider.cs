#region Copyright (C) 2007-2011 Team MediaPortal

/*
    Copyright (C) 2007-2011 Team MediaPortal
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
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using MediaPortal.Core;
using MediaPortal.Core.Logging;
using MediaPortal.Core.MediaManagement;
using MediaPortal.Core.MediaManagement.DefaultItemAspects;
using MediaPortal.Core.Settings;
using MediaPortal.Core.SystemResolver;
using MediaPortal.Plugins.SlimTvClient.Interfaces;
using MediaPortal.Plugins.SlimTvClient.Interfaces.Items;
using MediaPortal.Plugins.SlimTvClient.Interfaces.LiveTvMediaItem;
using MediaPortal.Plugins.SlimTvClient.Providers.Items;
using MediaPortal.Plugins.SlimTvClient.Providers.Settings;
using TV4Home.Server.TVEInteractionLibrary.Interfaces;
using IChannel = MediaPortal.Plugins.SlimTvClient.Interfaces.Items.IChannel;

namespace MediaPortal.Plugins.SlimTv.Providers
{
  public class SlimTv4HomeProvider : ITvProvider, ITimeshiftControl, IProgramInfo, IChannelAndGroupInfo
  {
    private ITVEInteraction[] _tvServers;
    private string[] _serverNames;
    private readonly IChannel[] _channels = new IChannel[2];

    #region ITvProvider Member

    public IChannel GetChannel(int slotIndex)
    {
      return _channels[slotIndex];
    }

    public string Name
    {
      get { return "TV4Home Provider"; }
    }

    #endregion

    #region ITimeshiftControl Member

    public bool Init()
    {
      try
      {
        CreateAllTvServerConnections();
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error("SlimTv4HomeProvider Error: " + ex.Message);
        return false;
      }
    }

    public bool DeInit()
    {
      if (_tvServers == null)
        return false;

      try
      {
        foreach (ITVEInteraction tvServer in _tvServers)
        {
          if (tvServer != null)
          {
            tvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(0));
            tvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(1));
          }
        }
      }
      catch (Exception) { }

      _tvServers = null;
      return true;
    }

    public String GetTimeshiftUserName(int slotIndex)
    {
      return String.Format("SlimTvClient{0}", slotIndex);
    }

    public bool StartTimeshift(int slotIndex, IChannel channel, out MediaItem timeshiftMediaItem)
    {
      timeshiftMediaItem = null;
      if (!CheckConnection())
        return false;

      Channel indexChannel = channel as Channel;
      if (indexChannel == null)
        return false;

      try
      {
        String streamUrl = TvServer(indexChannel.ServerIndex).SwitchTVServerToChannelAndGetStreamingUrl(GetTimeshiftUserName(slotIndex), channel.ChannelId);
        if (String.IsNullOrEmpty(streamUrl))
          return false;

        _channels[slotIndex] = channel;

        // assign a MediaItem, can be null if streamUrl is the same.
        timeshiftMediaItem = CreateMediaItem(slotIndex, streamUrl, channel);
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return false;
      }
    }

    public bool StopTimeshift(int slotIndex)
    {
      if (!CheckConnection())
        return false;
      try
      {
        Channel slotChannel = _channels[slotIndex] as Channel;
        if (slotChannel == null)
          return false;

        TvServer(slotChannel.ServerIndex).CancelCurrentTimeShifting(GetTimeshiftUserName(slotIndex));
        _channels[slotIndex] = null;
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return false;
      }
    }

    private ITVEInteraction TvServer(int serverIndex)
    {
      return _tvServers[serverIndex];
    }

    private bool CheckConnection()
    {
      try
      {
        bool failure = false;
        foreach (ITVEInteraction tvServer in _tvServers)
        {
          if (tvServer != null)
            failure |= !tvServer.TestConnectionToTVService();
        }
        return !failure;
      }
      catch (Exception)
      {
        return Init();
      }
    }

    private static bool IsLocal(string host)
    {
      if (string.IsNullOrEmpty(host))
        return true;

      return host.ToLowerInvariant() == "localhost" || host == "127.0.0.1" || host == "::1";
    }

    private void CreateAllTvServerConnections()
    {
      TV4HomeProviderSettings setting = ServiceRegistration.Get<ISettingsManager>().Load<TV4HomeProviderSettings>();
      if (setting.TvServerHost == null)
        return;

      _serverNames = setting.TvServerHost.Split(';');
      _tvServers = new ITVEInteraction[_serverNames.Length];

      int serverIndex = 0;
      foreach (string serverName in _serverNames)
      {
        Binding binding;
        EndpointAddress endpointAddress;
        if (IsLocal(serverName))
        {
          endpointAddress = new EndpointAddress("net.pipe://localhost/TV4Home.Server.CoreService/TVEInteractionService");
          binding = new NetNamedPipeBinding { MaxReceivedMessageSize = 10000000 };
        }
        else
        {
          endpointAddress = new EndpointAddress(string.Format("http://{0}:4321/TV4Home.Server.CoreService/TVEInteractionService", serverName));
          binding = new BasicHttpBinding { MaxReceivedMessageSize = 10000000 };
        }
        ITVEInteraction tvServer = ChannelFactory<ITVEInteraction>.CreateChannel(binding, endpointAddress);
        _tvServers[serverIndex++] = tvServer;
      }
    }

    #endregion

    #region IChannelAndGroupInfo members

    public int SelectedChannelId { get; set; }

    public int SelectedChannelGroupId { get; set; }

    public bool GetChannelGroups(out IList<IChannelGroup> groups)
    {
      groups = new List<IChannelGroup>();
      if (!CheckConnection())
        return false;
      try
      {
        int idx = 0;
        foreach (ITVEInteraction tvServer in _tvServers)
        {
          List<WebChannelGroup> tvGroups = tvServer.GetGroups();
          foreach (WebChannelGroup webChannelGroup in tvGroups)
          {
            groups.Add(new ChannelGroup { ChannelGroupId = webChannelGroup.IdGroup, Name = String.Format("{0}: {1}", _serverNames[idx], webChannelGroup.GroupName), ServerIndex = idx});
          }
          idx++;
        }
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return false;
      }
    }

    public bool GetChannels(IChannelGroup group, out IList<IChannel> channels)
    {
      channels = new List<IChannel>();
      if (!CheckConnection())
        return false;
      ChannelGroup indexGroup = group as ChannelGroup;
      if (indexGroup == null)
        return false;
      try
      {
        List<WebChannelBasic> tvChannels = TvServer(indexGroup.ServerIndex).GetChannelsBasic(group.ChannelGroupId);
        foreach (WebChannelBasic webChannel in tvChannels)
        {
          channels.Add(new Channel { ChannelId = webChannel.IdChannel, Name = webChannel.DisplayName, ServerIndex = indexGroup.ServerIndex });
        }
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return false;
      }
    }

    public bool GetChannel(int channelId, out IChannel channel)
    {
      channel = null;
      if (!CheckConnection())
        return false;
      try
      {
        //FIXME: correct index?
        WebChannelBasic tvChannel = TvServer(0).GetChannelBasicById(channelId);
        if (tvChannel != null)
        {
          channel = new Channel { ChannelId = tvChannel.IdChannel, Name = tvChannel.DisplayName, ServerIndex = 0};
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
      }
      return false;
    }

    public bool GetChannel(IProgram program, out IChannel channel)
    {
      channel = null;
      if (!CheckConnection())
        return false;
      Program indexProgram = program as Program;
      if (indexProgram == null)
        return false;

      try
      {
        WebChannelBasic tvChannel = TvServer(indexProgram.ServerIndex).GetChannelBasicById(indexProgram.ChannelId);
        if (tvChannel != null)
        {
          channel = new Channel { ChannelId = tvChannel.IdChannel, Name = tvChannel.DisplayName, ServerIndex = indexProgram.ServerIndex };
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
      }
      return false;
    }

    #endregion

    public MediaItem CreateMediaItem(int slotIndex, string streamUrl, IChannel channel)
    {
      if (!String.IsNullOrEmpty(streamUrl))
      {
        ISystemResolver systemResolver = ServiceRegistration.Get<ISystemResolver>();
        IDictionary<Guid, MediaItemAspect> aspects = new Dictionary<Guid, MediaItemAspect>();
        MediaItemAspect providerResourceAspect;
        MediaItemAspect mediaAspect;

        SlimTvResourceAccessor resourceAccessor = new SlimTvResourceAccessor(slotIndex, streamUrl);
        aspects[ProviderResourceAspect.ASPECT_ID] =
          providerResourceAspect = new MediaItemAspect(ProviderResourceAspect.Metadata);
        aspects[MediaAspect.ASPECT_ID] = mediaAspect = new MediaItemAspect(MediaAspect.Metadata);
        // videoaspect needs to be included to associate player later!
        aspects[VideoAspect.ASPECT_ID] = new MediaItemAspect(VideoAspect.Metadata);
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_SYSTEM_ID, systemResolver.LocalSystemId);

        String raPath = resourceAccessor.LocalResourcePath.Serialize();
        providerResourceAspect.SetAttribute(ProviderResourceAspect.ATTR_RESOURCE_ACCESSOR_PATH, raPath);

        mediaAspect.SetAttribute(MediaAspect.ATTR_TITLE, "Live TV");
        mediaAspect.SetAttribute(MediaAspect.ATTR_MIME_TYPE, "video/livetv"); //Custom mimetype for LiveTv

        LiveTvMediaItem tvStream = new LiveTvMediaItem(new Guid(), aspects);

        tvStream.AdditionalProperties[LiveTvMediaItem.SLOT_INDEX] = slotIndex;
        tvStream.AdditionalProperties[LiveTvMediaItem.CHANNEL] = channel;
        tvStream.AdditionalProperties[LiveTvMediaItem.TUNING_TIME] = DateTime.Now;

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

    #region IProgramInfo Member

    public bool GetCurrentProgram(IChannel channel, out IProgram program)
    {
      program = null;
      if (!CheckConnection())
        return false;

      Channel indexChannel = channel as Channel;
      if (indexChannel == null)
        return false;

      try
      {
        WebProgramDetailed tvProgram = TvServer(indexChannel.ServerIndex).GetCurrentProgramOnChannel(channel.ChannelId);
        if (tvProgram != null)
        {
          program = new Program(tvProgram, indexChannel.ServerIndex);
          return true;
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
      }
      return false;
    }

    public bool GetNextProgram(IChannel channel, out IProgram program)
    {
      program = null;
      if (!CheckConnection())
        return false;
      if (channel == null)
        return false;

      Channel indexChannel = channel as Channel;
      if (indexChannel == null)
        return false;

      IProgram currentProgram;
      try
      {
        if (GetCurrentProgram(channel, out currentProgram))
        {
          List<WebProgramDetailed> nextPrograms = TvServer(indexChannel.ServerIndex).GetProgramsDetailedForChannel(channel.ChannelId,
                                                                                          currentProgram.EndTime.
                                                                                            AddMinutes(1),
                                                                                          currentProgram.EndTime.
                                                                                            AddMinutes(1));
          if (nextPrograms != null && nextPrograms.Count > 0)
          {
            program = new Program(nextPrograms[0], indexChannel.ServerIndex);
            return true;
          }
        }
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
      }
      return false;
    }

    public bool GetPrograms(IChannel channel, DateTime from, DateTime to, out IList<IProgram> programs)
    {
      programs = null;
      if (!CheckConnection())
        return false;
      if (channel == null)
        return false;

      Channel indexChannel = channel as Channel;
      if (indexChannel == null)
        return false;

      programs = new List<IProgram>();
      try
      {
        List<WebProgramDetailed> tvPrograms = TvServer(indexChannel.ServerIndex).GetProgramsDetailedForChannel(channel.ChannelId, from, to);
        foreach (WebProgramDetailed webProgram in tvPrograms)
          programs.Add(new Program(webProgram, indexChannel.ServerIndex));
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return false;
      }
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

    #endregion
  }
}
