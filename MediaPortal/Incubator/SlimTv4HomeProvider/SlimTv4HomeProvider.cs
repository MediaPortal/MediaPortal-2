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
    private ITVEInteraction _tvServer;
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
        EndpointAddress endPoint;
        Binding binding;
        GetBindingAndEndpoint(out binding, out endPoint);
        _tvServer = ChannelFactory<ITVEInteraction>.CreateChannel(binding, endPoint);
        _tvServer.TestConnectionToTVService();
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
      if (_tvServer == null)
        return false;

      try
      {
        _tvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(0));
        _tvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(1));
      }
      catch (Exception) { }

      _tvServer = null;
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

      try
      {
        String streamUrl = _tvServer.SwitchTVServerToChannelAndGetStreamingUrl(GetTimeshiftUserName(slotIndex), channel.ChannelId);
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
        _tvServer.CancelCurrentTimeShifting(GetTimeshiftUserName(slotIndex));
        _channels[slotIndex] = null;
        return true;
      }
      catch (Exception ex)
      {
        ServiceRegistration.Get<ILogger>().Error(ex.Message);
        return false;
      }
    }

    private bool CheckConnection()
    {
      try
      {
        if (_tvServer != null)
          _tvServer.TestConnectionToTVService();
        return true;
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

    private static void GetBindingAndEndpoint(out Binding binding, out EndpointAddress endpointAddress)
    {
      TV4HomeProviderSettings setting = ServiceRegistration.Get<ISettingsManager>().Load<TV4HomeProviderSettings>();
      if (IsLocal(setting.TvServerHost))
      {
        endpointAddress = new EndpointAddress("net.pipe://localhost/TV4Home.Server.CoreService/TVEInteractionService");
        binding = new NetNamedPipeBinding { MaxReceivedMessageSize = 10000000 };
      }
      else
      {
        endpointAddress = new EndpointAddress(string.Format("http://{0}:4321/TV4Home.Server.CoreService/TVEInteractionService", setting.TvServerHost));
        binding = new BasicHttpBinding { MaxReceivedMessageSize = 10000000 };
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
        List<WebChannelGroup> tvGroups = _tvServer.GetGroups();
        foreach (WebChannelGroup webChannelGroup in tvGroups)
        {
          groups.Add(new ChannelGroup { ChannelGroupId = webChannelGroup.IdGroup, Name = webChannelGroup.GroupName });
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
      if (group == null)
        return false;
      try
      {
        List<WebChannelBasic> tvChannels = _tvServer.GetChannelsBasic(group.ChannelGroupId);
        foreach (WebChannelBasic webChannel in tvChannels)
        {
          channels.Add(new Channel { ChannelId = webChannel.IdChannel, Name = webChannel.DisplayName });
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
        WebChannelBasic tvChannel = _tvServer.GetChannelBasicById(channelId);
        if (tvChannel != null)
        {
          channel = new Channel { ChannelId = tvChannel.IdChannel, Name = tvChannel.DisplayName };
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
      if (channel == null)
        return false;
      try
      {
        WebProgramDetailed tvProgram = _tvServer.GetCurrentProgramOnChannel(channel.ChannelId);
        if (tvProgram != null)
        {
          program = new Program(tvProgram);
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

      IProgram currentProgram;
      try
      {
        if (GetCurrentProgram(channel, out currentProgram))
        {
          List<WebProgramDetailed> nextPrograms = _tvServer.GetProgramsDetailedForChannel(channel.ChannelId,
                                                                                          currentProgram.EndTime.
                                                                                            AddMinutes(1),
                                                                                          currentProgram.EndTime.
                                                                                            AddMinutes(1));
          if (nextPrograms != null && nextPrograms.Count > 0)
          {
            program = new Program(nextPrograms[0]);
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

      programs = new List<IProgram>();
      try
      {
        List<WebProgramDetailed> tvPrograms = _tvServer.GetProgramsDetailedForChannel(channel.ChannelId, from, to);
        foreach (WebProgramDetailed webProgram in tvPrograms)
          programs.Add(new Program(webProgram));
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
